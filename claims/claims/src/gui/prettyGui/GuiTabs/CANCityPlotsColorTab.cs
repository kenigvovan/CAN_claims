using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANCityPlotsColorTab : CANGuiTab
    {
        private int selectedColorIndex = -1;

        public CANCityPlotsColorTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);

            // --- Header ---
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.3f);
            string title = Lang.Get("claims:gui-city-plots-color");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(title).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(title);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            // --- Current color ---
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-color-current"));
                ImGui.PopStyleColor();
                ImGui.SameLine();

                int currentColor = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsColor;
                Vector4 currentColorVec = IntToColorVec4(currentColor);
                ImGui.ColorButton("currentcolor", currentColorVec, ImGuiColorEditFlags.None, new Vector2(30, 30));

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }

            // --- Color picker grid ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-color-select"));
            ImGui.PopStyleColor();

            ImGui.Spacing();

            int[] colors = claims.config.PLOT_COLORS;
            if (colors == null || colors.Length == 0)
            {
                ImGui.Text(Lang.Get("claims:gui-color-none-available"));
                return;
            }

            float buttonSize = 30f;
            float spacing = 4f;
            float availWidth = ImGui.GetContentRegionAvail().X;
            int colsPerRow = (int)((availWidth + spacing) / (buttonSize + spacing));
            if (colsPerRow < 1) colsPerRow = 1;

            for (int i = 0; i < colors.Length; i++)
            {
                Vector4 colorVec = IntToColorVec4(colors[i]);

                if (i % colsPerRow != 0)
                    ImGui.SameLine(0, spacing);

                bool isSelected = (selectedColorIndex == i);
                if (isSelected)
                {
                    // Draw highlight border for selected color
                    Vector2 cursorPos = ImGui.GetCursorScreenPos();
                    var drawList = ImGui.GetWindowDrawList();
                    drawList.AddRect(
                        cursorPos - new Vector2(2, 2),
                        cursorPos + new Vector2(buttonSize + 2, buttonSize + 2),
                        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)),
                        0, ImDrawFlags.None, 2f);
                }

                ImGui.PushID(i);
                if (ImGui.ColorButton("##color", colorVec, ImGuiColorEditFlags.None, new Vector2(buttonSize, buttonSize)))
                {
                    selectedColorIndex = i;
                }
                ImGui.PopID();
            }

            ImGui.Spacing();
            ImGui.Spacing();

            // --- Selected preview + Apply button ---
            if (selectedColorIndex >= 0 && selectedColorIndex < colors.Length)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-color-selected"));
                ImGui.PopStyleColor();
                ImGui.SameLine();

                Vector4 selColorVec = IntToColorVec4(colors[selectedColorIndex]);
                ImGui.ColorButton("selectedpreview", selColorVec, ImGuiColorEditFlags.None, new Vector2(30, 30));

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.Button(Lang.Get("claims:gui-color-apply")))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(
                        GlobalConstants.CurrentChatGroup,
                        "/city set colorint " + colors[selectedColorIndex],
                        EnumChatType.Macro, "");
                    selectedColorIndex = -1;
                }
                ImGui.PopStyleColor(3);
            }

            // --- Back button ---
            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 40);
            if (ImGui.Button(Lang.Get("claims:gui-back-button")))
            {
                GuiSys.selectedTab = EnumSelectedTab.CITY;
            }
        }

        private Vector4 IntToColorVec4(int color)
        {
            // ARGB int to ImGui Vector4 (RGBA floats 0-1)
            int a = (color >> 24) & 0xFF;
            int r = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = color & 0xFF;
            return new Vector4(r / 255f, g / 255f, b / 255f, a == 0 ? 1f : a / 255f);
        }
    }
}
