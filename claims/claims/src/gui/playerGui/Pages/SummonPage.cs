using System;
using System.Collections.Generic;
using System.Globalization;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The city's summon points: where citizens can be teleported to, and what a jump costs them.
    /// </summary>
    public sealed class SummonPage : CANGuiPage
    {
        /// <summary>Height of the anchor the list is measured from.</summary>
        private const double HeadingHeight = 24;

        /// <summary>The list is never squeezed below this, however little room is left.</summary>
        private const double MinListHeight = 70;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var cells = Player.CityInfo.SummonCells;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = ctx.Line.fixedWidth;
            anchor.fixedHeight = HeadingHeight;

            var rows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-summon-label-points"),
                    Value = cells.Count.ToString(),
                    Key = "summonCount"
                },
                // What a jump costs was only ever visible on the prices tab, a page away from the
                // button that charges it.
                new CardRow
                {
                    Label = Lang.Get("claims:gui-teleportation-cost-label"),
                    Value = claims.config.SUMMON_PAYMENT.ToString("0.##", CultureInfo.InvariantCulture),
                    Key = "summonCost"
                }
            };

            double y = Card.Rows(compo, anchor, anchor.fixedY,
                Lang.Get("claims:gui-summon-points-title"), rows);

            // An empty list is a frame with nothing in it; say what a summon point is and where one
            // comes from instead.
            if (cells.Count == 0)
            {
                const double hintHeight = 60;
                ElementBounds inner = Card.Frame(compo, anchor, y,
                    Card.HeaderHeight + hintHeight + Card.Padding * 2,
                    Lang.Get("claims:gui-summon-section-empty"));

                compo.AddStaticText(Lang.Get("claims:gui-summon-description"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    inner.FlatCopy().WithFixedHeight(hintHeight), "summon-empty");
                return;
            }

            var listAnchor = anchor.FlatCopy();
            listAnchor.fixedY = y;

            // The list fills what is left of the window rather than a fixed reserve.
            var listOpts = new ScrollableListOptions { Key = "summon-cells", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - y - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:gui-summon-points-title"),
                cells,
                (SummonCellElement cell, ElementBounds bounds) => new GuiElementCitySummonCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            // What summon points are for, on the heading rather than as a paragraph.
            Tooltip.Add(compo, Lang.Get("claims:gui-summon-description"), list.Title, "tip-summondesc");

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
