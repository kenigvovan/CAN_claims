using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

                string fromTypeLabel = letter.FromType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                string toTypeLabel = letter.ToType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                ImGui.Text(Lang.Get("claims:gui-conflict-letter-from", letter.From, fromTypeLabel));
                ImGui.Text(Lang.Get("claims:gui-conflict-letter-to", letter.To, toTypeLabel));

                
                string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(letter.TimeStampExpire, true);
                ImGui.Text(Lang.Get("claims:gui-conflict-exp-date", expDate));

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
