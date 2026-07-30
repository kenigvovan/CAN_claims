using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.rights;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The city's prison: who is on the criminal list and which cells they can be sent to.
    /// </summary>
    public sealed class PrisonPage : CANGuiPage
    {
        /// <summary>How many criminals the card names before falling back to a count.</summary>
        private const int CriminalsShown = 4;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var perms = Player.PlayerPermissions;
            var city = Player.CityInfo;
            var cells = city.PrisonCells;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            double columnWidth = (ctx.Line.fixedWidth - Card.ColumnGap) / 2;

            // --- left: who the city has convicted ---
            var leftColumn = anchor.FlatCopy();
            leftColumn.fixedWidth = columnWidth;

            var criminalRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-label-criminals"),
                    Value = city.Criminals.Count.ToString(),
                    Tooltip = city.Criminals.Count > 0
                        ? StringFunctions.concatStringsWithDelim(city.Criminals, ',')
                        : null,
                    ValueColor = city.Criminals.Count > 0 ? ClaimsColors.Danger : null,
                    Key = "criminals"
                }
            };

            // The names themselves, not just how many there are: the list is consulted to check
            // whether a given player is on it, and hovering a number to find out is no way to do it.
            if (city.Criminals.Count == 0)
            {
                criminalRows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-no-criminals"),
                    Value = "",
                    Key = "no-criminals"
                });
            }
            else
            {
                int shown = 0;
                foreach (string criminal in city.Criminals.Take(CriminalsShown))
                {
                    criminalRows.Add(new CardRow
                    {
                        Label = StringFunctions.replaceUnderscore(criminal),
                        Value = "",
                        Key = "criminal" + shown++
                    });
                }

                int rest = city.Criminals.Count - CriminalsShown;
                if (rest > 0)
                {
                    criminalRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-prison-cell-more", rest),
                        Value = "",
                        Tooltip = StringFunctions.concatStringsWithDelim(city.Criminals, ','),
                        Key = "criminals-rest"
                    });
                }
            }

            double leftY = Card.RowsWithActions(compo, leftColumn, leftColumn.fixedY,
                Lang.Get("claims:gui-prison-section-criminals"), criminalRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (perms.HasPermission(EnumPlayerPermissions.CITY_ADD_CRIMINAL)
                 || perms.HasPermission(EnumPlayerPermissions.CITY_CRIMINAL_ALL))
                {
                    actions.Add("plus", "addCriminal",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ADD_CRIMINAL_NEED_NAME); },
                        Lang.Get("claims:gui-prison-add-criminal-tooltip"));
                }

                if (perms.HasPermission(EnumPlayerPermissions.CITY_REMOVE_CRIMINAL)
                 || perms.HasPermission(EnumPlayerPermissions.CITY_CRIMINAL_ALL))
                {
                    actions.Add("line", "removeCriminal",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.REMOVE_CRIMINAL); },
                        Lang.Get("claims:gui-prison-remove-criminal-tooltip"));
                }
            });

            // --- right: the prison itself ---
            var rightColumn = anchor.FlatCopy();
            rightColumn.fixedWidth = columnWidth;
            rightColumn.fixedX += columnWidth + Card.ColumnGap;

            int occupiedCells = cells.Count(cell => cell.Players.Count > 0);
            int inmates = cells.Sum(cell => cell.Players.Count);

            var prisonRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-label-cells"),
                    Value = cells.Count.ToString(),
                    Key = "cellCount"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-label-occupied"),
                    Value = occupiedCells.ToString(),
                    ValueColor = occupiedCells > 0 ? ClaimsColors.Danger : null,
                    Key = "cellsOccupied"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-label-free"),
                    Value = (cells.Count - occupiedCells).ToString(),
                    ValueColor = ClaimsColors.Success,
                    Key = "cellsFree"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-label-inmates"),
                    Value = inmates.ToString(),
                    ValueColor = inmates > 0 ? ClaimsColors.Danger : null,
                    Key = "inmates"
                }
            };

            double rightY = Card.RowsWithActions(compo, rightColumn, rightColumn.fixedY,
                Lang.Get("claims:gui-prison-section-prison"), prisonRows, slot =>
            {
                // A cell is claimed where the player stands, so there is nothing to pick or confirm.
                // The button belongs to the prison, so it sits in the prison's card rather than
                // loose under the list, the way every other page places its actions.
                if (perms.HasPermission(EnumPlayerPermissions.CITY_PRISON_ADD_CELL)
                 || perms.HasPermission(EnumPlayerPermissions.CITY_PRISON_ALL))
                {
                    new ActionRow(compo, slot).Add("claims:prisoner", "addPrisonCell",
                        on => { if (on) Send("/c prison addcell"); },
                        Lang.Get("claims:gui-prison-add-cell-tooltip"));
                }
            });

            double belowCards = System.Math.Max(leftY, rightY);

            if (cells.Count == 0)
            {
                BuildNoCells(ctx, anchor, belowCards);
                return;
            }

            // Cells keep the numbers of their place in the city's list, but occupied ones are shown
            // first: with a dozen cells the empty ones otherwise stand between the reader and the
            // one thing they opened the tab for.
            var numbers = new Dictionary<PrisonCellElement, int>();
            for (int i = 0; i < cells.Count; i++) numbers[cells[i]] = i + 1;

            var ordered = cells
                .OrderByDescending(cell => cell.Players.Count)
                .ThenBy(cell => numbers[cell])
                .ToList();

            var listAnchor = anchor.FlatCopy();
            listAnchor.fixedWidth = ctx.Line.fixedWidth;
            listAnchor.fixedY = belowCards;

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:gui-prison-cells-title"),
                ordered,
                (PrisonCellElement cell, ElementBounds bounds) =>
                    new GuiElementCityPrisonCell(compo.Api, cell, bounds, numbers[cell]) { On = true },
                new ScrollableListOptions { Key = "prison-cells", HeightReserve = 400, TitleHeightShrink = 0 });

            // What the prison is for, on the heading rather than as a paragraph taking up the page.
            Tooltip.Add(compo, Lang.Get("claims:gui-prison-description"), list.Title, "tip-prisondesc");

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        /// <summary>
        /// A city with no cells at all got an empty framed box that said nothing about how to fill
        /// it. The list is replaced with a card explaining where a cell comes from.
        /// </summary>
        private void BuildNoCells(PageBuildContext ctx, ElementBounds anchor, double y)
        {
            var compo = ctx.Compo;

            var column = anchor.FlatCopy();
            column.fixedWidth = ctx.Line.fixedWidth;

            const double hintHeight = 40;
            ElementBounds inner = Card.Frame(compo, column, y,
                Card.HeaderHeight + hintHeight + Card.Padding * 2,
                Lang.Get("claims:gui-prison-cells-title"));

            compo.AddStaticText(Lang.Get("claims:gui-prison-no-cells"),
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                inner.FlatCopy().WithFixedHeight(hintHeight), "prison-no-cells");
        }
    }
}
