using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    /// <summary>
    /// Peace offer with terms. Replaces the old yes/no dialog, which always sent the alliance
    /// command without any term - so cities without an alliance could not offer peace from the GUI
    /// at all, and the peace terms were command-only.
    /// </summary>
    public class CANPeaceOfferTab : CANGuiSecondaryTab
    {
        // 0 = none, 1 = reparations, 2 = vassalage, 3 = cession
        private int selectedTerm = 0;
        private int reparationsAmount = 100;
        private readonly string titleString;

        public CANPeaceOfferTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            this.titleString = titleString;
        }

        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, ImGuiWindowFlags.NoScrollWithMouse);

            ImGui.Text(Lang.Get(titleString, GuiSys.textInput2, GuiSys.textInput2));
            ImGui.Spacing();

            ImGui.Text(Lang.Get("claims:gui_peace_terms_title"));
            ImGui.RadioButton(Lang.Get("claims:gui_peace_term_none"), ref selectedTerm, 0);
            ImGui.RadioButton(Lang.Get("claims:gui_peace_term_reparations"), ref selectedTerm, 1);
            ImGui.RadioButton(Lang.Get("claims:gui_peace_term_vassalage"), ref selectedTerm, 2);
            ImGui.RadioButton(Lang.Get("claims:gui_peace_term_cession"), ref selectedTerm, 3);

            if (selectedTerm == 1)
            {
                ImGui.InputInt(Lang.Get("claims:gui_peace_reparations_amount"), ref reparationsAmount);
                if (reparationsAmount < 0) reparationsAmount = 0;
            }
            if (selectedTerm == 3)
            {
                ImGui.TextWrapped(Lang.Get("claims:gui_peace_cession_hint"));
            }

            ImGui.Spacing();

            if (ImGui.Button(Lang.Get("claims:gui-confirm-button")))
            {
                SendOffer();
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-decline-button")))
            {
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }

            ImGui.End();
        }

        private void SendOffer()
        {
            string term = selectedTerm switch
            {
                1 => "reparations",
                2 => "vassalage",
                3 => "cession",
                _ => "none"
            };

            // Alliance members go through the alliance command, lone cities through the city one -
            // the server rejects the wrong variant.
            bool hasAlliance = claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null;
            string command = hasAlliance
                ? $"/alliance conflict offerstop {GuiSys.textInput2} {term}"
                : $"/city war offerpeace {GuiSys.textInput2} {term}";

            if (selectedTerm == 1) command += " " + reparationsAmount;

            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, command, EnumChatType.Macro, "");
        }
    }
}
