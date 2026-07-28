using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.auxialiry.ClaimLimiter;
using claims.src.bb;
using claims.src.beb;
using claims.src.blocks;
using claims.src.cropbehaviors;
using claims.src.claimsext.map;
using claims.src.clextentions;
using claims.src.clientMapHandling;
using claims.src.database;
using claims.src.events;
using claims.src.gui.playerGui;
using claims.src.gui.playerGui.structures;
using claims.src.gui.plotMovementGui;
using claims.src.harmony;
using claims.src.messages;
using claims.src.network.handlers;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.part.structure.union;
using claims.src.perms;
using claims.src.playerMovements;
using claims.src.timers;
using HarmonyLib;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace claims.src
{
    public class claims : ModSystem
    {
        /*==============================================================================================*/
        /*=====================================CLAIMS===================================================*/
        /*==============================================================================================*/
        public static Harmony harmonyInstance;
        public const string harmonyID = "claims.Patches";

        public static claims modInstance;
        public static claims clientModInstance;
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        static DatabaseHandler databaseHandler;

        //Because overwise on signleplayer it will collide
        public static DataStorage dataStorage { get; set; }
        public static DataStorage clientDataStorage {  get; private set; }
        public PlayerMovementListnerClient pmlc;
        public static PlayerMovementsListnerServer serverPlayerMovementListener;
        public static Config config;
        public static IMoneyProvider economyProvider = new NoopMoneyProvider();

        public static event Action<string> AccountBalanceChanged;
        public static void RaiseAccountBalanceChanged(string accountName) => AccountBalanceChanged?.Invoke(accountName);

        /*==============================================================================================*/
        /*=====================================GUI/CLIENT===============================================*/
        /*==============================================================================================*/
        public PlotsMapLayer plotsMapLayer;
        WorldMapManager mapmgr;
        public static CANClaimsGui CANCityGui { get; set; }
        public static CityInfo playerCityInfo;
        public static bool DebugValSet = false;

        public static ClaimsPlayerMovementGUI movementClaimGui { get; set; }
        public ClaimsModApi Api { get; private set; }
        /*==============================================================================================*/
        /*=====================================FUNCTIONS================================================*/
        /*==============================================================================================*/

        public claims()
        {
            //modInstance = this;
            playerCityInfo = null;
        }
        public static claims getModInstance()
        {
            return modInstance;
        }
        public DatabaseHandler getDatabaseHandler()
        {
            return databaseHandler;
        }
        public void AddCustomIcons()
        {
            List<string> iconList = new List<string> { "queen-crown", "exit-door", "achievement",
                                                       "flat-platform", "magnifying-glass", "price-tag",
                                                       "qaitbay-citadel", "large-paint-brush", "prisoner",
                                                       "magic-portal", "dodging", "highlighter",
                                                       "huts-village", "vertical-banner", "village", "stairs-goal",
                                                        "info", "pencil", "soldering-iron", "envelope", "peace-dove",
                                                        "sword-brandish", "frog-mouth-helm"};
            foreach (var icon in iconList)
            {
                capi.Gui.Icons.CustomIcons["claims:" + icon] = delegate (Context ctx, int x, int y, float w, float h, double[] rgba)
                {
                    AssetLocation location = new AssetLocation("claims:textures/icons/" + icon + ".svg");
                    IAsset svgAsset = capi.Assets.TryGet(location, true);
                    int value = ColorUtil.ColorFromRgba(175, 200, 175, 240);
                    capi.Gui.DrawSvg(svgAsset, ctx.GetTarget() as ImageSurface, x, y, (int)w, (int)h, new int?(value));
                };
            }
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("CANTempleBlock", typeof(CANTempleBlock));
            api.RegisterBlockClass("CANCaptureFlagBlock", typeof(CaptureFlagBlock));
            api.RegisterBlockEntityBehaviorClass("FlagEntity", typeof(BlockEntityBehaviorFlag));
            api.RegisterBlockBehaviorClass("Flag", typeof(BlockBehaviorFlag));
            api.RegisterBlockEntityClass("CampAnchor", typeof(BlockEntityCampAnchor));
            //Environment.SetEnvironmentVariable("CAIRO_DEBUG_DISPOSE", "1");
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            clientModInstance = this;
            capi = api;

            AddCustomIcons();

            harmonyInstance = new Harmony(harmonyID);
            ApplyPatches.ApplyClientPatches(harmonyInstance, harmonyID);

            PermsHandler.initDicts();
            if (config == null)
            {
                Config.LoadConfig(capi);
            }
            PlotInfo.initDicts();
            FindAlwaysUseBlocks(api);

            clientChannel = api.Network.RegisterChannel("claimsExt");
            Common.RegisterMessageTypes(clientChannel, capi);
            clientDataStorage = new DataStorage(false);

            mapmgr = api.ModLoader.GetModSystem<WorldMapManager>();
            mapmgr.RegisterMapLayer<PlotsMapLayer>("Plots", 2);
            plotsMapLayer = mapmgr.MapLayers.OfType<PlotsMapLayer>().FirstOrDefault();

            pmlc = new PlayerMovementListnerClient();
            ClientEvents.AddEvents(capi, pmlc);

            int chunkSize = api.World.BlockAccessor.ChunkSize;

            capi.Event.RegisterEventBusListener(onPlayerChangePlotEvent, 0.5, "claimsPlayerChangePlot");

            api.Input.RegisterHotKey("canclaimsgui", "CAN Claims GUI", GlKeys.U, HotkeyType.GUIOrOtherControls, ctrlPressed: true, shiftPressed: true);
            api.Input.SetHotKeyHandler("canclaimsgui", new ActionConsumable<KeyCombination>(this.OnHotKeySkillDialog));

            api.Input.RegisterHotKey("claimsplayermovementgui", "Plot info GUI", GlKeys.K, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("claimsplayermovementgui", new ActionConsumable<KeyCombination>(this.OnHotKeyPlayerMovementGUI));

            api.Event.LeftWorld += ShutDownClient;

            ClientPacketHandlers.RegisterHandlers();
            CANCityGui = new CANClaimsGui(capi);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            modInstance = this;
            sapi = api;
            Config.LoadConfig(sapi);

            Api = new ClaimsModApi();

            City.PlotsMapChanged += (guid, reason) => UsefullPacketsSend.AddToQueueCityInfoUpdate(guid, EnumPlayerRelatedInfo.CITY_PLOTS_MAP);
            AccountBalanceChanged += accountName =>
            {
                if (dataStorage == null) return;
                if (dataStorage.GetPlayerByUid(accountName, out _))
                {
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(accountName, EnumPlayerRelatedInfo.PLAYER_BALANCE);
                }
                else if (accountName.StartsWith(config.CITY_ACCOUNT_STRING_PREFIX))
                {
                    string guid = accountName.Substring(config.CITY_ACCOUNT_STRING_PREFIX.Length);
                    if (dataStorage.getCityByGUID(guid, out City city))
                        UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_BALANCE);
                    else if (dataStorage.GetAllianceByGUID(guid, out Alliance alliance))
                        UsefullPacketsSend.AddToQueueAllianceInfoUpdate(alliance.Guid, EnumPlayerRelatedInfo.ALLIANCE_BALANCE);
                }
            };
            RegisterClaimsEvents();


            PermsHandler.initDicts();
            PlotInfo.initDicts();

            //STORAGE WITH CITIES/PLAYERS/OTHER
            dataStorage = new DataStorage();

            serverChannel = sapi.Network.RegisterChannel("claimsExt");
            Common.RegisterMessageTypes(serverChannel, sapi);

            PlotEvents.lastTimePlayerAskedForPlotsAround = new Dictionary<string, long>();

            //Events for exclaims
            api.Event.RegisterEventBusListener(PlotEvents.updatedPlotHandlerUnclaimed, 0.5, "plotunclaimed");
            api.Event.RegisterEventBusListener(PlotEvents.updatedPlotHandlerClaimed, 0.5, "plotclaimed");

            sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, loadShowChunksMsgsValues);
            sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, ShutDownServer);

            ///////////////////////////////
            harmonyInstance = new Harmony(harmonyID);
            ApplyPatches.ApplyServerPatches(harmonyInstance, harmonyID);
            serverPlayerMovementListener = new PlayerMovementsListnerServer();           
            //uses playermovement method
            events.ServerEvents.AddEvents(sapi);
            commands.register.ServerCommands.RegisterCommands(sapi);
            RightsHandler.readOrCreateRightPerms();
            TimerGeneral.StartServerTimers(sapi);

            ServerPacketHandlers.RegisterHandlers();
            InitLimiters();
            sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, AttachOnlyOnFarmPlotBehavior);
        }
        private void RegisterClaimsEvents()
        {
            City.CityCreated += city =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                t.SetString("mayorUid", city.getMayor()?.Guid ?? "");
                sapi.Event.PushEvent("claims:cityCreated", t);
            };
            City.CityDestroyed += city =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                sapi.Event.PushEvent("claims:cityDestroyed", t);
            };
            City.CitizenJoined += (city, player) =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                t.SetString("playerUid", player.Guid);
                t.SetString("playerName", player.GetPartName());
                sapi.Event.PushEvent("claims:citizenJoined", t);
            };
            City.CitizenLeft += (city, player, reason) =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                t.SetString("playerUid", player.Guid);
                t.SetString("playerName", player.GetPartName());
                t.SetString("reason", reason.ToString());
                sapi.Event.PushEvent("claims:citizenLeft", t);
            };
            City.MayorChanged += (city, newMayor) =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                t.SetString("newMayorUid", newMayor.Guid);
                t.SetString("newMayorName", newMayor.GetPartName());
                sapi.Event.PushEvent("claims:mayorChanged", t);
            };
            City.PlotsMapChanged += (guid, reason) =>
            {
                if (reason == EnumPlotsMapChangeReason.TypeChanged)
                {
                    // detailed event pushed from plotCommand with plot coordinates
                }
            };
            Alliance.AllianceCreated += alliance =>
            {
                var t = new TreeAttribute();
                t.SetString("allianceGuid", alliance.Guid);
                t.SetString("allianceName", alliance.GetPartName());
                t.SetString("founderCityGuid", alliance.MainCity?.Guid ?? "");
                sapi.Event.PushEvent("claims:allianceCreated", t);
            };
            Alliance.AllianceDestroyed += alliance =>
            {
                var t = new TreeAttribute();
                t.SetString("allianceGuid", alliance.Guid);
                t.SetString("allianceName", alliance.GetPartName());
                sapi.Event.PushEvent("claims:allianceDestroyed", t);
            };
            Alliance.CityJoined += (alliance, city) =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                t.SetString("allianceGuid", alliance.Guid);
                t.SetString("allianceName", alliance.GetPartName());
                sapi.Event.PushEvent("claims:cityJoinedAlliance", t);
            };
            Alliance.CityLeft += (alliance, city, reason) =>
            {
                var t = new TreeAttribute();
                t.SetString("cityGuid", city.Guid);
                t.SetString("cityName", city.GetPartName());
                t.SetString("allianceGuid", alliance.Guid);
                t.SetString("allianceName", alliance.GetPartName());
                t.SetString("reason", reason.ToString());
                sapi.Event.PushEvent("claims:cityLeftAlliance", t);
            };
            Alliance.ConflictDeclared += (alliance, opponent) =>
            {
                var t = new TreeAttribute();
                t.SetString("attackerGuid", alliance.Guid);
                t.SetString("attackerName", alliance.GetPartName());
                t.SetString("defenderGuid", opponent.Guid);
                t.SetString("defenderName", opponent.GetPartName());
                sapi.Event.PushEvent("claims:conflictDeclared", t);
            };
            Alliance.ConflictEnded += (alliance, opponent, reason) =>
            {
                var t = new TreeAttribute();
                t.SetString("firstGuid", alliance.Guid);
                t.SetString("firstName", alliance.GetPartName());
                t.SetString("secondGuid", opponent.Guid);
                t.SetString("secondName", opponent.GetPartName());
                t.SetString("reason", reason.ToString());
                sapi.Event.PushEvent("claims:conflictEnded", t);
            };
        }
        private static void AttachOnlyOnFarmPlotBehavior()
        {
            if (!config.CROPS_ONLY_ON_FARM_PLOTS) return;
            foreach (var block in sapi.World.Blocks)
            {
                if (block?.CropProps == null) continue;
                var existing = block.CropProps.Behaviors ?? new CropBehavior[0];
                var extended = new CropBehavior[existing.Length + 1];
                Array.Copy(existing, extended, existing.Length);
                extended[existing.Length] = new BehaviorOnlyOnFarmPlot(block);
                block.CropProps.Behaviors = extended;
            }
        }
        public static void InitLimiters()
        {
            if (claims.config.CLAIM_LIMITERS_ENABLED)
            {
                foreach (var it in claims.config.CLAIM_LIMITERS)
                {
                    if(it.Key == "NearClaimLimiter")
                    {
                        NearClaimLimiter limiter = new NearClaimLimiter(it.Value);
                        if (limiter.Zones != null)
                        {
                            dataStorage.ClaimLimiters.Add(limiter);
                        }
                    }
                    else if(it.Key == "DistantClaimLimiter")
                    {
                        DistantClaimLimiter limiter = new DistantClaimLimiter(it.Value);
                        if (limiter.Zones != null)
                        {
                            dataStorage.ClaimLimiters.Add(limiter);
                        }
                    }
                }
            }
        }
        public static void NullOnServerExit()
        {
            dataStorage = null;
            databaseHandler = null;
        }       
        public static void FindAlwaysUseBlocks(ICoreAPI api)
        {
            foreach (var it in claims.config.ALWAYS_ACCESS_BLOCKS)
            {
                string[] split = it.Split(':');
                if (split.Length < 2) continue;
                foreach (var mod in api.ModLoader.Mods)
                {
                    if (mod.FileName.StartsWith(split[0]))
                    {
                        Type[] allTypes = ((Vintagestory.Common.ModContainer)mod).Assembly.GetTypes();
                        foreach (var ii in allTypes)
                        {
                            if (ii.Name == split[1])
                            {
                                claims.config.blockTypesAccess.Add(ii);
                            }
                        }
                    }
                }
            }
        }
        public static void loadShowChunksMsgsValues()
        {
            var showMsgsDict = sapi.WorldManager.SaveGame.GetData<Dictionary<string, int>>("claimsshowchunkmsgs");
            
            if (showMsgsDict != null)
            {
                foreach (var pl in claims.dataStorage.getPlayersDict())
                {
                    if (showMsgsDict.TryGetValue(pl.Key, out int value))
                    {
                        pl.Value.showPlotMovement = (EnumShowPlotMovement)value;
                    }
                }
            }
        }           
        public static bool loadDatabase()
        {
            try
            {
                databaseHandler = new SQLiteDatabaseHanlder();
                claims.getModInstance().getDatabaseHandler().loadDummyWolrdInfo();
            }
            catch (SqliteException ex)
            {
                MessageHandler.sendErrorMsg("loadDatabase:" + ex.Message);
                return false;
            }
            return true;
        }
        public static bool saveDatabase()
        {
            try
            {
                databaseHandler.saveEveryThing();
            }
            catch (SqliteException ex)
            {
                MessageHandler.sendErrorMsg("saveDatabase:" + ex.Message);
                return false;
            }
            return true;
        }

        public static void ShutDownServer()
        {
            ServerEvents.onShutdown();
            harmonyInstance?.UnpatchAll(harmonyID);
            harmonyInstance = null;
            modInstance = null;
            
            serverChannel = null;
            databaseHandler = null;
            dataStorage = null;
            serverPlayerMovementListener = null;
            config = null;
            economyProvider = new NoopMoneyProvider();
            ConflictHandler.clearAll();
            UnionHander.clearAll();
        }

        public static void ShutDownClient()
        {
            if (claims.clientModInstance.pmlc != null && clientDataStorage != null)
            {
                claims.clientModInstance.pmlc.saveActiveZonesToDb();
            }
            if (claims.clientModInstance.pmlc != null)
            {
                claims.clientModInstance.pmlc.OnShutDown();
            }
            CANCityGui = null;
            harmonyInstance.UnpatchAll(harmonyID);
            harmonyInstance = null;
            clientModInstance = null;
            clientChannel = null;
            clientDataStorage = null;
            playerCityInfo = null;
            movementClaimGui = null;
            gui.ClientChat.Reset();
            config = null;
        }

        /*==============================================================================================*/
        /*=====================================ClaimsExt================================================*/
        /*==============================================================================================*/
        private bool OnHotKeySkillDialog(KeyCombination comb)
        {
            CANCityGui ??= new CANClaimsGui(capi);
            if (CANCityGui.IsOpened())
            {
                CANCityGui.TryClose();
            }
            else
                CANCityGui.TryOpen();
            return true;
        }
        private bool OnHotKeyPlayerMovementGUI(KeyCombination comb)
        {
            movementClaimGui ??= new ClaimsPlayerMovementGUI(capi);
            if (movementClaimGui.IsOpened())
            {
                movementClaimGui.TryClose();
            }
            else
                movementClaimGui.TryOpen();
            return true;
        }
        public void onPlayerChangePlotEvent(string eventName, ref EnumHandling handling, IAttribute data)
        {

            TreeAttribute tree = data as TreeAttribute;
            Vec2i from = new Vec2i(tree.GetInt("xChO"), tree.GetInt("zChO"));
            Vec2i to = new Vec2i(tree.GetInt("xCh"), tree.GetInt("zCh"));

            clientDataStorage.getSavedPlot(from, out SavedPlotInfo savedPlotInfoFrom);
            clientDataStorage.getSavedPlot(to, out SavedPlotInfo savedPlotInfoTo);
            //var c = capi.Assets;
            //STILL IN THE WILD LANDS
            if (savedPlotInfoFrom == null && savedPlotInfoTo == null)
            {
                //from wild to wild
                return;
            }
            if (movementClaimGui == null)
            {
                movementClaimGui = new ClaimsPlayerMovementGUI(capi);
                movementClaimGui.TryOpen();
                capi.Event.RegisterCallback((dt =>
                {
                    checkMovementStatus();
                }), 1000 * 25);
                return;
            }


            if (!movementClaimGui.IsOpened())
            {
                movementClaimGui.TryOpen();
                capi.Event.RegisterCallback((dt =>
                {
                    checkMovementStatus();
                }), 1000 * 25);
            }

            if (savedPlotInfoTo == null)
            {
                updateMovementGUIInfo();
            }
            else
            {
                updateMovementGUIInfo(savedPlotInfoTo);
            }
            movementClaimGui.timeStampShouldBeClosed = TimeFunctions.getEpochSeconds() + 15;
        }
        public void checkMovementStatus()
        {
            if (movementClaimGui == null) return;
            if (movementClaimGui.timeStampShouldBeClosed < TimeFunctions.getEpochSeconds())
            {
                movementClaimGui.TryClose();
                //capi.World.Player.ShowChatNotification("close gui");
            }
            else
            {
                //capi.World.Player.ShowChatNotification("wait again");
                capi.Event.RegisterCallback((dt =>
                {
                    checkMovementStatus();
                }), 1000 * 15);
            }
        }
        public static void updateMovementGUIInfo(SavedPlotInfo plot = null)
        {
            if (plot == null)
            {
                var cai = CairoFont.WhiteDetailText().WithFontSize(18);
                movementClaimGui.SingleComposer.GetRichtext("line_1")
                    .SetNewText(Lang.Get("claims:movementgui-wild-lands"), cai);
                movementClaimGui.SingleComposer.GetRichtext("line_2")
              .SetNewText("", cai);
                movementClaimGui.SingleComposer.GetRichtext("line_3")
               .SetNewText("", cai);
                movementClaimGui.SingleComposer.GetRichtext("line_4")
               .SetNewText("", cai);
                movementClaimGui.SingleComposer.GetRichtext("line_5")
              .SetNewText("", cai);
            }
            else
            {
                var cai = CairoFont.WhiteDetailText().WithFontSize(18).WithOrientation(EnumTextOrientation.Center);
               /* var f = movementClaimGui.Single*Composer.GetRichtext("line_1");
                var p = f.Components[0];*/

                //(p as RichTextComponent).Font.Orientation = EnumTextOrientation.Center;
                movementClaimGui.SingleComposer.GetRichtext("line_1")
               .SetNewText(Lang.Get("claims:movementgui-city-name", plot.cityName), cai);
                
                int currentLine = 2;

                if (plot.plotName.Length > 0)
                {
                    movementClaimGui.SingleComposer.GetRichtext("line_" + currentLine)
                   .SetNewText(Lang.Get("claims:movementgui-plot-name", plot.plotName), cai);
                    currentLine++;
                }

                if (plot.groupName.Length > 0)
                {
                    movementClaimGui.SingleComposer.GetRichtext("line_" + currentLine)
                    .SetNewText(Lang.Get("claims:movementgui-group-name", plot.groupName), cai);
                    currentLine++;
                }

                if (plot.PvPIsOn)
                {
                    movementClaimGui.SingleComposer.GetRichtext("line_" + currentLine)
                    .SetNewText(Lang.Get("claims:movementgui-pvp-on"), cai);
                }
                else
                {
                    movementClaimGui.SingleComposer.GetRichtext("line_" + currentLine)
                    .SetNewText(Lang.Get("claims:movementgui-pvp-off"), cai);
                }
                currentLine++;
                if (plot.price > -1)
                {
                    movementClaimGui.SingleComposer.GetRichtext("line_" + currentLine)
                   .SetNewText(Lang.Get("claims:movementgui-price", plot.price), cai);
                    currentLine++;
                }
                
                for (; currentLine <= 5; currentLine++)
                {
                    movementClaimGui.SingleComposer.GetRichtext("line_" + currentLine)
                   .SetNewText("", cai);
                }

                //claimsext.movementClaimGui.SetupDialog();
            }

        }

    }
}