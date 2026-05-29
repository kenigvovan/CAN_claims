using claims.src.part.structure;
using claims.src.part.structure.plots;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPricesTab: CANGuiTab
    {
        ItemIconAtlas itemIconAtlas;
        ImGuiSlotRenderer slotRenderer;
        ImGuiInventoryGrid inventoryGrid;
        bool inventoryGridInitialized;
        public CANPricesTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            itemIconAtlas = new(capi);
            slotRenderer = new ImGuiSlotRenderer(capi, 48);
        }
        public override void DrawTab()
        {
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 valueColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);

            // --- Currency section header ---
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.2f);
            ImGui.Text(Lang.Get("claims:gui-currency-item"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            if (claims.config.COINS_VALUES_TO_CODE != null && claims.config.COINS_VALUES_TO_CODE.Count > 0)
            {
                if (ImGui.BeginTable("CurrencyTable", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthStretch);

                    foreach (var it in claims.config.COINS_VALUES_TO_CODE)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, valueColor);
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12);
                        ImGui.Text(it.Key.ToString());
                        ImGui.PopStyleColor();

                        ImGui.TableNextColumn();
                        ItemStack coin = new ItemStack(capi.World.GetItem(new AssetLocation(it.Value)), 1);
                        itemIconAtlas.Draw(coin, new Vector2(48, 48));
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Costs section header ---
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.2f);
            ImGui.Text(Lang.Get("claims:gui-prices-costs-header"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTable("CostsTable", 2, ImGuiTableFlags.None))
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                DrawCostRow(Lang.Get("claims:gui-new-city-cost-label"), claims.config.NEW_CITY_COST, labelColor, valueColor);
                DrawCostRow(Lang.Get("claims:gui-city-plot-cost-label"), claims.config.PLOT_CLAIM_PRICE, labelColor, valueColor);
                DrawCostRow(Lang.Get("claims:gui-city-name-change-cost-label"), claims.config.CITY_NAME_CHANGE_COST, labelColor, valueColor);
                DrawCostRow(Lang.Get("claims:gui-city-base-cost-label"), claims.config.CITY_BASE_CARE, labelColor, valueColor);
                DrawCostRow(Lang.Get("claims:gui-teleportation-cost-label"), claims.config.SUMMON_PAYMENT, labelColor, valueColor);
                DrawCostRow(Lang.Get("claims:gui-new-alliance-cost-label"), claims.config.NEW_ALLIANCE_COST, labelColor, valueColor);

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Plot type costs (collapsible) ---
            if (PlotInfo.dictPlotTypes != null && ImGui.CollapsingHeader(Lang.Get("claims:gui-prices-plot-types-header")))
            {
                ImGui.Spacing();
                if (ImGui.BeginTable("PlotCostsTable", 4, ImGuiTableFlags.None))
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
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(plotTypes[i].Value.getFullName());
                        ImGui.PopStyleColor();
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, valueColor);
                        ImGui.Text(plotTypes[i].Value.getCost().ToString());
                        ImGui.PopStyleColor();

                        if (i + 1 < plotTypes.Count)
                        {
                            ImGui.TableNextColumn();
                            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                            ImGui.Text(plotTypes[i + 1].Value.getFullName());
                            ImGui.PopStyleColor();
                            ImGui.TableNextColumn();
                            ImGui.PushStyleColor(ImGuiCol.Text, valueColor);
                            ImGui.Text(plotTypes[i + 1].Value.getCost().ToString());
                            ImGui.PopStyleColor();
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();

                // Extra plot costs
                if (ImGui.BeginTable("ExtraPlotCosts", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-outpost-cost"), claims.config.OUTPOST_PLOT_COST, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-extra-plot-cost"), claims.config.EXTRA_PLOT_COST, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-no-pvp-flag-cost"), claims.config.PLOT_NO_PVP_FLAG_COST, labelColor, valueColor);

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
                if (ImGui.BeginTable("RansomTable", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-no-citizen"), claims.config.RANSOM_FOR_NO_CITIZEN, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-citizen"), claims.config.RANSOM_FOR_CITIZEN, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-mayor"), claims.config.RANSOM_FOR_MAYOR, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-leader"), claims.config.RANSOM_FOR_LEADER, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-ransom-chief"), claims.config.RANSOM_FOR_CHIEF, labelColor, valueColor);

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
                if (ImGui.BeginTable("AllianceCostsTable", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-alliance-rename"), claims.config.ALLIANCE_RENAME_COST, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-alliance-base-care"), claims.config.ALLIANCE_BASE_CARE, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-alliance-max-fee"), claims.config.ALLIANCE_MAX_FEE, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-neutral-alliance"), claims.config.NEUTRAL_ALLANCE_PAYMENT, labelColor, valueColor);

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
                if (ImGui.BeginTable("CityLimitsTable", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    DrawCostRow(Lang.Get("claims:gui-prices-max-city-fee"), claims.config.MAX_CITY_FEE, labelColor, valueColor);
                    DrawCostRow(Lang.Get("claims:gui-prices-city-max-debt"), claims.config.CITY_MAX_DEBT, labelColor, valueColor);

                    ImGui.EndTable();
                }
            }
        }

        private void DrawCostRow(string label, double value, Vector4 labelColor, Vector4 valueColor)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(label);
            ImGui.PopStyleColor();
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Text, valueColor);
            ImGui.Text(value.ToString());
            ImGui.PopStyleColor();
        }
    }
}
