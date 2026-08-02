namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// Why the lot exists. A forced sale is announced differently and its proceeds go to paying off
    /// the debt that caused it. Stored as a plain int - append only.
    /// </summary>
    public enum EnumAuctionReason
    {
        /// <summary>A city chose to put its land up for bids.</summary>
        NORMAL,
        /// <summary>The city went bankrupt and is being sold off instead of demolished.</summary>
        BANKRUPTCY
    }
}
