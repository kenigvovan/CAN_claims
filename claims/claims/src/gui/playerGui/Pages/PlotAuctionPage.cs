using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>Land put up for bids - lots our city may bid on, plus the ones it opened itself.</summary>
    public sealed class PlotAuctionPage : CANGuiPage
    {
        private const double MinListHeight = 70;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            if (claims.config?.CITY_PLOT_TRADE_ENABLED != true || claims.config?.CITY_PLOT_TRADE_GUI != true
                || claims.config?.CITY_PLOT_AUCTION_ENABLED != true)
            {
                reasonLangKey = "claims:plot_auction_disabled";
                return false;
            }
            reasonLangKey = "claims:you_dont_have_city";
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var column = ctx.Line.BelowCopy(0, 14);
            column.Alignment = EnumDialogArea.LeftTop;
            column.fixedWidth = ctx.Line.fixedWidth;

            // Timed lots only - a plain price tag belongs to the market tab.
            var lots = (Player.CityInfo.PlotAuctions ?? new List<PlotAuctionCellElement>())
                .Where(l => l.EndsAt > 0);

            // Closing first: a lot about to end is the one a bidder has to act on.
            var sorted = lots.OrderBy(l => l.EndsAt).ThenBy(l => l.SellerCityName).ToList();

            var listOpts = new ScrollableListOptions { Key = "plot-auction", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                System.Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - column.fixedY - Card.Gap - ScrollableList.Overhead(column, listOpts)));

            if (sorted.Count == 0)
            {
                const double hintHeight = 40;
                ElementBounds emptyInner = Card.Frame(compo, column, column.fixedY,
                    Card.HeaderHeight + hintHeight + Card.Padding * 2,
                    Lang.Get("claims:gui-plot-auction-title"));

                compo.AddStaticText(Lang.Get("claims:gui-plot-auction-empty"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    emptyInner.FlatCopy().WithFixedHeight(hintHeight), "plotauction-empty");

                BuildNav(ctx, column);
                return;
            }

            var list = ScrollableList.Add(Gui, column,
                Lang.Get("claims:gui-plot-auction-title") + " (" + sorted.Count + ")",
                sorted,
                (PlotAuctionCellElement cell, ElementBounds bounds) => new GuiElementPlotAuctionCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            BuildNav(ctx, column);

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        private void BuildNav(PageBuildContext ctx, ElementBounds column)
        {
            NavRow.Build(Gui, column, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.PlotMarket), Lang.Get("claims:gui-nav-back")));
        }
    }
}
