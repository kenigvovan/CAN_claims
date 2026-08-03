using System.Collections.Generic;
using claims.src.auxialiry;

namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// Keeps track of which lots are open and where they stand.
    ///
    /// Closed lots stay in storage forever for the history, so "is this plot on offer?" - asked for
    /// every plot a player walks onto - cannot be a walk over all of them. Open lots are indexed by
    /// plot; the index lives in memory only and is rebuilt on load.
    /// </summary>
    public static class AuctionRegistry
    {
        private static readonly Dictionary<PlotPosition, PlotAuction> runningByPlot
            = new Dictionary<PlotPosition, PlotAuction>();

        /// <summary>Open lots standing on no single plot - the whole-city ones.</summary>
        private static readonly List<PlotAuction> runningCityLots = new List<PlotAuction>();

        public static bool TryGet(string guid, out PlotAuction auction) =>
            claims.dataStorage.Auctions.TryGetValue(guid, out auction);

        /// <summary>
        /// Every lot ever, closed ones included - for code that reads history. Callers must not add
        /// to it: a lot inserted behind the index would be invisible to every "is this on offer?".
        /// </summary>
        public static IEnumerable<PlotAuction> All => claims.dataStorage.Auctions.Values;

        /// <summary>Open lots. A copy: closing one during the walk would modify the index.</summary>
        public static List<PlotAuction> GetRunning()
        {
            var result = new List<PlotAuction>(runningByPlot.Values);
            result.AddRange(runningCityLots);
            return result;
        }

        public static bool TryGetRunningFor(Plot plot, out PlotAuction auction)
        {
            auction = null;
            if (plot == null) return false;
            return runningByPlot.TryGetValue(plot.plotPosition, out auction);
        }

        public static bool HasRunningFor(Plot plot) => TryGetRunningFor(plot, out _);

        /// <summary>The open lot on a whole settlement, looked up by city - it stands on no plot.</summary>
        public static bool TryGetRunningForCity(City city, out PlotAuction auction)
        {
            auction = null;
            if (city == null) return false;
            foreach (PlotAuction it in runningCityLots)
            {
                if (it.LotCityGuid == city.Guid) { auction = it; return true; }
            }
            return false;
        }

        /// <summary>Adds a freshly built lot and writes its first row.</summary>
        public static void Add(PlotAuction auction)
        {
            claims.dataStorage.Auctions[auction.Guid] = auction;
            Index(auction);
            auction.saveToDatabase(update: false);
        }

        /// <summary>Rebuilds the index from storage. Called once after the lots are read in.</summary>
        public static void Rebuild()
        {
            runningByPlot.Clear();
            runningCityLots.Clear();
            foreach (PlotAuction auction in claims.dataStorage.Auctions.Values)
            {
                if (auction.IsRunning) Index(auction);
            }
        }

        public static void Index(PlotAuction auction)
        {
            if (auction.Kind == EnumAuctionKind.WHOLE_CITY) runningCityLots.Add(auction);
            else runningByPlot[new PlotPosition(auction.PlotX, auction.PlotZ)] = auction;
        }

        public static void Unindex(PlotAuction auction)
        {
            if (auction.Kind == EnumAuctionKind.WHOLE_CITY) runningCityLots.Remove(auction);
            else runningByPlot.Remove(new PlotPosition(auction.PlotX, auction.PlotZ));
        }
    }
}
