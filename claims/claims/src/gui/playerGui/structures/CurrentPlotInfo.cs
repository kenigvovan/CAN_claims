using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.structures
{
    public class CurrentPlotInfo
    {
        /// <summary>
        /// False when the player stands on unclaimed ground. Such a spot is still sent, so the page
        /// stops describing the plot last walked on.
        /// </summary>
        public bool IsClaimed { get; set; } = true;
        public string PlotName { get; set; }
        public string OwnerName { get; set; }
        public PlotType PlotType { get; set; }
        public Vec2i PlotPosition {  get; set; }
        public double CustomTax { get; set; } = 0;
        public double Price { get; set; } = -1;
        public PermsHandler PermsHandler {  get; set; }
        public bool ExtraBought { get; set; }

        /// <summary>City this plot belongs to, so the page can tell "ours" from "theirs".</summary>
        public string CityName { get; set; } = "";
        /// <summary>Asking price on the inter-city market; -1 when the plot is not listed.</summary>
        public int PriceForCityBuy { get; set; } = -1;
        public EnumPlotSaleAudience SaleAudience { get; set; } = EnumPlotSaleAudience.ALLIES;
        /// <summary>Named buyer of a SPECIFIC_CITY listing, for display.</summary>
        public string SaleTargetCityName { get; set; } = "";
        /// <summary>
        /// Whether the viewer's city may buy this listing right now. Decided by the server so the
        /// page does not re-implement the market rules and disagree with them.
        /// </summary>
        public bool CanBuyAsCity { get; set; } = false;

        /// <summary>Unix seconds when the auction on this plot closes; 0 when there is none.</summary>
        public long AuctionEndsAt { get; set; } = 0;
        /// <summary>Leading bid, -1 while nobody has bid.</summary>
        public long AuctionCurrentBid { get; set; } = -1;
        /// <summary>Smallest bid the server would take right now.</summary>
        public long AuctionMinNextBid { get; set; } = 0;
        /// <summary>Bid that ends the lot at once; -1 when the lot has no buyout.</summary>
        public long AuctionBuyout { get; set; } = -1;
        /// <summary>Whether the viewer's city may bid, decided by the server for the same reason as CanBuyAsCity.</summary>
        public bool CanBidAsCity { get; set; } = false;
        /// <summary>
        /// Lang key of why this city may not take the offer; empty when it may, or when the viewer is
        /// the seller. Shown instead of a button that would otherwise just be missing.
        /// </summary>
        public string CityBuyBlockedReason { get; set; } = "";

        public CurrentPlotInfo(string plotName, string ownerName, PlotType plotType, double customTax,
            double price, PermsHandler permsHandler, bool extraBoungt, Vec2i plotPosition)
        {
            PlotName = plotName;
            OwnerName = ownerName;
            PlotType = plotType;
            CustomTax = customTax;
            Price = price;
            PlotPosition = plotPosition;
            PermsHandler = permsHandler;
            ExtraBought = extraBoungt;
        }

        public CurrentPlotInfo()
        {
            PermsHandler = new PermsHandler();
            PlotPosition = new Vec2i();
        }

    }
}
