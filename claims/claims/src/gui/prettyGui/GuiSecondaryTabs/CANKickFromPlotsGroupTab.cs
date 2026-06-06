using System.Linq;
using System.Numerics;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANKickFromPlotsGroupTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANKickFromPlotsGroupTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
            ImGui.Text(Lang.Get(TitleString, cell.Name));

            ImGui.Combo("Name", ref GuiSys.selectedComboFirst, cell.PlayersNames.ToArray(), cell.PlayersNames.Count);

            if (ImGui.Button(Lang.Get(ButtonString)))
            {
                if (cell.PlayersNames.Count > GuiSys.selectedComboFirst)
                {
                    string playerName = cell.PlayersNames[GuiSys.selectedComboFirst];
                    GuiSys.textInput2 = playerName;
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_PLOTSGROUP_MEMBER_CONFIRM;
                }
            }
            ImGui.End();
            }
        
    }
}
