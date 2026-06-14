using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANAllianceListTab : CANGuiTab
    {
        private enum SortMode { Default, Oldest, Cities, Name }
        private SortMode sortMode = SortMode.Default;

        private static readonly Vector4 NeutralColor = new Vector4(0.6f, 0.85f, 1.0f, 1.0f);
        private static readonly Vector4 GoldColor    = new Vector4(1.00f, 0.84f, 0.00f, 1.0f);
        private static readonly Vector4 SilverColor  = new Vector4(0.80f, 0.80f, 0.82f, 1.0f);
        private static readonly Vector4 BronzeColor  = new Vector4(0.85f, 0.55f, 0.25f, 1.0f);

        public CANAllianceListTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            // --- Header ---
            CenteredTitle(Lang.Get("claims:gui_alliance_list_title"), ColSection);

            ImGui.Separator();

            // --- Sort buttons ---
            Label(Lang.Get("claims:gui-alliancelist-sort-label"));
            ImGui.SameLine();
            DrawSortButton(SortMode.Default, "claims:gui-alliancelist-sort-default");
            ImGui.SameLine();
            DrawSortButton(SortMode.Oldest, "claims:gui-alliancelist-sort-oldest");
            ImGui.SameLine();
            DrawSortButton(SortMode.Cities, "claims:gui-alliancelist-sort-cities");
            ImGui.SameLine();
            DrawSortButton(SortMode.Name, "claims:gui-alliancelist-sort-name");

            ImGui.Spacing();

            IEnumerable<ClientAllianceInfoCellElement> sorted = GetSortedAlliances();

            ImGui.BeginChild("AlliancesScroll", new Vector2(0, 0), false);
            int i = 0;
            foreach (var alliance in sorted)
            {
                ImGui.PushID(i);

                // --- Alliance name (large, top-3 colored when sorted) ---
                Vector4 nameColor = ColValue;
                if (sortMode != SortMode.Default)
                {
                    if (i == 0) nameColor = GoldColor;
                    else if (i == 1) nameColor = SilverColor;
                    else if (i == 2) nameColor = BronzeColor;
                }
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.15f);
                ImGui.Text(alliance.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                if (alliance.Neutral)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, NeutralColor);
                    ImGui.Text(Lang.Get("claims:neutral"));
                    ImGui.PopStyleColor();
                }

                // --- Details table ---
                if (ImGui.BeginTable("AllianceDetails" + i, 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    // Leader
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-alliancelist-leader-label"));
                    ImGui.TableNextColumn();
                    ImGui.Text(alliance.LeaderName ?? "");

                    // Cities
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-alliancelist-cities-label"));
                    ImGui.TableNextColumn();
                    ImGui.Text(alliance.CitiesCount.ToString());
                    if (ImGui.IsItemHovered() && alliance.CitiesNames.Count > 0)
                    {
                        ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(alliance.CitiesNames, ','));
                    }

                    // Created
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(alliance.TimeStampCreated));

                    ImGui.EndTable();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.PopID();
                i++;
            }
            ImGui.EndChild();
        }

        private void DrawSortButton(SortMode mode, string langKey)
        {
            bool active = sortMode == mode;
            if (active)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            if (ImGui.Button(Lang.Get(langKey)))
                sortMode = mode;
            if (active)
                ImGui.PopStyleColor();
        }

        private IEnumerable<ClientAllianceInfoCellElement> GetSortedAlliances()
        {
            var list = claims.clientDataStorage.clientPlayerInfo.AllAlliancesList;
            return sortMode switch
            {
                SortMode.Oldest => list.OrderBy(a => a.TimeStampCreated),
                SortMode.Cities => list.OrderByDescending(a => a.CitiesCount),
                SortMode.Name   => list.OrderBy(a => a.Name),
                _               => list,
            };
        }
    }
}
