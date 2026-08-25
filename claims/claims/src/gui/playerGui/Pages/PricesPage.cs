using System.Collections.Generic;
using System.Globalization;
using claims.src.gui.playerGui.Widgets;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Every number the server charges, in two columns of cards: what things cost on the left, what
    /// they cost you when they go wrong on the right.
    /// </summary>
    public sealed class PricesPage : CANGuiPage
    {
        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // Anchored under the separator rather than under the tab row: the caption sat on top of
            // the line, because the tab row's own height does not reach it.
            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            double columnWidth = (ctx.Line.fixedWidth - Card.ColumnGap) / 2;

            anchor = AddCoins(compo, anchor, ctx.Line.fixedWidth);

            // The two columns are balanced by row count, not by topic: the plot type list alone is
            // longer than everything else on the left, and stacking it under the costs card ran the
            // column past the bottom of the window.

            // --- left column: the plot types, then what captivity costs ---
            var left = anchor.FlatCopy();
            left.fixedWidth = columnWidth;
            double y = left.fixedY;

            var plotRows = new List<CardRow>();
            if (PlotInfo.dictPlotTypes != null)
            {
                foreach (var entry in PlotInfo.dictPlotTypes)
                {
                    string name = entry.Value.getFullName();
                    plotRows.Add(new CardRow
                    {
                        // The code name is the lang key, not the caption - the list used to read
                        // "default, mainplot, orchard" regardless of the player's language.
                        Label = Lang.Get("claims:gui-plot-type-" + name),
                        Value = Number(entry.Value.getCost()),
                        Tooltip = Lang.Get("claims:gui-plot-type-desc-" + name),
                        Key = "plottype-" + name
                    });
                }
            }
            y = Card.Rows(compo, left, y, Lang.Get("claims:gui-prices-plot-types-header"), plotRows);

            Card.Rows(compo, left, y, Lang.Get("claims:gui-prices-ransom-header"), new List<CardRow>
            {
                Row("claims:gui-prices-ransom-no-citizen", claims.config.RANSOM_FOR_NO_CITIZEN),
                Row("claims:gui-prices-ransom-citizen", claims.config.RANSOM_FOR_CITIZEN),
                Row("claims:gui-prices-ransom-mayor", claims.config.RANSOM_FOR_MAYOR),
                Row("claims:gui-prices-ransom-leader", claims.config.RANSOM_FOR_LEADER),
                Row("claims:gui-prices-ransom-chief", claims.config.RANSOM_FOR_CHIEF),
            });

            // --- right column: what things cost, and the limits around them ---
            var right = anchor.FlatCopy();
            right.fixedWidth = columnWidth;
            right.fixedX += columnWidth + Card.ColumnGap;
            y = right.fixedY;

            y = Card.Rows(compo, right, y, Lang.Get("claims:gui-prices-costs-header"), new List<CardRow>
            {
                Row("claims:gui-new-city-cost-label", claims.config.NEW_CITY_COST),
                Row("claims:gui-city-plot-cost-label", claims.config.PLOT_CLAIM_PRICE),
                Row("claims:gui-city-name-change-cost-label", claims.config.CITY_NAME_CHANGE_COST),
                Row("claims:gui-city-base-cost-label", claims.config.CITY_BASE_CARE),
                Row("claims:gui-teleportation-cost-label", claims.config.SUMMON_PAYMENT),
                Row("claims:gui-new-alliance-cost-label", claims.config.NEW_ALLIANCE_COST),
                Row("claims:gui-prices-outpost-cost", claims.config.OUTPOST_PLOT_COST),
                Row("claims:gui-prices-extra-plot-cost", claims.config.EXTRA_PLOT_COST),
                Row("claims:gui-prices-no-pvp-flag-cost", claims.config.PLOT_NO_PVP_FLAG_COST),
                Row("claims:gui-prices-no-mobspawn-flag-cost", claims.config.PLOT_NO_MOBSPAWN_FLAG_COST),
            });

            y = Card.Rows(compo, right, y, Lang.Get("claims:gui-prices-alliance-header"), new List<CardRow>
            {
                Row("claims:gui-prices-alliance-rename", claims.config.ALLIANCE_RENAME_COST),
                Row("claims:gui-prices-alliance-base-care", claims.config.ALLIANCE_BASE_CARE),
                Row("claims:gui-prices-alliance-max-fee", claims.config.ALLIANCE_MAX_FEE),
                Row("claims:gui-prices-neutral-alliance", claims.config.NEUTRAL_ALLANCE_PAYMENT),
                Row("claims:gui-prices-neutral-city", claims.config.NEUTRAL_CITY_PAYMENT),
            });

            Card.Rows(compo, right, y, Lang.Get("claims:gui-prices-city-limits-header"), new List<CardRow>
            {
                Row("claims:gui-prices-max-city-fee", claims.config.MAX_CITY_FEE),
                Row("claims:gui-prices-city-max-debt", claims.config.CITY_MAX_DEBT),
            });
        }

        private static CardRow Row(string labelLangKey, double value) => new CardRow
        {
            Label = Lang.Get(labelLangKey),
            Value = Number(value),
            Key = labelLangKey
        };

        /// <summary>
        /// The coins the server accepts, with the value of each one. A caption and one row of icons
        /// rather than a card of its own: the two columns below need every pixel of height.
        /// Returns where those columns start.
        /// </summary>
        private static ElementBounds AddCoins(GuiComposer compo, ElementBounds anchor, double fullWidth)
        {
            var denominations = claims.config.COIN_DENOMINATIONS;
            if (denominations == null || denominations.Count == 0) return anchor;

            const double coinRowHeight = 30;
            const double coinIconSize = 28;

            var caption = anchor.FlatCopy().WithFixedSize(fullWidth, 20);
            compo.AddStaticText(Lang.Get("claims:gui-currency-item"),
                CairoFont.WhiteSmallishText().WithColor(ClaimsColors.Section), caption, "currency-item");

            var coinBounds = caption.BelowCopy(0, 2).WithFixedSize(coinIconSize, coinRowHeight);

            foreach (var coinInfo in denominations)
            {
                ItemStack coin = new ItemStack(compo.Api.World.GetItem(new AssetLocation(coinInfo.CollectibleCode)), 1);
                var attributes = coinInfo.ToTreeAttribute();
                if (attributes != null) coin.Attributes = attributes;

                var stack = new ItemstackTextComponent(compo.Api, coin, coinIconSize);
                // Coins can share a code but differ by value/attributes, so key on both.
                compo.AddRichtext(new RichTextComponentBase[] { stack }, coinBounds,
                    "coin-item" + coinInfo.CollectibleCode + coinInfo.Value);

                // The number is what the player is actually after; the icon alone says nothing.
                var valueBounds = coinBounds.RightCopy(2).WithFixedSize(46, coinRowHeight);
                valueBounds.fixedY += 7;
                compo.AddStaticText(Number(coinInfo.Value),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Value),
                    valueBounds, "coin-value" + coinInfo.CollectibleCode + coinInfo.Value);

                coinBounds = valueBounds.RightCopy(12).WithFixedSize(coinIconSize, coinRowHeight);
                coinBounds.fixedY -= 7;
            }

            var below = anchor.FlatCopy();
            below.fixedY += 20 + 2 + coinRowHeight + Card.Gap;
            return below;
        }

        /// <summary>Costs are doubles; whole numbers should not read "1500.0".</summary>
        private static string Number(double value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>Coin values are decimals, and their fractions matter, so they keep more places.</summary>
        private static string Number(decimal value) =>
            value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
