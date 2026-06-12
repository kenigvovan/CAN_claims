using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiPages
{
    public static class SummonPage
    {
        public static void BuildSummonPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
            {
                compo.Compose();
                return;
            }
            var criminalsTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds.fixedWidth /= 2;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            currentBounds.fixedWidth = lineBounds.fixedWidth;

            currentBounds = currentBounds.BelowCopy(0, 0);
            ElementBounds createCityBounds = currentBounds.FlatCopy();
            int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

            ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 250).FixedUnder(topTextBounds, 5);
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

            invitationTextBounds.fixedHeight -= 50;
            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

            ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

            compo.AddStaticText(Lang.Get("claims:gui-summon-points-title"),
                CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                invitationTextBounds);
            if (clientInfo.CityInfo == null)
            {
                compo.Compose();
                return;
            }
            gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

            compo.BeginChildElements(invitationTextBounds)
                .BeginClip(clippingBounds)
                .AddInset(insetBounds, 3)
                .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                (SummonCellElement cell, ElementBounds bounds) =>
                {
                    return new GuiElementCitySummonCell(compo.Api, cell, bounds)
                    {
                        On = true
                    };
                },
                claims.clientDataStorage.clientPlayerInfo.CityInfo.SummonCells, "summoncellscells")
                .EndClip()
                .AddVerticalScrollbar((float value) =>
                {
                    ElementBounds bounds = compo.GetCellList<SummonCellElement>("summoncellscells").Bounds;
                    bounds.fixedY = (double)(0f - value);
                    bounds.CalcWorldBounds();
                }, scrollbarBounds, "scrollbar")
                .EndChildElements();
            compo.GetCellList<SummonCellElement>("summoncellscells").BeforeCalcBounds();

            compo.Compose();

            compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);
        }
    }
}
