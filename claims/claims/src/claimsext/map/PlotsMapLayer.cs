using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
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

        /// <summary>Game chunks whose image is out of date, in game-chunk coordinates. A game chunk
        /// is the unit the map is painted in - <see cref="GenerateChunkImage"/> always produces a
        /// whole one - so marking plots here collapses the several plots of a chunk into one repaint.
        /// </summary>
        object dirtyChunksLock = new object();
        UniqueQueue<Vec2i> dirtyChunks = new UniqueQueue<Vec2i>();

        /// <summary>Transparent chunk, used to wipe a tile whose plots are gone. Shared and never
        /// written to.</summary>
        int[] emptyChunkPixels;

        /// <summary>Chunks painted per tick. Each paint is a texture upload plus a slice of a
        /// framebuffer pass, so this is what keeps a fresh zone off the frame time.</summary>
        const int MaxChunkPaintsPerTick = 32;

        /// <summary>Chunks *examined* per tick. Unclaimed chunks cost only a few dictionary lookups
        /// and never reach the paint budget, so the scan runs well ahead of it.</summary>
        const int MaxChunkScansPerTick = 1024;
        public ConcurrentDictionary<Vec2i, CANMultiChunkMapComponent> loadedMapData = new ConcurrentDictionary<Vec2i, CANMultiChunkMapComponent>();
        HashSet<Vec2i> curVisibleChunks = new HashSet<Vec2i>();
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
                markZoneDirty(zone.Key);
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
                RedrawPlots();
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
            emptyChunkPixels = new int[chunksize * chunksize];
        }


        public override void OnMapClosedClient()
        {
            // Pending repaints are kept: the minimap hud keeps rendering these textures, and a
            // dropped repaint would leave them stale until something touches the plot again.
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

        /// <summary>Plots per game chunk, per axis.</summary>
        private int plotsPerChunkDim => Math.Max(1, chunksize / PlotPosition.plotSize);

        /// <summary>The zone is repainted rather than thrown away: a repaint overwrites every tile it
        /// touches (including back to transparent), and dropping the components leaked their
        /// textures.</summary>
        public void clearZoneSavedPlotsFromMap(Vec2i zoneCord)
        {
            markZoneDirty(zoneCord);
        }

        public void generateFromZoneSavedPlotsOnMap(Vec2i zoneCord)
        {
            markZoneDirty(zoneCord);
        }

        /// <summary>Queues the chunk holding this plot, plus the chunks of its four neighbours - a
        /// plot draws the border it shares with them. Neighbours usually land in the same chunk, and
        /// the queue drops the duplicates.</summary>
        public void OnResChunkPixels(Vec2i cord, string cityName)
        {
            if (cord == null) return;

            int ppcd = plotsPerChunkDim;
            lock (dirtyChunksLock)
            {
                enqueueChunkOfPlot(cord.X, cord.Y, ppcd);
                enqueueChunkOfPlot(cord.X + 1, cord.Y, ppcd);
                enqueueChunkOfPlot(cord.X - 1, cord.Y, ppcd);
                enqueueChunkOfPlot(cord.X, cord.Y + 1, ppcd);
                enqueueChunkOfPlot(cord.X, cord.Y - 1, ppcd);
            }
        }

        /// <summary>Queues every game chunk of a zone, plus a one-chunk ring around it for the borders
        /// the zone shares with its neighbours.</summary>
        private void markZoneDirty(Vec2i zoneCord)
        {
            if (zoneCord == null) return;

            int ppcd = plotsPerChunkDim;
            int chunksPerZone = Math.Max(1, claims.config.ZONE_PLOTS_LENGTH / ppcd);
            int baseX = zoneCord.X * chunksPerZone;
            int baseY = zoneCord.Y * chunksPerZone;

            lock (dirtyChunksLock)
            {
                for (int i = -1; i <= chunksPerZone; i++)
                {
                    for (int j = -1; j <= chunksPerZone; j++)
                    {
                        int gx = baseX + i;
                        int gy = baseY + j;
                        if (gx < 0 || gy < 0) continue;
                        dirtyChunks.Enqueue(new Vec2i(gx, gy));
                    }
                }
            }
        }

        /// <summary>Caller holds <see cref="dirtyChunksLock"/>.</summary>
        private void enqueueChunkOfPlot(int plotX, int plotY, int ppcd)
        {
            if (plotX < 0 || plotY < 0) return;
            dirtyChunks.Enqueue(new Vec2i(plotX / ppcd, plotY / ppcd));
        }

        /// <summary>Repaints a slice of the dirty chunks. Runs every 20ms off the world map manager,
        /// map open or not.</summary>
        public override void OnTick(float dt)
        {
            // Not before OnLoaded: chunksize is still 0 and the marks made so far are in wrong units.
            if (chunksize == 0) return;

            lock (dirtyChunksLock)
            {
                if (dirtyChunks.Count == 0) return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null) return;

            List<CANMultiChunkMapComponent> modified = new List<CANMultiChunkMapComponent>();
            int cl = CANMultiChunkMapComponent.ChunkLen;
            int painted = 0;
            int scanned = 0;

            while (painted < MaxChunkPaintsPerTick && scanned < MaxChunkScansPerTick)
            {
                Vec2i gc;
                lock (dirtyChunksLock)
                {
                    if (dirtyChunks.Count == 0) break;
                    gc = dirtyChunks.Dequeue();
                }
                scanned++;
                if (gc.X < 0 || gc.Y < 0) continue;

                Vec2i mcord = new Vec2i(gc.X / cl, gc.Y / cl);
                int dx = gc.X - mcord.X * cl;
                int dz = gc.Y - mcord.Y * cl;

                int[] pixels = generateGameChunkImage(gc.X, gc.Y);
                loadedMapData.TryGetValue(mcord, out CANMultiChunkMapComponent mccomp);

                bool cleared = false;
                if (pixels == null)
                {
                    // Nothing claimed here. Only worth a paint if the tile is currently showing
                    // something - otherwise a whole zone of empty land would build textures for
                    // nothing.
                    if (mccomp == null || !mccomp.IsChunkSet(dx, dz)) continue;
                    pixels = emptyChunkPixels;
                    cleared = true;
                }

                if (mccomp == null)
                {
                    Vec2i baseCord = new Vec2i(mcord.X * cl, mcord.Y * cl);
                    loadedMapData[mcord] = mccomp = new CANMultiChunkMapComponent(capi, baseCord);
                }

                mccomp.setChunk(dx, dz, pixels);
                if (cleared) mccomp.unsetChunk(dx, dz);
                if (!modified.Contains(mccomp)) modified.Add(mccomp);
                painted++;
            }

            foreach (CANMultiChunkMapComponent mccomp in modified)
            {
                mccomp.FinishSetChunks();
            }
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
        public static string EmblemOfCity(string cityName)
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


        /// <summary>Image of the game chunk holding the given plot.</summary>
        public int[] GenerateChunkImage(Vec2i chunkPos, bool informOthers = false)
        {
            int ppcd = plotsPerChunkDim;
            return generateGameChunkImage(chunkPos.X / ppcd, chunkPos.Y / ppcd);
        }

        /// <summary>Image of one game chunk, in game-chunk coordinates. Returns null when no plot of
        /// the chunk is claimed - the caller then knows there is nothing to paint at all.</summary>
        private int[] generateGameChunkImage(int gameChunkX, int gameChunkY)
        {
            int ppcd = plotsPerChunkDim;
            int[] texDataTmp = null;

            Vec2i leftUpperCorner = new Vec2i(gameChunkX * ppcd, gameChunkY * ppcd);

            for (int worldZOffset = 0; worldZOffset < ppcd; worldZOffset++)
            {
                for (int worldXOffset = 0; worldXOffset < ppcd; worldXOffset++)
                {
                    Vec2i tmpVec = new Vec2i(leftUpperCorner.X + worldXOffset, leftUpperCorner.Y + worldZOffset);
                    if (!claims.clientDataStorage.getSavedPlot(tmpVec, out SavedPlotInfo savedPlot)) continue;

                    // Allocated on the first claimed plot: most chunks of a zone have none.
                    if (texDataTmp == null) texDataTmp = new int[chunksize * chunksize];
                    GenerateChunkPart(worldZOffset, worldXOffset, claims.clientDataStorage.ClientGetCityColor(savedPlot.cityName), ref texDataTmp, tmpVec, savedPlot.cityName);
                }
            }
            return texDataTmp;
        }
    }
}
