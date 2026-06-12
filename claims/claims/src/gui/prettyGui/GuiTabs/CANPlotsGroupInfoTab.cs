using System.Linq;
using System.Numerics;
using claims.src.auxialiry;
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

            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);

            // City label (gray, small, centered)
            float windowWidth = ImGui.GetWindowSize().X;
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            string cityLabel = Lang.Get("claims:gui-plotsgroup-city-label", cell.CityName);
            float cityLabelWidth = ImGui.CalcTextSize(cityLabel).X;
            ImGui.SetCursorPosX((windowWidth - cityLabelWidth) * 0.5f);
            ImGui.Text(cityLabel);
            ImGui.PopStyleColor();

            // Group name (gold, large, centered)
            ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
            ImGui.SetWindowFontScale(1.3f);
            float nameWidth = ImGui.CalcTextSize(cell.Name).X;
            ImGui.SetCursorPosX((windowWidth - nameWidth) * 0.5f);
            ImGui.Text(cell.Name);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button(Lang.Get("claims:gui-back")))
            {
                GuiSys.selectedTab = EnumSelectedTab.PlotsGroup;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Members section
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.Text(Lang.Get("claims:gui-group-members", cell.PlayersNames.Count));
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered() && cell.PlayersNames.Count > 0)
            {
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(cell.PlayersNames, ','));
            }

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLAYER))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("addplotsgroupmember", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ADD_PLOTSGROUP_MEMBER_NEED_NAME;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-add-plotsgroup-member"));
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("removeplotsgroupmember", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_PLOTSGROUP_MEMBER_SELECT;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-remove-plotsgroup-member"));
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Plot actions
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLOT))
            {
                if (ImGui.ImageButton("plotsgrpupaddplot", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-add-plot"));
                ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT))
            {
                if (ImGui.ImageButton("plotsgrpupremoveplot", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-remove-plot"));
                ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET))
            {
                if (ImGui.ImageButton("plotsgrpuppermissions", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PERMISSIONS;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-plot-permissions"));
            }
        }
    }
}
