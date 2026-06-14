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
                string cancelTooltip = letter.Purpose == LetterPurpose.START_CONFLICT
                    ? Lang.Get(isAttacker ? "claims:gui-conflict-cancel-button" : "claims:gui-conflict-deny-button")
                    : Lang.Get("claims:gui-conflict-denystop-button");

                if (IconButton("cancel", "peace-dove", 32, cancelTooltip))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    string cmd = CmdPrefix;
                    if (letter.Purpose == LetterPurpose.START_CONFLICT)
                    {
                        if (isAttacker)
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "revoke " + letter.To, EnumChatType.Macro, "");
                        else
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "deny " + letter.From, EnumChatType.Macro, "");
                    }
                    else
                    {
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "denystop " + (isAttacker ? letter.To : letter.From), EnumChatType.Macro, "");
                    }
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                    if (cell != null) toRemove.Add(cell);
                }

                if (IsMyPartyGuid(letter.ToGuid))
                {
                    ImGui.SameLine();
                    string acceptTooltip = Lang.Get(letter.Purpose == LetterPurpose.START_CONFLICT
                        ? "claims:gui-conflict-accept-button"
                        : "claims:gui-conflict-stop-button");
                    if (IconButton("acceptconflict", "sword-brandish", 32, acceptTooltip))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        string cmd = CmdPrefix;
                        if (letter.Purpose == LetterPurpose.START_CONFLICT)
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "accept " + letter.From, EnumChatType.Macro, "");
                        else
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "acceptstop " + letter.From, EnumChatType.Macro, "");
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
