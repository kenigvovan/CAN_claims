using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class CityPage : CANGuiPage
    {
        private const double InviteMinListHeight = 70;

        protected override void BuildContent(PageBuildContext ctx)
        {
            var gui = Gui;
            var lineBounds = ctx.Line;
            var compo = ctx.Compo;
            var currentBounds = ctx.Current.BelowCopy(0, 40);
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo != null && claims.clientDataStorage.clientPlayerInfo?.CityInfo.Name != "")
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;
                var city = clientInfo.CityInfo;

                // Buttons a citizen has no right to press are left out entirely - pressing them
                // only ever produced a refusal from the server.
                var cityPerms = clientInfo.PlayerPermissions;
                bool canSetAll = cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL);

                // Two columns, like the prices tab: stacked, the four cards reach past the navigation
                // row at the bottom of the window.
                var anchor = CardAnchor(ctx);
                Columns(anchor, out ElementBounds column, out ElementBounds rightColumn);
                double y = column.fixedY;

                // --- the city itself ---
                var cityRows = new List<CardRow>
                {
                    new CardRow { Label = Lang.Get("claims:gui-city-label-name"), Value = city.Name, Key = "cityName" },
                    new CardRow { Label = Lang.Get("claims:gui-city-label-mayor"), Value = city.MayorName, Key = "mayorName" },
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-created"),
                        Value = TimeFunctions.getDateFromEpochSeconds(city.TimeStampCreated),
                        Key = "createdAt"
                    },
                    // Both states a newcomer judges the city by, shown to the city itself.
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-open"),
                        Value = Lang.Get(city.Open ? "claims:gui-yes" : "claims:gui-no"),
                        Key = "cityOpen"
                    },
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-inv-msg"),
                        // Up to 100 characters on the server: shortened to what the card column
                        // holds, with the whole of it on hover.
                        Value = city.InvMsg.Length > 32
                                    ? city.InvMsg.Substring(0, 32).TrimEnd() + "…"
                                    : city.InvMsg.Length > 0 ? city.InvMsg : Lang.Get("claims:gui-city-inv-msg-none"),
                        Tooltip = city.InvMsg.Length > 0 ? city.InvMsg : null,
                        Key = "cityInvMsg"
                    },
                };

                // When this village is open to attack. Sent only to its own citizens, so an
                // outsider still has to come and find out on the spot.
                if (city.IsVillage && city.RaidWindowStart > 0)
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long windowEnd = city.RaidWindowStart + city.RaidWindowMinutes * 60L;
                    bool openNow = now >= city.RaidWindowStart && now < windowEnd;

                    cityRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-raid-window"),
                        Value = openNow
                            ? Lang.Get("claims:gui-village-raid-now", (windowEnd - now) / 60)
                            : TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(city.RaidWindowStart)
                              + " (" + city.RaidWindowMinutes + Lang.Get("claims:gui-minutes-short") + ")",
                        ValueColor = openNow ? ClaimsColors.Danger : null,
                        Key = "raidWindow"
                    });
                }

                // The ranks this player holds in the city, if any.
                if (city.CityTitles != null && city.CityTitles.Count > 0)
                {
                    cityRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-rank"),
                        Value = string.Join(", ", city.CityTitles),
                        Key = "yourRank"
                    });
                }

                y = Card.RowsWithActions(compo, column, y,
                    Lang.Get(city.IsVillage ? "claims:gui-city-section-village" : "claims:gui-city-section-city"), cityRows, slot =>
                {
                    // The arms go at the right end of the action strip, where nothing else is drawn.
                    var emblemBounds = slot.FlatCopy().WithFixedSize(Card.ActionSize, Card.ActionSize);
                    emblemBounds.fixedX = column.fixedX + column.fixedWidth - Card.Padding - Card.ActionSize;
                    compo.AddEmblem(city.Emblem, emblemBounds, "city-emblem");
                    Tooltip.Add(compo, Lang.Get("claims:gui-emblem-city-tooltip"), emblemBounds, "tip-city-emblem");

                    var actions = new ActionRow(compo, slot);

                    // The name may only be changed by whoever is allowed to; everyone else just reads it.
                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_NAME) || canSetAll)
                    {
                        actions.Add("claims:pencil", "setCityName",
                            on => OpenDialog(on ? EnumUpperWindowSelectedState.SELECT_NEW_CITY_NAME : EnumUpperWindowSelectedState.NONE),
                            Lang.Get("claims:gui-set-name-button"), toggleable: true);
                    }

                    // Whether the city takes anyone who asks. It was a command with no readout
                    // anywhere on this tab, so a mayor could not tell the state without /city info.
                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_OPEN_STATE) || canSetAll)
                    {
                        // Asks first: an open city takes anyone who presses join, and the mayor gets
                        // no say afterwards.
                        actions.Add("claims:stairs-goal", "setCityOpen",
                            on => { if (on) OpenDialog(city.Open
                                        ? EnumUpperWindowSelectedState.CITY_CLOSE_CONFIRM
                                        : EnumUpperWindowSelectedState.CITY_OPEN_CONFIRM); },
                            Lang.Get(city.Open ? "claims:gui-city-close-button" : "claims:gui-city-open-button"));
                    }

                    // The city's advertisement in the world-wide list. It was set by command only, so
                    // the one thing newcomers read about a city was invisible to the mayor.
                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_INV_MSG) || canSetAll)
                    {
                        actions.Add("claims:info", "setInvMsg",
                            on => OpenDialog(on ? EnumUpperWindowSelectedState.SELECT_CITY_INV_MSG : EnumUpperWindowSelectedState.NONE),
                            Lang.Get("claims:gui-set-inv-msg-button"), toggleable: true);
                    }

                    // For every citizen: the page shows the rules to all and lets only those with
                    // the right change them.
                    actions.Add("claims:open-book", "cityRules",
                        on => { if (on) GoTo(EnumSelectedTab.CityRules); },
                        Lang.Get("claims:gui-city-plots-permissions"));

                    // Growing up and giving up: both are the head's calls, and both only exist while
                    // the settlement is still a village. A village grants CITY_SET_NAME to its head
                    // and to nobody else (it has no custom ranks), so the permission stands in for
                    // "is the head" here; the server checks mayorship itself either way.
                    if (city.IsVillage && cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_NAME))
                    {
                        actions.Add("claims:queen-crown", "upgradeVillage",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.VILLAGE_UPGRADE_CONFIRM); },
                            Lang.Get("claims:gui-village-upgrade-button"));
                        actions.Add("claims:cancel", "abandonVillage",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.VILLAGE_ABANDON_CONFIRM); },
                            Lang.Get("claims:gui-village-abandon-button"));
                    }

                    actions.Add("claims:exit-door", "leaveCity",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.LEAVE_CITY_CONFIRM); },
                        Lang.Get("claims:gui-leave-city-confirm-button"));
                });

                // --- plots ---
                city.MaxCountPlots.TryGetValue("base", out int baseAmount);
                city.MaxCountPlots.TryGetValue("bonus", out int bonus);
                city.MaxCountPlots.TryGetValue("alliance", out int alliance);

                var plotRows = new List<CardRow>
                {
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-plots"),
                        Value = city.CountPlots + " / " + (baseAmount + bonus + alliance),
                        Key = "claimedPlotsToMax"
                    }
                };
                if (bonus > 0)
                {
                    plotRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-plots-bonus"),
                        Value = bonus.ToString(),
                        Key = "plotsBonus"
                    });
                }
                if (alliance > 0)
                {
                    plotRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-city-label-plots-alliance"),
                        Value = alliance.ToString(),
                        Key = "plotsAlliance"
                    });
                }

                y = Card.RowsWithActions(compo, column, y, Lang.Get("claims:gui-city-section-plots"), plotRows, slot =>
                {
                    var actions = new ActionRow(compo, slot);

                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_CLAIM_PLOT))
                    {
                        actions.Add("plus", "claimPlot",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CLAIM_CITY_PLOT_CONFIRM); },
                            Lang.Get("claims:gui-buy-plot"));
                    }

                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_UNCLAIM_PLOT))
                    {
                        actions.Add("claims:cancel", "unclaimPlot",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.UNCLAIM_CITY_PLOT_CONFIRM); },
                            Lang.Get("claims:gui-unclaim-plot"));
                    }
                });

                // --- right column: who lives here and what the city is worth ---
                double rightY = rightColumn.fixedY;

                // Every citizen by name, each with its own ✕ - it used to be a count with the names in
                // a tooltip, and kicking meant picking the name again in a separate form.
                bool canKick = cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_KICK);
                string myName = claims.capi.World.Player?.PlayerName;
                var citizenItems = new List<ItemRow>();
                foreach (string name in city.PlayersNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    bool isMayor = name == city.MayorName;
                    var item = new ItemRow
                    {
                        Label = StringFunctions.replaceUnderscore(name),
                        Value = isMayor ? Lang.Get("claims:gui-city-label-mayor") : "",
                        Key = "citizen-" + name
                    };
                    if (canKick && !isMayor && name != myName)
                    {
                        string target = name;
                        item.Buttons.Add(new RowButton("claims:cancel",
                            () => OpenDialog(EnumUpperWindowSelectedState.KICK_FROM_CITY_CONFIRM, a => a.Text = target),
                            Lang.Get("claims:gui-kick-player")));
                    }
                    citizenItems.Add(item);
                }
                // The treasury card follows in this column; more rows push it onto the navigation row.
                const int MaxCitizensShown = 6;
                bool citizensCut = citizenItems.Count > MaxCitizensShown;

                rightY = Card.Items(compo, rightColumn, rightY,
                    Lang.Get(city.IsVillage ? "claims:gui-village-section-citizens" : "claims:gui-city-section-citizens")
                        + " (" + city.PlayersNames.Count + ")",
                    Card.Limit(citizenItems, MaxCitizensShown), Lang.Get("claims:gui-list-empty"), slot =>
                {
                    var actions = new ActionRow(compo, slot);

                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_INVITE))
                    {
                        actions.Add("plus", "invitePlayer",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.INVITE_TO_CITY_NEED_NAME); },
                            Lang.Get("claims:gui-invite-player"));
                    }

                    // The rows carry their own ✕; the form that asks for a name is kept only for the
                    // citizens a cut list leaves out.
                    if (canKick && citizensCut)
                    {
                        // The game's own minus icon, the counterpart of the "plus" that invites.
                        actions.Add("line", "kickPlayer",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.KICK_FROM_CITY_NEED_NAME); },
                            Lang.Get("claims:gui-kick-player"));
                    }

                    if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_UNINVITE))
                    {
                        actions.Add("eraser", "uninvitePlayer",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.UNINVITE_TO_CITY); },
                            Lang.Get("claims:gui-uninvite-player"));
                    }
                });

                // --- treasury: only where there is money to speak of (a village has none at all) ---
                bool virtualMoney = claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY" && !city.IsVillage;
                bool seesBalance = cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE);
                bool canSetFee = cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_GLOBAL_FEE) || canSetAll;

                // A village is outside the economy entirely - no balance, no debt, no daily payment
                // and no fee - so its treasury card is never built.
                var treasuryRows = new List<CardRow>();
                if (!city.IsVillage)
                {
                    if (virtualMoney && seesBalance)
                    {
                        treasuryRows.Add(new CardRow
                        {
                            Label = Lang.Get("claims:gui-city-label-balance"),
                            Value = Number(city.CityBalance),
                            Key = "cityBalance"
                        });
                    }
                    if (claims.config.GUI_SHOW_DEBT && city.CityDebt > 0)
                    {
                        treasuryRows.Add(new CardRow
                        {
                            Label = Lang.Get("claims:gui-city-label-debt"),
                            Value = Number(city.CityDebt),
                            ValueColor = ClaimsColors.Danger,
                            Key = "cityDebt"
                        });
                    }
                    if (city.CityDayPayment > 0)
                    {
                        treasuryRows.Add(new CardRow
                        {
                            Label = Lang.Get("claims:gui-city-label-payment"),
                            Value = Number(city.CityDayPayment),
                            Key = "cityPayment"
                        });
                    }
                    if (canSetFee)
                    {
                        treasuryRows.Add(new CardRow
                        {
                            Label = Lang.Get("claims:gui-city-label-fee"),
                            Value = Number(city.CityFee),
                            Key = "cityFee"
                        });
                    }
                }

                if (treasuryRows.Count > 0)
                {
                    rightY = Card.RowsWithActions(compo, rightColumn, rightY, Lang.Get("claims:gui-city-section-treasury"), treasuryRows, slot =>
                    {
                        var actions = new ActionRow(compo, slot);

                        // Anyone may pay into the treasury; taking out of it needs the permission.
                        if (virtualMoney && seesBalance)
                        {
                            actions.Add("claims:receive-money", "cityDeposit",
                                on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_DEPOSIT_CONFIRM); },
                                Lang.Get("claims:gui-city-deposit-tooltip"));

                            if (cityPerms.HasPermission(rights.EnumPlayerPermissions.CITY_WITHDRAW_MONEY))
                            {
                                actions.Add("claims:exit-door", "cityWithdraw",
                                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_WITHDRAW); },
                                    Lang.Get("claims:gui-city-withdraw-tooltip"));
                            }
                        }

                        if (canSetFee)
                        {
                            actions.Add("claims:price-tag", "cityFeeSet",
                                on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_SET_FEE); },
                                Lang.Get("claims:gui-city-set-fee-tooltip"));
                        }
                    });
                }

                currentBounds = anchor.FlatCopy();
                currentBounds.fixedWidth = lineBounds.fixedWidth;
                currentBounds.fixedY = System.Math.Max(y, rightY);

                /*==============================================================================================*/
                /*=====================================UNDER 2 LINE=============================================*/
                /*==============================================================================================*/
                // Leaving the city is an action on the city, so it lives in that card now; this row
                // is only for moving to another page.
                var navButtons = new List<NavButton>();

                var perms2 = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;
                if (perms2.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK)
                    || perms2.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    navButtons.Add(new NavButton("claims:achievement", () => GoTo(EnumSelectedTab.Ranks), Lang.Get("claims:gui-nav-ranks")));
                }
                if (perms2.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOTS_COLOR))
                {
                    navButtons.Add(new NavButton("claims:large-paint-brush", () => GoTo(EnumSelectedTab.CityPlotsColorSelector), Lang.Get("claims:gui-nav-plots-color")));
                }
                if (perms2.HasPermission(rights.EnumPlayerPermissions.CITY_SET_EMBLEM) || canSetAll)
                {
                    navButtons.Add(new NavButton("claims:tower-flag", () =>
                    {
                        // The draft always starts from what the city carries now, so leaving the page
                        // and coming back does not resume an edit the player already walked away from.
                        State.Emblem.Begin(false, city.Emblem);
                        GoTo(EnumSelectedTab.EmblemEditor);
                    }, Lang.Get("claims:gui-nav-emblem")));
                }

                // A village joins no alliances and fights no wars, so those three lead nowhere for it.
                if (!city.IsVillage)
                {
                    navButtons.Add(new NavButton("claims:vertical-banner", () => GoTo(EnumSelectedTab.AllianceInfoPage), Lang.Get("claims:gui-nav-alliance")));
                }
                navButtons.Add(new NavButton("claims:files", () => GoTo(EnumSelectedTab.CityLog),
                    Lang.Get("claims:gui-city-log-title")));
                navButtons.Add(new NavButton("claims:flat-platform", () => GoTo(EnumSelectedTab.CityMap),
                    Lang.Get("claims:gui-city-map-title")));
                // A village trades no land, and the host may have the market or its window switched off.
                if (!city.IsVillage && claims.config?.CITY_PLOT_TRADE_ENABLED == true
                    && claims.config?.CITY_PLOT_TRADE_GUI == true)
                {
                    navButtons.Add(new NavButton("claims:price-tag", () => GoTo(EnumSelectedTab.PlotMarket),
                        Lang.Get("claims:gui-plot-market-title")));
                }
                if (!city.IsVillage)
                {
                    navButtons.Add(new NavButton("claims:envelope", () =>
                    {
                        State.ConflictSourceTab = EnumSelectedTab.City;
                        GoTo(EnumSelectedTab.ConflictLettersPage);
                    }, Lang.Get("claims:gui-nav-conflict-letters")));
                    navButtons.Add(new NavButton("claims:frog-mouth-helm", () =>
                    {
                        State.ConflictSourceTab = EnumSelectedTab.City;
                        GoTo(EnumSelectedTab.ConflictsPage);
                    }, Lang.Get("claims:gui-nav-conflicts")));
                }

                NavRow.Build(gui, currentBounds, lineBounds, 0, navButtons.ToArray());
            }
            else
            {
                // The one screen a player without a city ever sees: a card that says what founding a
                // city costs them, and the invitations they may accept instead.
                var anchor = ctx.Line.BelowCopy(0, 14);
                anchor.Alignment = EnumDialogArea.LeftTop;

                var column = anchor.FlatCopy();
                column.fixedWidth = lineBounds.fixedWidth;

                // Measured, not a fixed 40: a longer translation of the hint ran under the buttons.
                double belowCard = Card.Note(compo, column, column.fixedY, Lang.Get("claims:gui-city-section-found"),
                    Lang.Get("claims:gui-city-found-hint"), slot =>
                {
                    var foundActions = new ActionRow(compo, slot);
                    foundActions.Add("claims:queen-crown", "createCity",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.NEED_NAME); },
                        Lang.Get("claims:gui-new-city-button"));
                    // The cheap way in: a village costs nothing and lives off supplies instead of coin.
                    if (claims.config?.VILLAGE_ENABLED != false)
                    {
                        foundActions.Add("claims:huts-village", "createVillage",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.NEW_VILLAGE_NEED_NAME); },
                            Lang.Get("claims:gui-new-village-button"));
                    }
                });

                var invites = claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations;
                if (invites.Count > 0)
                {
                    // The list keeps its own anchor rather than the page cursor: it has to start
                    // under the card, not where the tab bar left off.
                    ElementBounds listAnchor = ctx.Current.FlatCopy();
                    listAnchor.fixedY = belowCard;

                    // A fixed reserve does not know where the list starts, so the frame and its
                    // scrollbar ran past the separator and under the navigation row.
                    var listOpts = new ScrollableListOptions { Key = "city-invitations", ContainerBelowTitle = true };
                    listOpts.HeightReserve = ScrollableList.ReserveFor(gui,
                        Math.Max(InviteMinListHeight,
                            gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                                - listAnchor.fixedY - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

                    var list = ScrollableList.Add(gui, listAnchor,
                        Lang.Get("claims:gui-city-tab-invitations", invites.Count),
                        invites,
                        (ClientToCityInvitation cell, ElementBounds bounds) => new GuiElementCityInvitation(compo.Api, cell, bounds)
                        {
                            On = true,
                            OnMouseDownOnCellMiddle = new Action<int>(OnClickCellMiddle),
                            OnMouseDownOnCellRight = new Action<int>(OnClickCellRight)
                        },
                        listOpts);

                    ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
                }
                else
                {
                    // Without this the space under the card is simply blank, which reads as a
                    // half-loaded tab rather than as "nobody has invited you".
                    var emptyBounds = column.FlatCopy().WithFixedHeight(24);
                    emptyBounds.fixedY = belowCard;
                    compo.AddStaticText(Lang.Get("claims:gui-city-no-invitations"),
                        CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), emptyBounds, "no-invitations");
                }

                NavRow.Build(gui, ctx.Current, lineBounds, 0,
                    new NavButton("claims:village", () => GoTo(EnumSelectedTab.CitiesListPage), Lang.Get("claims:gui-nav-cities-list")),
                    new NavButton("claims:vertical-banner", () => GoTo(EnumSelectedTab.AllianceListPage), Lang.Get("claims:gui_alliance_list_title")));
            }
        }

        /// <summary>Balances are doubles; whole values should not read "1500.0".</summary>
        private static string Number(double value) =>
            value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        internal static void OnClickCellMiddle(int cellIndex)
        {
            ClientChat.Send("/accept " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[cellIndex].CityName);
        }

        internal static void OnClickCellRight(int cellIndex)
        {
            ClientChat.Send("/deny " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[cellIndex].CityName);
        }
    }

    public sealed class PlotColorSelectorPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        /// <summary>Edge of one colour swatch in the palette, and the gap under a swatch row.</summary>
        private const double SwatchSize = 25;

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var lineBounds = ctx.Line;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            var column = anchor.FlatCopy();
            column.fixedWidth = lineBounds.fixedWidth;
            double y = column.fixedY;

            // --- the colour the city's plots are drawn in right now ---
            double currentHeight = Card.HeaderHeight + SwatchSize + Card.Padding * 2;
            ElementBounds currentInner = Card.Frame(compo, column, y, currentHeight,
                Lang.Get("claims:gui-color-current"));

            compo.AddColorListPicker(new int[] { claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsColor },
                OnCurrentColorPicked,
                currentInner.FlatCopy().WithFixedSize(SwatchSize, SwatchSize), 100, "picker-current");

            y += currentHeight + Card.Gap;

            // --- the palette the host allows, and the button that commits a pick ---
            int[] palette = claims.config.PLOT_COLORS ?? new int[0];

            const double buttonHeight = 28;
            double paletteWidth = column.fixedWidth - Card.Padding * 2;
            int perRow = System.Math.Max(1, (int)(paletteWidth / SwatchSize));
            int rowCount = palette.Length == 0 ? 1 : (palette.Length + perRow - 1) / perRow;
            double paletteHeight = rowCount * SwatchSize;

            double selectHeight = Card.HeaderHeight + paletteHeight + Card.ActionGap + buttonHeight + Card.Padding * 2;
            ElementBounds selectInner = Card.Frame(compo, column, y, selectHeight,
                Lang.Get("claims:gui-color-select"));

            if (palette.Length == 0)
            {
                // A picker over an empty array draws nothing at all, so the tab looked broken rather
                // than switched off by the host.
                compo.AddStaticText(Lang.Get("claims:gui-color-none-available"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    selectInner.FlatCopy().WithFixedHeight(paletteHeight), "palette-empty");
            }
            else
            {
                compo.AddColorListPicker(palette, OnColorPicked,
                    selectInner.FlatCopy().WithFixedSize(SwatchSize, SwatchSize), (int)paletteWidth, "picker-2");
            }

            var applyBounds = selectInner.FlatCopy().WithFixedSize(120, buttonHeight);
            applyBounds.fixedY = selectInner.fixedY + paletteHeight + Card.ActionGap;
            compo.AddButton(Lang.Get("claims:gui-color-apply"), new ActionConsumable(() =>
            {
                if (State.SelectedColor == -1)
                {
                    return true;
                }
                ClientChat.Send("/city set colorint " + State.SelectedColor);
                State.SelectedColor = -1;
                return true;
            }), applyBounds, EnumButtonStyle.Normal);

            Tooltip.Add(compo, Lang.Get("claims:gui-color-apply-tooltip"), applyBounds, "tip-colorapply");

            // The tab bar has no entry for this page, so without this there is no way back to it.
            NavRow.Build(Gui, ctx.Current, lineBounds, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
        }

        public static void OnColorPicked(int index)
        {
            var palette = claims.config.PLOT_COLORS;
            if (palette == null || index < 0 || index >= palette.Length) return;

            claims.CANCityGui.State.SelectedColor = palette[index];
        }

        /// <summary>
        /// The single-swatch picker showing what the city uses today. It indexes its own one-element
        /// array, so it must not be routed through the palette lookup - that read PLOT_COLORS[0] and
        /// silently picked an unrelated colour.
        /// </summary>
        public static void OnCurrentColorPicked(int index)
        {
            claims.CANCityGui.State.SelectedColor = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsColor;
        }
    }

    public sealed class CitiesListPage : CANGuiPage
    {
        /// <summary>Height of the anchor the list is measured from - same shape as the alliance list.</summary>
        private const double HeadingHeight = 24;

        /// <summary>The list is never squeezed below this, however little room is left.</summary>
        private const double MinListHeight = 70;

        /// <summary>What GuiElementHorizontalTabs itself pads and spaces its tabs by.</summary>
        private const double TabPadding = 4;
        private const double TabSpacing = 5;
        private const double TabsHeight = 26;

        /// <summary>How far under the list heading the tabs sit. The list itself starts 75 below it,
        /// so this row is free space that would otherwise stay empty.</summary>
        private const double TabsBelowTitle = 44;

        private static readonly EnumCitySort[] SortOrder =
        {
            EnumCitySort.Default, EnumCitySort.Oldest, EnumCitySort.Population, EnumCitySort.Plots
        };

        private static readonly string[] SortLangKeys =
        {
            "claims:gui-citylist-sort-default",
            "claims:gui-citylist-sort-oldest",
            "claims:gui-citylist-sort-population",
            "claims:gui-citylist-sort-plots"
        };

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var column = ctx.Line.BelowCopy(0, 14);
            column.Alignment = EnumDialogArea.LeftTop;
            column.fixedWidth = ctx.Line.fixedWidth;
            column.fixedHeight = HeadingHeight;

            ElementBounds listAnchor = column.FlatCopy();

            // The list fills what is left down to the navigation row. A fixed reserve (it was 340)
            // does not know where the list actually starts, so the frame and its scrollbar ran past
            // the separator and under the back button.
            var listOpts = new ScrollableListOptions { Key = "city-stats", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                System.Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - listAnchor.fixedY - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            // How many cities there are belongs in the heading: the sort tabs beside it are only
            // worth touching once the list is long, and the count says when that is.
            var cities = Sorted().ToList();

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:gui_city_list_title") + " (" + cities.Count + ")",
                cities,
                (ClientCityInfoCellElement cell, ElementBounds bounds) => new GuiElementCityStatCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            AddSortTabs(compo, column, list.Title);

            // Reached from both the player tab and the city tab of a player with no city; either way
            // there was no way back but the top tab bar.
            NavRow.Build(Gui, column, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.Player), Lang.Get("claims:gui-nav-back")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        /// <summary>
        /// The sort control, under the list's own heading and against its right edge. It used to be
        /// a framed card of its own above the list, which cost 79 pixels of cities to label four
        /// buttons that already say what they sort by; here it costs none, because the list already
        /// leaves 75 pixels between its heading and its frame.
        /// </summary>
        private void AddSortTabs(GuiComposer compo, ElementBounds column, ElementBounds title)
        {
            GuiTab[] sortTabs = new GuiTab[SortOrder.Length];
            CairoFont tabFont = CairoFont.WhiteSmallText();

            // Horizontal tabs draw left to right from their bounds, so the width has to be known in
            // advance to right-align them. Same arithmetic the element uses: text, two paddings, a
            // rounding pixel, and the spacing between two tabs.
            double tabsWidth = 0;
            for (int i = 0; i < SortOrder.Length; i++)
            {
                string name = Lang.Get(SortLangKeys[i]);
                sortTabs[i] = new GuiTab { Name = name, DataInt = i };
                tabsWidth += tabFont.GetTextExtents(name).Width + TabPadding * 2 + 1 + TabSpacing;
            }

            var tabsBounds = column.FlatCopy().WithFixedSize(tabsWidth, TabsHeight);
            tabsBounds.fixedX = column.fixedX + column.fixedWidth - tabsWidth;
            tabsBounds.fixedY = title.fixedY + TabsBelowTitle;

            compo.AddHorizontalTabs(sortTabs, tabsBounds, (int value) =>
            {
                if (value < 0 || value >= SortOrder.Length) return;
                if (State.CitySort == SortOrder[value]) return;

                State.CitySort = SortOrder[value];
                Gui.BuildMainWindow();
            }, tabFont, tabFont, "citySortTabs");

            compo.GetHorizontalTabs("citySortTabs").activeElement = System.Array.IndexOf(SortOrder, State.CitySort);
            Tooltip.Add(compo, Lang.Get("claims:gui-citylist-sort-label"), tabsBounds, "tip-city-sort");
        }

        private IEnumerable<ClientCityInfoCellElement> Sorted()
        {
            var list = Player.AllCitiesList ?? new List<ClientCityInfoCellElement>();
            switch (State.CitySort)
            {
                case EnumCitySort.Oldest: return list.OrderBy(c => c.TimeStampCreated);
                case EnumCitySort.Population: return list.OrderByDescending(c => c.CitizensAmount);
                case EnumCitySort.Plots: return list.OrderByDescending(c => c.ClaimedPlotsAmount);
                default: return list;
            }
        }
    }
}
