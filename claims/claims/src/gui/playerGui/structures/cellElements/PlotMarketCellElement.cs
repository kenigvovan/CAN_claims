using claims.src.part.structure;
using claims.src.part.structure.plots;

namespace claims.src.gui.playerGui.structures.cellElements
{
    /// <summary>One plot another city offers to ours, as the market tab shows it.</summary>
    public class PlotMarketCellElement
    {
        public int X { get; set; }
        public int Z { get; set; }
        public string SellerCityName { get; set; } = "";
        public string PlotName { get; set; } = "";
        public int Price { get; set; }
        public EnumPlotSaleAudience Audience { get; set; } = EnumPlotSaleAudience.ALLIES;
        /// <summary>Whether our city may take this offer right now, decided by the server.</summary>
        public bool CanBuy { get; set; }

        public PlotMarketCellElement() { }

        public PlotMarketCellElement(Plot plot, bool canBuy)
        {
            X = plot.plotPosition.X;
            Z = plot.plotPosition.Z;
            SellerCityName = plot.hasCity() ? plot.getCity().GetPartName() : "";
            PlotName = plot.GetPartName();
            Price = plot.PriceForCityBuy;
            Audience = plot.SaleAudience;
            CanBuy = canBuy;
        }
    }
}
