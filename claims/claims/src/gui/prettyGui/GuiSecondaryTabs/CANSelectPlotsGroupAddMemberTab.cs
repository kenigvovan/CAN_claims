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
    public class CANSelectPlotsGroupAddMemberTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANSelectPlotsGroupAddMemberTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
            
            ImGui.Text(Lang.Get(TitleString));
            
            ImGui.InputText("", ref GuiSys.textInput2, 256);
            PlotsGroupCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(GuiSys.textInput), null);
            if (cell != null)
            {
                if (ImGui.Button(Lang.Get(ButtonString)))
                {
                    string memberToAdd = GuiSys.textInput2;
                    SendCommand(string.Format("/c plotsgroup add {0} {1}", cell.Name, memberToAdd));
                    // Optimistic local update
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLAYER)
                        && !cell.PlayersNames.Contains(memberToAdd))
                    {
                        cell.PlayersNames.Add(memberToAdd);
                    }
                    GuiSys.textInput2 = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;                   
                }

            }
            ImGui.End();
        }
    }
}
