using System.Collections.Generic;
using claims.src.auxialiry;

namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// One lot on the land auction: a plot (or a whole bankrupt city) offered to other cities for a
    /// limited time, with bids held in escrow until the lot closes.
    ///
    /// The lot keeps guids rather than object references, the way <see cref="PlotSaleRecord"/> does:
    /// a seller or a bidder may be demolished while the lot is still open, and a dangling reference
    /// would keep a dead city alive in memory.
    /// </summary>
    public class PlotAuction : Part
    {
        public PlotAuction(string val, string guid) : base(val, guid)
        {
        }

        public EnumAuctionKind Kind { get; set; } = EnumAuctionKind.PLOT;
        /// <summary>Plot coordinates for a PLOT lot. Meaningless for WHOLE_CITY.</summary>
        public int PlotX { get; set; }
        public int PlotZ { get; set; }
        /// <summary>The city being sold whole for a WHOLE_CITY lot. Empty for a PLOT lot.</summary>
        public string LotCityGuid { get; set; } = "";
        /// <summary>Who gets the money. For a bankruptcy lot this is the bankrupt city itself.</summary>
        public string SellerCityGuid { get; set; } = "";

        public long StartPrice { get; set; }
        public long MinIncrement { get; set; } = 1;
        /// <summary>Bid that closes the lot at once. -1 when the lot has no buyout.</summary>
        public long BuyoutPrice { get; set; } = -1;
        /// <summary>Highest bid so far, -1 while nobody has bid.</summary>
        public long CurrentBid { get; set; } = -1;
        public string CurrentBidderCityGuid { get; set; } = "";

        /// <summary>Unix seconds.</summary>
        public long StartedAt { get; set; }
        /// <summary>Unix seconds. Moved forward by a late bid - see AuctionHandler anti-sniping.</summary>
        public long EndsAt { get; set; }

        public EnumPlotSaleAudience Audience { get; set; } = EnumPlotSaleAudience.ALLIES;
        public string TargetCityGuid { get; set; } = "";
        public EnumAuctionReason Reason { get; set; } = EnumAuctionReason.NORMAL;
        public EnumAuctionState State { get; set; } = EnumAuctionState.RUNNING;

        /// <summary>
        /// Every bid ever placed here, oldest first. Kept whole rather than just the leader: when the
        /// leading city disappears mid-auction the lot falls back to the previous bidder, and that
        /// needs the bid before the current one.
        /// </summary>
        public List<AuctionBidRecord> Bids { get; } = new List<AuctionBidRecord>();

        public override bool saveToDatabase(bool update = true)
        {
            claims.getModInstance().getDatabaseHandler().saveAuction(this, update);
            return true;
        }

        /// <summary>The escrow account holding the leading bid. One per lot, deleted when it closes.</summary>
        public string EscrowAccountName => claims.config.AUCTION_ACCOUNT_STRING_PREFIX + Guid;

        public bool IsRunning => State == EnumAuctionState.RUNNING;
        public bool HasBid => CurrentBid >= 0 && CurrentBidderCityGuid != "";
        public bool HasBuyout => BuyoutPrice >= 0;
        /// <summary>A lot with no closing time: it stands until it is taken or withdrawn.</summary>
        public bool IsOpenEnded => EndsAt <= 0;
        public bool HasExpired(long now) => !IsOpenEnded && now >= EndsAt;
        public long SecondsLeft(long now) => IsOpenEnded ? 0 : (EndsAt - now > 0 ? EndsAt - now : 0);

        /// <summary>
        /// A plain "for sale at this price" offer: no closing time and a buyout equal to the asking
        /// price, so the first city to meet it takes the plot at once. Selling at a fixed price and
        /// selling by bids are the same thing here, differing only in these two fields - which is
        /// why there is one set of rules, one table and one browser rather than two of each.
        /// </summary>
        public bool IsFixedPrice => IsOpenEnded && HasBuyout && BuyoutPrice == StartPrice;

        /// <summary>Smallest bid that would be accepted right now.</summary>
        public long MinNextBid => HasBid ? CurrentBid + MinIncrement : StartPrice;

        public bool TryGetSeller(out City seller) =>
            claims.dataStorage.getCityByGUID(SellerCityGuid, out seller);

        /// <summary>The city put up whole. Only meaningful for a WHOLE_CITY lot.</summary>
        public bool TryGetLotCity(out City city) =>
            claims.dataStorage.getCityByGUID(LotCityGuid, out city);

        public bool TryGetLeader(out City leader)
        {
            leader = null;
            return HasBid && claims.dataStorage.getCityByGUID(CurrentBidderCityGuid, out leader);
        }

        /// <summary>The plot on sale. False for a WHOLE_CITY lot, or if the plot is no longer claimed.</summary>
        public bool TryGetPlot(out Plot plot)
        {
            plot = null;
            if (Kind != EnumAuctionKind.PLOT) return false;
            return claims.dataStorage.GetPlot(new PlotPosition(PlotX, PlotZ), out plot);
        }

    }
}
