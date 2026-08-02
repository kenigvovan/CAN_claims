using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One lot on the land auction: where it is, what it stands at, who leads, how long is left, and
    /// - when our city may bid - the button that does.
    /// </summary>
    public class GuiElementPlotAuctionCell : CANGuiElementCellBase
    {
        private readonly PlotAuctionCellElement lot;

        /// <summary>The bid button is the cell's own, so the row itself is not clickable.</summary>
        protected override bool UseHoverHighlights => false;

        private const double ButtonColumn = 60;
        private readonly double cellHeight;

        public GuiElementPlotAuctionCell(ICoreClientAPI capi, PlotAuctionCellElement lot, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.lot = lot;
            var font = CairoFont.WhiteDetailText();
            // A price tag has no countdown, so it is one line shorter.
            bool timed = lot.EndsAt > 0;
            cellHeight = timed ? 96 : 78;

            double textWidth = bounds.fixedWidth - 12 - ButtonColumn;
            if (textWidth < 120) textWidth = 120;

            ElementBounds row = ElementBounds.Fixed(12, 8, textWidth, 25).WithParent(Bounds);
            // A bankrupt city sold whole has no plot to point at, so it is named by the settlement.
            AddLine(capi, lot.IsWholeCity
                ? Lang.Get("claims:gui-auction-whole-city", lot.LotCityName)
                : (lot.PlotName?.Length > 0
                    ? lot.PlotName
                    : Lang.Get("claims:gui-plot-market-plot-at", lot.X, lot.Z)), 20, row);

            row = row.BelowCopy(0, 4);
            int plotSize = claims.config?.PLOT_SIZE ?? 1;
            int blockX = lot.X * plotSize + plotSize / 2;
            int blockZ = lot.Z * plotSize + plotSize / 2;
            AddLine(capi, lot.IsWholeCity
                ? Lang.Get("claims:gui-plot-market-seller", lot.SellerCityName)
                : Lang.Get("claims:gui-plot-market-seller", lot.SellerCityName)
                    + "  " + Lang.Get("claims:gui-plot-market-coords", blockX, blockZ, lot.X, lot.Z), 15, row);

            row = row.BelowCopy();
            if (timed)
            {
                string bidLine = lot.CurrentBid >= 0
                    ? Lang.Get("claims:gui-auction-current-bid", lot.CurrentBid, lot.LeaderCityName)
                    : Lang.Get("claims:gui-auction-no-bids", lot.MinNextBid);
                if (lot.BuyoutPrice > 0) bidLine += "  " + Lang.Get("claims:gui-auction-buyout", lot.BuyoutPrice);
                AddLine(capi, bidLine, 15, row);

                row = row.BelowCopy();
                // Counted down on the client from the closing stamp: a lot that re-sent itself every
                // second to tick a clock would push the market to every player each second.
                long left = lot.EndsAt - TimeFunctions.getEpochSeconds();
                AddLine(capi, left > 0
                    ? Lang.Get("claims:gui-auction-time-left", left / 3600, (left % 3600) / 60)
                    : Lang.Get("claims:gui-auction-closing"), 15, row);
            }
            else
            {
                AddLine(capi, Lang.Get("claims:gui-plot-market-price", lot.MinNextBid)
                            + "  " + Lang.Get(lot.Audience.LangKey()), 15, row);
            }

            // Same rule as the market cell: without remote deals the bid is placed on the plot
            // itself. A whole city is the exception - there is no plot to travel to, so the button
            // is the only way in and is always offered.
            if (lot.CanBid && (lot.IsWholeCity || claims.config?.CITY_PLOT_TRADE_REMOTE_BUY == true))
            {
                ElementBounds bidBounds = new ElementBounds().WithFixedSize(32, 32);
                bidBounds.fixedX = bounds.fixedWidth - 44;
                bidBounds.fixedY = (cellHeight - 32) / 2;
                bounds.WithChild(bidBounds);
                children.Add(new GuiElementToggleButton(capi, "claims:receive-money", "", font, (bool t) =>
                {
                    if (!t) return;
                    claims.CANCityGui.OpenDialog(
                        this.lot.IsWholeCity
                            ? EnumUpperWindowSelectedState.PLOT_AUCTION_BID_CITY
                            : EnumUpperWindowSelectedState.PLOT_AUCTION_BID,
                        dialogArgs =>
                        {
                            dialogArgs.Pos = new Vintagestory.API.MathTools.Vec3i(this.lot.X, 0, this.lot.Z);
                            dialogArgs.First = this.lot.IsWholeCity ? this.lot.LotCityName : this.lot.SellerCityName;
                            dialogArgs.Second = this.lot.MinNextBid.ToString();
                        });
                }, bidBounds));
                children.Add(new GuiElementHoverText(capi,
                    Lang.Get("claims:gui-auction-bid-tooltip", lot.MinNextBid), font, 250, bidBounds));
            }

            Bounds.fixedHeight = cellHeight;
        }

        private void AddLine(ICoreClientAPI capi, string line, int fontSize, ElementBounds bounds)
        {
            richTexts.Add(new GuiElementRichtext(capi,
                VtmlUtil.Richtextify(capi, line, CairoFont.WhiteMediumText().WithFontSize(fontSize)), bounds));
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
