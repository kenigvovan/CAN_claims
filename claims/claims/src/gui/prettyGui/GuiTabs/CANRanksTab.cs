using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANRanksTab : CANGuiTab
    {
        public CANRanksTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 citizenColor = new Vector4(0.8f, 0.8f, 0.9f, 1.0f);

            // --- Header ---
            string text = Lang.Get("claims:gui-ranks-title");
            float windowWidth = ImGui.GetWindowSize().X;
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.3f);
            float textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.SameLine();

            var perms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;

            // --- Create rank button ---
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_CREATE_CITY_RANK))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.ImageButton("createrank", this.iconHandler.GetOrLoadIcon("circle"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_RANK_CREATION_NEED_NAME;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-create-rank-tooltip"));
                }
            }

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.BeginChild("Ranksscroll", new Vector2(0, 0), false);
            int i = 100;
            foreach (var rankCell in claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks)
            {
                ImGui.PushID(i);

                // --- Rank name (gold, larger) ---
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.15f);
                ImGui.Text(rankCell.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // --- Action buttons (same line) ---
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                    if (ImGui.ImageButton("promotewithrank" + i.ToString(), this.iconHandler.GetOrLoadIcon("private"), new Vector2(14)))
                    {
                        GuiSys.textInput = rankCell.Name;
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_RANK_ADD;
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-add-rank-tooltip"));
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("openrankinfo" + i.ToString(), this.iconHandler.GetOrLoadIcon("info"), new Vector2(14)))
                {
                    GuiSys.selectedTab = EnumSelectedTab.RANKINFOPAGE;
                    GuiSys.textInput = rankCell.Name;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-info-rank-tooltip"));
                }

                // --- Members count ---
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-rank-members-label", rankCell.Citizens.Count));
                ImGui.PopStyleColor();

                // --- Citizens as styled buttons ---
                if (rankCell.Citizens.Count > 0)
                {
                    float btnSpacing = 4f;
                    bool first = true;
                    foreach (var plName in rankCell.Citizens)
                    {
                        if (!first)
                        {
                            ImGui.SameLine(0, btnSpacing);
                        }
                        first = false;

                        if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.25f, 0.25f, 0.35f, 1.0f));
                            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                            ImGui.PushStyleColor(ImGuiCol.Text, citizenColor);
                            if (ImGui.Button(plName))
                            {
                                GuiSys.textInput = rankCell.Name;
                                GuiSys.textInput2 = plName;
                                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_RANK_REMOVE_CONFIRM;
                            }
                            ImGui.PopStyleColor(4);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(Lang.Get("claims:gui-rank-remove-citizen-tooltip", plName));
                            }
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, citizenColor);
                            ImGui.Text(plName);
                            ImGui.PopStyleColor();
                        }
                    }
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
