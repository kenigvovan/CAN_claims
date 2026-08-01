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

            var listings = Player.CityInfo.PlotMarket ?? new List<PlotMarketCellElement>();

            // Cheapest first: the market is browsed to find what our treasury can afford.
            var sorted = listings.OrderBy(l => l.Price).ThenBy(l => l.SellerCityName).ToList();

            var listOpts = new ScrollableListOptions { Key = "plot-market", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                System.Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - column.fixedY - Card.Gap - ScrollableList.Overhead(column, listOpts)));

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

            var list = ScrollableList.Add(Gui, column,
                Lang.Get("claims:gui-plot-market-title") + " (" + sorted.Count + ")",
                sorted,
                (PlotMarketCellElement cell, ElementBounds bounds) => new GuiElementPlotMarketCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            BuildNav(ctx, column);

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        private void BuildNav(PageBuildContext ctx, ElementBounds column)
        {
            NavRow.Build(Gui, column, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")),
                new NavButton("claims:files", () => GoTo(EnumSelectedTab.PlotMarketHistory), Lang.Get("claims:gui-plot-market-history-title")));
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
