using System.Collections.Generic;
using System.Numerics;
using claims.src.gui.prettyGui;
using claims.src.network.packets;
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

        private int _adminActiveButton = -1;
        private bool _isAdmin = false;
        private bool _adminChecked = false;
        private bool _adminPanelVisible = true;
        public Dictionary<string, AdminCityFlagsItem> AdminCityFlags { get; } = new Dictionary<string, AdminCityFlagsItem>();
        public AdminWorldFlags AdminWorldState { get; set; }

        private static readonly string[] AdminTabIcons    = { "queen-crown", "highlighter", "sword-brandish", "soldering-iron" };
        private static readonly string[] AdminTabTooltips = { "Admin: World Settings", "Admin: Cities", "Admin: War", "Admin: Player & Plot" };
        private static readonly EnumSelectedTab[] AdminTabOrder = { EnumSelectedTab.ADMIN_WORLD, EnumSelectedTab.ADMIN_CITIES, EnumSelectedTab.ADMIN_WAR, EnumSelectedTab.ADMIN_PLAYER };
        private static readonly Vector4 AdminBtnColor  = new Vector4(0.50f, 0.12f, 0.12f, 1.0f);
        private static readonly Vector4 AdminBtnActive = new Vector4(0.70f, 0.22f, 0.22f, 1.0f);

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
                tabDrawHandler = new TabDrawHandler(api, iconHandler);
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
        private void CheckAdminRole()
        {
            if (_adminChecked) return;
            var role = capi.World?.Player?.Role;
            if (role == null) return;
            _adminChecked = true;
            bool wasAdmin = _isAdmin;
            _isAdmin = claims.config?.ROLE_CODES_WITH_ADMIN_RIGHTS?.Contains(role.Code) == true;
            if (_isAdmin && !wasAdmin)
            {
                tabDrawHandler.RegisterAdminTabs(capi, iconHandler);
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = network.packets.PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
            }
        }

        private CallbackGUIStatus Draw(float deltaSeconds)
        {
            // Reset each frame — will be set to true by ImGuiInventoryGrid.Draw() if active
            ImGuiInventoryGrid.SuppressMouseDrop = false;

            CheckAdminRole();

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

            for (int i = 0; i < labels.Length; i++)
            {
                ImGui.PushID(i);

                if (active_button == i)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[23]);

                if (ImGui.ImageButton("", this.iconHandler.GetOrLoadIcon(labels[i]), new Vector2(60)))
                {
                    active_button = i;
                    _adminActiveButton = -1;
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

            if (_isAdmin)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, _adminPanelVisible
                    ? new Vector4(0.50f, 0.12f, 0.12f, 1.0f)
                    : new Vector4(0.25f, 0.08f, 0.08f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.65f, 0.18f, 0.18f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.35f, 0.08f, 0.08f, 1.0f));
                if (ImGui.Button("##adminToggle", new Vector2(10, 60)))
                {
                    _adminPanelVisible = !_adminPanelVisible;
                    if (!_adminPanelVisible && System.Array.IndexOf(AdminTabOrder, selectedTab) >= 0)
                    {
                        selectedTab = EnumSelectedTab.CITY;
                        active_button = 0;
                        _adminActiveButton = -1;
                    }
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(_adminPanelVisible ? "Hide admin tabs" : "Show admin tabs");
                ImGui.SameLine();

                if (_adminPanelVisible)
                    DrawAdminButtons();
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

        private void DrawAdminButtons()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, AdminBtnColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, AdminBtnActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.08f, 0.08f, 1.0f));

            for (int j = 0; j < AdminTabIcons.Length; j++)
            {
                ImGui.PushID(1000 + j);

                if (_adminActiveButton == j)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[23]);

                if (ImGui.ImageButton("", this.iconHandler.GetOrLoadIcon(AdminTabIcons[j]), new Vector2(60)))
                {
                    _adminActiveButton = j;
                    active_button = -1;
                    selectedTab = AdminTabOrder[j];
                }

                if (_adminActiveButton == j)
                    ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(AdminTabTooltips[j]);
                }

                ImGui.PopID();
                ImGui.SameLine();
            }

            ImGui.PopStyleColor(3);
        }
    }
}
