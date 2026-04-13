using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANAllianceTab : CANGuiTab
    {
        List<ClientToAllianceInvitationCellElement> toRemove = new();
        public CANAllianceTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            if (claims.clientDataStorage.clientPlayerInfo?.AllianceInfo != null)
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;

                string text = clientInfo.AllianceInfo.Name;

                float windowWidth = ImGui.GetWindowSize().X;
                float textWidth = ImGui.CalcTextSize(text).X;

                ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                if (ImGui.Button(text))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.SELECT_NEW_ALLIANCE_NAME;
                }

                ImGui.Text(Lang.Get("claims:gui-leader-name", clientInfo.AllianceInfo.LeaderName));

                ImGui.Text(Lang.Get("claims:gui-date-created", TimeFunctions.getDateFromEpochSeconds(clientInfo.AllianceInfo.TimeStampCreated)));

                ImGui.Text(Lang.Get("claims:gui-alliance-cities-list", clientInfo.AllianceInfo.Cities.Count));

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.AllianceInfo.Cities, ','));
                }

                if (ImGui.ImageButton("invitecity", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.INVITE_TO_ALLIANCE_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-invite-city"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("kickcity", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_ALLIANCE_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-kick-city"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("uninvitecity", this.iconHandler.GetOrLoadIcon("anticlockwise-rotation"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_ALLIANCE_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-uninvite-city"));
                }

                ImGui.Text(Lang.Get("claims:gui-alliance-balance", clientInfo.AllianceInfo.Balance));

                ImGui.Text(Lang.Get("claims:gui-alliance-prefix", clientInfo.AllianceInfo.Prefix));
                ImGui.SameLine();
                if (ImGui.ImageButton("allianceprefix", this.iconHandler.GetOrLoadIcon("soldering-iron"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_PREFIX_NEED_NAME;
                }
               
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-set-alliance-prefix"));
                }

                ImGui.Text(Lang.Get("claims:gui-allies-list", string.Join(", ", clientInfo.AllianceInfo.Allies)));

                /*==============================================================================================*/
                /*=====================================UNDER 2 LINE=============================================*/
                /*==============================================================================================*/

                float availY = ImGui.GetContentRegionAvail().Y;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);


                if (ImGui.ImageButton("leavealliance", this.iconHandler.GetOrLoadIcon("exit-door"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.LEAVE_ALLIANCE_CONFIRM;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-leavealliance"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("conflictletters", this.iconHandler.GetOrLoadIcon("envelope"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().conflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictLettersPage;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-conflict-letters"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("conflictspage", this.iconHandler.GetOrLoadIcon("frog-mouth-helm"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().conflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictsPage;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-conflicts-page"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("unionspage", this.iconHandler.GetOrLoadIcon("tower-flag"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.UnionLettersPage;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-unions-letters-page"));
                }
            }
            else
            {

                if (ImGui.ImageButton("createalliance", this.iconHandler.GetOrLoadIcon("queen-crown"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NEW_ALLIANCE_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-new-alliance-button"));
                }

                var clientInfo = claims.clientDataStorage.clientPlayerInfo;


                ImGui.Text(Lang.Get("claims:gui-to-alliance-invites"));

                ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                int i = 0;
                foreach (var invite in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations)
                {
                    ImGui.PushID(i);

                    Vector2 start = ImGui.GetCursorScreenPos();
                    float width = ImGui.GetContentRegionAvail().X;

                    ImGui.BeginGroup();

                    ImGui.Text($"City: {claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations[i].AllianceName}");
                    ImGui.Text($"Time: {TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations[i].TimeoutStamp, true).ToString()}");

                    if (ImGui.Button("Accept"))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c inviteaccept " + invite.AllianceName, EnumChatType.Macro, "");
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.FirstOrDefault(c => c.AllianceName == invite.AllianceName);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }

                    /*ImGui.SameLine();

                    if (ImGui.Button("Decline"))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/deny " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[i].CityName, EnumChatType.Macro, "");
                    }*/

                    ImGui.EndGroup();

                    Vector2 end = ImGui.GetItemRectMax();
                    var draw = ImGui.GetWindowDrawList();

                    ImGui.PopID();

                    ImGui.Dummy(new Vector2(0, 8));
                    ImGui.Separator();
                }
                ImGui.EndChild();
                if (toRemove.Count() > 0)
                {
                    foreach (var it in toRemove)
                        claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.Remove(it);
                }
                
            }          
        }
    }
}
