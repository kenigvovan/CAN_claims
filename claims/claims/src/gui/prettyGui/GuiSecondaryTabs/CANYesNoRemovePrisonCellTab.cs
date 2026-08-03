using System.Linq;
using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANYesNoRemovePrisonCellTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandToCallOnYes;
        private string YesButtonString;
        private string NoButtonString;
        public CANYesNoRemovePrisonCellTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandToCallOnYes, string yesButtonString = "claims:gui-yes-string", string noButtonString = "claims:gui-no-string")
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            TitleString = titleString;
            CommandToCallOnYes = commandToCallOnYes;
            YesButtonString = yesButtonString;
            NoButtonString = noButtonString;
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            
            ImGui.Text(Lang.Get(TitleString, GuiSys.textInput, GuiSys.textInput2));
            //ImGui.InputText("", ref GuiSys.textInput, 256);

            if(ImGui.Button(Lang.Get(YesButtonString)))
            {
                SendCommand(this.CommandToCallOnYes + " " +
                    GuiSys.selectedPos.X + " " + GuiSys.selectedPos.Y + " " + GuiSys.selectedPos.Z);
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PrisonCells.FirstOrDefault(c => c.SpawnPosition == GuiSys.selectedPos);
                if (cell != null)
                {
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.PrisonCells.Remove(cell);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get(NoButtonString)))
            {
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            ImGui.End();
        }
    }
}
