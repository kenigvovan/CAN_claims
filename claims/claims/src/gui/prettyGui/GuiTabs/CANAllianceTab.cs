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

                // --- Alliance name (centered, clickable) ---
                string text = clientInfo.AllianceInfo.Name;
                if (CenteredTitleButton(text, ColValue))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.SELECT_NEW_ALLIANCE_NAME;

                ImGui.Separator();
                ImGui.Spacing();

                // --- Info table ---
                if (ImGui.BeginTable("AllianceInfoTable", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 130);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    // Leader
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-alliancelist-leader-label"));
                    ImGui.TableNextColumn();
                    ImGui.Text(clientInfo.AllianceInfo.LeaderName ?? "");

                    // Created
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(clientInfo.AllianceInfo.TimeStampCreated));

                    // Prefix
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-alliance-prefix-label"));
                    ImGui.TableNextColumn();
                    ImGui.Text(clientInfo.AllianceInfo.Prefix ?? "");
                    ImGui.SameLine();
                    if (IconButton("allianceprefix", "soldering-iron", 14, Lang.Get("claims:gui-set-alliance-prefix")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_PREFIX_NEED_NAME;

                    if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
                    {
                        // Balance
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        Label(Lang.Get("claims:gui-alliance-balance-label"));
                        ImGui.TableNextColumn();
                        double alBal = clientInfo.AllianceInfo.Balance;
                        if (alBal < 0) ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                        ImGui.Text(alBal.ToString("F0"));
                        if (alBal < 0) ImGui.PopStyleColor();
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // --- Cities ---
                ImGui.Text(Lang.Get("claims:gui-alliance-cities-list", clientInfo.AllianceInfo.Cities.Count));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.AllianceInfo.Cities, ','));
                }
                ImGui.SameLine();

                if (GreenIconButton("invitecity", "expander", 16, Lang.Get("claims:gui-invite-city")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.INVITE_TO_ALLIANCE_NEED_NAME;
                ImGui.SameLine();
                if (RedIconButton("kickcity", "contract", 16, Lang.Get("claims:gui-kick-city")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_ALLIANCE_NEED_NAME;
                ImGui.SameLine();
                if (IconButton("uninvitecity", "anticlockwise-rotation", 16, Lang.Get("claims:gui-uninvite-city")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.UNINVITE_TO_ALLIANCE;

                // --- Allies ---
                ImGui.Spacing();
                ImGui.Text(Lang.Get("claims:gui-allies-list", string.Join(", ", clientInfo.AllianceInfo.Allies)));

                // Announced union breaks: the union still holds until the timer runs out.
                long nowSeconds = TimeFunctions.getEpochSeconds();
                foreach (var pending in clientInfo.AllianceInfo.PendingUnionBreaks)
                {
                    if (pending.Value <= nowSeconds) continue;
                    ImGui.PushStyleColor(ImGuiCol.Text, ColWarning);
                    ImGui.TextWrapped(Lang.Get("claims:gui-union-break-pending",
                        StringFunctions.replaceUnderscore(pending.Key),
                        StringFunctions.FormatDuration(pending.Value - nowSeconds)));
                    ImGui.PopStyleColor();
                }

                // --- Bottom navigation ---
                AlignBottom();

                if (IconButton("leavealliance", "exit-door", 60, Lang.Get("claims:gui-leavealliance")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.LEAVE_ALLIANCE_CONFIRM;
                ImGui.SameLine();
                if (IconButton("conflictletters", "envelope", 60, Lang.Get("claims:gui-conflict-letters")))
                {
                    GuiSys.conflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                    GuiSys.selectedTab = EnumSelectedTab.ConflictLettersPage;
                }
                ImGui.SameLine();
                if (IconButton("conflictspage", "frog-mouth-helm", 60, Lang.Get("claims:gui-conflicts-page")))
                {
                    GuiSys.conflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                    GuiSys.selectedTab = EnumSelectedTab.ConflictsPage;
                }
                ImGui.SameLine();
                if (IconButton("unionspage", "tower-flag", 60, Lang.Get("claims:gui-unions-letters-page")))
                    GuiSys.selectedTab = EnumSelectedTab.UnionLettersPage;
            }
            else
            {
                // --- No alliance: create button ---
                if (GreenIconButton("createalliance", "queen-crown", 60, Lang.Get("claims:gui-new-alliance-button")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NEW_ALLIANCE_NEED_NAME;

                // --- Invitations ---
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                    ImGui.SetWindowFontScale(1.15f);
                    ImGui.Text(Lang.Get("claims:gui-to-alliance-invites"));
                    ImGui.SetWindowFontScale(1.0f);
                    ImGui.PopStyleColor();

                    ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                    int i = 0;
                    foreach (var invite in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations)
                    {
                        ImGui.PushID(i);
                        ImGui.BeginGroup();

                        LabelValue(Lang.Get("claims:gui-alliance-invite-alliance-label"), invite.AllianceName ?? "");
                        LabelValue(Lang.Get("claims:gui-city-tab-invite-expires"),
                            TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(invite.TimeoutStamp, true));

                        if (GreenButton(Lang.Get("claims:gui-city-tab-accept")))
                        {
                            SendCommand("/c inviteaccept " + invite.AllianceName);
                            var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.FirstOrDefault(c => c.AllianceName == invite.AllianceName);
                            if (cell != null)
                            {
                                toRemove.Add(cell);
                            }
                        }

                        ImGui.EndGroup();
                        ImGui.PopID();

                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                    ImGui.EndChild();
                    if (toRemove.Count > 0)
                    {
                        foreach (var it in toRemove)
                            claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.Remove(it);
                        toRemove.Clear();
                    }
                }
            }
        }
    }
}
