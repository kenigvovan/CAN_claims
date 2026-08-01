namespace claims.src.part.structure.plots
{
    /// <summary>
    /// Who may buy a plot its owning city put on the inter-city market. ALLIES is the default
    /// everywhere: an offer that was meant for friends must never widen itself by omission.
    /// Stored in the database as a plain int, so new members may only be appended.
    /// </summary>
    public enum EnumPlotSaleAudience
    {
        /// <summary>Any city, including one at war with the seller.</summary>
        EVERYONE,
        /// <summary>Any city except hostiles and parties in a running conflict with the seller.</summary>
        NON_HOSTILE,
        /// <summary>The seller's own alliance and its allied alliances.</summary>
        ALLIES,
        /// <summary>One named city, addressed by guid in Plot.SaleTargetCityGuid.</summary>
        SPECIFIC_CITY
    }

    public static class PlotSaleAudienceExtensions
    {
        /// <summary>
        /// How this audience is written out for a player. Lives next to the enum rather than in the
        /// command class, so the GUI can name an audience without depending on server-side commands.
        /// </summary>
        public static string LangKey(this EnumPlotSaleAudience audience) => audience switch
        {
            EnumPlotSaleAudience.NON_HOSTILE => "claims:plot_trade_audience_nonhostile",
            EnumPlotSaleAudience.ALLIES => "claims:plot_trade_audience_allies",
            EnumPlotSaleAudience.SPECIFIC_CITY => "claims:plot_trade_audience_city",
            _ => "claims:plot_trade_audience_everyone"
        };
    }
}
