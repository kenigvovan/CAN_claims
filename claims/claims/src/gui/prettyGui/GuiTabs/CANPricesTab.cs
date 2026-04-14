using ImGuiNET;
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
            ImGui.Text(Lang.Get("claims:gui-currency-item"));
            if (caneconomy.caneconomy.config != null)
            {
                foreach (var it in caneconomy.caneconomy.config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE)
                {
                    var coinData = it.Value;
                    //currencyStr = coinData.CollectibleCode;
                    ImGui.Text(coinData.CoinValue.ToString() + ":");
                    ImGui.SameLine();
                    ItemStack coin = new ItemStack(capi.World.GetItem(new AssetLocation(coinData.CollectibleCode)), 1);
                    if (coinData.CoinAttributes != null)
                    {
                        var attributeTree = new TreeAttribute();
                        foreach (var attr in coinData.CoinAttributes)
                        {
                            coin.Attributes[attr.Key] = attr.Value;
                        }
                    }
                    itemIconAtlas.Draw(coin, new Vector2(48, 48));
                }
            }
            ImGui.Text(Lang.Get("claims:gui-new-city-cost", claims.config.NEW_CITY_COST.ToString()));

            ImGui.Text(Lang.Get("claims:gui-city-plot-cost", claims.config.PLOT_CLAIM_PRICE.ToString()));

            ImGui.Text(Lang.Get("claims:gui-city-name-change-cost", claims.config.CITY_NAME_CHANGE_COST.ToString()));

            ImGui.Text(Lang.Get("claims:gui-city-base-cost", claims.config.CITY_BASE_CARE.ToString()));

            ImGui.Text(Lang.Get("claims:gui-teleportation-cost", claims.config.SUMMON_PAYMENT.ToString()));

            ImGui.Text(Lang.Get("claims:gui-new-alliance-cost", claims.config.NEW_ALLIANCE_COST.ToString()));
        }
    }
}
