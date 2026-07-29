using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.rights;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class PrisonPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var perms = Player.PlayerPermissions;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = ctx.Line.fixedWidth;

            // --- the city's criminals, and who may change the list ---
            var criminalRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-prison-label-criminals"),
                    Value = Player.CityInfo.Criminals.Count.ToString(),
                    Tooltip = Player.CityInfo.Criminals.Count > 0
                        ? StringFunctions.concatStringsWithDelim(Player.CityInfo.Criminals, ',')
                        : null,
                    ValueColor = Player.CityInfo.Criminals.Count > 0 ? ClaimsColors.Danger : null,
                    Key = "criminals"
                }
            };

            double y = Card.RowsWithActions(compo, anchor, anchor.fixedY,
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

            var listAnchor = anchor.FlatCopy();
            listAnchor.fixedY = y;

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:gui-prison-cells-title"),
                Player.CityInfo.PrisonCells,
                (PrisonCellElement cell, ElementBounds bounds) => new GuiElementCityPrisonCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "prison-cells", HeightReserve = 400, TitleHeightShrink = 0 });

            // What the prison is for, on the heading rather than as a paragraph taking up the page.
            Tooltip.Add(compo, Lang.Get("claims:gui-prison-description"), list.Title, "tip-prisondesc");

            // A cell is claimed where the player stands, so there is nothing to pick or confirm.
            if (perms.HasPermission(EnumPlayerPermissions.CITY_PRISON_ADD_CELL)
             || perms.HasPermission(EnumPlayerPermissions.CITY_PRISON_ALL))
            {
                var slot = list.Inset.BelowCopy(0, 12).WithFixedSize(Card.ActionSize, Card.ActionSize);
                new ActionRow(compo, slot).Add("claims:prisoner", "addPrisonCell",
                    on => { if (on) Send("/c prison addcell"); },
                    Lang.Get("claims:gui-prison-add-cell-tooltip"));
            }

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
