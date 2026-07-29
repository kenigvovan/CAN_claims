using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class SummonPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var currentBounds = ctx.Current;
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            currentBounds = currentBounds.BelowCopy(0, 0);

            // An empty list is just a frame with nothing in it; say so, and say what to do about it.
            if (Player.CityInfo.SummonCells.Count == 0)
            {
                var emptyBounds = ctx.Line.BelowCopy(0, 20).WithFixedHeight(24);
                emptyBounds.Alignment = EnumDialogArea.LeftTop;
                emptyBounds.fixedWidth = ctx.Line.fixedWidth;

                compo.AddStaticText(Lang.Get("claims:gui-summon-points-title"),
                    CairoFont.WhiteSmallishText().WithColor(ClaimsColors.Section), emptyBounds, "summon-title");

                var hintBounds = emptyBounds.BelowCopy(0, 6).WithFixedHeight(60);
                compo.AddStaticText(Lang.Get("claims:gui-summon-description"),
                    CairoFont.WhiteDetailText().WithColor(ClaimsColors.Label), hintBounds, "summon-empty");
                return;
            }

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:gui-summon-points-title"),
                Player.CityInfo.SummonCells,
                (SummonCellElement cell, ElementBounds bounds) => new GuiElementCitySummonCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "summon-cells", HeightReserve = 250 });

            // What summon points are for, on the heading rather than as a paragraph.
            Tooltip.Add(compo, Lang.Get("claims:gui-summon-description"), list.Title, "tip-summondesc");

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
