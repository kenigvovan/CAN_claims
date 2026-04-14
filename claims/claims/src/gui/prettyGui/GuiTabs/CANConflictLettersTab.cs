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


            ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
            int i = 0;
            
            foreach (var letter in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements)
            {
                ImGui.PushID(i);

                Vector2 start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;

                ImGui.BeginGroup();

                string fromTypeLabel = letter.FromType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                string toTypeLabel = letter.ToType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                ImGui.Text(Lang.Get("claims:gui-conflict-letter-from", letter.From, fromTypeLabel));
                ImGui.Text(Lang.Get("claims:gui-conflict-letter-to", letter.To, toTypeLabel));

                
                string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(letter.TimeStampExpire, true);
                ImGui.Text(Lang.Get("claims:gui-conflict-exp-date", expDate));



                ImGui.SameLine();
                float buttonSize = 32f;

                float avail = ImGui.GetContentRegionAvail().X;

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - buttonSize) * 0.2f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 20);
                
                if (ImGui.ImageButton("cancel", this.iconHandler.GetOrLoadIcon("peace-dove"), new Vector2(32)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    bool isAttacker = IsMyPartyGuid(letter.FromGuid);
                    string cmd = CmdPrefix;
                    if (letter.Purpose == LetterPurpose.START_CONFLICT)
                    {
                        if (isAttacker)
                        {
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "revoke " + letter.To, EnumChatType.Macro, "");
                        }
                        else
                        {
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "deny " + letter.From, EnumChatType.Macro, "");
                        }
                    }
                    else
                    {
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "denystop " + (isAttacker ? letter.To : letter.From), EnumChatType.Macro, "");
                    }
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                    if (cell != null)
                    {
                        toRemove.Add(cell);
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    if (letter.Purpose == LetterPurpose.START_CONFLICT)
                    {
                        bool isAttacker = IsMyPartyGuid(letter.FromGuid);
                        ImGui.SetTooltip(Lang.Get(isAttacker ? "claims:gui-conflict-cancel-button" : "claims:gui-conflict-deny-button"));
                    }
                    else
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-conflict-denystop-button"));
                    }
                }
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 20);
                if (IsMyPartyGuid(letter.ToGuid))
                {
                    if (ImGui.ImageButton("acceptconflict", this.iconHandler.GetOrLoadIcon("sword-brandish"), new Vector2(32)))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        string cmd = CmdPrefix;
                        if (letter.Purpose == LetterPurpose.START_CONFLICT)
                        {
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "accept " + letter.From, EnumChatType.Macro, "");
                        }
                        else
                        {
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd + "acceptstop " + letter.From, EnumChatType.Macro, "");
                        }
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get(letter.Purpose == LetterPurpose.START_CONFLICT
                            ? "claims:gui-conflict-accept-button"
                            : "claims:gui-conflict-stop-button"));
                    }
                }

                ImGui.EndGroup();

                Vector2 end = ImGui.GetItemRectMax();
                var draw = ImGui.GetWindowDrawList();

                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
            }
            ImGui.EndChild();
            if(toRemove.Count() > 0)
            {
                foreach(var it in toRemove)
                claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements.Remove(it);
            }
            if (ImGui.ImageButton("newconflict", this.iconHandler.GetOrLoadIcon("sword-brandish"), new Vector2(32)))
            {
                if (claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null)
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME;
                }
                else
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_SEND_NEW_CONFLICT_LETTER_NEED_NAME;
                }
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-send-new-conflict-letter"));
            }
            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/

            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);


            if (ImGui.ImageButton("allianceinfo", this.iconHandler.GetOrLoadIcon("vertical-banner"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = capi.ModLoader.GetModSystem<claimsGui>().conflictSourceTab;
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("conflictletters", this.iconHandler.GetOrLoadIcon("envelope"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictLettersPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-conflict-letters"));
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("conflictspage", this.iconHandler.GetOrLoadIcon("frog-mouth-helm"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictsPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-conflicts-page"));
            }         
        }
    }
}
