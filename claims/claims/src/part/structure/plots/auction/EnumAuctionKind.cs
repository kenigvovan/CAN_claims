namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// What is being sold at a lot. Stored in the database as a plain int, so new members may only
    /// be appended.
    /// </summary>
    public enum EnumAuctionKind
    {
        /// <summary>One plot, addressed by PlotX/PlotZ.</summary>
        PLOT,
        /// <summary>Every plot of a bankrupt city at once, addressed by LotCityGuid.</summary>
        WHOLE_CITY
    }
}
