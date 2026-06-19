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
    public class CANCityListTab : CANGuiTab
    {
        private enum SortMode { Default, Oldest, Population, Plots }

        private SortMode sortMode = SortMode.Default;

        private static readonly Vector4 GoldColor = new Vector4(1.00f, 0.84f, 0.00f, 1.0f);
        private static readonly Vector4 SilverColor = new Vector4(0.80f, 0.80f, 0.82f, 1.0f);
        private static readonly Vector4 BronzeColor = new Vector4(0.85f, 0.55f, 0.25f, 1.0f);

        public CANCityListTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        private static readonly Vector4 OpenColor = new Vector4(0.3f, 0.8f, 0.4f, 1.0f);

        public override void DrawTab()
        {
            // --- Header ---
            CenteredTitle(Lang.Get("claims:gui_city_list_title"), ColSection);

            ImGui.Separator();

            // --- Sort mode buttons ---
            Label(Lang.Get("claims:gui-citylist-sort-label"));
            ImGui.SameLine();
            DrawSortButton(SortMode.Default, "claims:gui-citylist-sort-default");
            ImGui.SameLine();
            DrawSortButton(SortMode.Oldest, "claims:gui-citylist-sort-oldest");
            ImGui.SameLine();
            DrawSortButton(SortMode.Population, "claims:gui-citylist-sort-population");
            ImGui.SameLine();
            DrawSortButton(SortMode.Plots, "claims:gui-citylist-sort-plots");

            ImGui.Spacing();

            IEnumerable<ClientCityInfoCellElement> sorted = GetSortedCities();

            ImGui.BeginChild("CitiesScroll", new Vector2(0, 0), false);
            int i = 0;
            foreach (var city in sorted)
            {
                ImGui.PushID(i);

                Vector4 rowNameColor = ColValue;
                if (sortMode != SortMode.Default)
                {
                    if (i == 0) rowNameColor = GoldColor;
                    else if (i == 1) rowNameColor = SilverColor;
                    else if (i == 2) rowNameColor = BronzeColor;
                }

                // --- City name (large) ---
                ImGui.PushStyleColor(ImGuiCol.Text, rowNameColor);
                ImGui.SetWindowFontScale(1.15f);
                ImGui.Text(city.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // --- Info message button (same line as name) ---
                if (city.InvMsg.Length > 0)
                {
                    ImGui.SameLine();
                    IconButton("cityinfo", "info", 14, city.InvMsg);
                }

                // --- Join button (same line as name, for open cities) ---
                if (city.Open)
                {
                    ImGui.SameLine();
                    if (GreenIconButton("joincity", "stairs-goal", 14, Lang.Get("claims:gui_citylist_join_city_hover")))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c join " + city.Name, EnumChatType.Macro, "");
                    }
                }

                // --- Details table ---
                if (ImGui.BeginTable("CityDetails" + i, 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    // Mayor
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-city-tab-mayor"));
                    ImGui.TableNextColumn();
                    ImGui.Text(city.MayorName ?? "");

                    // Alliance
                    if (city.AllianceName.Length > 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        Label(Lang.Get("claims:gui-citylist-alliance-label"));
                        ImGui.TableNextColumn();
                        ImGui.Text(city.AllianceName);
                    }

                    // Population
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-citylist-population-label"));
                    ImGui.TableNextColumn();
                    ImGui.Text(city.CitizensAmount.ToString());

                    // Plots
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-citylist-plots-label"));
                    ImGui.TableNextColumn();
                    ImGui.Text(city.ClaimedPlotsAmount.ToString());

                    // Created
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(city.TimeStampCreated));

                    // Open status
                    if (city.Open)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, OpenColor);
                        ImGui.Text(Lang.Get("claims:gui-citylist-open"));
                        ImGui.PopStyleColor();
                    }

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
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            }
            if (ImGui.Button(Lang.Get(langKey)))
            {
                sortMode = mode;
            }
            if (active)
            {
                ImGui.PopStyleColor();
            }
        }

        private IEnumerable<ClientCityInfoCellElement> GetSortedCities()
        {
            var cities = claims.clientDataStorage.clientPlayerInfo.AllCitiesList;
            return sortMode switch
            {
                SortMode.Oldest => cities.OrderBy(c => c.TimeStampCreated),
                SortMode.Population => cities.OrderByDescending(c => c.CitizensAmount),
                SortMode.Plots => cities.OrderByDescending(c => c.ClaimedPlotsAmount),
                _ => cities,
            };
        }
    }
}
