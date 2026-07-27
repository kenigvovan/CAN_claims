using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
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
        public Dictionary<string, LoadedTexture> texturesByIcon;
        public MeshRef quadModel;

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
            if (api.Side == EnumAppSide.Client)
            {
                quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
            }
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
            await Task.Run(async () =>
            {
                //System.Threading.Thread.Sleep(1000);
                int color = 0;
                if (cityName != null)
                {
                    claims.clientDataStorage.ClientGetCityColor(cityName);
                }


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
        public override void OnMapOpenedClient()
        {
            if (texturesByIcon == null)
            {
                if (texturesByIcon != null)
                {
                    foreach (var val in texturesByIcon)
                    {
                        val.Value.Dispose();
                    }
                }

                texturesByIcon = new Dictionary<string, LoadedTexture>();

                double scale = RuntimeEnv.GUIScale;
                int size = (int)(27 * scale);

                ImageSurface surface = new ImageSurface(Format.Argb32, size, size);
                Context ctx = new Context(surface);

                string[] icons = new string[] { "circle", "bee", "cave", "home", "ladder", "pick", "rocks", "ruins", "spiral", "star1", "star2", "trader", "vessel", "cross" };
                ICoreClientAPI capi = api as ICoreClientAPI;

                foreach (var val in icons)
                {
                    ctx.Operator = Operator.Clear;
                    ctx.SetSourceRGBA(0, 0, 0, 0);
                    ctx.Paint();
                    ctx.Operator = Operator.Over;

                    capi.Gui.Icons.DrawIcon(ctx, "wp" + val.UcFirst(), 1, 1, size - 2, size - 2, new double[] { 0, 0, 0, 1 });
                    capi.Gui.Icons.DrawIcon(ctx, "wp" + val.UcFirst(), 2, 2, size - 4, size - 4, ColorUtil.WhiteArgbDouble);

                    texturesByIcon[val] = new LoadedTexture(api as ICoreClientAPI, (api as ICoreClientAPI).Gui.LoadCairoTexture(surface, false), (int)(20 * scale), (int)(20 * scale));
                }

                ctx.Dispose();
                surface.Dispose();
            }
        }
        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {

            foreach (var val in loadedMapData)
            {
                val.Value.OnMouseMove(args, mapElem, hoverText);
                var c = val.Value;
                Vec2f viewPos = new Vec2f();
                mapElem.TranslateWorldPosToViewPos(new Vec3d(val.Value.chunkCoord.X * PlotPosition.plotSize + PlotPosition.plotSize / 2, 0, val.Value.chunkCoord.Y * PlotPosition.plotSize + PlotPosition.plotSize / 2), ref viewPos);
            }
            double mouseX = args.X - mapElem.Bounds.renderX;
            double mouseY = args.Y - mapElem.Bounds.renderY;
            float halfPlot = PlotPosition.plotSize / 2f * mapElem.ZoomLevel;

            // Only ever describe ONE plot: the one closest to the cursor. Appending for every plot whose
            // hit box contains the cursor printed the city line two (or more) times on plot boundaries.
            SavedPlotInfo hovered = null;
            double bestDist = double.MaxValue;
            Vec2f plotViewPos = new Vec2f();
            foreach (var zone in claims.clientDataStorage.getClientSavedPlots())
            {
                foreach (var savedPlot in zone.Value.savedPlots)
                {
                    mapElem.TranslateWorldPosToViewPos(new Vec3d(savedPlot.Key.X * PlotPosition.plotSize + PlotPosition.plotSize / 2, 0, savedPlot.Key.Y * PlotPosition.plotSize + PlotPosition.plotSize / 2), ref plotViewPos);

                    double dx = plotViewPos.X - mouseX;
                    double dy = plotViewPos.Y - mouseY;
                    if (Math.Abs(dx) >= halfPlot || Math.Abs(dy) >= halfPlot) continue;

                    double dist = dx * dx + dy * dy;
                    if (dist >= bestDist) continue;
                    bestDist = dist;
                    hovered = savedPlot.Value;
                }
            }
            if (hovered != null && hovered.cityName.Length > 0)
            {
                hoverText.AppendLine(Lang.Get("claims:map_plot_city_name", hovered.cityName));
                if (hovered.price > 0)
                {
                    hoverText.AppendLine(Lang.Get("claims:map_plot_price", hovered.price));
                }
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
