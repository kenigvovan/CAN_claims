using System.Collections.Generic;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class RanksPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var currentBounds = ctx.Current.BelowCopy(0, 20);

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:gui-ranks-title"),
                Player.CityInfo.CityRanks,
                (CityRankCellElement cell, ElementBounds bounds) => new GuiElementCityRanks(compo.Api, cell, bounds) { On = true },
                // 280 rather than 230: the list has to stop above the navigation row below it.
                new ScrollableListOptions { Key = "city-ranks", HeightReserve = 280, ContainerBelowTitle = true });

            // Creating a rank is a permission; without it the button only earned a refusal.
            var buttons = new List<NavButton>
            {
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back"))
            };

            if (Player.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_CREATE_CITY_RANK))
            {
                buttons.Add(new NavButton("plus", () => OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_CREATION_NEED_NAME),
                    Lang.Get("claims:gui-create-rank-tooltip")));
            }

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0, buttons.ToArray());

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
