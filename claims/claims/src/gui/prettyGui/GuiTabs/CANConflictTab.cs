using System.Linq;
using System.Numerics;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.network.packets;
using claims.src.part.structure.conflict;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANConflictTab : CANGuiTab
    {
        public CANConflictTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text(Lang.Get("claims:conflict_list"));
            ImGui.SetWindowFontScale(1.0f);

            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null) return;
            ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
            int i = 0;
            foreach (var conflict in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements)
            {
                ImGui.PushID(i++);
                ImGui.BeginGroup();

                string firstType = WarTargetTypeHelper.LangLabel(conflict.FirstPartyType);
                string secondType = WarTargetTypeHelper.LangLabel(conflict.SecondPartyType);

                // --- Party names ---
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(conflict.FirstPartyName);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
                ImGui.Text($" ({firstType})");
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.Text("  vs  ");
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(conflict.SecondPartyName);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
                ImGui.Text($" ({secondType})");
                ImGui.PopStyleColor();

                // --- Date ---
                Label(Lang.Get("claims:gui_conflict_cell_started_line",
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(conflict.TimeStampCreated, true)));

                // --- Battle active ---
                if (conflict.ActiveWarTime)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.Text(Lang.Get("claims:gui_battle_active"));
                    ImGui.PopStyleColor();
                }

                ImGui.Spacing();

                // --- Buttons ---
                if (GreenButton(Lang.Get("claims:gui_conflict_peace_offer_btn")))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_PEACE_OFFER_CONFIRM;
                    GuiSys.textInput = conflict.Guid;
                    string ourName = claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Name
                        ?? claims.clientDataStorage.clientPlayerInfo.CityInfo?.Name ?? "";
                    string targetAlliance = conflict.FirstPartyName.Equals(ourName) ? conflict.SecondPartyName : conflict.FirstPartyName;
                    GuiSys.textInput2 = targetAlliance;
                }

                ImGui.SameLine();

                if (BlueButton(Lang.Get("claims:gui_conflict_info_btn")))
                {
                    GuiSys.textInput = conflict.Guid;
                    GuiSys.selectedTab = EnumSelectedTab.ConflictInfoPage;
                }

                ImGui.EndGroup();
                ImGui.PopID();

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
            ImGui.EndChild();

            DrawCasusBelli();
            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            AlignBottom();


            if (IconButton("allianceinfo", "vertical-banner", 60, Lang.Get("claims:gui-back")))
                GuiSys.selectedTab = GuiSys.conflictSourceTab;
            ImGui.SameLine();
            if (IconButton("conflictletters", "envelope", 60, Lang.Get("claims:gui-conflict-letters")))
                GuiSys.selectedTab = EnumSelectedTab.ConflictLettersPage;
        }

        // Ally-at-war reasons change without any event addressed to us, so re-ask periodically
        // while the tab is on screen instead of relying on pushes alone.
        private long nextRefreshMs;

        private void DrawCasusBelli()
        {
            var cityInfo = claims.clientDataStorage.clientPlayerInfo.CityInfo;
            if (cityInfo == null) return;

            long nowMs = capi.World.ElapsedMilliseconds;
            if (nowMs >= nextRefreshMs)
            {
                nextRefreshMs = nowMs + 5000;
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.CLIENT_REQUEST_CASUS_BELLI });
            }

            SectionTitle(Lang.Get("claims:gui_casus_belli_list"));
            HelpMarker(Lang.Get("claims:gui_casus_belli_hint"));

            long now = TimeFunctions.getEpochSeconds();
            // Drop rows that have nothing left to say: lapsed reason, no cooldown, no pact.
            var live = cityInfo.ClientCasusBelliCellElements.Where(cb =>
                cb.Kind == CasusBelliKind.AllyAtWar || cb.ExpiresAt > now
                || cb.CooldownUntil > now || cb.PactUntil > now || cb.UnionBreakUntil > now).ToList();

            ImGui.BeginChild("CasusBelliScroll", new Vector2(0, 170), true);
            if (live.Count == 0)
            {
                Hint(Lang.Get("claims:cb_none"));
            }
            int row = 0;
            foreach (var cb in live)
            {
                ImGui.PushID(1000 + row++);
                string typeLabel = WarTargetTypeHelper.LangLabel(cb.TargetType);

                // A free war is the strongest reason: it also waives cooldown and declaration cost.
                ImGui.PushStyleColor(ImGuiCol.Text, cb.Kind == CasusBelliKind.FreeWar ? ColWarning : ColValue);
                ImGui.Text(StringFunctions.replaceUnderscore(cb.TargetName));
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
                ImGui.Text($" ({typeLabel})");
                ImGui.PopStyleColor();

                if (cb.Kind != CasusBelliKind.None)
                {
                    string reason = Lang.Get(cb.Kind switch
                    {
                        CasusBelliKind.FreeWar => "claims:cb_reason_freewar",
                        CasusBelliKind.AllyAtWar => "claims:cb_reason_ally",
                        _ => "claims:cb_reason_grievance"
                    });
                    Label(cb.ExpiresAt > now
                        ? Lang.Get("claims:gui_casus_belli_line_timed", reason, StringFunctions.FormatDuration(cb.ExpiresAt - now))
                        : reason);
                }

                // Blockers. A free war ignores both, so say so instead of scaring the player off.
                bool freeWar = cb.Kind == CasusBelliKind.FreeWar;
                if (cb.CooldownUntil > now)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, freeWar ? ColHint : ColDanger);
                    ImGui.TextWrapped(Lang.Get(freeWar ? "claims:gui_war_cooldown_waived" : "claims:gui_war_cooldown_left",
                        StringFunctions.FormatDuration(cb.CooldownUntil - now)));
                    ImGui.PopStyleColor();
                }
                if (cb.UnionBreakUntil > now)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                    ImGui.TextWrapped(Lang.Get("claims:gui_war_union_break_left",
                        StringFunctions.FormatDuration(cb.UnionBreakUntil - now)));
                    ImGui.PopStyleColor();
                }
                if (cb.PactUntil > now)
                {
                    bool canBreak = cb.Kind != CasusBelliKind.None;
                    ImGui.PushStyleColor(ImGuiCol.Text, canBreak ? ColHint : ColDanger);
                    ImGui.TextWrapped(Lang.Get(canBreak ? "claims:gui_war_pact_breakable" : "claims:gui_war_pact_left",
                        StringFunctions.FormatDuration(cb.PactUntil - now)));
                    ImGui.PopStyleColor();
                }

                double cost = freeWar ? 0 : claims.config.WAR_DECLARATION_COST;
                if (cost > 0) Label(Lang.Get("claims:gui_war_declaration_cost", cost));

                if (cb.CanDeclareNow(now))
                {
                    if (RedButton(Lang.Get("claims:gui_war_declare_btn")))
                    {
                        string command = (HasAlliance ? "/a conflict declare " : "/city war declare ")
                            + (cb.TargetType == WarTargetType.Alliance ? "alliance:" : "city:") + cb.TargetName;
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, command, EnumChatType.Macro, "");
                    }
                }

                ImGui.PopID();
                ImGui.Separator();
            }
            ImGui.EndChild();
        }

        private bool HasAlliance => claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null;
    }
}
