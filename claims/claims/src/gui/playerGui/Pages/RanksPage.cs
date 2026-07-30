using System.Collections.Generic;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>The city's ranks, what they are for, and the roles this player holds.</summary>
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
            var city = Player.CityInfo;
            var ranks = city.CityRanks;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            var column = anchor.FlatCopy();
            column.fixedWidth = ctx.Line.fixedWidth;

            var rows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-ranks-label-count"),
                    Value = ranks.Count.ToString(),
                    Key = "rankCount"
                }
            };

            // What the player themselves holds, which is why most of them open this page at all.
            bool hasTitles = city.CityTitles != null && city.CityTitles.Count > 0;
            rows.Add(new CardRow
            {
                Label = Lang.Get("claims:gui-ranks-label-yours"),
                Value = hasTitles ? string.Join(", ", city.CityTitles) : "-",
                Tooltip = hasTitles ? string.Join(", ", city.CityTitles) : null,
                Key = "yourRanks"
            });

            double y = Card.RowsWithActions(compo, column, column.fixedY, Lang.Get("claims:gui-ranks-title"), rows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                // Creating a rank is a permission; without it the button only earned a refusal.
                if (Player.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_CREATE_CITY_RANK))
                {
                    actions.Add("plus", "createRank",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_CREATION_NEED_NAME); },
                        Lang.Get("claims:gui-create-rank-tooltip"));
                }
            });

            if (ranks.Count == 0)
            {
                // A city with no ranks yet gets the explanation instead of an empty frame: the
                // description had a lang key but was never shown anywhere.
                var hintBounds = column.FlatCopy().WithFixedHeight(90);
                hintBounds.fixedY = y;
                compo.AddStaticText(Lang.Get("claims:gui-ranks-description"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), hintBounds, "ranks-description");
            }
            else
            {
                // No heading on the list itself: the card above already names the section.
                ElementBounds listAnchor = ctx.Current.FlatCopy();
                listAnchor.fixedWidth = ctx.Line.fixedWidth;
                listAnchor.fixedY = y;

                var list = ScrollableList.Add(Gui, listAnchor, null,
                    ranks,
                    (CityRankCellElement cell, ElementBounds bounds) => new GuiElementCityRanks(compo.Api, cell, bounds) { On = true },
                    // 280 rather than 230: the list has to stop above the navigation row below it.
                    new ScrollableListOptions { Key = "city-ranks", HeightReserve = 280, ContainerBelowTitle = true });

                ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
            }

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
        }
    }
}
