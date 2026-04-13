using claims.src.auxialiry;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANAllianceListTab : CANGuiTab
    {
        public CANAllianceListTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            string text = Lang.Get("claims:gui_alliance_list_title");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(text);

            ImGui.BeginChild("AlliancesScroll", new Vector2(0, 300), true);
            int i = 0;
            foreach (var allianceCell in claims.clientDataStorage.clientPlayerInfo.AllAlliancesList)
            {
                ImGui.PushID(i);

                ImGui.BeginGroup();

                ImGui.Text(Lang.Get("claims:gui_alliancelist_name", allianceCell.Name));

                ImGui.Text(Lang.Get("claims:gui-leader-name", allianceCell.LeaderName));

                ImGui.Text(Lang.Get("claims:gui_alliancelist_cities_count", allianceCell.CitiesCount));
                if (ImGui.IsItemHovered() && allianceCell.CitiesNames.Count > 0)
                {
                    ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(allianceCell.CitiesNames, ','));
                }

                if (allianceCell.Neutral)
                {
                    ImGui.Text(Lang.Get("claims:neutral"));
                }

                ImGui.Text(Lang.Get("claims:gui-date-created", TimeFunctions.getDateFromEpochSeconds(allianceCell.TimeStampCreated)));

                ImGui.EndGroup();

                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
                i++;
            }
            ImGui.EndChild();
        }
    }
}
