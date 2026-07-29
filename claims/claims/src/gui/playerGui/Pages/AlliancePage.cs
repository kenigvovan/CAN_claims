using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The player's alliance: who leads it, which cities belong to it, what it holds and who it is
    /// at peace or at war with - in the same cards the city and player tabs are built from.
    /// </summary>
    public sealed class AlliancePage : CANGuiPage
    {
        protected override void BuildContent(PageBuildContext ctx)
        {
            if (Player?.AllianceInfo == null)
            {
                BuildNoAlliance(ctx);
                return;
            }

            var compo = ctx.Compo;
            var lineBounds = ctx.Line;
            var alliance = Player.AllianceInfo;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            double columnWidth = (lineBounds.fixedWidth - Card.ColumnGap) / 2;

            var column = anchor.FlatCopy();
            column.fixedWidth = columnWidth;
            double y = column.fixedY;

            // --- the alliance itself ---
            var allianceRows = new List<CardRow>
            {
                new CardRow { Label = Lang.Get("claims:gui-alliance-label-name"), Value = alliance.Name, Key = "allianceName" },
                new CardRow { Label = Lang.Get("claims:gui-alliance-label-leader"), Value = alliance.LeaderName, Key = "allianceLeader" },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-alliance-label-created"),
                    Value = TimeFunctions.getDateFromEpochSeconds(alliance.TimeStampCreated),
                    Key = "allianceCreated"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-alliance-label-prefix"),
                    Value = string.IsNullOrEmpty(alliance.Prefix) ? "-" : alliance.Prefix,
                    Key = "alliancePrefix"
                }
            };

            y = Card.RowsWithActions(compo, column, y, Lang.Get("claims:gui-alliance-section-alliance"), allianceRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                actions.Add("claims:pencil", "setAllianceName",
                    on => OpenDialog(on ? EnumUpperWindowSelectedState.SELECT_NEW_ALLIANCE_NAME : EnumUpperWindowSelectedState.NONE),
                    Lang.Get("claims:gui-set-name-button"), toggleable: true);

                actions.Add("claims:soldering-iron", "setAlliancePrefix",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ALLIANCE_PREFIX_NEED_NAME); },
                    Lang.Get("claims:gui-alliance-set-prefix-tooltip"));

                // Leaving is an action on the alliance, so it lives in this card rather than in the
                // navigation row, which is for moving between pages.
                actions.Add("claims:exit-door", "leaveAlliance",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.LEAVE_ALLIANCE_CONFIRM); },
                    Lang.Get("claims:gui-leavealliance"));
            });

            // --- treasury, where the host runs on virtual money at all ---
            if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
            {
                var treasuryRows = new List<CardRow>
                {
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-alliance-label-balance"),
                        Value = Number(alliance.Balance),
                        Key = "allianceBalance"
                    }
                };

                Card.Rows(compo, column, y, Lang.Get("claims:gui-city-section-treasury"), treasuryRows);
            }

            // --- right column: member cities and who the alliance stands with ---
            var rightColumn = anchor.FlatCopy();
            rightColumn.fixedWidth = columnWidth;
            rightColumn.fixedX += columnWidth + Card.ColumnGap;
            double rightY = rightColumn.fixedY;

            var cityRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-alliance-label-cities"),
                    Value = alliance.Cities.Count.ToString(),
                    Tooltip = alliance.Cities.Count > 0
                        ? StringFunctions.concatStringsWithDelim(alliance.Cities, ',')
                        : null,
                    Key = "allianceCities"
                }
            };

            rightY = Card.RowsWithActions(compo, rightColumn, rightY, Lang.Get("claims:gui-alliance-section-cities"), cityRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                actions.Add("plus", "inviteCity",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.INVITE_TO_ALLIANCE_NEED_NAME); },
                    Lang.Get("claims:gui-alliance-invite-city-tooltip"));

                actions.Add("line", "kickCity",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.KICK_FROM_ALLIANCE_NEED_NAME); },
                    Lang.Get("claims:gui-alliance-kick-city-tooltip"));

                // No "uninvite from alliance" button: the server has no such command - only
                // /city uninvite exists - so this opened an empty window.
            });

            // --- diplomacy: allies, enemies, and unions running out their notice ---
            var diplomacyRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-alliance-label-allies"),
                    Value = alliance.Allies.Count.ToString(),
                    Tooltip = alliance.Allies.Count > 0 ? string.Join(", ", alliance.Allies) : null,
                    Key = "allianceAllies"
                }
            };

            if (alliance.Hostiles != null && alliance.Hostiles.Count > 0)
            {
                diplomacyRows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-alliance-label-hostiles"),
                    Value = alliance.Hostiles.Count.ToString(),
                    ValueColor = ClaimsColors.Danger,
                    Tooltip = string.Join(", ", alliance.Hostiles),
                    Key = "allianceHostiles"
                });
            }

            // A union that has been given notice still holds until its timer runs out, so it gets a
            // row of its own with the time left rather than vanishing from the ally count silently.
            if (alliance.PendingUnionBreaks != null)
            {
                long nowSeconds = TimeFunctions.getEpochSeconds();
                int index = 0;
                foreach (var pending in alliance.PendingUnionBreaks)
                {
                    if (pending.Value <= nowSeconds) continue;

                    string allyName = StringFunctions.replaceUnderscore(pending.Key);
                    string left = StringFunctions.FormatDuration(pending.Value - nowSeconds);

                    diplomacyRows.Add(new CardRow
                    {
                        Label = allyName,
                        Value = left,
                        ValueColor = ClaimsColors.Danger,
                        Tooltip = Lang.Get("claims:gui-union-break-pending", allyName, left),
                        Key = "unionBreak" + index++
                    });
                }
            }

            Card.Rows(compo, rightColumn, rightY, Lang.Get("claims:gui-alliance-section-diplomacy"), diplomacyRows);

            NavRow.Build(Gui, ctx.Current, lineBounds, 0,
                new NavButton("claims:envelope", () =>
                {
                    State.ConflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                    GoTo(EnumSelectedTab.ConflictLettersPage);
                }, Lang.Get("claims:gui-nav-conflict-letters")),
                new NavButton("claims:frog-mouth-helm", () =>
                {
                    State.ConflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                    GoTo(EnumSelectedTab.ConflictsPage);
                }, Lang.Get("claims:gui-nav-conflicts")),
                new NavButton("claims:tower-flag", () => GoTo(EnumSelectedTab.UnionLettersPage),
                    Lang.Get("claims:union_letters_list")),
                new NavButton("claims:vertical-banner", () => GoTo(EnumSelectedTab.AllianceListPage),
                    Lang.Get("claims:gui_alliance_list_title")));
        }

        /// <summary>
        /// What a player outside an alliance sees: how to found one, and the invitations their city
        /// has been sent.
        /// </summary>
        private void BuildNoAlliance(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var lineBounds = ctx.Line;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            var column = anchor.FlatCopy();
            column.fixedWidth = lineBounds.fixedWidth;

            const double hintHeight = 40;
            double foundHeight = Card.HeaderHeight + hintHeight + Card.ActionGap + Card.ActionSize + Card.Padding * 2;

            ElementBounds inner = Card.Frame(compo, column, column.fixedY, foundHeight,
                Lang.Get("claims:gui-alliance-section-found"));

            var hintBounds = inner.FlatCopy().WithFixedHeight(hintHeight);
            compo.AddStaticText(Lang.Get("claims:gui-alliance-found-hint"),
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), hintBounds, "alliance-found-hint");

            var slot = inner.FlatCopy().WithFixedSize(Card.ActionSize, Card.ActionSize);
            slot.fixedY = inner.fixedY + hintHeight + Card.ActionGap;

            var actions = new ActionRow(compo, slot);
            actions.Add("claims:queen-crown", "createAlliance",
                on => { if (on) OpenDialog(EnumUpperWindowSelectedState.NEW_ALLIANCE_NEED_NAME); },
                Lang.Get("claims:gui-new-alliance-button"));

            double belowCard = column.fixedY + foundHeight + Card.Gap;

            // Only a city can be invited into an alliance, so a player without one has no list to
            // show - reading CityInfo unchecked here threw instead.
            var invites = Player?.CityInfo?.ClientToAllianceInvitations;
            if (invites != null && invites.Count > 0)
            {
                ElementBounds listAnchor = ctx.Current.FlatCopy();
                listAnchor.fixedY = belowCard;

                var list = ScrollableList.Add(Gui, listAnchor,
                    Lang.Get("claims:gui-to-alliance-invites"),
                    invites,
                    (ClientToAllianceInvitationCellElement cell, ElementBounds bounds) => new GuiElementToAllianceInvitation(compo.Api, cell, bounds) { On = true },
                    new ScrollableListOptions { Key = "alliance-invitations", HeightReserve = 280, ContainerBelowTitle = true });

                ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
            }
            else
            {
                var emptyBounds = column.FlatCopy().WithFixedHeight(24);
                emptyBounds.fixedY = belowCard;
                compo.AddStaticText(Lang.Get("claims:gui-alliance-no-invitations"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), emptyBounds, "no-alliance-invitations");
            }

            NavRow.Build(Gui, ctx.Current, lineBounds, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")),
                new NavButton("claims:vertical-banner", () => GoTo(EnumSelectedTab.AllianceListPage),
                    Lang.Get("claims:gui_alliance_list_title")));
        }

        /// <summary>Balances are doubles; whole values should not read "1500.0".</summary>
        private static string Number(double value) =>
            value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
