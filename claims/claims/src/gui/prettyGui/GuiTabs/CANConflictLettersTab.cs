using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANConflictLettersTab : CANGuiTab
    {
        List<ClientConflictLetterCellElement> toRemove = new();
        public CANConflictLettersTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        private bool IsMyPartyGuid(string guid)
        {
            var info = claims.clientDataStorage.clientPlayerInfo;
            if (info.AllianceInfo?.Guid?.Equals(guid) ?? false)
                return true;
            if (info.CityInfo?.Guid?.Equals(guid) ?? false)
                return true;
            return false;
        }

        private string TermText(ClientConflictLetterCellElement letter)
        {
            switch (letter.TermType)
            {
                case PeaceTermType.Reparations:
                    return Lang.Get("claims:gui_peace_term_reparations_value", letter.TermAmount);
                case PeaceTermType.Vassalage:
                    return Lang.Get("claims:gui_peace_term_vassalage");
                case PeaceTermType.Cession:
                    if (!letter.HasCededPlot) return Lang.Get("claims:gui_peace_term_cession");
                    // Show the map coordinates the player actually sees, not the raw world ones
                    var local = PosFunctions.TranslateCoordsToLocalVec3d(capi,
                        new BlockPos(letter.CededPlotBlockX, 0, letter.CededPlotBlockZ));
                    return Lang.Get("claims:gui_peace_term_cession_at", (int)local.X, (int)local.Z);
                default:
                    return Lang.Get("claims:gui_peace_term_none");
            }
        }

        /// <summary>Headline of a letter: what accepting it would actually mean.</summary>
        private static string PurposeText(ClientConflictLetterCellElement letter)
        {
            switch (letter.Purpose)
            {
                case LetterPurpose.NON_AGGRESSION:
                    return letter.NapDays > 0
                        ? Lang.Get("claims:gui_letter_purpose_nap_days", letter.NapDays)
                        : Lang.Get("claims:gui_letter_purpose_nap");
                case LetterPurpose.ULTIMATUM:
                    return Lang.Get("claims:gui_letter_purpose_ultimatum");
                case LetterPurpose.CESSION_CONFIRM:
                    return Lang.Get("claims:gui_letter_purpose_cession_confirm");
                case LetterPurpose.END_CONFLICT:
                    return Lang.Get("claims:gui_letter_purpose_peace");
                default:
                    return Lang.Get("claims:gui_letter_purpose_war");
            }
        }

        // Button green is too dark to read as text.
        private static readonly Vector4 ColPeaceful = new Vector4(0.45f, 0.85f, 0.50f, 1f);

        /// <summary>Red for what starts or threatens a war, calm for what ends or prevents one.</summary>
        private static Vector4 PurposeColor(LetterPurpose purpose)
        {
            switch (purpose)
            {
                case LetterPurpose.NON_AGGRESSION:
                case LetterPurpose.END_CONFLICT:
                    return ColPeaceful;
                case LetterPurpose.ULTIMATUM:
                case LetterPurpose.CESSION_CONFIRM:
                    return ColWarning;
                default:
                    return ColDanger;
            }
        }

        private bool HasAlliance => claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null;
        private string CmdPrefix => HasAlliance ? "/a conflict " : "/city war ";
        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            ImGui.Text(Lang.Get("claims:conflict_letters_list"));


            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null) return;
            ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
            int i = 0;

            foreach (var letter in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements)
            {
                ImGui.PushID(i++);
                ImGui.BeginGroup();

                // What the letter actually is. Without this line a non-aggression offer, an ultimatum
                // and a war declaration look identical - the purpose only showed up in button tooltips.
                ImGui.PushStyleColor(ImGuiCol.Text, PurposeColor(letter.Purpose));
                ImGui.TextWrapped(PurposeText(letter));
                ImGui.PopStyleColor();

                string fromTypeLabel = WarTargetTypeHelper.LangLabel(letter.FromType);
                string toTypeLabel = WarTargetTypeHelper.LangLabel(letter.ToType);
                ImGui.Text(Lang.Get("claims:gui-conflict-letter-from", letter.From, fromTypeLabel));
                ImGui.Text(Lang.Get("claims:gui-conflict-letter-to", letter.To, toTypeLabel));

                
                string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(letter.TimeStampExpire, true);
                ImGui.Text(Lang.Get("claims:gui-conflict-exp-date", expDate));

                // Time left matters most on an ultimatum, where silence counts as a refusal.
                long secondsLeft = letter.TimeStampExpire - TimeFunctions.getEpochSeconds();
                if (secondsLeft > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
                    ImGui.Text(Lang.Get("claims:gui-conflict-time-left", StringFunctions.FormatDuration(secondsLeft)));
                    ImGui.PopStyleColor();
                }

                // Demands of a peace offer / ultimatum - accepting blindly used to be the only option
                if (letter.TermType != PeaceTermType.None)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColWarning);
                    ImGui.TextWrapped(Lang.Get("claims:gui-conflict-letter-terms", TermText(letter)));
                    ImGui.PopStyleColor();
                }

                ImGui.Spacing();

                bool isAttacker = IsMyPartyGuid(letter.FromGuid);
                bool isRecipient = IsMyPartyGuid(letter.ToGuid);

                // NAP offers, ultimatums and cession-confirms are answered only by the recipient — no revoke.
                bool recipientOnlyPurpose = letter.Purpose == LetterPurpose.NON_AGGRESSION || letter.Purpose == LetterPurpose.ULTIMATUM || letter.Purpose == LetterPurpose.CESSION_CONFIRM;
                bool showCancel = !recipientOnlyPurpose || isRecipient;
                if (showCancel)
                {
                    string cancelTooltip;
                    if (letter.Purpose == LetterPurpose.START_CONFLICT)
                        cancelTooltip = Lang.Get(isAttacker ? "claims:gui-conflict-cancel-button" : "claims:gui-conflict-deny-button");
                    else if (letter.Purpose == LetterPurpose.NON_AGGRESSION)
                        cancelTooltip = Lang.Get("claims:gui-conflict-nap-deny-button");
                    else if (letter.Purpose == LetterPurpose.ULTIMATUM)
                        cancelTooltip = Lang.Get("claims:gui-conflict-ultimatum-deny-button");
                    else if (letter.Purpose == LetterPurpose.CESSION_CONFIRM)
                        cancelTooltip = Lang.Get("claims:gui-conflict-cession-deny-button");
                    else
                        cancelTooltip = Lang.Get("claims:gui-conflict-denystop-button");

                    if (IconButton("cancel", "peace-dove", 32, cancelTooltip))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        string cmd = CmdPrefix;
                        string macro;
                        if (letter.Purpose == LetterPurpose.START_CONFLICT)
                            macro = cmd + (isAttacker ? "revoke " + letter.To : "deny " + letter.From);
                        else if (letter.Purpose == LetterPurpose.NON_AGGRESSION)
                            macro = cmd + "nap deny " + letter.From;
                        else if (letter.Purpose == LetterPurpose.ULTIMATUM)
                            macro = cmd + "ultimatum deny " + letter.From;
                        else if (letter.Purpose == LetterPurpose.CESSION_CONFIRM)
                            macro = "/city war cession deny " + letter.From; // city-mayor action even for an allianced member
                        else
                            macro = cmd + "denystop " + (isAttacker ? letter.To : letter.From);
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, macro, EnumChatType.Macro, "");
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                        if (cell != null) toRemove.Add(cell);
                    }
                }

                if (isRecipient)
                {
                    if (showCancel) ImGui.SameLine();
                    string acceptTooltip;
                    if (letter.Purpose == LetterPurpose.START_CONFLICT)
                        acceptTooltip = Lang.Get("claims:gui-conflict-accept-button");
                    else if (letter.Purpose == LetterPurpose.NON_AGGRESSION)
                        acceptTooltip = Lang.Get("claims:gui-conflict-nap-accept-button");
                    else if (letter.Purpose == LetterPurpose.ULTIMATUM)
                        acceptTooltip = Lang.Get("claims:gui-conflict-ultimatum-accept-button");
                    else if (letter.Purpose == LetterPurpose.CESSION_CONFIRM)
                        acceptTooltip = Lang.Get("claims:gui-conflict-cession-accept-button");
                    else
                        acceptTooltip = Lang.Get("claims:gui-conflict-stop-button");
                    string acceptIcon = (letter.Purpose == LetterPurpose.NON_AGGRESSION || letter.Purpose == LetterPurpose.ULTIMATUM || letter.Purpose == LetterPurpose.CESSION_CONFIRM) ? "peace-dove" : "sword-brandish";
                    if (IconButton("acceptconflict", acceptIcon, 32, acceptTooltip))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        string cmd = CmdPrefix;
                        string macro;
                        if (letter.Purpose == LetterPurpose.START_CONFLICT)
                            macro = cmd + "accept " + letter.From;
                        else if (letter.Purpose == LetterPurpose.NON_AGGRESSION)
                            macro = cmd + "nap accept " + letter.From;
                        else if (letter.Purpose == LetterPurpose.ULTIMATUM)
                            macro = cmd + "ultimatum accept " + letter.From;
                        else if (letter.Purpose == LetterPurpose.CESSION_CONFIRM)
                            macro = "/city war cession accept " + letter.From; // city-mayor action even for an allianced member
                        else
                            macro = cmd + "acceptstop " + letter.From;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, macro, EnumChatType.Macro, "");
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                        if (cell != null) toRemove.Add(cell);
                    }
                }

                ImGui.EndGroup();
                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
            }
            ImGui.EndChild();
            if (toRemove.Count > 0)
            {
                foreach (var it in toRemove)
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.Remove(it);
                toRemove.Clear();
            }
            if (IconButton("newconflict", "sword-brandish", 32, Lang.Get("claims:gui-send-new-conflict-letter")))
            {
                if (claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null)
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME;
                }
                else
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_SEND_NEW_CONFLICT_LETTER_NEED_NAME;
                }
            }
            // Non-aggression pacts are a peacetime tool for any party (alliance leader or independent city mayor).
            if (claims.config.WAR_NAP_ENABLED)
            {
                ImGui.SameLine();
                if (IconButton("newnap", "peace-dove", 32, Lang.Get("claims:gui-send-new-nap-letter")))
                    GuiSys.secondaryWindowTab = claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null
                        ? EnumSecondaryWindowTab.ALLIANCE_SEND_NAP_OFFER_NEED_NAME
                        : EnumSecondaryWindowTab.CITY_SEND_NAP_OFFER_NEED_NAME;
            }
            // An ultimatum is also a peacetime move: comply, or hand the sender a free war.
            if (claims.config.WAR_ULTIMATUM_ENABLED)
            {
                ImGui.SameLine();
                if (IconButton("newultimatum", "price-tag", 32, Lang.Get("claims:gui-send-new-ultimatum")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.SEND_ULTIMATUM;
            }
            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/

            AlignBottom();


            if (IconButton("allianceinfo", "vertical-banner", 60, Lang.Get("claims:gui-back")))
                GuiSys.selectedTab = GuiSys.conflictSourceTab;
            ImGui.SameLine();
            if (IconButton("conflictletters", "envelope", 60, Lang.Get("claims:gui-conflict-letters")))
                GuiSys.selectedTab = EnumSelectedTab.ConflictLettersPage;
            ImGui.SameLine();
            if (IconButton("conflictspage", "frog-mouth-helm", 60, Lang.Get("claims:gui-conflicts-page")))
                GuiSys.selectedTab = EnumSelectedTab.ConflictsPage;
        }
    }
}
