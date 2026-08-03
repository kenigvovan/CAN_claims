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

            // Buttons the player may not use are left out, as in the ImGui version.
            var perms = clientInfo.PlayerPermissions;
            var groups = clientInfo.CityInfo.PlotsGroupCells ?? new List<PlotsGroupCellElement>();

            var column = ctx.Line.BelowCopy(0, 14);
            column.Alignment = EnumDialogArea.LeftTop;
            column.fixedWidth = ctx.Line.fixedWidth;

            // The page used to open on a bare list with a strip of buttons under it. The card says
            // what the tab is about and carries the actions, as every other tab does.
            double y = Card.RowsWithActions(compo, column, column.fixedY,
                Lang.Get("claims:gui-plots-group-title"), SummaryRows(groups), slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE))
                {
                    actions.Add("plus", "addPlotsGroup",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME); },
                        Lang.Get("claims:gui-add-new-plotsgroup"));
                }

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE) && groups.Count > 0)
                {
                    actions.Add("line", "removePlotsGroup",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_SELECT); },
                        Lang.Get("claims:gui-remove-plotsgroup"));
                }

                actions.Add("claims:envelope", "plotsGroupInvites",
                    on => { if (on) GoTo(EnumSelectedTab.PlotsGroupReceivedInvites); },
                    Lang.Get("claims:gui-show-received-invites"));
            });

            if (groups.Count == 0)
            {
                // What a plots group is, where the list would have been - an empty framed box told
                // the player nothing, and the explanation used to hide in a tooltip.
                // No heading: the card above already says "Plots groups", and repeating it here read
                // as a second, empty section.
                const double hintHeight = 60;
                ElementBounds emptyInner = Card.Frame(compo, column, y,
                    hintHeight + Card.Padding * 2, null);

                compo.AddStaticText(Lang.Get("claims:gui-plotsgroup-description"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    emptyInner.FlatCopy().WithFixedHeight(hintHeight), "plotsgroup-empty");
                return;
            }

            var listAnchor = column.FlatCopy();
            listAnchor.fixedY = y;

            var listOpts = new ScrollableListOptions { Key = "plots-groups", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(gui,
                System.Math.Max(MinListHeight,
                    gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - listAnchor.fixedY - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            var list = ScrollableList.Add(gui, listAnchor,
                Lang.Get("claims:gui-plotsgroup-section-plots-groups") + " (" + groups.Count + ")",
                groups,
                (PlotsGroupCellElement cell, ElementBounds bounds) => new GuiElementCityPlotsGroupCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            Tooltip.Add(compo, Lang.Get("claims:gui-plotsgroup-description"), list.Title, "tip-plotsgroupdesc");

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        private const double MinListHeight = 70;

        /// <summary>
        /// What the tab is worth reading before the rows: how many groups there are, how many of them
        /// the player is in, and what they cost them a day - the number that decides whether they
        /// stay in one.
        /// </summary>
        private List<CardRow> SummaryRows(List<PlotsGroupCellElement> groups)
        {
            string me = claims.capi?.World?.Player?.PlayerName ?? "";
            var mine = groups.FindAll(g => me.Length > 0 && g.PlayersNames.Contains(me));
            double owed = 0;
            bool pending = false;
            foreach (PlotsGroupCellElement group in mine)
            {
                owed += group.PlotsGroupFee;
                if (group.HasPendingFee && !group.AcceptedBy(claims.capi?.World?.Player?.PlayerUID ?? ""))
                {
                    pending = true;
                }
            }

            var rows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-count"),
                    Value = groups.Count.ToString(),
                    Key = "groupsCount"
                }
            };

            if (mine.Count > 0)
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-yours"),
                    Value = mine.Count.ToString(),
                    Key = "groupsMine"
                });
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-you-pay"),
                    Value = owed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                    ValueColor = owed > 0 ? null : ClaimsColors.Label,
                    Key = "groupsOwed"
                });
            }

            // Only when the player owes an answer: a raise they already accepted, or one on a group
            // they are not in, is not theirs to act on.
            if (pending)
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-fee-pending"),
                    Value = Lang.Get("claims:gui-plotsgroup-fee-pending-yours"),
                    ValueColor = ClaimsColors.Warning,
                    // The accept button lives on the group's own page, which is a click away and not
                    // obvious from here.
                    Tooltip = Lang.Get("claims:gui-plotsgroup-fee-pending-tooltip")
                        + "\n" + Lang.Get("claims:gui-plotsgroup-fee-pending-where"),
                    Key = "groupsPending"
                });
            }
            return rows;
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
                    Tooltip = Lang.Get("claims:gui-plotsgroup-fee-tooltip"),
                    Key = "groupFee"
                },
            };

            // An announced raise nobody pays yet. Spelled out with the deadline, because what the
            // member does before it - accept or leave - is the whole point of the waiting period.
            if (cell.HasPendingFee)
            {
                long secondsLeft = cell.PendingFeeAt - auxialiry.TimeFunctions.getEpochSeconds();
                bool accepted = cell.AcceptedBy(claims.capi?.World?.Player?.PlayerUID ?? "");
                groupRows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-fee-pending"),
                    Value = Number(cell.PendingFee) + " ("
                        + (secondsLeft > 0
                            ? Lang.Get("claims:gui-auction-left-value", secondsLeft / 3600, (secondsLeft % 3600) / 60)
                            : Lang.Get("claims:gui-plotsgroup-fee-pending-due"))
                        + ")",
                    ValueColor = accepted ? ClaimsColors.Success : ClaimsColors.Warning,
                    Tooltip = Lang.Get(accepted
                        ? "claims:gui-plotsgroup-fee-pending-accepted-tooltip"
                        : "claims:gui-plotsgroup-fee-pending-tooltip"),
                    Key = "groupFeePending"
                });
            }

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

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_SET_FEE))
                {
                    actions.Add("claims:price-tag", "setGroupFee",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_SET_FEE); },
                        Lang.Get("claims:gui-plotsgroup-set-fee"));
                }

                // Only for a member who has an announced raise waiting on them: everyone else either
                // has nothing to answer or has answered already.
                if (cell.HasPendingFee && !cell.AcceptedBy(claims.capi?.World?.Player?.PlayerUID ?? "")
                    && cell.PlayersNames.Contains(claims.capi?.World?.Player?.PlayerName ?? " "))
                {
                    actions.Add("claims:contract", "acceptGroupFee",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_ACCEPT_FEE); },
                        Lang.Get("claims:gui-plotsgroup-accept-fee"));
                }
            });

            // --- its plots ---
            // With a count: adding a plot changes nothing else on screen, so without it the button
            // looked like it had done nothing at all.
            var plotRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-plots"),
                    Value = cell.PlotsCount.ToString(),
                    Key = "groupPlots"
                }
            };

            Card.RowsWithActions(compo, left, y, Lang.Get("claims:gui-plotsgroup-section-plots"), plotRows, slot =>
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

            // Reached by clicking a group in the list, and the tab bar entry rebuilds this same page
            // - so without this there was no way back to the group list at all.
            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.PlotsGroup),
                    Lang.Get("claims:gui-nav-back")));
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

                BuildNav(ctx);
                return;
            }

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:gui-city-tab-invitations", invites.Count),
                invites,
                (ClientToPlotsGroupInvitation cell, ElementBounds bounds) => new GuiElementPlotsGroupInvitation(compo.Api, cell, bounds) { On = true },
                // 280 rather than 230: the list has to stop above the navigation row below it.
                new ScrollableListOptions { Key = "plotsgroup-invitations", HeightReserve = 280, TitleGap = 35, ContainerBelowTitle = true });

            BuildNav(ctx);

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        /// <summary>
        /// This page is opened from the group list's envelope button and has no tab of its own, so
        /// the way back has to be drawn here.
        /// </summary>
        private void BuildNav(PageBuildContext ctx)
        {
            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.PlotsGroup),
                    Lang.Get("claims:gui-nav-back")));
        }
    }
}
