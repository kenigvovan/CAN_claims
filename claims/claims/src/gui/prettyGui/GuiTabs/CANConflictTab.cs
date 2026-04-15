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
            Vector4 partyColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);

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
                ImGui.PushStyleColor(ImGuiCol.Text, partyColor);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(conflict.FirstPartyName);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text($" ({firstType})");
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.Text("  vs  ");
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, partyColor);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(conflict.SecondPartyName);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();
                ImGui.SameLine(0, 0);
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text($" ({secondType})");
                ImGui.PopStyleColor();

                // --- Date ---
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui_conflict_cell_started_line",
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(conflict.TimeStampCreated, true)));
                ImGui.PopStyleColor();

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
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.Button(Lang.Get("claims:gui_conflict_peace_offer_btn")))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_PEACE_OFFER_CONFIRM;
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = conflict.Guid;
                    string ourName = claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Name
                        ?? claims.clientDataStorage.clientPlayerInfo.CityInfo?.Name ?? "";
                    string targetAlliance = conflict.FirstPartyName.Equals(ourName) ? conflict.SecondPartyName : conflict.FirstPartyName;
                    capi.ModLoader.GetModSystem<claimsGui>().textInput2 = targetAlliance;
                }
                ImGui.PopStyleColor(3);

                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.45f, 0.7f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.55f, 0.8f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.35f, 0.6f, 1.0f));
                if (ImGui.Button(Lang.Get("claims:gui_conflict_info_btn")))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = conflict.Guid;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictInfoPage;
                }
                ImGui.PopStyleColor(3);

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

        }
    }
}
