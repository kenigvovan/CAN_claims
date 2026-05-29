using Cairo;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace claims.src.gui.playerGui.GuiPages
{
    public static class PricesPage
    {
        public static void BuildPricesPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            currentBounds = currentBounds.BelowCopy(0, 40);
            var pricesTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds.Alignment = EnumDialogArea.LeftTop;
            currentBounds.fixedY += 15;
            string currencyStr = Lang.Get("claims:gui-currency-item");
            TextExtents textExtents;
            if (claims.config.COINS_VALUES_TO_CODE != null && claims.config.COINS_VALUES_TO_CODE.Count > 0)
            {
                var tempBound = currentBounds;
                textExtents = pricesTabFont.GetTextExtents(currencyStr);
                currentBounds.fixedWidth = textExtents.Width + 10;
                compo.AddStaticText(currencyStr,
                       pricesTabFont,
                       tempBound, "currency-item");
                foreach (var it in claims.config.COINS_VALUES_TO_CODE)
                {
                    string collectibleCode = it.Value;

                    tempBound = tempBound.RightCopy();
                    tempBound.fixedWidth = 48;

                    ItemStack coin = new ItemStack(compo.Api.World.GetItem(new AssetLocation(collectibleCode)), 1);
                    ItemstackTextComponent currencyStack = new ItemstackTextComponent(compo.Api, coin, 48);
                    compo.AddRichtext(new RichTextComponentBase[] { currencyStack }, tempBound, "coin-item" + collectibleCode);
                }
                currentBounds = currentBounds.BelowCopy(0, 0);
            }
            
           
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            string cityCostString = Lang.Get("claims:gui-new-city-cost", claims.config.NEW_CITY_COST.ToString());
            textExtents = pricesTabFont.GetTextExtents(cityCostString);
            currentBounds.fixedWidth = textExtents.Width + 10;
            compo.AddStaticText(Lang.Get("claims:gui-new-city-cost", claims.config.NEW_CITY_COST.ToString()),
                    pricesTabFont,
                    currentBounds, "new-city-price");

            currentBounds = currentBounds.BelowCopy(0, 0);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            string cityPlotCost = Lang.Get("claims:gui-city-plot-cost", claims.config.PLOT_CLAIM_PRICE.ToString());
            currentBounds.fixedWidth = pricesTabFont.GetTextExtents(cityPlotCost).Width + 10;
            compo.AddStaticText(cityPlotCost,
                    pricesTabFont,
                    currentBounds, "plot-claim-price");

            currentBounds = currentBounds.BelowCopy(0, 0);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            string cityNameChangeCost = Lang.Get("claims:gui-city-name-change-cost", claims.config.CITY_NAME_CHANGE_COST.ToString());
            currentBounds.fixedWidth = pricesTabFont.GetTextExtents(cityNameChangeCost).Width + 10;
            compo.AddStaticText(cityNameChangeCost,
                    pricesTabFont,
                    currentBounds, "city-name-price");

            currentBounds = currentBounds.BelowCopy(0, 0);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            compo.AddStaticText(Lang.Get("claims:gui-city-base-cost", claims.config.CITY_BASE_CARE.ToString()),
                    pricesTabFont,
                    currentBounds, "city-base-care");

            currentBounds = currentBounds.BelowCopy(0, 0);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            compo.AddStaticText(Lang.Get("claims:gui-teleportation-cost", claims.config.SUMMON_PAYMENT.ToString()),
                    pricesTabFont,
                    currentBounds, "city-summon-price");

            currentBounds = currentBounds.BelowCopy(0, 0);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



            compo.AddStaticText(Lang.Get("claims:gui-new-alliance-cost", claims.config.NEW_ALLIANCE_COST.ToString()),
                    pricesTabFont,
                    currentBounds, "city-alliance-price");
        }
    }
}
