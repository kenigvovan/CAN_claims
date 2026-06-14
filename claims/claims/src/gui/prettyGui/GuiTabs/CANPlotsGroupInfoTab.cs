using System.Linq;
using System.Numerics;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPlotsGroupInfoTab : CANGuiTab
    {
        public CANPlotsGroupInfoTab(ICoreClientAPI capi, IconHandler iconHandler)
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
            PlotsGroupCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(GuiSys.textInput), null);
            if (cell == null)
            {
                return;
            }
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            var perms = clientInfo.PlayerPermissions;

            // City label (gray, small, centered)
            CenteredTitle(Lang.Get("claims:gui-plotsgroup-city-label", cell.CityName), ColLabel, 1.0f);

            // Group name (gold, large, centered)
            CenteredTitle(cell.Name, ColValue);

            ImGui.Separator();
            ImGui.Spacing();

            if (BackButton())
                GuiSys.selectedTab = EnumSelectedTab.PlotsGroup;

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Members section
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(Lang.Get("claims:gui-group-members", cell.PlayersNames.Count));
            ImGui.PopStyleColor();

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLAYER))
            {
                ImGui.SameLine();
                if (GreenIconButton("addplotsgroupmember", "expander", 16, Lang.Get("claims:gui-add-plotsgroup-member")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ADD_PLOTSGROUP_MEMBER_NEED_NAME;
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER))
            {
                ImGui.SameLine();
                if (RedIconButton("removeplotsgroupmember", "contract", 16, Lang.Get("claims:gui-remove-plotsgroup-member")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_PLOTSGROUP_MEMBER_SELECT;
            }

            if (cell.PlayersNames.Count > 0)
            {
                ImGui.Spacing();
                ImGui.BeginChild("MembersList", new Vector2(0, 120), true);
                foreach (var name in cell.PlayersNames)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                    ImGui.Text(name);
                    ImGui.PopStyleColor();
                }
                ImGui.EndChild();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Plot actions
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLOT))
            {
                if (GreenIconButton("plotsgrpupaddplot", "expander", 16, Lang.Get("claims:gui-plotsgroup-add-plot")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM;
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT) ||
                    perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET))
                    ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT))
            {
                if (RedIconButton("plotsgrpupremoveplot", "contract", 16, Lang.Get("claims:gui-plotsgroup-remove-plot")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM;
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET))
                    ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET))
            {
                if (IconButton("plotsgrpuppermissions", "medal", 16, Lang.Get("claims:gui-plot-permissions")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PERMISSIONS;
            }
        }
    }
}
