using System;
using System.Collections.Generic;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>The city's plots drawn as a grid, with a legend of the types in use.</summary>
    public sealed class CityMapPage : CANGuiPage
    {
        /// <summary>Width the scrollbar needs beside the map, and the gap it keeps from it.</summary>
        private const double ScrollbarReserve = 27;

        /// <summary>One legend line, and how many of them share a row.</summary>
        private const double LegendRowHeight = 18;
        private const int LegendColumns = 2;

        /// <summary>The map viewport never shrinks below this, however long the legend gets.</summary>
        private const double MinMapHeight = 120;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = "claims:you_dont_have_city";
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            var column = anchor.FlatCopy();
            column.fixedWidth = ctx.Line.fixedWidth;

            double usable = Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction;

            var plots = Player.CityInfo.PlotsMap;
            if (plots == null || plots.Count == 0)
            {
                const double hintHeight = 40;
                ElementBounds emptyInner = Card.Frame(compo, column, column.fixedY,
                    Card.HeaderHeight + hintHeight + Card.Padding * 2,
                    Lang.Get("claims:gui-city-map-title"));

                compo.AddStaticText(Lang.Get("claims:gui-city-map-empty"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    emptyInner.FlatCopy().WithFixedHeight(hintHeight), "citymap-empty");

                BuildNav(ctx);
                return;
            }

            // The grid element sizes itself to the city; the viewport around it is what scrolls.
            var mapBounds = ElementBounds.Fixed(0, 0, 10, 10);
            var map = new GuiElementCityMap(compo.Api, plots, mapBounds);
            mapBounds.WithFixedSize(map.MapWidth, map.MapHeight);

            // The legend is measured first: it is what is left over that the map may fill, rather
            // than the map taking a fixed slice and the legend landing wherever it landed.
            var legend = LegendEntries(map);
            int legendRows = (legend.Count + LegendColumns - 1) / LegendColumns;
            double legendHeight = Card.HeaderHeight + legendRows * LegendRowHeight + Card.Padding * 2;

            double mapCardHeight = Math.Max(MinMapHeight + Card.HeaderHeight + Card.Padding * 2,
                usable - column.fixedY - legendHeight - Card.Gap * 2);

            // No taller than the city itself: a two-plot town in a full-height frame is mostly frame.
            mapCardHeight = Math.Min(mapCardHeight,
                map.MapHeight + Card.HeaderHeight + Card.Padding * 2 + 6);

            ElementBounds inner = Card.Frame(compo, column, column.fixedY, mapCardHeight,
                Lang.Get("claims:gui-city-map-title"));

            // The clip is added to the composer and everything else forks from it: a bounds that is
            // only forked from and never added has no parent, and the renderer dereferences that
            // parent while pushing the scissor.
            ElementBounds clipBounds = ElementBounds.Fixed(0, 0,
                inner.fixedWidth - ScrollbarReserve, inner.fixedHeight);
            ElementBounds scrollbarBounds = clipBounds.RightCopy(7).WithFixedWidth(20);
            ElementBounds containerBounds = clipBounds.FlatCopy();

            compo.BeginChildElements(inner)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "citymap-content")
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        ElementBounds bounds = compo.GetContainer("citymap-content").Bounds;
                        bounds.fixedY = 0 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "citymap-scrollbar")
                .EndChildElements();

            compo.GetContainer("citymap-content").Add(map);

            ctx.AfterCompose(() =>
                compo.GetScrollbar("citymap-scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)map.MapHeight));

            // --- legend ---
            ElementBounds legendInner = Card.Frame(compo, column,
                column.fixedY + mapCardHeight + Card.Gap, legendHeight,
                Lang.Get("claims:gui-city-map-legend"));

            double entryWidth = legendInner.fixedWidth / LegendColumns;

            for (int i = 0; i < legend.Count; i++)
            {
                var entryBounds = legendInner.FlatCopy().WithFixedSize(entryWidth - 6, LegendRowHeight);
                entryBounds.fixedX += (i % LegendColumns) * entryWidth;
                entryBounds.fixedY += (i / LegendColumns) * LegendRowHeight;

                // Each type named in its own colour on the map: the legend used to be plain white
                // text, so it said which types exist but not which square was which.
                compo.AddStaticText(legend[i].Label,
                    CairoFont.WhiteDetailText().WithColor(legend[i].Color), entryBounds, "citymap-legend-" + i);
            }

            BuildNav(ctx);
        }

        private sealed class LegendEntry
        {
            public string Label;
            public double[] Color;
        }

        /// <summary>The plot types actually present, plus the gaps between claims.</summary>
        private static List<LegendEntry> LegendEntries(GuiElementCityMap map)
        {
            var entries = new List<LegendEntry>();

            foreach (var kv in GuiElementCityMap.Colors)
            {
                if (!map.TypeCounts.TryGetValue(kv.Key, out int count)) continue;

                entries.Add(new LegendEntry
                {
                    Label = GuiElementCityMap.TypeName(kv.Key) + " (" + count + ")",
                    Color = kv.Value
                });
            }

            entries.Add(new LegendEntry
            {
                Label = Lang.Get("claims:gui-city-map-gap"),
                Color = ClaimsColors.Label
            });

            return entries;
        }

        private void BuildNav(PageBuildContext ctx)
        {
            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
        }
    }
}
