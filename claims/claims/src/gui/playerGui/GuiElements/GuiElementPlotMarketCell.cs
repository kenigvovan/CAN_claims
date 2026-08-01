using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One offer on the inter-city plot market: who sells, where it is, what it costs, and - when
    /// our city may take it - the button that does.
    /// </summary>
    public class GuiElementPlotMarketCell : CANGuiElementCellBase
    {
        private readonly PlotMarketCellElement listing;

        /// <summary>The buy button is the cell's own, so the row itself is not clickable.</summary>
        protected override bool UseHoverHighlights => false;

        private const double ButtonColumn = 60;
        private const double CellHeight = 78;

        public GuiElementPlotMarketCell(ICoreClientAPI capi, PlotMarketCellElement listing, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.listing = listing;
            var font = CairoFont.WhiteDetailText();

            double textWidth = bounds.fixedWidth - 12 - ButtonColumn;
            if (textWidth < 120) textWidth = 120;

            ElementBounds row = ElementBounds.Fixed(12, 8, textWidth, 25).WithParent(Bounds);
            AddLine(capi, listing.PlotName?.Length > 0
                ? listing.PlotName
                : Lang.Get("claims:gui-plot-market-plot-at", listing.X, listing.Z), 20, row);

            row = row.BelowCopy(0, 4);
            // Block coordinates of the plot centre, because that is what a player reads off their
            // own position; the plot coordinates follow in brackets, since those are what the buy
            // command takes.
            int plotSize = claims.config?.PLOT_SIZE ?? 1;
            int blockX = listing.X * plotSize + plotSize / 2;
            int blockZ = listing.Z * plotSize + plotSize / 2;
            AddLine(capi, Lang.Get("claims:gui-plot-market-seller", listing.SellerCityName)
                        + "  " + Lang.Get("claims:gui-plot-market-coords", blockX, blockZ, listing.X, listing.Z), 15, row);

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-plot-market-price", listing.Price)
                        + "  " + Lang.Get(listing.Audience.LangKey()), 15, row);

            // Without remote buying the deal is closed on the spot, from the plot page - a button here
            // could only ever answer "come to the plot", so it is not offered.
            if (listing.CanBuy && claims.config?.CITY_PLOT_TRADE_REMOTE_BUY == true)
            {
                ElementBounds buyBounds = new ElementBounds().WithFixedSize(32, 32);
                buyBounds.fixedX = bounds.fixedWidth - 44;
                buyBounds.fixedY = (CellHeight - 32) / 2;
                bounds.WithChild(buyBounds);
                children.Add(new GuiElementToggleButton(capi, "claims:receive-money", "", font, (bool t) =>
                {
                    if (!t) return;
                    // The confirm dialog needs the plot it acts on and the price it is about to spend.
                    claims.CANCityGui.OpenDialog(EnumUpperWindowSelectedState.PLOT_MARKET_BUY_CONFIRM, dialogArgs =>
                    {
                        dialogArgs.Pos = new Vintagestory.API.MathTools.Vec3i(this.listing.X, 0, this.listing.Z);
                        dialogArgs.First = this.listing.SellerCityName;
                        dialogArgs.Second = this.listing.Price.ToString();
                    });
                }, buyBounds));
                children.Add(new GuiElementHoverText(capi, Lang.Get("claims:gui-plot-market-buy-tooltip"), font, 250, buyBounds));
            }

            Bounds.fixedHeight = CellHeight;
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
