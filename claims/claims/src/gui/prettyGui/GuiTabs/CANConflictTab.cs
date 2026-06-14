using System.Numerics;
using claims.src.auxialiry;
using claims.src.part.structure.conflict;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

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

                string firstType = conflict.FirstPartyType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                string secondType = conflict.SecondPartyType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");

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
    }
}
