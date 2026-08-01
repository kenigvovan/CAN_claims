using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.economy;
using claims.src.messages;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// The inter-city plot market: a city lists one of its plots for a price and an audience,
    /// another city buys it, and the plot changes hands through <see cref="PlotTransferHelper"/>.
    /// Every rule lives here so the command, the GUI gate and the purchase all judge a listing the
    /// same way - the checks are re-run at purchase time, because a listing outlives the state it
    /// was created in.
    /// </summary>
    public static class PlotMarketHelper
    {
        /// <summary>Whether this plot may be put on the market by its own city. Error key on refusal.</summary>
        public static bool CanList(Plot plot, City seller, out string errorKey)
        {
            errorKey = null;
            if (!claims.config.CITY_PLOT_TRADE_ENABLED) { errorKey = "claims:plot_trade_disabled"; return false; }
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

        /// <summary>Puts the plot up for sale. Caller has already run <see cref="CanList"/>.</summary>
        public static void List(Plot plot, int price, EnumPlotSaleAudience audience, City targetCity)
        {
            plot.PriceForCityBuy = price;
            plot.SaleAudience = audience;
            plot.SaleTargetCityGuid = audience == EnumPlotSaleAudience.SPECIFIC_CITY ? targetCity?.Guid ?? "" : "";
            plot.saveToDatabase();
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            NotifyMarketChanged();
        }

        public static void Unlist(Plot plot)
        {
            PlotTransferHelper.ClearListing(plot);
            plot.saveToDatabase();
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            NotifyMarketChanged();
        }

        /// <summary>
        /// Pushes the market to every city, because who may see a listing depends on the viewer.
        /// Listings change rarely enough that a full refresh is cheaper than tracking which city's
        /// view of the market actually moved.
        /// </summary>
        public static void NotifyMarketChanged()
        {
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                    gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_PLOT_MARKET);
            }
        }

        /// <summary>Whether <paramref name="buyer"/> is allowed to see and buy this listing at all.</summary>
        public static bool IsVisibleTo(Plot plot, City buyer)
        {
            if (plot == null || buyer == null || !plot.IsForSaleForCity || !plot.hasCity()) return false;
            City seller = plot.getCity();
            if (seller.Equals(buyer)) return false;
            // A plot that has since been taken by a citizen or folded into a plot group is no longer
            // sellable; showing it would be an offer that refuses itself when clicked.
            if (plot.hasPlotOwner() || plot.hasCityPlotsGroup()) return false;

            switch (plot.SaleAudience)
            {
                case EnumPlotSaleAudience.SPECIFIC_CITY:
                    return buyer.Guid == plot.SaleTargetCityGuid;
                case EnumPlotSaleAudience.ALLIES:
                    return AreAllied(seller, buyer);
                case EnumPlotSaleAudience.NON_HOSTILE:
                    return !AreHostile(seller, buyer);
                default:
                    return true;
            }
        }

        /// <summary>Everything the buyer's side must satisfy, re-checked at purchase time.</summary>
        public static bool CanBuy(Plot plot, City buyer, out string errorKey)
        {
            errorKey = null;
            if (!claims.config.CITY_PLOT_TRADE_ENABLED) { errorKey = "claims:plot_trade_disabled"; return false; }
            if (plot == null || !plot.IsForSaleForCity) { errorKey = "claims:plot_trade_not_listed"; return false; }
            if (buyer == null) { errorKey = "claims:you_dont_have_city"; return false; }
            if (buyer.IsVillage()) { errorKey = "claims:village_feature_locked"; return false; }
            // Symmetrical with the seller check in CanList: a service city trades no land either way.
            if (buyer.isTechnicalCity()) { errorKey = "claims:plot_trade_technical_city"; return false; }
            if (!IsVisibleTo(plot, buyer)) { errorKey = "claims:plot_trade_not_for_you"; return false; }

            City seller = plot.getCity();
            // The listing may have gone stale: an owner appeared, the plot became the last one, a war
            // broke out. Judging it by CanList keeps one set of rules for both ends of the deal.
            if (!CanList(plot, seller, out errorKey)) return false;
            if (IsAtWar(buyer)) { errorKey = "claims:plot_trade_at_war"; return false; }

            if (buyer.getCityPlots().Count >= Settings.getMaxNumberOfPlotForCity(buyer))
            {
                errorKey = "claims:max_amount_claimed";
                return false;
            }
            if (claims.config.CITY_PLOT_TRADE_REQUIRE_ADJACENCY && !TouchesCity(plot, buyer))
            {
                errorKey = "claims:should_be_on_the_border_with_another_claimed_plot";
                return false;
            }
            if (claims.economyProvider.GetBalance(buyer.MoneyAccountName) < plot.PriceForCityBuy)
            {
                errorKey = "claims:not_enough_money";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Executes the sale: money moves first, then the plot. Returns false with an error key if
        /// the payment failed, leaving the listing untouched.
        /// </summary>
        public static bool Buy(Plot plot, City buyer, out string errorKey)
        {
            errorKey = null;
            City seller = plot.getCity();
            int price = plot.PriceForCityBuy;

            // Money first: a failed transfer must not leave the plot already handed over.
            if (price > 0 && claims.economyProvider.Transfer(buyer.MoneyAccountName, seller.MoneyAccountName, price)
                != MoneyOperationResult.Success)
            {
                errorKey = "claims:economy_money_transaction_error";
                return false;
            }

            PlotTransferHelper.Transfer(plot, seller, buyer, markCaptured: false);

            RecordSale(plot, seller, buyer, price);

            seller.AddLogEntry(EnumCityLogEvent.PlotSoldToCity, buyer.GetPartName(),
                plot.getPos().X + " " + plot.getPos().Y, price.ToString());
            buyer.AddLogEntry(EnumCityLogEvent.PlotBoughtFromCity, seller.GetPartName(),
                plot.getPos().X + " " + plot.getPos().Y, price.ToString());
            UsefullPacketsSend.AddToQueueCityInfoUpdate(seller.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(buyer.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY);
            NotifyMarketChanged();

            seller.FirePlotsMapChanged(EnumPlotsMapChangeReason.Unclaimed);
            buyer.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);

            MessageHandler.sendMsgInCity(seller,
                Lang.Get("claims:plot_trade_sold", buyer.getPartNameReplaceUnder(), price));
            MessageHandler.sendMsgInCity(buyer,
                Lang.Get("claims:plot_trade_bought", seller.getPartNameReplaceUnder(), price));
            return true;
        }

        private static void RecordSale(Plot plot, City seller, City buyer, int price)
        {
            var record = new PlotSaleRecord(Guid.NewGuid().ToString(), plot.getPos().X, plot.getPos().Y,
                seller, buyer, price, TimeFunctions.getEpochSeconds());
            claims.dataStorage.PlotSaleHistory.Add(record);
            claims.getModInstance().getDatabaseHandler().savePlotSale(record);
        }

        /// <summary>Listings this city may act on, for the market tab.</summary>
        public static List<Plot> GetListingsFor(City buyer)
        {
            var result = new List<Plot>();
            if (buyer == null || !claims.config.CITY_PLOT_TRADE_ENABLED) return result;
            foreach (Plot plot in claims.dataStorage.getClaimedPlots().Values)
            {
                if (IsVisibleTo(plot, buyer)) result.Add(plot);
            }
            return result;
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
            // Read from both sides, as AreHostile does: a union recorded on one side only would
            // otherwise make the offer visible to one of the two allies and not the other.
            return a.Alliance.ComradAlliancies.Contains(b.Alliance)
                || b.Alliance.ComradAlliancies.Contains(a.Alliance);
        }

        /// <summary>True when one of the four neighbouring plots already belongs to this city.</summary>
        private static bool TouchesCity(Plot plot, City city)
        {
            PlotPosition probe = new PlotPosition(0, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (Math.Abs(i) + Math.Abs(j) != 1) continue;
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
