using System.Numerics;
using claims.src.gui.prettyGui;
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
            api.Event.LevelFinalize += () =>
            {
                api.ModLoader.GetModSystem<ImGuiModSystem>().Draw += Draw;
            };
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

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
                 | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs;
            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.252f, 0.161f, 0.016f, 1f));
            ImGui.Begin("Claims", p_open: ref this.prettyGuiState.IsOpen,  flags1);

            var assetPath = new AssetLocation("claims:textures/images/RWS_Tarot_18_Moon.jpg");
            var asset = capi.Assets.TryGet(assetPath);
            string p = "";
            foreach(var it in capi.Assets.AllAssets)
            {
                if(it.Key.Path.Contains("Tarot"))
                {
                    var c = 3;
                    asset = it.Value;
                    p = it.Key;
                }
            }
            LoadedTexture guiTex = new LoadedTexture(capi);

            capi.Render.GetOrLoadTexture(
                new AssetLocation("claims", "textures/RWS_Tarot_18_Moon.jpg"),
                ref guiTex
            );
            



            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.4f, 0.8f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.9f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.3f, 0.7f, 1.0f));
            ImGui.PopStyleColor(3);

            string[] labels = { "qaitbay-citadel", "magnifying-glass", "price-tag", "flat-platform", "prisoner", "magic-portal", "huts-village" };
            string[] labelsToolTips = { "gui-city-tooltip", "gui-citizen-info-tooltip", "gui-prices-tooltip", "gui-plot-tooltip", "gui-prison-tooltip", "gui-summon-tooltip", "gui-plotsgroup-tooltip" };
              var newTexture = capi.Render.GetOrLoadTexture(assetPath);

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
            for (int i = 0; i < 7; i++)
            {
                
                ImGui.PushID(i);

                if (active_button == i)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[23]);

                if (ImGui.ImageButton("", this.iconHandler.GetOrLoadIcon(labels[i]), new Vector2(60)))
                {
                    active_button = i;
                    selectedTab = (EnumSelectedTab)i;
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
