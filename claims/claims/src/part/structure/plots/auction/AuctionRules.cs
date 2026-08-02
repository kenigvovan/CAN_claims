using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.economy;

namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// Who may open a lot, who may bid on it, and who may see it.
    ///
    /// Every answer here is re-asked when the lot closes: a lot outlives the state it was opened in,
    /// so a war may have started, the plot may have gained an owner and the leading city may be gone.
    /// </summary>
    public static class AuctionRules
    {
        /// <summary>
        /// Bidding needs a real economy: with the no-op provider every balance is decimal.MaxValue,
        /// so escrow holds nothing and any city could outbid any other forever.
        /// </summary>
        public static bool IsEnabled =>
            claims.config.CITY_PLOT_TRADE_ENABLED
            && claims.config.CITY_PLOT_AUCTION_ENABLED
            && !(claims.economyProvider is NoopMoneyProvider);

        /// <summary>Whether this lot is addressed to <paramref name="bidder"/> at all.</summary>
        public static bool IsVisibleTo(PlotAuction auction, City bidder)
        {
            if (auction == null || bidder == null || !auction.IsRunning) return false;
            if (!auction.TryGetSeller(out City seller)) return false;
            return PlotSaleRules.MatchesAudience(seller, bidder, auction.Audience, auction.TargetCityGuid);
        }

        /// <summary>
        /// Lots this city sees in the market tab: the ones it may bid on, plus its own - a seller has
        /// to be able to watch what its land is fetching.
        /// </summary>
        public static List<PlotAuction> GetLotsFor(City bidder)
        {
            var result = new List<PlotAuction>();
            if (bidder == null || !claims.config.CITY_PLOT_TRADE_ENABLED) return result;
            foreach (PlotAuction it in AuctionRegistry.GetRunning())
            {
                bool ours = it.SellerCityGuid == bidder.Guid;
                if (!ours && !IsVisibleTo(it, bidder)) continue;
                // An offer whose ground stopped being sellable would refuse every click. The seller
                // keeps seeing it - it is their standing offer to withdraw.
                if (!ours && !IsStillSellable(it)) continue;
                result.Add(it);
            }
            return result;
        }

        /// <summary>Whether the ground behind a lot could still change hands right now.</summary>
        public static bool IsStillSellable(PlotAuction auction)
        {
            if (!auction.TryGetSeller(out City seller)) return false;
            if (auction.Kind != EnumAuctionKind.PLOT) return true;
            return auction.TryGetPlot(out Plot plot) && PlotSaleRules.CanSellPlot(plot, seller, out _);
        }

        /// <summary>
        /// Whether this city may put this plot up. <paramref name="timed"/> tells a lot with bids
        /// apart from a plain price tag: bidding needs the auction switch, a price tag does not.
        /// </summary>
        public static bool CanCreateForPlot(Plot plot, City seller, bool timed, out string errorKey)
        {
            errorKey = null;
            if (!claims.config.CITY_PLOT_TRADE_ENABLED) { errorKey = "claims:plot_trade_disabled"; return false; }
            if (timed && !IsEnabled) { errorKey = "claims:plot_auction_disabled"; return false; }
            if (!PlotSaleRules.CanSellPlot(plot, seller, out errorKey)) return false;
            // One plot, one offer - whichever form it takes.
            if (AuctionRegistry.TryGetRunningFor(plot, out PlotAuction existing))
            {
                errorKey = existing.IsFixedPrice
                    ? "claims:plot_trade_already_listed"
                    : "claims:plot_auction_already_running";
                return false;
            }
            return true;
        }

        public static bool CanBid(PlotAuction auction, City bidder, long amount, out string errorKey)
        {
            errorKey = null;
            if (!claims.config.CITY_PLOT_TRADE_ENABLED) { errorKey = "claims:plot_trade_disabled"; return false; }
            if (auction == null || !auction.IsRunning) { errorKey = "claims:plot_auction_not_running"; return false; }
            // Meeting a price tag is buying, not bidding, so it works without the auction switch.
            if (!auction.IsFixedPrice && !IsEnabled) { errorKey = "claims:plot_auction_disabled"; return false; }
            if (auction.HasExpired(TimeFunctions.getEpochSeconds()))
            {
                errorKey = "claims:plot_auction_not_running";
                return false;
            }
            if (bidder == null) { errorKey = "claims:you_dont_have_city"; return false; }
            if (bidder.Guid == auction.SellerCityGuid) { errorKey = "claims:plot_auction_own_lot"; return false; }
            if (!IsVisibleTo(auction, bidder)) { errorKey = "claims:plot_trade_not_for_you"; return false; }
            if (bidder.Guid == auction.CurrentBidderCityGuid)
            {
                errorKey = "claims:plot_auction_already_leading";
                return false;
            }
            if (amount < auction.MinNextBid) { errorKey = "claims:plot_auction_bid_too_low"; return false; }

            // The lot must still be sellable and the bidder able to receive it: a bid on land that
            // could never change hands is money held for nothing.
            if (!auction.TryGetSeller(out City seller)) { errorKey = "claims:plot_auction_lot_gone"; return false; }
            if (!CanOwnLand(bidder, out errorKey)) return false;

            if (auction.Kind == EnumAuctionKind.PLOT)
            {
                if (!auction.TryGetPlot(out Plot plot)) { errorKey = "claims:plot_auction_lot_gone"; return false; }
                if (!PlotSaleRules.CanSellPlot(plot, seller, out errorKey)) return false;
                if (!PlotSaleRules.CanTakePlot(plot, bidder, out errorKey)) return false;
            }
            if (claims.economyProvider.GetBalance(bidder.MoneyAccountName) < amount)
            {
                errorKey = "claims:not_enough_money";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Who may own bought land at all - checked for every kind of lot. A whole-city lot skips the
        /// plot limit (absorbing a city always exceeds it) but not this.
        /// </summary>
        public static bool CanOwnLand(City city, out string errorKey)
        {
            errorKey = null;
            if (city == null) { errorKey = "claims:you_dont_have_city"; return false; }
            if (city.IsVillage()) { errorKey = "claims:village_feature_locked"; return false; }
            if (city.isTechnicalCity()) { errorKey = "claims:plot_trade_technical_city"; return false; }
            if (PlotSaleRules.IsAtWar(city)) { errorKey = "claims:plot_trade_at_war"; return false; }
            return true;
        }
    }
}
