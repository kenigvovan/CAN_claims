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
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 neutralColor = new Vector4(0.6f, 0.85f, 1.0f, 1.0f);

            // --- Header ---
            string text = Lang.Get("claims:gui_alliance_list_title");
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

            ImGui.BeginChild("AlliancesScroll", new Vector2(0, 0), false);
            int i = 0;
            foreach (var alliance in claims.clientDataStorage.clientPlayerInfo.AllAlliancesList)
            {
                ImGui.PushID(i);

                // --- Alliance name (large, gold) ---
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.15f);
                ImGui.Text(alliance.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                if (alliance.Neutral)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, neutralColor);
                    ImGui.Text(Lang.Get("claims:neutral"));
                    ImGui.PopStyleColor();
                }

                // --- Details table ---
                if (ImGui.BeginTable("AllianceDetails" + i, 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    // Leader
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-alliancelist-leader-label"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(alliance.LeaderName ?? "");

                    // Cities
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-alliancelist-cities-label"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(alliance.CitiesCount.ToString());
                    if (ImGui.IsItemHovered() && alliance.CitiesNames.Count > 0)
                    {
                        ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(alliance.CitiesNames, ','));
                    }

                    // Created
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.PopStyleColor();
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
    }
}
