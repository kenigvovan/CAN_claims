using claims.src.part.structure.conflict;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// What makes a plot sellable at all, independent of how it is being sold. Both the fixed-price
    /// market and the auction judge a plot by these rules, at listing time and again when the deal
    /// closes - a listing outlives the state it was created in.
    ///
    /// Bottom of the land-trade stack: depends on nothing above it.
    /// </summary>
    public static class PlotSaleRules
    {
        /// <summary>Whether this city may hand this plot to another city for money. Error key on refusal.</summary>
        public static bool CanSellPlot(Plot plot, City seller, out string errorKey)
        {
            errorKey = null;
            if (plot == null) { errorKey = "claims:plot_not_claimed"; return false; }
            if (seller == null || !plot.hasCity() || !plot.getCity().Equals(seller))
            {
                errorKey = "claims:not_your_city";
                return false;
            }
            // A village has no treasury to be paid into, and its main plot is what a raid must reach.
            if (seller.IsVillage()) { errorKey = "claims:village_feature_locked"; return false; }
            if (seller.isTechnicalCity()) { errorKey = "claims:plot_trade_technical_city"; return false; }
            if (plot.Type == PlotType.MAIN_CITY_PLOT || plot.Type == PlotType.VILLAGE_MAIN)
            {
                errorKey = "claims:plot_trade_main_plot";
                return false;
            }
            // A war camp belongs to a running conflict, not to the land market: it is torn down when
            // the war ends, and selling it would hand the buyer a plot about to demolish itself.
            if (plot.Type == PlotType.CAMP) { errorKey = "claims:plot_trade_camp"; return false; }
            if (seller.getCityPlots().Count <= 1) { errorKey = "claims:last_city_plot"; return false; }
            // Selling out from under a citizen would take their buildings with the ground. The mayor
            // has to free the plot first, which pays the citizen back through the usual refund.
            if (plot.hasPlotOwner()) { errorKey = "claims:plot_trade_has_owner"; return false; }
            if (plot.hasCityPlotsGroup()) { errorKey = "claims:has_plots_group"; return false; }
            if (IsAtWar(seller)) { errorKey = "claims:plot_trade_at_war"; return false; }
            return true;
        }

        /// <summary>
        /// Whether this city could take the plot over if it won it - the buyer's half of the rules,
        /// without the price. Shared by the fixed-price market and by bidding, so a city cannot bid
        /// on land it would never be allowed to receive.
        /// </summary>
        public static bool CanTakePlot(Plot plot, City buyer, out string errorKey)
        {
            errorKey = null;
            if (buyer == null) { errorKey = "claims:you_dont_have_city"; return false; }
            if (buyer.IsVillage()) { errorKey = "claims:village_feature_locked"; return false; }
            // Symmetrical with the seller check: a service city trades no land either way.
            if (buyer.isTechnicalCity()) { errorKey = "claims:plot_trade_technical_city"; return false; }
            if (IsAtWar(buyer)) { errorKey = "claims:plot_trade_at_war"; return false; }
            if (buyer.getCityPlots().Count >= auxialiry.Settings.getMaxNumberOfPlotForCity(buyer))
            {
                errorKey = "claims:max_amount_claimed";
                return false;
            }
            if (claims.config.CITY_PLOT_TRADE_REQUIRE_ADJACENCY && !TouchesCity(plot, buyer))
            {
                errorKey = "claims:should_be_on_the_border_with_another_claimed_plot";
                return false;
            }
            return true;
        }

        /// <summary>Whether an offer with this audience is addressed to <paramref name="buyer"/>.</summary>
        public static bool MatchesAudience(City seller, City buyer, EnumPlotSaleAudience audience, string targetCityGuid)
        {
            if (seller == null || buyer == null || seller.Equals(buyer)) return false;
            switch (audience)
            {
                case EnumPlotSaleAudience.SPECIFIC_CITY:
                    return buyer.Guid == targetCityGuid;
                case EnumPlotSaleAudience.ALLIES:
                    return AreAllied(seller, buyer);
                case EnumPlotSaleAudience.NON_HOSTILE:
                    return !AreHostile(seller, buyer);
                default:
                    return true;
            }
        }

        /// <summary>A city is at war while it - or its alliance - has a running conflict.</summary>
        public static bool IsAtWar(City city)
        {
            if (city == null) return false;
            if (city.RunningConflicts.Count > 0) return true;
            return city.HasAlliance() && city.Alliance.RunningConflicts.Count > 0;
        }

        private static bool AreHostile(City a, City b)
        {
            if (a.HostileCities.Contains(b) || b.HostileCities.Contains(a)) return true;
            IConflictParty partyA = a.HasAlliance() ? (IConflictParty)a.Alliance : a;
            IConflictParty partyB = b.HasAlliance() ? (IConflictParty)b.Alliance : b;
            return ConflictHandler.TryGetConflictWithSides(partyA, partyB, out _);
        }

        private static bool AreAllied(City a, City b)
        {
            if (!a.HasAlliance() || !b.HasAlliance()) return false;
            if (a.Alliance.Equals(b.Alliance)) return true;
            // Read from both sides: a union recorded on one side only would otherwise show the offer
            // to one of the two allies and not the other.
            return a.Alliance.ComradAlliancies.Contains(b.Alliance)
                || b.Alliance.ComradAlliancies.Contains(a.Alliance);
        }

        /// <summary>True when one of the four neighbouring plots already belongs to this city.</summary>
        public static bool TouchesCity(Plot plot, City city)
        {
            if (plot == null || city == null) return false;
            auxialiry.PlotPosition probe = new auxialiry.PlotPosition(0, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (System.Math.Abs(i) + System.Math.Abs(j) != 1) continue;
                    probe.X = plot.plotPosition.X + i;
                    probe.Z = plot.plotPosition.Z + j;
                    if (claims.dataStorage.GetPlot(probe, out Plot near)
                        && near.hasCity() && near.getCity().Equals(city))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
