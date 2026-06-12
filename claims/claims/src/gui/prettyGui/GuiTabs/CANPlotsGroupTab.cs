using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPlotsGroupTab : CANGuiTab
    {
        public CANPlotsGroupTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
            {
                return;
            }

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            var perms = clientInfo.PlayerPermissions;

            Vector4 titleColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 groupNameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);

            ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
            ImGui.SetWindowFontScale(1.2f);
            string titleText = Lang.Get("claims:gui-plots-group-title");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(titleText).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(titleText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-description"));

            ImGui.Separator();
            ImGui.Spacing();

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE))
            {
                if (ImGui.ImageButton("addnewplotsgroup", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-add-new-plotsgroup"));
                ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE))
            {
                if (ImGui.ImageButton("removeplotsgroup", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_REMOVE_SELECT;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-remove-plotsgroup"));
                ImGui.SameLine();
            }
            if (ImGui.ImageButton("showreceivedplotsgroupinvites", this.iconHandler.GetOrLoadIcon("circle"), new Vector2(16)))
            {
                GuiSys.selectedTab = EnumSelectedTab.PLOTSGROUPRECEIVEDINVITES;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-show-received-invites"));

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.BeginChild("PlotsGroupsScroll", new Vector2(0, 0), false);
            int i = 0;
            foreach (var plotsGroup in clientInfo.CityInfo.PlotsGroupCells)
            {
                ImGui.PushID(i);
                ImGui.BeginGroup();

                // Group name (gold, larger)
                ImGui.PushStyleColor(ImGuiCol.Text, groupNameColor);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(plotsGroup.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // City label (gray, on same line as info button)
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-plotsgroup-city-label", plotsGroup.CityName));
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text("  " + Lang.Get("claims:gui-group-members", plotsGroup.PlayersNames.Count));
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered() && plotsGroup.PlayersNames.Count > 0)
                {
                    ImGui.SetTooltip(string.Join(", ", plotsGroup.PlayersNames));
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("infoplotsgroup", this.iconHandler.GetOrLoadIcon("info"), new Vector2(14)))
                {
                    GuiSys.selectedTab = EnumSelectedTab.PlotsGroupInfoPage;
                    GuiSys.textInput = plotsGroup.Guid;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-open-info"));

                ImGui.EndGroup();
                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 4));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 4));
                i++;
            }
            ImGui.EndChild();
        }
    }
}
