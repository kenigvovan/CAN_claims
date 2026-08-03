using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANDeclareConflictTab : CANGuiSecondaryTab
    {
        private int selectedTargetType = 0; // 0 = city, 1 = alliance
        private string baseCommand;
        public CANDeclareConflictTab(ICoreClientAPI capi, IconHandler iconHandler, string baseCommand)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            this.baseCommand = baseCommand;
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            ImGui.Text(Lang.Get("claims:name_of_target_to_send_conflict_letter"));

            ImGui.RadioButton(Lang.Get("claims:conflict_target_city"), ref selectedTargetType, 0);
            ImGui.SameLine();
            ImGui.RadioButton(Lang.Get("claims:conflict_target_alliance"), ref selectedTargetType, 1);

            ImGui.InputText("", ref GuiSys.textInput, 256);

            if (ImGui.Button(Lang.Get("claims:gui-confirm-button")))
            {
                string prefix = selectedTargetType == 0 ? "city:" : "alliance:";
                string command = baseCommand + prefix + GuiSys.textInput;
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, command, EnumChatType.Macro, "");
                GuiSys.textInput = "";
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            ImGui.End();
        }
    }
}
