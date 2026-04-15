using claims.src.auxialiry;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANCityListTab : CANGuiTab
    {
        public CANCityListTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 openColor = new Vector4(0.3f, 0.8f, 0.4f, 1.0f);

            // --- Header ---
            string text = Lang.Get("claims:gui_city_list_title");
            float windowWidth = ImGui.GetWindowSize().X;
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.3f);
            float textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.BeginChild("CitiesScroll", new Vector2(0, 0), false);
            int i = 0;
            foreach (var city in claims.clientDataStorage.clientPlayerInfo.AllCitiesList)
            {
                ImGui.PushID(i);

                // --- City name (large, gold) ---
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.15f);
                ImGui.Text(city.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // --- Info message button (same line as name) ---
                if (city.InvMsg.Length > 0)
                {
                    ImGui.SameLine();
                    if (ImGui.ImageButton("cityinfo", this.iconHandler.GetOrLoadIcon("info"), new Vector2(14)))
                    {
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(city.InvMsg);
                        ImGui.EndTooltip();
                    }
                }

                // --- Join button (same line as name, for open cities) ---
                if (city.Open)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                    if (ImGui.ImageButton("joincity", this.iconHandler.GetOrLoadIcon("stairs-goal"), new Vector2(14)))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c join " + city.Name, EnumChatType.Macro, "");
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(Lang.Get("claims:gui_citylist_join_city_hover"));
                        ImGui.EndTooltip();
                    }
                }

                // --- Details table ---
                if (ImGui.BeginTable("CityDetails" + i, 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    // Mayor
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-mayor"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(city.MayorName ?? "");

                    // Alliance
                    if (city.AllianceName.Length > 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(Lang.Get("claims:gui-citylist-alliance-label"));
                        ImGui.PopStyleColor();
                        ImGui.TableNextColumn();
                        ImGui.Text(city.AllianceName);
                    }

                    // Population
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-citylist-population-label"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(city.CitizensAmount.ToString());

                    // Plots
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-citylist-plots-label"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(city.ClaimedPlotsAmount.ToString());

                    // Created
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(city.TimeStampCreated));

                    // Open status
                    if (city.Open)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, openColor);
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
    }
}
