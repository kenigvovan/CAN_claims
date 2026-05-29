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
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);

            if (claims.clientDataStorage.clientPlayerInfo?.AllianceInfo != null)
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;

                // --- Alliance name (centered, clickable) ---
                string text = clientInfo.AllianceInfo.Name;
                ImGui.SetWindowFontScale(1.3f);
                float textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetWindowFontScale(1.0f);
                float windowWidth = ImGui.GetWindowSize().X;
                ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.05f));
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.3f);
                if (ImGui.Button(text))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.SELECT_NEW_ALLIANCE_NAME;
                }
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor(4);

                ImGui.Separator();
                ImGui.Spacing();

                // --- Info table ---
                if (ImGui.BeginTable("AllianceInfoTable", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 130);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    // Leader
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-alliancelist-leader-label"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(clientInfo.AllianceInfo.LeaderName ?? "");

                    // Created
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(clientInfo.AllianceInfo.TimeStampCreated));

                    // Prefix
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-alliance-prefix-label"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(clientInfo.AllianceInfo.Prefix ?? "");
                    ImGui.SameLine();
                    if (ImGui.ImageButton("allianceprefix", this.iconHandler.GetOrLoadIcon("soldering-iron"), new Vector2(14)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_PREFIX_NEED_NAME;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-set-alliance-prefix"));
                    }

                    if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
                    {
                        // Balance
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(Lang.Get("claims:gui-alliance-balance-label"));
                        ImGui.PopStyleColor();
                        ImGui.TableNextColumn();
                        ImGui.Text(clientInfo.AllianceInfo.Balance.ToString());
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

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.ImageButton("invitecity", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.INVITE_TO_ALLIANCE_NEED_NAME;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-invite-city"));
                }
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                if (ImGui.ImageButton("kickcity", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_ALLIANCE_NEED_NAME;
                }
                ImGui.PopStyleColor(3);
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

                // --- Allies ---
                ImGui.Spacing();
                ImGui.Text(Lang.Get("claims:gui-allies-list", string.Join(", ", clientInfo.AllianceInfo.Allies)));

                // --- Bottom navigation ---
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
                // --- No alliance: create button ---
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.ImageButton("createalliance", this.iconHandler.GetOrLoadIcon("queen-crown"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NEW_ALLIANCE_NEED_NAME;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-new-alliance-button"));
                }

                // --- Invitations ---
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
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

                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(Lang.Get("claims:gui-alliance-invite-alliance-label"));
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0, 0);
                        ImGui.Text(invite.AllianceName ?? "");

                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(Lang.Get("claims:gui-city-tab-invite-expires"));
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0, 0);
                        ImGui.Text(TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(invite.TimeoutStamp, true));

                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                        if (ImGui.Button(Lang.Get("claims:gui-city-tab-accept")))
                        {
                            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c inviteaccept " + invite.AllianceName, EnumChatType.Macro, "");
                            var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.FirstOrDefault(c => c.AllianceName == invite.AllianceName);
                            if (cell != null)
                            {
                                toRemove.Add(cell);
                            }
                        }
                        ImGui.PopStyleColor(3);

                        ImGui.EndGroup();
                        ImGui.PopID();

                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                    ImGui.EndChild();
                    if (toRemove.Count() > 0)
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
