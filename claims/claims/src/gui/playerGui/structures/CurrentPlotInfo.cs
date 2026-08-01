using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.structures
{
    public class CurrentPlotInfo
    {
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
