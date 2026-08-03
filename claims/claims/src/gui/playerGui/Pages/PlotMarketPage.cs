using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>Plots other cities are offering to ours, with the deal our city can take.</summary>
    public sealed class PlotMarketPage : CANGuiPage
    {
        private const double MinListHeight = 70;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            if (claims.config?.CITY_PLOT_TRADE_ENABLED != true || claims.config?.CITY_PLOT_TRADE_GUI != true)
            {
                reasonLangKey = "claims:plot_trade_disabled";
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

            // Price tags only - the same list holds timed lots, which have their own tab.
            var listings = (Player.CityInfo.PlotAuctions ?? new List<PlotAuctionCellElement>())
                .Where(l => l.EndsAt <= 0);

            // Cheapest first: the market is browsed to find what our treasury can afford.
            var sorted = listings.OrderBy(l => l.MinNextBid).ThenBy(l => l.SellerCityName).ToList();

            if (sorted.Count == 0)
            {
                const double hintHeight = 40;
                ElementBounds emptyInner = Card.Frame(compo, column, column.fixedY,
                    Card.HeaderHeight + hintHeight + Card.Padding * 2,
                    Lang.Get("claims:gui-plot-market-title"));

                compo.AddStaticText(Lang.Get("claims:gui-plot-market-empty"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    emptyInner.FlatCopy().WithFixedHeight(hintHeight), "plotmarket-empty");

                BuildNav(ctx, column);
                return;
            }

            // A summary before the rows: how many offers stand, and what the cheapest of them costs -
            // the two questions a mayor opens this tab with.
            double y = Card.Rows(compo, column, column.fixedY, Lang.Get("claims:gui-plot-market-title"),
                SummaryRows(sorted));

            var listAnchor = column.FlatCopy();
            listAnchor.fixedY = y;

            var listOpts = new ScrollableListOptions { Key = "plot-market", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                System.Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - listAnchor.fixedY - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:gui-plot-market-offers") + " (" + sorted.Count + ")",
                sorted,
                (PlotAuctionCellElement cell, ElementBounds bounds) => new GuiElementPlotAuctionCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            BuildNav(ctx, column);

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        /// <summary>What the offers add up to: how many there are and what the cheapest one asks.</summary>
        private List<CardRow> SummaryRows(List<PlotAuctionCellElement> listings)
        {
            long cheapest = listings.Min(l => l.MinNextBid);
            int ours = listings.Count(l => l.IsOurs);

            var rows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-market-label-offers"),
                    Value = listings.Count.ToString(),
                    Key = "market-count"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-market-label-cheapest"),
                    Value = cheapest.ToString(),
                    ValueColor = ClaimsColors.Success,
                    Key = "market-cheapest"
                }
            };

            // Only when there is something to say - a row reading "0" every visit is noise.
            if (ours > 0)
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-auction-label-ours"),
                    Value = ours.ToString(),
                    ValueColor = ClaimsColors.Label,
                    Key = "market-ours"
                });
            }
            return rows;
        }

        private void BuildNav(PageBuildContext ctx, ElementBounds column)
        {
            var buttons = new List<NavButton>
            {
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")),
                new NavButton("claims:files", () => GoTo(EnumSelectedTab.PlotMarketHistory), Lang.Get("claims:gui-plot-market-history-title"))
            };
            // The bidding tab only exists when the host runs auctions at all.
            if (claims.config?.CITY_PLOT_AUCTION_ENABLED == true)
            {
                buttons.Add(new NavButton("claims:receive-money", () => GoTo(EnumSelectedTab.PlotAuction),
                    Lang.Get("claims:gui-plot-auction-title")));
            }
            NavRow.Build(Gui, column, ctx.Line, 15, buttons.ToArray());
        }
    }

    /// <summary>Inter-city plot deals our city took part in, newest first.</summary>
    public sealed class PlotMarketHistoryPage : CANGuiPage
    {
        /// <summary>One entry line, matching the city log's row height.</summary>
        private const double RowHeight = 22;
        private const double ScrollbarReserve = 27;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            if (claims.config?.CITY_PLOT_TRADE_ENABLED != true || claims.config?.CITY_PLOT_TRADE_GUI != true)
            {
                reasonLangKey = "claims:plot_trade_disabled";
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

            double cardHeight = System.Math.Max(120,
                Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction - column.fixedY - Card.Gap);

            var history = Player.CityInfo.PlotMarketHistory ?? new List<PlotSaleRecord>();

            ElementBounds inner = Card.Frame(compo, column, column.fixedY, cardHeight,
                Lang.Get("claims:gui-plot-market-history-title"));

            if (history.Count == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-plot-market-history-empty"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    inner.FlatCopy().WithFixedHeight(24), "plotmarket-history-empty");

                BuildNav(ctx, column);
                return;
            }

            ElementBounds clipBounds = ElementBounds.Fixed(0, 0,
                inner.fixedWidth - ScrollbarReserve, inner.fixedHeight);
            ElementBounds scrollbarBounds = clipBounds.RightCopy(7).WithFixedWidth(20);
            ElementBounds containerBounds = clipBounds.FlatCopy();

            compo.BeginChildElements(inner)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "plotmarket-history-content")
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        ElementBounds bounds = compo.GetContainer("plotmarket-history-content").Bounds;
                        bounds.fixedY = 5 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "plotmarket-history-scrollbar")
                .EndChildElements();

            var stampFont = CairoFont.WhiteDetailText().WithColor(ClaimsColors.Label);
            ElementBounds rowBounds = ElementBounds.Fixed(0, 0, clipBounds.fixedWidth, RowHeight);
            GuiElementContainer scrollArea = compo.GetContainer("plotmarket-history-content");

            string ourCity = Player.CityInfo.Name;
            // Block coordinates of the plot centre, matching the market tab - a deal is looked up on
            // the map by where it happened, not by plot indices nobody navigates with.
            int plotSize = claims.config?.PLOT_SIZE ?? 1;
            foreach (var record in history)
            {
                bool weBought = record.BuyerName == ourCity;
                int blockX = record.X * plotSize + plotSize / 2;
                int blockZ = record.Z * plotSize + plotSize / 2;
                string line = weBought
                    ? Lang.Get("claims:gui-plot-market-history-bought", record.SellerName, blockX, blockZ, record.Price)
                    : Lang.Get("claims:gui-plot-market-history-sold", record.BuyerName, blockX, blockZ, record.Price);

                var stamp = VtmlUtil.Richtextify(compo.Api,
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(record.TimeStamp) + "  ", stampFont);
                var text = VtmlUtil.Richtextify(compo.Api, line,
                    CairoFont.WhiteDetailText().WithColor(weBought ? ClaimsColors.Success : ClaimsColors.Warning));

                scrollArea.Add(new GuiElementRichtext(compo.Api, stamp.Concat(text).ToArray(), rowBounds));
                rowBounds = rowBounds.BelowCopy();
            }

            int entryCount = history.Count;
            ctx.AfterCompose(() =>
                compo.GetScrollbar("plotmarket-history-scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)(RowHeight * entryCount)));

            BuildNav(ctx, column);
        }

        private void BuildNav(PageBuildContext ctx, ElementBounds column)
        {
            NavRow.Build(Gui, column, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.PlotMarket), Lang.Get("claims:gui-nav-back")));
        }
    }
}
