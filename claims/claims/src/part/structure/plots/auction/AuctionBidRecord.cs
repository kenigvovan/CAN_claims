namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// One bid placed on a lot. The bidder's name is stored next to the guid for the same reason the
    /// sale history does it: the city may be gone by the time anyone reads the row back.
    /// </summary>
    public class AuctionBidRecord
    {
        public string Guid { get; set; } = "";
        public string AuctionGuid { get; set; } = "";
        public string CityGuid { get; set; } = "";
        public string CityName { get; set; } = "";
        public long Amount { get; set; }
        /// <summary>Unix seconds when the bid was placed.</summary>
        public long TimeStamp { get; set; }

        public AuctionBidRecord() { }

        public AuctionBidRecord(string guid, string auctionGuid, City city, long amount, long timeStamp)
        {
            Guid = guid;
            AuctionGuid = auctionGuid;
            CityGuid = city?.Guid ?? "";
            CityName = city?.GetPartName() ?? "";
            Amount = amount;
            TimeStamp = timeStamp;
        }
    }
}
