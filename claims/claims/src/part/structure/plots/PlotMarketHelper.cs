using claims.src.part.structure.plots.auction;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// Selling land between cities at a fixed price.
    ///
    /// There is no separate market underneath: a price tag is a lot with no closing time whose
    /// buyout equals the asking price, so meeting it takes the plot at once. Everything here is a
    /// named entry point into <see cref="AuctionHandler"/> - one set of rules, one table, one
    /// browser, whether the seller wanted a price or a contest.
    /// </summary>
    public static class PlotMarketHelper
    {
        /// <summary>
        /// Whether this plot may be put up at a price by its own city. Listing a plot that already
        /// carries a price tag is allowed - that is how the price and the audience are changed, and
        /// the GUI does exactly that when either is edited.
        /// </summary>
        public static bool CanList(Plot plot, City seller, out string errorKey)
        {
            if (TryGetFixedPriceLot(plot, out PlotAuction existing) && existing.SellerCityGuid == seller?.Guid)
            {
                errorKey = null;
                if (!claims.config.CITY_PLOT_TRADE_ENABLED) { errorKey = "claims:plot_trade_disabled"; return false; }
                return PlotSaleRules.CanSellPlot(plot, seller, out errorKey);
            }
            return AuctionRules.CanCreateForPlot(plot, seller, timed: false, out errorKey);
        }

        /// <summary>
        /// Puts the plot up at a price, or re-prices the offer already standing on it. Caller has
        /// already run <see cref="CanList"/>.
        /// </summary>
        public static void List(Plot plot, int price, EnumPlotSaleAudience audience, City targetCity)
        {
            if (TryGetFixedPriceLot(plot, out PlotAuction existing))
            {
                AuctionHandler.RepriceFixedLot(existing, price, audience, targetCity);
                return;
            }
            AuctionHandler.CreateFixedPrice(plot, plot.getCity(), price, audience, targetCity);
        }

        /// <summary>Takes the offer down. Bids, if any were placed, come back to their cities.</summary>
        public static void Unlist(Plot plot)
        {
            if (AuctionRegistry.TryGetRunningFor(plot, out PlotAuction lot))
            {
                AuctionHandler.CancelLot(lot, "claims:plot_auction_cancelled_by_seller");
            }
        }

        /// <summary>
        /// Everything the buyer's side must satisfy, re-checked at purchase time. A price tag is
        /// judged as the bid that would meet it, so a purchase and a bid cannot disagree.
        /// </summary>
        public static bool CanBuy(Plot plot, City buyer, out string errorKey)
        {
            errorKey = null;
            if (!TryGetFixedPriceLot(plot, out PlotAuction lot))
            {
                errorKey = "claims:plot_trade_not_listed";
                return false;
            }
            return AuctionRules.CanBid(lot, buyer, lot.BuyoutPrice, out errorKey);
        }

        /// <summary>
        /// Buys the plot at its asking price. That is a bid meeting the buyout, so the money goes
        /// through escrow like any other - it lands on the seller's account in the same tick.
        /// </summary>
        /// <param name="expectedPrice">
        /// The price the buyer was shown, or -1 when they did not go through a screen that shows one.
        /// The seller can re-price a standing offer at any moment, so a purchase started against an
        /// older price is refused rather than charged the new one.
        /// </param>
        public static bool Buy(Plot plot, City buyer, long expectedPrice, out string errorKey)
        {
            errorKey = null;
            if (!TryGetFixedPriceLot(plot, out PlotAuction lot))
            {
                errorKey = "claims:plot_trade_not_listed";
                return false;
            }
            if (expectedPrice >= 0 && expectedPrice != lot.BuyoutPrice)
            {
                errorKey = "claims:plot_trade_price_changed";
                return false;
            }
            return AuctionHandler.PlaceBid(lot, buyer, lot.BuyoutPrice, out errorKey);
        }

        /// <summary>The plain price-tag offer standing on this plot, if there is one.</summary>
        public static bool TryGetFixedPriceLot(Plot plot, out PlotAuction lot)
        {
            lot = null;
            if (!AuctionRegistry.TryGetRunningFor(plot, out PlotAuction found)) return false;
            if (!found.IsFixedPrice) return false;
            lot = found;
            return true;
        }

    }
}
