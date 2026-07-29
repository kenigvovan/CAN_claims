using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Pending union offers and dissolution requests, with buttons to propose a union or to leave
    /// one.
    /// </summary>
    public sealed class UnionLettersPage : CANGuiPage
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
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds = currentBounds.BelowCopy(0, 0);

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:union_letters_list"),
                Player.CityInfo.ClientUnionLetterCellElements,
                (ClientUnionLetterCellElement cell, ElementBounds bounds) => new GuiElementUnionLetterCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "union-letters", HeightReserve = 350 });

            ElementBounds actionBounds = list.Inset.BelowCopy(15, 15);
            actionBounds.WithFixedWidth(25).WithFixedHeight(25);
            compo.AddInset(actionBounds);
            var proposeBounds = actionBounds;
            compo.AddIconButton("claims:tower-flag", (bool t) =>
            {
                if (t) OpenDialog(EnumUpperWindowSelectedState.ALLIANCE_SEND_NEW_UNION_LETTER_NEED_NAME);
            }, proposeBounds);
            compo.AddHoverText(Lang.Get("claims:gui-send-new-union-letter"),
                CairoFont.SmallButtonText(), 200, proposeBounds);

            ElementBounds leaveBounds = proposeBounds.RightCopy(15);
            compo.AddInset(leaveBounds);
            compo.AddIconButton("claims:exit-door", (bool t) =>
            {
                if (t) OpenDialog(EnumUpperWindowSelectedState.ALLIANCE_CANCEL_UNION_SELECT);
            }, leaveBounds);
            compo.AddHoverText(Lang.Get("claims:gui-send-leave-union"),
                CairoFont.SmallButtonText(), 200, leaveBounds);

            NavRow.Build(Gui, currentBounds, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.AllianceInfoPage), Lang.Get("claims:gui-nav-back")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
