using System;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>The city's plots drawn as a grid, with a legend of the types in use.</summary>
    public sealed class CityMapPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = "claims:you_dont_have_city";
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var currentBounds = ctx.Current.BelowCopy(0, 20);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.CenterTop);

            compo.AddStaticText(Lang.Get("claims:gui-city-map-title"), ClaimsFonts.ListHeader, currentBounds);

            var plots = Player.CityInfo.PlotsMap;
            if (plots == null || plots.Count == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-city-map-empty"),
                    ClaimsFonts.ListHeader, currentBounds.BelowCopy(0, 20));
                NavRow.Build(Gui, currentBounds, ctx.Line, 15,
                    new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
                return;
            }

            // The grid element sizes itself to the city; the viewport around it is what scrolls.
            var mapBounds = ElementBounds.Fixed(0, 0, 10, 10);
            var map = new GuiElementCityMap(compo.Api, plots, mapBounds);
            mapBounds.WithFixedSize(map.MapWidth, map.MapHeight);

            double viewWidth = ctx.Line.fixedWidth - 40;
            double viewHeight = Math.Min(map.MapHeight + 10, Gui.mainBounds.fixedHeight - 330);

            var areaBounds = ElementBounds.Fixed(15, 0, viewWidth, viewHeight).FixedUnder(currentBounds, 15);
            ElementBounds insetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, viewWidth, viewHeight);
            // 7px clear of the inset, the same gap the shared list widget uses.
            ElementBounds scrollbarBounds = insetBounds.RightCopy(7).WithFixedWidth(20);
            ElementBounds clipBounds = insetBounds.ForkContainingChild(3, 3, 3, 3);
            ElementBounds containerBounds = clipBounds.ForkContainingChild(0, 0, 0, 0);

            compo.BeginChildElements(areaBounds)
                .AddInset(insetBounds, 3)
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
            var legendBounds = areaBounds.BelowCopy(0, 12);
            legendBounds.fixedX = 15;
            legendBounds.WithFixedSize(ctx.Line.fixedWidth - 40, 20);
            compo.AddStaticText(Lang.Get("claims:gui-city-map-legend"), CairoFont.WhiteDetailText(), legendBounds);

            var entryBounds = legendBounds.BelowCopy(0, 6).WithFixedSize(150, 20);
            int column = 0;
            foreach (var kv in GuiElementCityMap.Colors)
            {
                if (!map.TypeCounts.TryGetValue(kv.Key, out int count)) continue;

                AddLegendEntry(compo, entryBounds, GuiElementCityMap.TypeName(kv.Key) + " (" + count + ")");
                entryBounds = NextLegendSlot(entryBounds, legendBounds, ref column);
            }

            AddLegendEntry(compo, entryBounds, Lang.Get("claims:gui-city-map-gap"));

            NavRow.Build(Gui, currentBounds, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
        }

        private static void AddLegendEntry(GuiComposer compo, ElementBounds bounds, string label)
        {
            compo.AddStaticText(label, CairoFont.WhiteDetailText(), bounds);
        }

        /// <summary>Legend entries run two to a row.</summary>
        private static ElementBounds NextLegendSlot(ElementBounds current, ElementBounds rowStart, ref int column)
        {
            column++;
            if (column % 2 == 0)
            {
                var next = current.BelowCopy(0, 2);
                next.fixedX = rowStart.fixedX;
                return next;
            }
            return current.RightCopy(10);
        }
    }
}
