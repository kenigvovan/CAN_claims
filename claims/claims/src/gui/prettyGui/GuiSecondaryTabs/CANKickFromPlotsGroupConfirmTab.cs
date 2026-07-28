using System.Linq;
using System.Numerics;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANKickFromPlotsGroupConfirmTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANKickFromPlotsGroupConfirmTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            this.TitleString = titleString;
            this.CommandCallOnClick = commandCallOnClick;
            this.ButtonString = buttonString;
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
               new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
           );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            PlotsGroupCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(GuiSys.textInput), null);
            if (cell == null) { ImGui.End(); return; }
            ImGui.Text(Lang.Get(TitleString, GuiSys.textInput2, cell.Name));

            if (ImGui.Button(Lang.Get(ButtonString, GuiSys.textInput, GuiSys.textInput2)))
            {
                string memberToKick = GuiSys.textInput2;
                SendCommand(string.Format("/c plotsgroup kick {0} {1}", cell.Name, memberToKick));
                // Optimistic local update
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER))
                {
                    cell.PlayersNames.Remove(memberToKick);
                }
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                GuiSys.textInput2 = "";
            }
            ImGui.End();
            }
        
    }
}
