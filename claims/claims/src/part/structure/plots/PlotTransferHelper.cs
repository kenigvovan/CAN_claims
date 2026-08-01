using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using Vintagestory.API.Common;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// Moves a plot from one city to another. Shared by every path that hands territory over -
    /// a war capture, a ceded plot in a peace deal, and a market sale - so all three leave the
    /// plot, both cities, the map and the clients in the same state.
    /// </summary>
    public static class PlotTransferHelper
    {
        /// <summary>
        /// Reassigns <paramref name="plot"/> from its current city to <paramref name="to"/> and
        /// strips everything that belonged to the old owner: the owning player, the custom tax, the
        /// plot type (with its deactivation routine), the plot group and any market listing.
        /// Callers do their own eligibility checks - this only performs the move.
        /// </summary>
        /// <param name="markCaptured">Sets WasCaptured; true for war captures and cessions.</param>
        public static void Transfer(Plot plot, City from, City to, bool markCaptured)
        {
            if (plot == null || to == null) return;

            // Reset the type BEFORE the plot changes hands. Camps, summon points and temple respawns
            // are unregistered by PlotDesc.OnDeactivated through plot.getCity(), so doing this after
            // setCity would clean the buyer's lists and leave the seller pointing at a plot it no
            // longer owns - a summon point on foreign ground that still works.
            //shouldn't crash with default but better to remake it somehow with init functions
            plot.setNewType(new TextCommandResult(), "default", null, true);

            plot.setCity(to);
            from?.getCityPlots().Remove(plot);
            to.getCityPlots().Add(plot);

            plot.setPlotOwner(null);
            plot.setCustomTax(0);
            plot.setPlotGroup(null);
            bool wasListed = plot.IsForSaleForCity;
            ClearListing(plot);
            plot.Price = -1;

            // The seller paid for this extra chunk and keeps the slot free once the chunk is gone;
            // clearing the flag without giving the slot back cost them the purchase permanently.
            if (plot.extraBought && from != null && from.Extrachunksbought > 0)
            {
                from.Extrachunksbought--;
            }
            plot.extraBought = false;
            if (markCaptured) plot.WasCaptured = true;

            plot.UpdateBorderPlotValue();
            from?.saveToDatabase();
            to.saveToDatabase();
            plot.saveToDatabase();
            plot.CheckBorderPlotValue();

            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            if (from != null)
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(from.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            }
            UsefullPacketsSend.AddToQueueCityInfoUpdate(to.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            UsefullPacketsSend.AddToQueueAllPlayersInfoUpdate(
                new Dictionary<string, object> { { "value", plot.getPos() } }, EnumPlayerRelatedInfo.CITY_PLOT_RECOLOR);

            // A plot taken by war or ceded in a peace deal leaves the market without anyone running
            // the market commands, so the browsers of every other city have to be told separately.
            if (wasListed) PlotMarketHelper.NotifyMarketChanged();
        }

        /// <summary>Takes the plot off the inter-city market, leaving no stale price or audience behind.</summary>
        public static void ClearListing(Plot plot)
        {
            if (plot == null) return;
            plot.PriceForCityBuy = -1;
            plot.SaleAudience = EnumPlotSaleAudience.ALLIES;
            plot.SaleTargetCityGuid = "";
        }
    }
}
