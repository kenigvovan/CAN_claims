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

            CenteredTitle(Lang.Get("claims:gui-plots-group-title"), ColSection, 1.2f);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-description"));

            ImGui.Separator();
            ImGui.Spacing();

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE))
            {
                if (GreenIconButton("addnewplotsgroup", "expander", 16, Lang.Get("claims:gui-add-new-plotsgroup")))
                {
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME;
                }
                ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE))
            {
                if (RedIconButton("removeplotsgroup", "contract", 16, Lang.Get("claims:gui-remove-plotsgroup")))
                {
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_REMOVE_SELECT;
                }
                ImGui.SameLine();
            }
            if (IconButton("showreceivedplotsgroupinvites", "circle", 16, Lang.Get("claims:gui-show-received-invites")))
                GuiSys.selectedTab = EnumSelectedTab.PLOTSGROUPRECEIVEDINVITES;

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
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(plotsGroup.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // City label (gray, on same line as info button)
                Label(Lang.Get("claims:gui-plotsgroup-city-label", plotsGroup.CityName));

                ImGui.SameLine();
                Label("  " + Lang.Get("claims:gui-group-members", plotsGroup.PlayersNames.Count));
                if (ImGui.IsItemHovered() && plotsGroup.PlayersNames.Count > 0)
                {
                    ImGui.SetTooltip(string.Join(", ", plotsGroup.PlayersNames));
                }

                ImGui.SameLine();
                if (IconButton("infoplotsgroup", "info", 14, Lang.Get("claims:gui-plotsgroup-open-info")))
                {
                    GuiSys.selectedTab = EnumSelectedTab.PlotsGroupInfoPage;
                    GuiSys.textInput = plotsGroup.Guid;
                }

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
