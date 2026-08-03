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
        // Citizen "chip" buttons keep a cool blue-grey tint distinct from the parchment theme.
        static readonly Vector4 citizenColor = new Vector4(0.8f, 0.8f, 0.9f, 1.0f);

        public override void DrawTab()
        {
            // --- Header ---
            CenteredTitle(Lang.Get("claims:gui-ranks-title"), ColSection);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-ranks-description"));

            ImGui.SameLine();

            var perms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;

            // --- Create rank button ---
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_CREATE_CITY_RANK))
            {
                if (GreenIconButton("createrank", "circle", 16, Lang.Get("claims:gui-create-rank-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_RANK_CREATION_NEED_NAME;
            }

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.BeginChild("Ranksscroll", new Vector2(0, 0), false);
            int i = 100;
            foreach (var rankCell in claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks)
            {
                ImGui.PushID(i);

                // --- Rank name (gold, larger) ---
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.SetWindowFontScale(1.15f);
                ImGui.Text(rankCell.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // --- Action buttons (same line) ---
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    ImGui.SameLine();
                    if (GreenIconButton("promotewithrank", "private", 14, Lang.Get("claims:gui-add-rank-tooltip")))
                    {
                        GuiSys.textInput = rankCell.Name;
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_RANK_ADD;
                    }
                }

                ImGui.SameLine();
                if (IconButton("openrankinfo", "info", 14, Lang.Get("claims:gui-info-rank-tooltip")))
                {
                    GuiSys.selectedTab = EnumSelectedTab.RANKINFOPAGE;
                    GuiSys.textInput = rankCell.Name;
                }

                // --- Members count ---
                Label(Lang.Get("claims:gui-rank-members-label", rankCell.Citizens.Count));

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
