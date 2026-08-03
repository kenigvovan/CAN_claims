using System.Collections.Generic;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One offer of land, as both the price-tag market and the auction tab draw it: the seller's arms,
    /// what is on offer, what it costs and how long is left, then the button that takes it.
    ///
    /// Laid out like the city and alliance rows - arms column, heading, caption/value lines in the
    /// shared palette - so a market row does not read as a different program from the rest of the
    /// window.
    /// </summary>
    public class GuiElementPlotAuctionCell : CANGuiElementCellBase
    {
        private readonly PlotAuctionCellElement lot;

        /// <summary>The bid button is the cell's own, so the row itself is not clickable.</summary>
        protected override bool UseHoverHighlights => false;

        /// <summary>Right edge kept clear for the bid button.</summary>
        private const double ButtonColumn = 60;

        private const double TopPadding = 8;
        private const double TitleHeight = 25;
        private const double RowHeight = 19;
        private const double LabelWidth = 96;

        /// <summary>One caption/value line of the row.</summary>
        private struct Line
        {
            public string Label;
            public string Value;
            public double[] Color;
        }

        public GuiElementPlotAuctionCell(ICoreClientAPI capi, PlotAuctionCellElement lot, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.lot = lot;
            var font = CairoFont.WhiteDetailText();
            bool timed = lot.EndsAt > 0;

            List<Line> lines = BuildLines(timed);
            double cellHeight = TopPadding * 2 + TitleHeight + 4 + lines.Count * RowHeight;

            double textWidth = bounds.fixedWidth - EmblemTextX - ButtonColumn;
            if (textWidth < 120) textWidth = 120;

            // A bankrupt settlement sold whole has no plot to point at, so it is named by the city.
            ElementBounds row = ElementBounds.Fixed(EmblemTextX, TopPadding, textWidth, TitleHeight).WithParent(Bounds);
            AddTitle(lot.IsWholeCity
                ? Lang.Get("claims:gui-auction-whole-city", lot.LotCityName)
                : (lot.PlotName?.Length > 0
                    ? lot.PlotName
                    : Lang.Get("claims:gui-plot-market-plot-at", lot.X, lot.Z)), row, 20);

            row = row.BelowCopy(0, 4).WithFixedHeight(RowHeight);
            foreach (Line line in lines)
            {
                AddLabelValue(line.Label, line.Value, row, LabelWidth, line.Color);
                row = row.BelowCopy();
            }

            // Whose land it is - the settlement itself when the whole of it is on the block. Resolved
            // by guid from the world-wide emblem cache, as the city and alliance lists do it.
            string emblemOwner = lot.IsWholeCity ? lot.LotCityGuid : lot.SellerCityGuid;
            AddEmblemColumn(capi, claims.clientDataStorage?.ClientGetEmblem(emblemOwner) ?? "", cellHeight);

            // Same rule as before: without remote deals the offer is taken on the plot itself. A whole
            // city is the exception - there is no plot to travel to, so the button is the only way in.
            if (lot.CanBid && (lot.IsWholeCity || claims.config?.CITY_PLOT_TRADE_REMOTE_BUY == true))
            {
                ElementBounds bidBounds = new ElementBounds().WithFixedSize(32, 32);
                bidBounds.fixedX = bounds.fixedWidth - 44;
                bidBounds.fixedY = (cellHeight - 32) / 2;
                bounds.WithChild(bidBounds);
                children.Add(new GuiElementToggleButton(capi, "claims:receive-money", "", font, (bool t) =>
                {
                    if (!t) return;
                    // A price tag is taken at its price - asking for a number would be asking the
                    // buyer to retype what the row already says. Bidding is the timed lots' dialog.
                    claims.CANCityGui.OpenDialog(
                        this.lot.IsWholeCity
                            ? EnumUpperWindowSelectedState.PLOT_AUCTION_BID_CITY
                            : (timed
                                ? EnumUpperWindowSelectedState.PLOT_AUCTION_BID
                                : EnumUpperWindowSelectedState.PLOT_MARKET_BUY_CONFIRM),
                        dialogArgs =>
                        {
                            dialogArgs.Pos = new Vintagestory.API.MathTools.Vec3i(this.lot.X, 0, this.lot.Z);
                            dialogArgs.First = this.lot.IsWholeCity ? this.lot.LotCityName : this.lot.SellerCityName;
                            dialogArgs.Second = this.lot.MinNextBid.ToString();
                        });
                }, bidBounds));
                AddTooltip(bidBounds, timed && !lot.IsWholeCity
                    ? Lang.Get("claims:gui-auction-bid-tooltip", lot.MinNextBid)
                    : Lang.Get("claims:gui-plot-market-buy-tooltip"));
            }

            Bounds.fixedHeight = cellHeight;
        }

        /// <summary>
        /// The lines under the heading. A price tag and a running lot describe themselves differently
        /// - one has a price, the other a standing bid and a clock - so the row grows to what it has
        /// to say instead of leaving a blank line where a countdown would be.
        /// </summary>
        private List<Line> BuildLines(bool timed)
        {
            var lines = new List<Line>();

            lines.Add(new Line
            {
                Label = Lang.Get("claims:gui-auction-label-seller"),
                Value = lot.IsOurs
                    ? lot.SellerCityName + "  " + Lang.Get("claims:gui-auction-yours")
                    : lot.SellerCityName,
                Color = lot.IsOurs ? ClaimsColors.Label : null
            });

            if (!lot.IsWholeCity)
            {
                int plotSize = claims.config?.PLOT_SIZE ?? 1;
                lines.Add(new Line
                {
                    Label = Lang.Get("claims:gui-auction-label-where"),
                    // Block coordinates first: that is what a player navigates by.
                    Value = Lang.Get("claims:gui-plot-market-coords",
                        lot.X * plotSize + plotSize / 2, lot.Z * plotSize + plotSize / 2, lot.X, lot.Z),
                    Color = ClaimsColors.Label
                });
            }

            if (timed)
            {
                lines.Add(new Line
                {
                    Label = Lang.Get("claims:gui-auction-label-bid"),
                    Value = lot.CurrentBid >= 0
                        ? Lang.Get("claims:gui-auction-bid-value", lot.CurrentBid, lot.LeaderCityName)
                        : Lang.Get("claims:gui-auction-no-bids", lot.MinNextBid),
                    Color = lot.CurrentBid >= 0 ? ClaimsColors.Success : ClaimsColors.Label
                });

                if (lot.BuyoutPrice > 0)
                {
                    lines.Add(new Line
                    {
                        Label = Lang.Get("claims:gui-auction-buyout-label"),
                        Value = lot.BuyoutPrice.ToString(),
                    });
                }

                // Counted down on the client from the closing stamp: a lot that re-sent itself every
                // second to tick a clock would push the market to every player each second.
                long left = lot.EndsAt - TimeFunctions.getEpochSeconds();
                lines.Add(new Line
                {
                    Label = Lang.Get("claims:gui-auction-label-left"),
                    Value = left > 0
                        ? Lang.Get("claims:gui-auction-left-value", left / 3600, (left % 3600) / 60)
                        : Lang.Get("claims:gui-auction-closing"),
                    // The last hour is the one worth noticing, and the last minutes are urgent.
                    Color = left <= 0 ? ClaimsColors.Danger
                          : left < 3600 ? ClaimsColors.Warning
                          : null
                });
            }
            else
            {
                lines.Add(new Line
                {
                    Label = Lang.Get("claims:gui-auction-label-price"),
                    Value = lot.MinNextBid.ToString(),
                    Color = ClaimsColors.Success
                });
            }

            // Who the offer is open to matters to whoever wrote it: a buyer reading the row is by
            // definition already among them, and the line would only make every row taller.
            if (lot.IsOurs)
            {
                lines.Add(new Line
                {
                    Label = Lang.Get("claims:gui-plot-label-city-audience"),
                    Value = Lang.Get(lot.Audience.LangKey()),
                    Color = ClaimsColors.Label
                });
            }

            return lines;
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
