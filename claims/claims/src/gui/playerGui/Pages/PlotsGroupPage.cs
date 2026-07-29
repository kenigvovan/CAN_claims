using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class PlotsGroupPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var gui = Gui;
            var compo = ctx.Compo;
            var clientInfo = Player;

            var currentBounds = ctx.Current;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;

            currentBounds = currentBounds.BelowCopy(0, 0);

            var list = ScrollableList.Add(gui, currentBounds,
                Lang.Get("claims:gui-plots-group-title"),
                clientInfo.CityInfo.PlotsGroupCells,
                (PlotsGroupCellElement cell, ElementBounds bounds) => new GuiElementCityPlotsGroupCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "plots-groups", HeightReserve = 250, TitleHeightShrink = 0 });

            // What a plots group is, on the heading rather than as a paragraph taking up the page.
            Tooltip.Add(compo, Lang.Get("claims:gui-plotsgroup-description"), list.Title, "tip-plotsgroupdesc");

            // Buttons the player may not use are left out, as in the ImGui version.
            var perms = clientInfo.PlayerPermissions;

            // Same strip the cards use, so the buttons here are the size and spacing of every other
            // action row in the dialog.
            var firstSlot = list.Inset.BelowCopy(0, 12).WithFixedSize(Card.ActionSize, Card.ActionSize);
            var actions = new ActionRow(compo, firstSlot);

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE))
            {
                actions.Add("plus", "addPlotsGroup",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME); },
                    Lang.Get("claims:gui-add-new-plotsgroup"));
            }

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE))
            {
                actions.Add("line", "removePlotsGroup",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_SELECT); },
                    Lang.Get("claims:gui-remove-plotsgroup"));
            }

            actions.Add("claims:envelope", "plotsGroupInvites",
                on => { if (on) GoTo(EnumSelectedTab.PlotsGroupReceivedInvites); },
                Lang.Get("claims:gui-show-received-invites"));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }

    public sealed class PlotsGroupInfoPage : CANGuiPage
    {
        private PlotsGroupCellElement SelectedGroup()
            => Player.CityInfo?.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(State.DialogArgs.Selected), null);

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null && SelectedGroup() != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            PlotsGroupCellElement cell = SelectedGroup();

            // Each button appears only for whoever may press it, as in the ImGui version.
            var perms = Player.PlayerPermissions;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            double columnWidth = (ctx.Line.fixedWidth - Card.ColumnGap) / 2;

            var left = anchor.FlatCopy();
            left.fixedWidth = columnWidth;

            // --- the group itself ---
            var groupRows = new List<CardRow>
            {
                new CardRow { Label = Lang.Get("claims:gui-plotsgroup-label-name"), Value = cell.Name, Key = "groupName" },
                new CardRow { Label = Lang.Get("claims:gui-plotsgroup-label-city"), Value = cell.CityName, Key = "groupCity" },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-fee"),
                    Value = Number(cell.PlotsGroupFee),
                    Key = "groupFee"
                },
            };

            double y = Card.RowsWithActions(compo, left, left.fixedY,
                Lang.Get("claims:gui-plotsgroup-section-group"), groupRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET))
                {
                    actions.Add("claims:open-book", "setPermissions",
                        on => OpenDialog(on
                            ? EnumUpperWindowSelectedState.CITY_PLOTSGROUP_PERMISSIONS
                            : EnumUpperWindowSelectedState.NONE),
                        Lang.Get("claims:gui-plotsgroup-permissions-title"), toggleable: true);
                }
            });

            // --- its plots ---
            Card.Actions(compo, left, y, Lang.Get("claims:gui-plotsgroup-section-plots"), slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLOT))
                {
                    actions.Add("plus", "groupAddPlot",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM); },
                        Lang.Get("claims:gui-plotsgroup-add-plot"));
                }

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT))
                {
                    actions.Add("line", "groupRemovePlot",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM); },
                        Lang.Get("claims:gui-plotsgroup-remove-plot"));
                }
            });

            // --- its members ---
            var right = anchor.FlatCopy();
            right.fixedWidth = columnWidth;
            right.fixedX += columnWidth + Card.ColumnGap;

            var memberRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-members"),
                    Value = cell.PlayersNames.Count.ToString(),
                    Tooltip = cell.PlayersNames.Count > 0
                        ? StringFunctions.concatStringsWithDelim(cell.PlayersNames, ',')
                        : null,
                    Key = "groupMembers"
                }
            };

            Card.RowsWithActions(compo, right, right.fixedY,
                Lang.Get("claims:gui-plotsgroup-section-members"), memberRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLAYER))
                {
                    actions.Add("plus", "addGroupMember",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ADD_PLOTSGROUP_MEMBER_NEED_NAME); },
                        Lang.Get("claims:gui-add-plotsgroup-member"));
                }

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER))
                {
                    actions.Add("line", "removeGroupMember",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_SELECT); },
                        Lang.Get("claims:gui-remove-plotsgroup-member"));
                }
            });
        }

        /// <summary>Fees are doubles; whole values should not read "5.0".</summary>
        private static string Number(double value) =>
            value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    public sealed class PlotsGroupInvitesPage : CANGuiPage
    {
        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var currentBounds = ctx.Current;
            var invites = Player.ReceivedPlotsGroupInvitations;

            if (invites.Count == 0)
            {
                ElementBounds emptyBounds = currentBounds.FlatCopy().BelowCopy(0, 35);
                emptyBounds.fixedHeight -= 50;
                emptyBounds.WithAlignment(EnumDialogArea.CenterTop);
                compo.AddStaticText(Lang.Get("claims:gui-no-plotsgroup-invites"),
                    CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                    emptyBounds);
                return;
            }

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:gui-city-tab-invitations", invites.Count),
                invites,
                (ClientToPlotsGroupInvitation cell, ElementBounds bounds) => new GuiElementPlotsGroupInvitation(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "plotsgroup-invitations", HeightReserve = 230, TitleGap = 35, ContainerBelowTitle = true });

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
