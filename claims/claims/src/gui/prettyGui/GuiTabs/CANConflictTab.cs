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
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            ImGui.Text(Lang.Get("claims:conflict_list"));

            ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
            int i = 0;
            foreach (var conflict in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements)
            {
                ImGui.PushID(i);

                Vector2 start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;

                ImGui.BeginGroup();

                string firstType = conflict.FirstPartyType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                string secondType = conflict.SecondPartyType == WarTargetType.Alliance
                    ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
                ImGui.Text($"Sides: {conflict.FirstPartyName} ({firstType}) x {conflict.SecondPartyName} ({secondType})");
                ImGui.Text($"Started: {Lang.Get("claims:gui_conflict_cell_started_line", TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(conflict.TimeStampCreated, true))}");

                if (conflict.ActiveWarTime)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                    ImGui.Text(Lang.Get("claims:gui_battle_active"));
                    ImGui.PopStyleColor();
                }

                if (ImGui.Button("Peace offer"))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_PEACE_OFFER_CONFIRM;
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = conflict.Guid;
                    string targetAlliance = conflict.FirstPartyName.Equals(claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Name) ? conflict.SecondPartyName : conflict.FirstPartyName;
                    capi.ModLoader.GetModSystem<claimsGui>().textInput2 = targetAlliance;
                }

                ImGui.SameLine();

                if (ImGui.Button("Info"))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = conflict.Guid;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictInfoPage;
                }


                ImGui.EndGroup();

                Vector2 end = ImGui.GetItemRectMax();
                var draw = ImGui.GetWindowDrawList();

                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
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
