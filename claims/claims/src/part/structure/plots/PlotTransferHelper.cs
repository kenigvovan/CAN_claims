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
        /// Raised before the plot changes hands, while it still points at the city losing it.
        /// Lets standing offers on the ground be taken down without this file knowing they exist.
        /// </summary>
        public static event System.Action<Plot> PlotLeavingCity;

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

            PlotLeavingCity?.Invoke(plot);

            // Reset the type BEFORE the plot changes hands. Camps, summon points and temple respawns
            // are unregistered by PlotDesc.OnDeactivated through plot.getCity(), so doing this after
            // setCity would clean the buyer's lists and leave the seller pointing at a plot it no
            // longer owns - a summon point on foreign ground that still works.
            //shouldn't crash with default but better to remake it somehow with init functions
            plot.setNewType(new TextCommandResult(), "default", null, true);

            plot.setCity(to);
            from?.getCityPlots().Remove(plot);
            to.getCityPlots().Add(plot);

            // The plot takes on the receiving city's permissions, exactly as a freshly claimed one
            // does. Keeping the seller's settings would hand over ground whose rules the new owner
            // never chose - a plot left open to strangers or to foes stays open under its new flag,
            // and nothing on the page tells them so.
            plot.getPermsHandler().setPerm(to.getPermsHandler());

            plot.setPlotOwner(null);
            plot.setCustomTax(0);
            plot.setPlotGroup(null);
            plot.Price = -1;

            // The plot is new to its owner, whatever its history. Without this the unclaim refund -
            // which pays out the claim price once a plot is old enough - would be handed to a city
            // that bought the ground a moment ago, and the waiting period it enforces could be
            // skipped by buying an old plot instead of claiming a fresh one.
            plot.TimeStampClaimed = TimeFunctions.getEpochSeconds();
            // What a citizen of the previous city once paid is no basis for refunding a citizen of
            // this one; the next personal sale sets it again.
            plot.lastPaidPrice = 0;

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
            // The daily upkeep is charged per plot, so both treasuries owe a different amount from
            // now on - the same pair of updates an unclaim sends.
            if (from != null)
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(from.Guid,
                    EnumPlayerRelatedInfo.CLAIMED_PLOTS, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            }
            UsefullPacketsSend.AddToQueueCityInfoUpdate(to.Guid,
                EnumPlayerRelatedInfo.CLAIMED_PLOTS, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            UsefullPacketsSend.AddToQueueAllPlayersInfoUpdate(
                new Dictionary<string, object> { { "value", plot.getPos() } }, EnumPlayerRelatedInfo.CITY_PLOT_RECOLOR);
        }
    }
}
