using System.Collections.Generic;
using System.Globalization;
using claims.src.auxialiry;
using claims.src.rights;
using claims.src.gui.playerGui.Widgets;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The plot the player is standing on: what it is in cards of caption/value rows, and every
    /// action on it collected into one row of icon buttons.
    /// </summary>
    public sealed class PlotPage : CANGuiPage
    {
        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            var plot = clientInfo.CurrentPlotInfo;
            var perms = clientInfo.PlayerPermissions;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = ctx.Line.fixedWidth;

            // The plot arrives with a packet, and the page can be built before the first one lands.
            if (plot?.PlotPosition == null)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-plot-data"),
                    CairoFont.WhiteDetailText().WithColor(ClaimsColors.Label),
                    anchor.FlatCopy().WithFixedHeight(24), "noPlotData");
                return;
            }

            // Unclaimed ground: there is nothing to describe and nothing to do to it, and the fields
            // of a plot page would all read empty. Its position is still worth showing - it is what
            // the claim commands take.
            if (!plot.IsClaimed)
            {
                Card.Rows(compo, anchor, anchor.fixedY, Lang.Get("claims:gui-plot-section-plot"), new List<CardRow>
                {
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-plot-label-position"),
                        Value = plot.PlotPosition.X + " / " + plot.PlotPosition.Y,
                        Key = "plotpos"
                    },
                    new CardRow
                    {
                        Label = Lang.Get("claims:gui-plot-label-owner"),
                        Value = Lang.Get("claims:plot_not_claimed"),
                        ValueColor = ClaimsColors.Label,
                        Key = "plotunclaimed"
                    },
                });
                return;
            }

            // Buttons the player has no right to press are left out: pressing them only ever earned
            // a refusal from the server.
            bool canEditPlot = perms.HasPermission(EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS)
                            || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_ALL_OWN_PLOT);
            bool owned = plot.OwnerName?.Length > 0;
            bool forSale = plot.Price > -1;

            double y = anchor.fixedY;

            // --- what this plot is ---
            y = Card.Rows(compo, anchor, y, Lang.Get("claims:gui-plot-section-plot"), new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plot-label-name"),
                    Value = plot.PlotName?.Length > 0 ? plot.PlotName : Lang.Get("claims:gui-plot-unnamed"),
                    Key = "plotname"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plot-label-position"),
                    Value = plot.PlotPosition.X + " / " + plot.PlotPosition.Y,
                    Key = "plotpos"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plot-label-type"),
                    Value = PlotInfo.dictPlotTypes.TryGetValue(plot.PlotType, out PlotInfo plotInfo)
                        ? Lang.Get("claims:gui-plot-type-" + plotInfo.getFullName())
                        : "-",
                    Key = "plottype"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plot-label-owner"),
                    Value = owned ? plot.OwnerName : Lang.Get("claims:gui-plot-no-owner"),
                    ValueColor = owned ? null : ClaimsColors.Label,
                    Key = "plotowner"
                },
            });

            // --- what it costs ---
            y = Card.Rows(compo, anchor, y, Lang.Get("claims:gui-plot-section-economy"), new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plot-label-price"),
                    Value = forSale ? Number(plot.Price) : Lang.Get("claims:gui-not-for-sale"),
                    ValueColor = forSale ? ClaimsColors.Success : ClaimsColors.Label,
                    Key = "plotprice"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-plot-label-tax"),
                    Value = Number(plot.CustomTax),
                    Key = "plottax"
                },
            });

            // --- what another city would pay for the ground itself ---
            bool tradeOn = claims.config?.CITY_PLOT_TRADE_ENABLED == true;
            bool ourPlot = plot.CityName?.Length > 0 && plot.CityName == clientInfo.CityInfo?.Name;
            bool listedForCities = plot.PriceForCityBuy > -1;
            bool auctionOn = tradeOn && claims.config?.CITY_PLOT_AUCTION_ENABLED == true;
            bool onAuction = plot.AuctionEndsAt > 0;

            if (tradeOn && (ourPlot || listedForCities || onAuction))
            {
                var marketRows = new List<CardRow>();
                // While the plot is under the hammer the asking-price row would read "not for sale",
                // which is the opposite of what is happening to it.
                if (!onAuction)
                {
                    marketRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-plot-label-city-price"),
                        Value = listedForCities
                            ? Number(plot.PriceForCityBuy)
                            : Lang.Get("claims:gui-not-for-sale"),
                        ValueColor = listedForCities ? ClaimsColors.Success : ClaimsColors.Label,
                        Key = "plotcityprice"
                    });
                }
                if (listedForCities)
                {
                    marketRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-plot-label-city-audience"),
                        Value = plot.SaleAudience == EnumPlotSaleAudience.SPECIFIC_CITY
                                && plot.SaleTargetCityName?.Length > 0
                            ? plot.SaleTargetCityName
                            : Lang.Get(plot.SaleAudience.LangKey()),
                        Key = "plotcityaudience"
                    });
                }
                if (onAuction)
                {
                    long left = plot.AuctionEndsAt - TimeFunctions.getEpochSeconds();
                    marketRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-plot-label-auction-bid"),
                        Value = plot.AuctionCurrentBid >= 0
                            ? Number(plot.AuctionCurrentBid)
                            : Lang.Get("claims:plot_auction_no_bids"),
                        ValueColor = plot.AuctionCurrentBid >= 0 ? ClaimsColors.Success : ClaimsColors.Label,
                        Key = "plotauctionbid"
                    });
                    marketRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-auction-duration"),
                        Value = left > 0
                            ? Lang.Get("claims:gui-auction-time-left", left / 3600, (left % 3600) / 60)
                            : Lang.Get("claims:gui-auction-closing"),
                        Key = "plotauctionleft"
                    });
                    // Without this the buyer has to guess whether the lot can be taken outright.
                    if (plot.AuctionBuyout >= 0)
                    {
                        marketRows.Add(new CardRow
                        {
                            Label = Lang.Get("claims:gui-auction-buyout-label"),
                            Value = Number(plot.AuctionBuyout),
                            Key = "plotauctionbuyout"
                        });
                    }
                }
                // Why the buy/bid button is not there. The rule itself is the server's verdict;
                // the one thing it cannot know is whether this particular player may press it.
                string blockedReason = plot.CityBuyBlockedReason;
                if (!(blockedReason?.Length > 0)
                    && (plot.CanBuyAsCity || plot.CanBidAsCity)
                    && !perms.HasPermission(EnumPlayerPermissions.CITY_BUY_PLOT_FROM_CITY))
                {
                    blockedReason = "claims:you_dont_have_right_for_that_command";
                }
                if (blockedReason?.Length > 0)
                {
                    marketRows.Add(new CardRow
                    {
                        Label = Lang.Get("claims:gui-plot-label-city-buy-blocked"),
                        Value = Lang.Get(blockedReason),
                        ValueColor = ClaimsColors.Warning,
                        Key = "plotcityblocked"
                    });
                }

                y = Card.RowsWithActions(compo, anchor, y, Lang.Get("claims:gui-plot-section-city-market"), marketRows, slot =>
                {
                    var marketActions = new ActionRow(compo, slot);

                    if (ourPlot && perms.HasPermission(EnumPlayerPermissions.CITY_SELL_PLOT_TO_CITY))
                    {
                        // A plot is sold one way at a time, so the auction controls replace the
                        // fixed-price ones while a lot is running, and vice versa.
                        // Withdrawal is offered only while nobody has bid: after the first bid the
                        // server refuses it, and a button that always answers "no" is worse than none.
                        if (auctionOn && onAuction && plot.AuctionCurrentBid < 0)
                        {
                            marketActions.Add("claims:cancel", "cancelPlotAuction",
                                on => { if (on) OpenDialog(EnumUpperWindowSelectedState.PLOT_AUCTION_CANCEL_CONFIRM); },
                                Lang.Get("claims:gui-auction-cancel-tooltip"));
                        }
                        else if (auctionOn && !listedForCities)
                        {
                            marketActions.Add("claims:open-book", "startPlotAuction",
                                on => Toggle(on, EnumUpperWindowSelectedState.PLOT_AUCTION_START),
                                Lang.Get("claims:gui-auction-start-tooltip"), toggleable: true);
                        }

                        if (!onAuction)
                        {
                            marketActions.Add("claims:price-tag", "setPlotCityPrice",
                                on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_CITY_PRICE_NEED_NUMBER),
                                Lang.Get("claims:gui-plot-set-city-price-tooltip"), toggleable: true);
                        }

                        if (listedForCities)
                        {
                            marketActions.Add("claims:open-book", "setPlotCityAudience",
                                on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_CITY_AUDIENCE),
                                Lang.Get("claims:gui-plot-set-city-audience-tooltip"), toggleable: true);

                            // Naming the buyer belongs to a listing addressed to one city and to no
                            // other kind: under "allies" or "everyone" the button asked a question
                            // whose answer the offer would not use.
                            if (plot.SaleAudience == part.structure.plots.EnumPlotSaleAudience.SPECIFIC_CITY)
                            {
                                marketActions.Add("claims:village", "setPlotCityTarget",
                                    on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_CITY_TARGET),
                                    Lang.Get("claims:gui-plot-set-city-target-tooltip"), toggleable: true);
                            }

                            marketActions.Add("claims:cancel", "setPlotCityNfs", on =>
                            {
                                ClientChat.Send("/plot citynfs");
                                // Optimistic local update, as with the citizen listing above.
                                plot.PriceForCityBuy = -1;
                                Gui.BuildMainWindow();
                            }, Lang.Get("claims:gui-plot-city-not-for-sale-tooltip"));
                        }
                    }

                    // CanBuyAsCity is the server's own verdict: no need to re-judge war, limits or money here.
                    if (plot.CanBuyAsCity && perms.HasPermission(EnumPlayerPermissions.CITY_BUY_PLOT_FROM_CITY))
                    {
                        marketActions.Add("claims:receive-money", "buyPlotAsCity",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.PLOT_CITY_BUY_CONFIRM); },
                            Lang.Get("claims:gui-plot-city-buy-tooltip"));
                    }

                    // Bidding on the plot underfoot needs no coordinates, so the dialog is opened
                    // without them and the command reads the lot from where the player stands.
                    if (plot.CanBidAsCity && perms.HasPermission(EnumPlayerPermissions.CITY_BUY_PLOT_FROM_CITY))
                    {
                        marketActions.Add("claims:price-tag", "bidOnPlotAuction", on =>
                        {
                            if (!on) return;
                            // Pos is cleared explicitly: it may still hold the plot of a lot bid on
                            // from the market tab, which would send the bid to the wrong lot.
                            OpenDialog(EnumUpperWindowSelectedState.PLOT_AUCTION_BID, dialogArgs =>
                            {
                                dialogArgs.Pos = null;
                                dialogArgs.Second = plot.AuctionMinNextBid.ToString();
                            });
                        }, Lang.Get("claims:gui-auction-bid-tooltip", plot.AuctionMinNextBid));
                    }
                });
            }

            // --- everything that can be done to it ---
            Card.Actions(compo, anchor, y, Lang.Get("claims:gui-plot-section-actions"), slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (canEditPlot || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_NAME))
                {
                    actions.Add("claims:pencil", "setPlotName",
                        on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_NAME),
                        Lang.Get("claims:gui-plot-set-name-tooltip"), toggleable: true);
                }

                if (canEditPlot || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_TYPE))
                {
                    actions.Add("claims:hamburger-menu", "setPlotType",
                        on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_TYPE),
                        Lang.Get("claims:gui-plot-set-type-tooltip"), toggleable: true);
                }

                // A citizen buys a plot of their own city; an embassy is the one plot a stranger may
                // take. Elsewhere the button only ever earned cannot_buy_plot_in_another_city.
                if (forSale && (ourPlot || plot.PlotType == PlotType.EMBASSY))
                {
                    actions.Add("claims:receive-money", "buyPlot",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.PLOT_CLAIM); },
                        Lang.Get("claims:gui-plot-buy-tooltip"));
                }

                if (owned)
                {
                    actions.Add("claims:exit-door", "unclaimPlot",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.PLOT_UNCLAIM); },
                        Lang.Get("claims:gui-plot-unclaim-tooltip"));
                }

                if (canEditPlot || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_FS))
                {
                    actions.Add("claims:price-tag", "setPlotPrice",
                        on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_PRICE_NEED_NUMBER),
                        Lang.Get("claims:gui-plot-set-price-tooltip"), toggleable: true);
                }

                if (canEditPlot || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_NFS))
                {
                    actions.Add("claims:cancel", "setPlotNfs", on =>
                    {
                        ClientChat.Send("/plot nfs");
                        // Optimistic local update, so the price line reads "not for sale" at once.
                        plot.Price = -1;
                        Gui.BuildMainWindow();
                    }, Lang.Get("claims:gui-plot-not-for-sale-tooltip"));
                }

                if (canEditPlot || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_FEE))
                {
                    actions.Add("claims:contract", "setPlotTax",
                        on => Toggle(on, EnumUpperWindowSelectedState.PLOT_SET_TAX),
                        Lang.Get("claims:gui-plot-set-tax-tooltip"), toggleable: true);
                }

                if (canEditPlot || perms.HasPermission(EnumPlayerPermissions.PLOT_SET_PLOT_ACCESS_PERMISSIONS))
                {
                    actions.Add("claims:open-book", "setPermissions",
                        on => Toggle(on, EnumUpperWindowSelectedState.PLOT_PERMISSIONS),
                        Lang.Get("claims:gui-plot-set-permissions-tooltip"), toggleable: true);
                }

                // The prison and summon plots each get the one command that only makes sense on them.
                if (plot.PlotType == PlotType.PRISON && perms.HasPermission(EnumPlayerPermissions.CITY_PRISON_ADD_CELL))
                {
                    actions.Add("claims:prisoner", "addPrisonCell", on => Run("/c prison addcell"),
                        Lang.Get("claims:gui-plot-add-prison-cell-tooltip"));
                }
                else if (plot.PlotType == PlotType.SUMMON && perms.HasPermission(EnumPlayerPermissions.CITY_SET_SUMMON))
                {
                    actions.Add("claims:magic-portal", "setSummonPoint", on => Run("/c summon set point"),
                        Lang.Get("claims:gui-plot-set-summon-tooltip"));
                }

                actions.Add("claims:highlighter", "showBorders", on => Run("/plot borders on"),
                    Lang.Get("claims:gui-plot-show-borders-tooltip"));

                // "eraser" is one of the game's own icons, not one of the mod's - no "claims:" prefix.
                actions.Add("eraser", "hideBorders", on => Run("/plot borders off"),
                    Lang.Get("claims:gui-plot-hide-borders-tooltip"));
            });
        }

        /// <summary>A toggleable button opens its form, and closes it again when switched back off.</summary>
        private void Toggle(bool on, EnumUpperWindowSelectedState dialog) =>
            OpenDialog(on ? dialog : EnumUpperWindowSelectedState.NONE);

        private void Run(string command)
        {
            ClientChat.Send(command);
            Gui.BuildUpperWindow();
        }

        /// <summary>Prices and taxes are doubles; whole values should not read "1500.0".</summary>
        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
