using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.part.structure.plots.auction;

namespace claims.src.gui.playerGui.structures.cellElements
{
    /// <summary>
    /// One lot as the market tab shows it. The closing time travels as a unix stamp rather than as
    /// "minutes left": the client counts down from it on its own, so an open lot does not have to be
    /// re-sent to every player every second.
    /// </summary>
    public class PlotAuctionCellElement
    {
        public string Guid { get; set; } = "";
        public int X { get; set; }
        public int Z { get; set; }
        public string SellerCityName { get; set; } = "";
        public string PlotName { get; set; } = "";
        public long CurrentBid { get; set; } = -1;
        public string LeaderCityName { get; set; } = "";
        /// <summary>Smallest bid the server would accept right now.</summary>
        public long MinNextBid { get; set; }
        public long BuyoutPrice { get; set; } = -1;
        /// <summary>Unix seconds when the lot closes; 0 for a price tag, which has no closing time.</summary>
        public long EndsAt { get; set; }
        public EnumPlotSaleAudience Audience { get; set; } = EnumPlotSaleAudience.ALLIES;
        /// <summary>True when this is our own lot - shown, but bid on by nobody here.</summary>
        public bool IsOurs { get; set; }
        /// <summary>
        /// A bankrupt settlement sold whole. Such a lot stands on no single plot, so it is named by
        /// the city rather than by coordinates.
        /// </summary>
        public bool IsWholeCity { get; set; }
        public string LotCityName { get; set; } = "";
        /// <summary>Whether our city may bid right now, decided by the server.</summary>
        public bool CanBid { get; set; }

        public PlotAuctionCellElement() { }

        public PlotAuctionCellElement(PlotAuction auction, Plot plot, City viewer, bool canBid)
        {
            Guid = auction.Guid;
            X = auction.PlotX;
            Z = auction.PlotZ;
            SellerCityName = auction.TryGetSeller(out City seller) ? seller.GetPartName() : "";
            PlotName = plot?.GetPartName() ?? "";
            CurrentBid = auction.CurrentBid;
            LeaderCityName = auction.TryGetLeader(out City leader) ? leader.GetPartName() : "";
            MinNextBid = auction.MinNextBid;
            BuyoutPrice = auction.BuyoutPrice;
            EndsAt = auction.EndsAt;
            Audience = auction.Audience;
            IsOurs = viewer != null && viewer.Guid == auction.SellerCityGuid;
            IsWholeCity = auction.Kind == EnumAuctionKind.WHOLE_CITY;
            LotCityName = auction.TryGetLotCity(out City lotCity) ? lotCity.GetPartName() : "";
            CanBid = canBid;
        }
    }
}
