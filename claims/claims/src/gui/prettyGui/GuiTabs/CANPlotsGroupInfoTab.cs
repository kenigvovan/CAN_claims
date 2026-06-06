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
            string titleText = string.Format("{0}: {1}", cell.CityName, cell.Name);
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(titleText).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(titleText);

            var perms = clientInfo.PlayerPermissions;

            ImGui.Text(Lang.Get("claims:gui-group-members", cell.PlayersNames.Count));
            if (ImGui.IsItemHovered())
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
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-add-plotsgroup-member"));
                }
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("removeplotsgroupmember", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_PLOTSGROUP_MEMBER_SELECT;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-remove-plotsgroup-member"));
                }
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET))
            {
                ImGui.Text(Lang.Get("claims:gui-plot-permissions"));
                ImGui.SameLine();
                if (ImGui.ImageButton("plotsgrpuppermissions", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PERMISSIONS;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-permissions"));
                }
            }

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLOT))
            {
                if (ImGui.ImageButton("plotsgrpupaddplot", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-add-plot"));
                }
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("plotsgrpupremoveplot", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-remove-plot"));
                }
            }
        }
    }
}
