using System.Numerics;
using claims.src.gui.prettyGui;
using claims.src.rights;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VSImGui;
using VSImGui.API;

namespace claims.src
{
    public class claimsGui: ModSystem
    {
        public ICoreClientAPI capi;
        public PrettyGuiState prettyGuiState = new PrettyGuiState();
        public static LoadedTexture myTex = null;
        public static int active_button = 0;
        public static int svgid = 0;
        public static bool showBalanceHud = false;
        private ImGuiModSystem imguiSys;
        private IconHandler iconHandler;
        private TabDrawHandler tabDrawHandler;
        private ImageHandler imageHandler;
        private SecondaryTabDrawHandler secondaryTabDrawHandler;
        public EnumSelectedTab selectedTab = EnumSelectedTab.CITY;
        public EnumSelectedTab conflictSourceTab = EnumSelectedTab.AllianceInfoPage;
        public EnumSecondaryWindowTab _secondaryWindowTab;
        public EnumSecondaryWindowTab secondaryWindowTab { set { this.secondaryWindowOpen = true; this._secondaryWindowTab = value; } get { return _secondaryWindowTab;  } }
        public Vector2 mainWindowPos;
        public Vector2 mainWindowSize;
        public string textInput = "";
        public string textInput2 = "";
        public int intInput = 0;
        public int doubleInput = 0;
        public int selectedComboFirst = 0;
        public bool secondaryWindowOpen = false;
        public Vec3i selectedPos { get; set; } = null;
        public string[] multiSelectItems = { };
        public bool[] selectedItems = { };
        public string[] multiSelectItems2 = { };
        public bool[] selectedItems2 = { };
        public int selectedWarrangeTab = -1;
        public override double ExecuteOrder()
        {
            return 1;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.Input.RegisterHotKey("prettycangui", "Pretty CAN Claims GUI", GlKeys.U, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("prettycangui", new ActionConsumable<KeyCombination>(this.SwitchGui));
            this.imguiSys = api.ModLoader.GetModSystem<ImGuiModSystem>();
            iconHandler = new IconHandler(api);
            imageHandler = new ImageHandler(api);
            tabDrawHandler = new TabDrawHandler(api, iconHandler);
            secondaryTabDrawHandler = new SecondaryTabDrawHandler(api, iconHandler);
            if (claims.config?.BalanceHudOverride.HasValue == true)
                showBalanceHud = claims.config.BalanceHudOverride.Value;
            api.ChatCommands.Create("claimshud")
                .WithDescription("Toggle balance HUD")
                .HandleWith(args =>
                {
                    showBalanceHud = !showBalanceHud;
                    claims.config.BalanceHudOverride = showBalanceHud;
                    // Load the file first so we only update BalanceHudOverride,
                    // not overwrite server settings with the client's in-memory copy
                    var savedCfg = api.LoadModConfig<Config>("claims.json") ?? new Config();
                    savedCfg.BalanceHudOverride = showBalanceHud;
                    api.StoreModConfig(savedCfg, "claims.json");
                    return TextCommandResult.Success("Balance HUD: " + (showBalanceHud ? "on" : "off"));
                });
            api.Event.LevelFinalize += () =>
            {
                api.ModLoader.GetModSystem<ImGuiModSystem>().Draw += Draw;
                api.ModLoader.GetModSystem<ImGuiModSystem>().Draw += DrawBalanceHUD;
            };
        }
        private CallbackGUIStatus DrawBalanceHUD(float deltaSeconds)
        {
            if (claims.config?.SELECTED_ECONOMY_HANDLER != "VIRTUAL_MONEY")
                return CallbackGUIStatus.DontGrabMouse;
            if (!showBalanceHud)
                return CallbackGUIStatus.DontGrabMouse;
            var clientInfo = claims.clientDataStorage?.clientPlayerInfo;
            if (clientInfo == null)
                return CallbackGUIStatus.DontGrabMouse;

            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.78f, io.DisplaySize.Y - 60), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.75f);
            ImGui.Begin("##balancehud",
                ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav);

            ImGui.Text(Lang.Get("claims:gui-hud-player-balance", clientInfo.PlayerBalance));

            if (clientInfo.CityInfo != null &&
                clientInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
            {
                ImGui.Text(Lang.Get("claims:gui-city-balance-hud", clientInfo.CityInfo.CityBalance));
            }

            ImGui.End();
            return CallbackGUIStatus.DontGrabMouse;
        }

        private bool SwitchGui(KeyCombination comb)
        {
            if (this.prettyGuiState.IsOpen)
            {
                this.prettyGuiState.IsOpen = false;
            }
            else
            {
                this.prettyGuiState.IsOpen = true;
                this.imguiSys.Show();
            }
            return true;
        }
        private void OpenGui()
        {
            if (this.prettyGuiState.IsOpen)
            {
                return;
            }
            this.prettyGuiState.IsOpen = true;
            this.imguiSys.Show();
        }
        private CallbackGUIStatus Draw(float deltaSeconds)
        {
            // Reset each frame — will be set to true by ImGuiInventoryGrid.Draw() if active
            ImGuiInventoryGrid.SuppressMouseDrop = false;

            if(!this.prettyGuiState.IsOpen)
            {
                return CallbackGUIStatus.Closed;
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                this.prettyGuiState.IsOpen = false;
            }

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
                 | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs;
            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.252f, 0.161f, 0.016f, 1f));
            ImGui.Begin("Claims", p_open: ref this.prettyGuiState.IsOpen,  flags1);

            



            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.4f, 0.8f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.9f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.3f, 0.7f, 1.0f));
            ImGui.PopStyleColor(3);

            bool economyEnabled = !string.IsNullOrEmpty(claims.config?.SELECTED_ECONOMY_HANDLER);
            string[] labels = economyEnabled
                ? new[] { "qaitbay-citadel", "magnifying-glass", "price-tag", "flat-platform", "prisoner", "magic-portal", "huts-village" }
                : new[] { "qaitbay-citadel", "magnifying-glass", "flat-platform", "prisoner", "magic-portal", "huts-village" };
            string[] labelsToolTips = economyEnabled
                ? new[] { "gui-city-tooltip", "gui-citizen-info-tooltip", "gui-prices-tooltip", "gui-plot-tooltip", "gui-prison-tooltip", "gui-summon-tooltip", "gui-plotsgroup-tooltip" }
                : new[] { "gui-city-tooltip", "gui-citizen-info-tooltip", "gui-plot-tooltip", "gui-prison-tooltip", "gui-summon-tooltip", "gui-plotsgroup-tooltip" };
            var tabOrder = economyEnabled
                ? new[] { EnumSelectedTab.CITY, EnumSelectedTab.PLAYER, EnumSelectedTab.PRICES, EnumSelectedTab.PLOT, EnumSelectedTab.PRISON, EnumSelectedTab.SUMMON, EnumSelectedTab.PlotsGroup }
                : new[] { EnumSelectedTab.CITY, EnumSelectedTab.PLAYER, EnumSelectedTab.PLOT, EnumSelectedTab.PRISON, EnumSelectedTab.SUMMON, EnumSelectedTab.PlotsGroup };

            var draw = ImGui.GetWindowDrawList();

            var p0 = ImGui.GetWindowPos();
            var p1 = new Vector2(
                p0.X + ImGui.GetWindowWidth(),
                p0.Y + ImGui.GetWindowHeight()
            );

            /*draw.AddImage(
                guiTex.TextureId,
                p0,
                p1
            );*/
            //ImGui.ImageButton("c", guiTex.TextureId, new Vector2(120));
            //int te = capi.Assets
            for (int i = 0; i < labels.Length; i++)
            {

                ImGui.PushID(i);

                if (active_button == i)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[23]);

                if (ImGui.ImageButton("", this.iconHandler.GetOrLoadIcon(labels[i]), new Vector2(60)))
                {
                    active_button = i;
                    selectedTab = tabOrder[i];
                }

                if (active_button == i)
                    ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get($"claims:{labelsToolTips[i]}"));
                }

                ImGui.PopID();
                ImGui.SameLine();
            }
            ImGui.NewLine();
            ImGui.Separator();
            this.tabDrawHandler.DrawTab(this.selectedTab);
            this.mainWindowPos = ImGui.GetWindowPos();
            this.mainWindowSize = ImGui.GetWindowSize();
          
            
            ImGui.End();
            if (this.secondaryWindowTab != EnumSecondaryWindowTab.NONE && this.secondaryWindowOpen)
            {
                secondaryTabDrawHandler.DrawTab(this.secondaryWindowTab);
            }

            ImGui.End();
            return CallbackGUIStatus.GrabMouse;
        }
    }
}
