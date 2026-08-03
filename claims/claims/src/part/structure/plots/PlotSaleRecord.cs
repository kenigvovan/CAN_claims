namespace claims.src.part.structure.plots
{
    /// <summary>
    /// One completed inter-city plot sale. City names are kept next to the guids because a city may
    /// be demolished later, and a history entry that then reads as a bare guid is worth nothing.
    /// </summary>
    public class PlotSaleRecord
    {
        public string Guid { get; set; } = "";
        public int X { get; set; }
        public int Z { get; set; }
        public string SellerGuid { get; set; } = "";
        public string SellerName { get; set; } = "";
        public string BuyerGuid { get; set; } = "";
        public string BuyerName { get; set; } = "";
        public long Price { get; set; }
        /// <summary>Unix seconds when the sale went through.</summary>
        public long TimeStamp { get; set; }

        public PlotSaleRecord() { }

        public PlotSaleRecord(string guid, int x, int z, City seller, City buyer, long price, long timeStamp)
        {
            Guid = guid;
            X = x;
            Z = z;
            SellerGuid = seller?.Guid ?? "";
            SellerName = seller?.GetPartName() ?? "";
            BuyerGuid = buyer?.Guid ?? "";
            BuyerName = buyer?.GetPartName() ?? "";
            Price = price;
            TimeStamp = timeStamp;
        }

        /// <summary>True when this city was either side of the deal.</summary>
        public bool Involves(City city) =>
            city != null && (city.Guid == SellerGuid || city.Guid == BuyerGuid);
    }
}
