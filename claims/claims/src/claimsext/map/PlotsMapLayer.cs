using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.gui.playerGui.GuiElements;
using claims.src.playerMovements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace claims.src.claimsext.map
{
    public class PlotsMapLayer : RGBMapLayer
    {
        //static claimsext modInstance;
        int chunksize;
        IWorldChunk[] chunksTmp;

        object chunksToGenLock = new object();
        UniqueQueue<Vec2i> chunksToGen = new UniqueQueue<Vec2i>();
        public ConcurrentDictionary<Vec2i, CANMultiChunkMapComponent> loadedMapData = new ConcurrentDictionary<Vec2i, CANMultiChunkMapComponent>();
        HashSet<Vec2i> curVisibleChunks = new HashSet<Vec2i>();
        public Dictionary<Vec2i, string> chunkToCityName = new Dictionary<Vec2i, string>();
       // public Dictionary<string, CityPlotInfo> cityNameToCityInfo = new Dictionary<string, CityPlotInfo>();
        public Dictionary<string, Vec3d> cityNameToChestCoords = new Dictionary<string, Vec3d>();
        bool shouldRender = true;
        bool shouldRenderBanks = false;

        public override MapLegendItem[] LegendItems => throw new NotImplementedException();
        public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
        public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
        public override string Title => "Plots";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

        public override string LayerGroupCode => "claims";

        public string getMapDbFilePath()
        {
            string path = System.IO.Path.Combine(GamePaths.DataPath, "PlotsMaps");
            GamePaths.EnsurePathExists(path);

            return System.IO.Path.Combine(path, api.World.SavegameIdentifier + ".db");
        }
        public PlotsMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            // Used to upload a quad mesh here for icon rendering that never happened; the field it
            // went into was read nowhere.
        }
        public bool toggleRender(KeyCombination comb)
        {
            shouldRender = !shouldRender;
            return true;
        }
        public bool toggleRenderBanks(KeyCombination comb)
        {
            shouldRenderBanks = !shouldRenderBanks;
            return true;
        }
        public void RedrawPlots()
        {
            foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
            {
                cmp.ActuallyDispose();
            }
            loadedMapData.Clear();
            foreach (var zone in claims.clientDataStorage.getClientSavedPlots())
            {
                foreach (var pl in zone.Value.savedPlots)
                {
                    OnResChunkPixels(pl.Key.Copy(), "");
                }
            }
        }

        public TextCommandResult onMapCmd(TextCommandCallingArgs args)
        {
            //var mapmgr = api.ModLoader.GetModSystem<WorldMapManager>();
            // mapmgr.MapLayers.Remove(mapmgr.MapLayers[0]);
            IPlayer player = args.Caller.Player;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            string cmd = (string)args.LastArg;
            ICoreClientAPI capi = api as ICoreClientAPI;

            if (cmd == "purgedb")
            {
                //mapdb.Purge();
                capi.ShowChatMessage("Ok, db purged");
            }

            if (cmd == "redraw")
            {
                foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
                {
                    cmp.ActuallyDispose();
                }
                loadedMapData.Clear();
                foreach(var zone in claims.clientDataStorage.getClientSavedPlots())
                {
                    foreach(var pl in zone.Value.savedPlots)
                    {
                        OnResChunkPixels(pl.Key.Copy(), "");
                    }
                    
                }
               

                /*lock (chunksToGenLock)
                {
                    foreach (Vec2i cord in curVisibleChunks)
                    {
                        chunksToGen.Enqueue(cord.Copy());
                    }
                }*/
            }
            return tcr;
        }

        /*Vec2i tmpMccoord = new Vec2i();
        Vec2i tmpCoord = new Vec2i();*/

        public override void OnLoaded()
        {
            //modInstance = api.ModLoader.GetModSystem<claimsext>();

            chunksize = api.World.BlockAccessor.ChunkSize;

            chunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / chunksize];
        }


        public override void OnMapClosedClient()
        {

            lock (chunksToGenLock)
            {
                chunksToGen.Clear();
            }

            curVisibleChunks.Clear();
            // Otherwise the arms of whatever was last pointed at reappear the moment the map opens.
            hoveredEmblem = "";
        }

        public override void Dispose()
        {
            if (loadedMapData != null)
            {
                foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
                {
                    cmp?.ActuallyDispose();
                }
            }

            CANMultiChunkMapComponent.DisposeStatic();
            EmblemCache.Invalidate();

            base.Dispose();
        }

        public override void OnShutDown()
        {
            CANMultiChunkMapComponent.tmpTexture?.Dispose();
        }

        float mtThread1secAccum = 0f;

        float genAccum = 0f;
        float diskSaveAccum = 0f;
        Dictionary<Vec2i, MapPieceDB> toSaveList = new Dictionary<Vec2i, MapPieceDB>();

        public void clearZoneSavedPlotsFromMap(Vec2i zoneCord)
        {
            int ppcd = chunksize / PlotPosition.plotSize;
            int cl = CANMultiChunkMapComponent.ChunkLen;
            // How many mcords (multi-chunks) a single zone spans per axis
            int mCordsPerZone = Math.Max(1, claims.config.ZONE_PLOTS_LENGTH / (cl * ppcd));
            int baseX = zoneCord.X * mCordsPerZone;
            int baseY = zoneCord.Y * mCordsPerZone;
            for (int i = 0; i < mCordsPerZone; i++)
                for (int j = 0; j < mCordsPerZone; j++)
                    loadedMapData.TryRemove(new Vec2i(baseX + i, baseY + j), out _);
        }
        public async void generateFromZoneSavedPlotsOnMap(Vec2i zoneCord)
        {
            try
            {
                if (claims.clientDataStorage.getClientSavedZone(zoneCord, out ClientSavedZone clientSavedZone))
                {
                    var tasks = new System.Collections.Generic.List<Task>();
                    foreach (var it in clientSavedZone.savedPlots)
                        tasks.Add(OnResChunkPixelsAsync(it.Key, it.Value.cityName));
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("[claims] generateFromZoneSavedPlotsOnMap error: " + ex);
            }
        }
        public async void OnResChunkPixels(Vec2i cord, string cityName)
        {
            try { await OnResChunkPixelsAsync(cord, cityName); }
            catch (Exception ex) { api.Logger.Error("[claims] OnResChunkPixels error: " + ex); }
        }
        private async Task OnResChunkPixelsAsync(Vec2i cord, string cityName)
        {
            await Task.Run(() =>
            {
                // Colour is resolved per plot inside GenerateChunkImage - a chunk can hold plots of
                // different cities.
                int[] pixels = (int[])GenerateChunkImage(cord, true)?.Clone();
                
                if (pixels == null)
                {
                    return;
                }

                Vec2i[] UpdateChunkImage = new Vec2i[]
                {
                    new Vec2i(cord.X + 1, cord.Y),
                    new Vec2i(cord.X - 1, cord.Y),
                    new Vec2i(cord.X, cord.Y + 1),
                    new Vec2i(cord.X, cord.Y - 1)
                };

                loadFromChunkPixels(cord, pixels, cityName);

                foreach(var it in UpdateChunkImage)
                {
                    pixels = (int[])GenerateChunkImage(it, false)?.Clone();
                    loadFromChunkPixels(it, pixels, "");
                }
            });
        }
         
        /// <summary>Arms of the city under the cursor, drawn inside the map's hover box as a custom
        /// icon (see <see cref="EmblemIconName"/>).</summary>
        private string hoveredEmblem = "";

        /// <summary>Name in the engine's icon table. A single entry redrawn from
        /// <see cref="hoveredEmblem"/> - that table lives as long as the client, so one icon per city
        /// would leak a delegate per city.</summary>
        private const string EmblemIconName = "claims-hovered-emblem";

        private bool emblemIconRegistered;

        /// <summary>Unscaled font size of the icon - IconComponent takes its size from the font, and
        /// the hover box's own ~14 is too small for a coat of arms.</summary>
        private const int EmblemFontSize = 32;

        /// <summary>Scratch for the cursor-to-world conversion, kept to avoid allocating per move.</summary>
        private Vec3d hoveredWorldPos = new Vec3d();

        public override void Render(GuiElementMap mapElem, float dt)
        {
            if(!this.Active)
            {
                return;
            }
            if (!shouldRender) return;
            foreach (var val in loadedMapData)
            {
                val.Value.Render(mapElem, dt);
            }

        }

        /// <summary>Registers the icon that "&lt;icon name=...&gt;" in the hover text renders. Once,
        /// on first use.</summary>
        private void EnsureEmblemIcon(ICoreClientAPI capi)
        {
            if (emblemIconRegistered) return;
            emblemIconRegistered = true;

            capi.Gui.Icons.CustomIcons[EmblemIconName] = (ctx, x, y, w, h, rgba) =>
            {
                if (hoveredEmblem.Length == 0) return;
                EmblemCache.Draw(capi, ctx, hoveredEmblem, x, y, w, h);
            };
        }

        /// <summary>Arms of a city by name - plots carry names, the emblem cache is keyed by guid,
        /// and the world city list maps one to the other.</summary>
        private static string EmblemOfCity(string cityName)
        {
            var cities = claims.clientDataStorage?.clientPlayerInfo?.AllCitiesList;
            if (cities == null) return "";

            foreach (var city in cities)
            {
                if (city.Name != cityName) continue;
                return claims.clientDataStorage.ClientGetEmblem(city.Guid);
            }
            return "";
        }

        /// <summary>
        /// Whether the settlement on the hovered plot is a village. The plot itself only carries a
        /// name, so the tier comes from the settlement list the client already holds.
        /// </summary>
        private static bool IsVillage(string cityName)
        {
            var cities = claims.clientDataStorage?.clientPlayerInfo?.AllCitiesList;
            if (cities == null) return false;

            foreach (var city in cities)
            {
                if (city.Name == cityName) return city.Tier == part.structure.CityTier.VILLAGE;
            }
            return false;
        }

        // OnMapOpenedClient used to build fourteen waypoint-style icons into GL textures every time
        // the map opened, into a dictionary nothing ever read. Removed with the dictionary itself.

        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {

            // The plot under the cursor is a coordinate conversion, not a search: cursor to world
            // position, world position to exactly one plot.
            mapElem.TranslateViewPosToWorldPos(
                new Vec2f((float)(args.X - mapElem.Bounds.renderX), (float)(args.Y - mapElem.Bounds.renderY)),
                ref hoveredWorldPos);

            // Floor, not truncation - plots -1 and 0 both truncate to 0.
            var plotPos = new Vec2i(
                (int)Math.Floor(hoveredWorldPos.X / PlotPosition.plotSize),
                (int)Math.Floor(hoveredWorldPos.Z / PlotPosition.plotSize));

            claims.clientDataStorage.getSavedPlot(plotPos, out SavedPlotInfo hovered);
            if (hovered != null && hovered.cityName.Length > 0)
            {
                hoveredEmblem = EmblemOfCity(hovered.cityName);

                string cityLine = Lang.Get(IsVillage(hovered.cityName)
                    ? "claims:map_plot_village_name"
                    : "claims:map_plot_city_name", hovered.cityName);
                if (hoveredEmblem.Length > 0 && api is ICoreClientAPI capi)
                {
                    EnsureEmblemIcon(capi);
                    // The font tag sets the icon size. Closing the icon explicitly is required - the
                    // parser rejects a </font> over an open <icon>.
                    cityLine = "<font size=\"" + EmblemFontSize + "\"><icon name=\"" + EmblemIconName
                             + "\"></icon></font> " + cityLine;
                }

                hoverText.AppendLine(cityLine);
                if (hovered.price > 0)
                {
                    hoverText.AppendLine(Lang.Get("claims:map_plot_price", hovered.price));
                }
            }
            else
            {
                hoveredEmblem = "";
            }
        }
        public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
        {
            foreach (var val in loadedMapData)
            {
                val.Value.OnMouseUpOnElement(args, mapElem);
            }
        }
        void loadFromChunkPixels(Vec2i cord, int[] pixels, string structureName)
        {
            if (pixels == null) return;
            int ppcd = chunksize / PlotPosition.plotSize;
            int gameChunkX = cord.X / ppcd;
            int gameChunkY = cord.Y / ppcd;
            if (gameChunkX < 0 || gameChunkY < 0) return;
            Vec2i mcord = new Vec2i(gameChunkX / CANMultiChunkMapComponent.ChunkLen, gameChunkY / CANMultiChunkMapComponent.ChunkLen);
            Vec2i baseCord = new Vec2i(mcord.X * CANMultiChunkMapComponent.ChunkLen, mcord.Y * CANMultiChunkMapComponent.ChunkLen);
            int dx = gameChunkX - baseCord.X;
            int dz = gameChunkY - baseCord.Y;
            if (dx < 0 || dx >= CANMultiChunkMapComponent.ChunkLen || dz < 0 || dz >= CANMultiChunkMapComponent.ChunkLen) return;
            api.Event.EnqueueMainThreadTask(() =>
            {
                if (!loadedMapData.TryGetValue(mcord, out CANMultiChunkMapComponent mccomp))
                {
                    loadedMapData[mcord] = mccomp = new CANMultiChunkMapComponent(api as ICoreClientAPI, baseCord);
                }
                mccomp.setChunk(dx, dz, pixels);
            }, "plotmaplayerready");
        }

        public void triggeredPlotImageUpdate(Vec2i chunkPos)
        {
            Vec2i mm = new Vec2i(chunkPos.X, chunkPos.Y);

            chunkToCityName.TryGetValue(chunkPos, out string cityName);

            if (!claims.clientDataStorage.getSavedPlot(chunkPos, out SavedPlotInfo savedPlotInfo))
            {
                /*Task.Factory.StartNew(() =>
                {
                    int[] pixels = (int[])GenerateChunkImage(chunkPos, 0, false)?.Clone();

                    loadFromChunkPixels(mm, pixels, "");
                });*/
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    int[] pixels = (int[])GenerateChunkImage(chunkPos, false)?.Clone();

                    loadFromChunkPixels(mm, pixels, savedPlotInfo.cityName);
                });
            }

            
        }
        public static int ToRgba(int a, int r, int g, int b)
        {
            int iCol = (a << 24) | (r << 16) | (g << 8) | b;
            return iCol;
        }

        public void GenerateChunkPart(int X, int Y, int color, ref int[] pixels, Vec2i tmpVec, string cityName)
        {
            int ps = PlotPosition.plotSize;
            int stride = chunksize;
            // X = world-Z offset (row), Y = world-X offset (col)
            int pivot = Y * ps + X * ps * stride;
            color &= unchecked((int)0x80ffffff);
            int place = 0;
            if (claims.config.CITY_AREA_VISIBILITY_STATE != Config.CITY_AREA_VISIBILITY.WITHOUT_INNER)
            {
                for (int i = 0; i < ps; i++)
                {
                    for (int j = 0; j < ps; j++)
                    {
                        place = i * stride + j + pivot;
                        pixels[place] = color;
                        continue;
                    }
                }
            }
            if (claims.config.CITY_AREA_VISIBILITY_STATE == Config.CITY_AREA_VISIBILITY.WITHOUT_BORDER)
            {
                return;
            }
            var copyVec = tmpVec.Copy();
            copyVec.X--;
            SavedPlotInfo plot;
            color &= unchecked((int)0x80ffff11);
            //left
            if (!claims.clientDataStorage.getSavedPlot(copyVec, out plot) || plot.cityName != cityName)
            {
                for (int i = 0; i < ps; i++)
                {
                    place = i * stride + pivot;
                    pixels[place] = color;
                }
            }
            copyVec.X++;
            copyVec.Y++;
            //down
            if (!claims.clientDataStorage.getSavedPlot(copyVec, out plot) || plot.cityName != cityName)
            {
                int lastRow = (ps - 1) * stride;
                for (int i = 0; i < ps; i++)
                {
                    place = i + lastRow + pivot;
                    pixels[place] = color;
                }
            }
            copyVec.X++;
            copyVec.Y--;
            //right
            if (!claims.clientDataStorage.getSavedPlot(copyVec, out plot) || plot.cityName != cityName)
            {
                for (int i = 0; i < ps; i++)
                {
                    place = i * stride + (ps - 1) + pivot;
                    pixels[place] = color;
                }
            }
            copyVec.X--;
            copyVec.Y--;
            //up
            if (!claims.clientDataStorage.getSavedPlot(copyVec, out plot) || plot.cityName != cityName)
            {
                for (int i = 0; i < ps; i++)
                {
                    place = i + pivot;
                    pixels[place] = color;
                }
            }
        }


        public int[] GenerateChunkImage(Vec2i chunkPos, bool informOthers = false)
        {
            int ps = PlotPosition.plotSize;
            int ppcd = chunksize / ps; // plots per game-chunk dimension
            int[] texDataTmp = new int[chunksize * chunksize];

            Vec2i leftUpperCorner = new Vec2i(
                chunkPos.X - (chunkPos.X % ppcd),
                chunkPos.Y - (chunkPos.Y % ppcd));

            for (int worldZOffset = 0; worldZOffset < ppcd; worldZOffset++)
            {
                for (int worldXOffset = 0; worldXOffset < ppcd; worldXOffset++)
                {
                    Vec2i tmpVec = new Vec2i(leftUpperCorner.X + worldXOffset, leftUpperCorner.Y + worldZOffset);
                    if (claims.clientDataStorage.getSavedPlot(tmpVec, out SavedPlotInfo savedPlot))
                        GenerateChunkPart(worldZOffset, worldXOffset, claims.clientDataStorage.ClientGetCityColor(savedPlot.cityName), ref texDataTmp, tmpVec, savedPlot.cityName);
                }
            }
            return texDataTmp;
        }
    }
}
