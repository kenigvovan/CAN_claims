using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    /// <summary>
    /// Sends an ultimatum: name the target, pick what is demanded (money or the plot you stand on).
    /// Until now this was command-only, so the GUI could answer an ultimatum but never issue one.
    /// </summary>
    public class CANUltimatumTab : CANGuiSecondaryTab
    {
        private static readonly Vector4 ColHint = new Vector4(0.50f, 0.50f, 0.50f, 1f);

        private int selectedTargetType;   // 0 = city, 1 = alliance
        private int selectedDemand;       // 0 = money, 1 = plot
        private int demandAmount = 100;
        private string targetName = "";

        public CANUltimatumTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, ImGuiWindowFlags.NoScrollWithMouse);

            ImGui.Text(Lang.Get("claims:gui_ultimatum_title"));
            ImGui.Spacing();

            ImGui.RadioButton(Lang.Get("claims:conflict_target_city"), ref selectedTargetType, 0);
            ImGui.SameLine();
            ImGui.RadioButton(Lang.Get("claims:conflict_target_alliance"), ref selectedTargetType, 1);
            ImGui.InputText(Lang.Get("claims:gui_ultimatum_target"), ref targetName, 256);

            ImGui.Spacing();
            ImGui.Text(Lang.Get("claims:gui_ultimatum_demand_title"));
            ImGui.RadioButton(Lang.Get("claims:gui_ultimatum_demand_money"), ref selectedDemand, 0);
            ImGui.RadioButton(Lang.Get("claims:gui_ultimatum_demand_plot"), ref selectedDemand, 1);

            if (selectedDemand == 0)
            {
                ImGui.InputInt(Lang.Get("claims:gui_ultimatum_amount"), ref demandAmount);
                if (demandAmount < 1) demandAmount = 1;
            }
            else
            {
                // The server takes the plot from the sender's own position, so the demand cannot be
                // picked from a list - the player has to be standing on it.
                ImGui.TextWrapped(Lang.Get("claims:gui_ultimatum_plot_hint"));
            }

            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
            ImGui.TextWrapped(Lang.Get("claims:gui_ultimatum_deadline_hint",
                auxialiry.StringFunctions.FormatDuration((long)claims.config.WAR_ULTIMATUM_EXPIRE_HOURS * 3600)));
            ImGui.TextWrapped(Lang.Get("claims:gui_ultimatum_refusal_hint"));
            ImGui.PopStyleColor();
            ImGui.Spacing();

            bool canSend = targetName.Length > 0;
            if (!canSend) ImGui.BeginDisabled();
            if (ImGui.Button(Lang.Get("claims:gui-confirm-button")))
            {
                SendUltimatum();
                targetName = "";
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            if (!canSend) ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-decline-button")))
            {
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }

            ImGui.End();
        }

        private void SendUltimatum()
        {
            string target = (selectedTargetType == 0 ? "city:" : "alliance:") + targetName;
            // Alliance members go through the alliance command, lone cities through the city one -
            // the handler resolves the party from the caller, but the subcommand paths differ.
            bool hasAlliance = claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null;
            string command = hasAlliance
                ? $"/alliance conflict ultimatum offer {target} "
                : $"/city war ultimatum offer {target} ";
            command += selectedDemand == 0 ? "money " + demandAmount : "plot";

            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, command, EnumChatType.Macro, "");
        }
    }
}
