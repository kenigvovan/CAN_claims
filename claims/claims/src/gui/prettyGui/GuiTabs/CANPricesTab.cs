using claims.src.economy;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPricesTab: CANGuiTab
    {
        ItemIconAtlas itemIconAtlas;
        public CANPricesTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            itemIconAtlas = new(capi);
        }
        public override void DrawTab()
        {
            // --- Currency section header ---
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.SetWindowFontScale(1.2f);
            ImGui.Text(Lang.Get("claims:gui-currency-item"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            if (claims.config.COIN_DENOMINATIONS != null && claims.config.COIN_DENOMINATIONS.Count > 0)
            {
                if (ImGui.BeginTable("CurrencyTable", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthStretch);

                    foreach (var coinInfo in claims.config.COIN_DENOMINATIONS)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12);
                        ImGui.Text(coinInfo.Value.ToString());
                        ImGui.PopStyleColor();

                        ImGui.TableNextColumn();
                        ItemStack coin = new ItemStack(capi.World.GetItem(new AssetLocation(coinInfo.CollectibleCode)), 1);
                        var attributes = coinInfo.ToTreeAttribute();
                        if (attributes != null) coin.Attributes = attributes;
                        itemIconAtlas.Draw(coin, new Vector2(48, 48));
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Costs section header ---
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.SetWindowFontScale(1.2f);
            ImGui.Text(Lang.Get("claims:gui-prices-costs-header"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTable("CostsTable", 2, TableFlags))
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                DrawCostRow(Lang.Get("claims:gui-new-city-cost-label"), claims.config.NEW_CITY_COST);
                DrawCostRow(Lang.Get("claims:gui-city-plot-cost-label"), claims.config.PLOT_CLAIM_PRICE);
                DrawCostRow(Lang.Get("claims:gui-city-name-change-cost-label"), claims.config.CITY_NAME_CHANGE_COST);
                DrawCostRow(Lang.Get("claims:gui-city-base-cost-label"), claims.config.CITY_BASE_CARE);
                DrawCostRow(Lang.Get("claims:gui-teleportation-cost-label"), claims.config.SUMMON_PAYMENT);
                DrawCostRow(Lang.Get("claims:gui-new-alliance-cost-label"), claims.config.NEW_ALLIANCE_COST);

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Plot type costs (collapsible) ---
            if (PlotInfo.dictPlotTypes != null && ImGui.CollapsingHeader(Lang.Get("claims:gui-prices-plot-types-header")))
            {
                ImGui.Spacing();
                if (ImGui.BeginTable("PlotCostsTable", 4, TableFlags))
                {
                    ImGui.TableSetupColumn("Type1", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Cost1", ImGuiTableColumnFlags.WidthFixed, 50);
                    ImGui.TableSetupColumn("Type2", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Cost2", ImGuiTableColumnFlags.WidthFixed, 50);

                    var plotTypes = new List<KeyValuePair<PlotType, PlotInfo>>(PlotInfo.dictPlotTypes);
                    for (int i = 0; i < plotTypes.Count; i += 2)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
                        ImGui.Text(plotTypes[i].Value.getFullName());
                        ImGui.PopStyleColor();
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(Lang.Get($"claims:gui-plot-type-desc-{plotTypes[i].Value.getFullName()}"));
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                        ImGui.Text(plotTypes[i].Value.getCost().ToString());
                        ImGui.PopStyleColor();

                        if (i + 1 < plotTypes.Count)
                        {
                            ImGui.TableNextColumn();
                            ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
                            ImGui.Text(plotTypes[i + 1].Value.getFullName());
                            ImGui.PopStyleColor();
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(Lang.Get($"claims:gui-plot-type-desc-{plotTypes[i + 1].Value.getFullName()}"));
                            ImGui.TableNextColumn();
                            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                            ImGui.Text(plotTypes[i + 1].Value.getCost().ToString());
                            ImGui.PopStyleColor();
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();

                // Extra plot costs
                if (ImGui.BeginTable("ExtraPlotCosts", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-outpost-cost"), claims.config.OUTPOST_PLOT_COST);
                    DrawCostRow(Lang.Get("claims:gui-prices-extra-plot-cost"), claims.config.EXTRA_PLOT_COST);
                    DrawCostRow(Lang.Get("claims:gui-prices-no-pvp-flag-cost"), claims.config.PLOT_NO_PVP_FLAG_COST);

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Ransom costs (collapsible) ---
            if (ImGui.CollapsingHeader(Lang.Get("claims:gui-prices-ransom-header")))
            {
                ImGui.Spacing();
                if (ImGui.BeginTable("RansomTable", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-no-citizen"), claims.config.RANSOM_FOR_NO_CITIZEN);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-citizen"), claims.config.RANSOM_FOR_CITIZEN);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-mayor"), claims.config.RANSOM_FOR_MAYOR);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-leader"), claims.config.RANSOM_FOR_LEADER);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-chief"), claims.config.RANSOM_FOR_CHIEF);

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Alliance economy (collapsible) ---
            if (ImGui.CollapsingHeader(Lang.Get("claims:gui-prices-alliance-header")))
            {
                ImGui.Spacing();
                if (ImGui.BeginTable("AllianceCostsTable", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-alliance-rename"), claims.config.ALLIANCE_RENAME_COST);
                    DrawCostRow(Lang.Get("claims:gui-prices-alliance-base-care"), claims.config.ALLIANCE_BASE_CARE);
                    DrawCostRow(Lang.Get("claims:gui-prices-alliance-max-fee"), claims.config.ALLIANCE_MAX_FEE);
                    DrawCostRow(Lang.Get("claims:gui-prices-neutral-alliance"), claims.config.NEUTRAL_ALLANCE_PAYMENT);

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- City limits (collapsible) ---
            if (ImGui.CollapsingHeader(Lang.Get("claims:gui-prices-city-limits-header")))
            {
                ImGui.Spacing();
                if (ImGui.BeginTable("CityLimitsTable", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-max-city-fee"), claims.config.MAX_CITY_FEE);
                    DrawCostRow(Lang.Get("claims:gui-prices-city-max-debt"), claims.config.CITY_MAX_DEBT);

                    ImGui.EndTable();
                }
            }
        }

        private static void DrawCostRow(string label, double value)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            Label(label);
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
            ImGui.Text(value.ToString());
            ImGui.PopStyleColor();
        }
    }
}
