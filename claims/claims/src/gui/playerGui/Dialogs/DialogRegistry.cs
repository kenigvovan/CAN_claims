using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.plots;
using Vintagestory.API.Config;
using static claims.src.gui.playerGui.EnumUpperWindowSelectedState;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Which form each secondary-window state shows. This replaces a 1500-line if/else chain in
    /// which twenty branches were "prompt, text field, button" spelled out longhand.
    ///
    /// Add() is public so later work can register the missing dialogs - city fee, withdraw, deposit,
    /// union letters, NAP offers - without editing this constructor.
    /// </summary>
    public sealed class DialogRegistry
    {
        private readonly Dictionary<EnumUpperWindowSelectedState, CANGuiDialogPanel> dialogs
            = new Dictionary<EnumUpperWindowSelectedState, CANGuiDialogPanel>();

        private static ClientPlayerInfoAccess P => new ClientPlayerInfoAccess();

        /// <summary>Small indirection so the option providers stay readable.</summary>
        internal sealed class ClientPlayerInfoAccess
        {
            public string[] Citizens => claims.clientDataStorage.clientPlayerInfo.CityInfo?.PlayersNames.ToArray() ?? new string[0];
            public string[] Friends => claims.clientDataStorage.clientPlayerInfo.Friends?.ToArray() ?? new string[0];
            public string[] Criminals => claims.clientDataStorage.clientPlayerInfo.CityInfo?.Criminals.ToArray() ?? new string[0];
            public string[] PlotsGroups => claims.clientDataStorage.clientPlayerInfo.CityInfo?.PlotsGroupCells.Select(g => g.Name).ToArray() ?? new string[0];
        }

        /// <summary>Plot types the host has not switched off, code plus display name key.</summary>
        private static List<KeyValuePair<string, string>> EnabledPlotTypes()
            => PlotInfo.plotAccessableForPlayersWithCode
                       .Where(p => claims.config?.DISABLED_PLOT_TYPES?.Contains(p.Key) != true)
                       .ToList();

        private static PlotsGroupCellElement SelectedGroup()
            => claims.clientDataStorage.clientPlayerInfo.CityInfo?.PlotsGroupCells
                     .FirstOrDefault(g => g.Guid.Equals(claims.CANCityGui.State.DialogArgs.Selected));

        private static string SelectedGroupName() => SelectedGroup()?.Name;

        /// <summary>Audiences offered in the GUI, command word plus display key. Selling to one named
        /// city stays a command-line option: it needs a city name the dropdown cannot ask for.</summary>
        private static readonly List<KeyValuePair<string, string>> MarketAudiences = new()
        {
            new("allies", "claims:plot_trade_audience_allies"),
            new("nonhostile", "claims:plot_trade_audience_nonhostile"),
            new("everyone", "claims:plot_trade_audience_everyone")
        };

        /// <summary>The audience the plot underfoot is listed with, so resending keeps it.</summary>
        private static string CurrentAudienceWord()
        {
            var plot = claims.clientDataStorage.clientPlayerInfo?.CurrentPlotInfo;
            return plot?.SaleAudience switch
            {
                EnumPlotSaleAudience.NON_HOSTILE => "nonhostile",
                EnumPlotSaleAudience.EVERYONE => "everyone",
                EnumPlotSaleAudience.SPECIFIC_CITY => "city",
                _ => "allies"
            };
        }

        /// <summary>
        /// The listing rebuilt as command arguments. An addressed offer has to carry its buyer along:
        /// resending it without the name would widen "only this city" to "all allies" behind the
        /// mayor's back, just because the price changed.
        /// </summary>
        private static string CurrentAudienceArgs()
        {
            string word = CurrentAudienceWord();
            if (word != "city") return word;

            string target = claims.clientDataStorage.clientPlayerInfo?.CurrentPlotInfo?.SaleTargetCityName;
            // Without a resolvable buyer the server would refuse the command, so fall back to the
            // narrowest audience rather than sending something that cannot be applied.
            return target?.Length > 0 ? "city " + target : "allies";
        }

        /// <summary>Every city but our own - the possible buyers of an addressed offer.</summary>
        internal static string[] OtherCityNames()
        {
            string ours = claims.clientDataStorage.clientPlayerInfo?.CityInfo?.Name;
            return claims.clientDataStorage.clientPlayerInfo?.AllCitiesList?
                       .Select(c => c.Name)
                       .Where(n => n != ours)
                       .OrderBy(n => n)
                       .ToArray()
                   ?? new string[0];
        }

        /// <summary>Plot coordinates of the plot underfoot, as the buy command takes them.</summary>
        private static int CurrentPlotX()
            => claims.clientDataStorage.clientPlayerInfo?.CurrentPlotInfo?.PlotPosition?.X ?? 0;

        private static int CurrentPlotZ()
            => claims.clientDataStorage.clientPlayerInfo?.CurrentPlotInfo?.PlotPosition?.Y ?? 0;

        /// <summary>Asking price of the plot underfoot; 0 when it is not listed yet.</summary>
        private static int CurrentCityPrice()
        {
            int price = claims.clientDataStorage.clientPlayerInfo?.CurrentPlotInfo?.PriceForCityBuy ?? -1;
            return price < 0 ? 0 : price;
        }

        public DialogRegistry()
        {
            // ---- city ----
            Add(NEED_NAME, new NeedNameDialog("claims:gui-enter-new-city-name", "/city new ", "-->", closeAfter: false));
            // ---- village ----
            Add(NEW_VILLAGE_NEED_NAME, new NeedNameDialog("claims:gui-enter-new-village-name", "/village new ", "-->", closeAfter: false));
            Add(VILLAGE_ABANDON_CONFIRM, new YesNoDialog("claims:gui-village-abandon-confirm", "/village abandon"));
            Add(VILLAGE_UPGRADE_CONFIRM, new YesNoDialog("claims:gui-village-upgrade-confirm", "/village upgrade"));
            Add(NEED_AGREE, new AgreeDialog());
            Add(SELECT_NEW_CITY_NAME, new NeedNameDialog("claims:gui-enter-new-city-name", "/city set name ", "claims:gui-set-name-button"));
            Add(INVITE_TO_CITY_NEED_NAME, new NeedNameDialog("claims:gui-enter-player-name", "/city invite ", "claims:gui-invite-button"));
            Add(UNINVITE_TO_CITY, new NeedNameDialog("claims:gui-enter-player-name", "/city uninvite ", "claims:gui-uninvite-button"));
            Add(KICK_FROM_CITY_NEED_NAME, new DropDownDialog("claims:gui-enter-player-name", () => P.Citizens, "/city kick ", "claims:gui-kick-button"));
            Add(LEAVE_CITY_CONFIRM, new YesNoDialog("claims:gui-leave-city-confirm-button", "/city leave"));
            Add(CLAIM_CITY_PLOT_CONFIRM, new YesNoDialog("claims:gui-claim-current-plot-confirm", "/city claim"));
            Add(UNCLAIM_CITY_PLOT_CONFIRM, new YesNoDialog("claims:gui-unclaim-current-plot-confirm", "/city unclaim"));
            Add(CITY_PLOTS_PERMISSIONS, new PermissionsDialog("claims:gui-city-permissions-title", PermissionsScopes.City));

            // ---- friends and criminals ----
            Add(ADD_FRIEND_NEED_NAME, new NeedNameDialog("claims:gui-enter-player-name", "/citizen friend add ", "claims:gui-add-button"));
            Add(REMOVE_FRIEND, new DropDownDialog("claims:gui-enter-player-name", () => P.Friends, "/citizen friend remove ", "claims:gui-remove-button"));
            Add(ADD_CRIMINAL_NEED_NAME, new NeedNameDialog("claims:gui-enter-player-name", "/city criminal add ", "claims:gui-add-button"));
            Add(REMOVE_CRIMINAL, new DropDownDialog("claims:gui-enter-player-name", () => P.Criminals, "/city criminal remove ", "claims:gui-remove-button"));

            // ---- plot ----
            Add(PLOT_CLAIM, new YesNoDialog("claims:gui-claim-current-plot-confirm", "/plot claim"));
            Add(PLOT_UNCLAIM, new YesNoDialog("claims:gui-unclaim-current-plot-confirm", "/plot unclaim"));
            Add(PLOT_SET_NAME, new NeedNameDialog("claims:gui-enter-plot-name", "/plot set name ", "claims:gui-set-name-short-button"));
            Add(PLOT_SET_PRICE_NEED_NUMBER, new NeedNameDialog("claims:gui-enter-plot-price", "/plot fs ", "claims:gui-set-price-button",
                    inputKind: EnumDialogInput.Integer));
            Add(PLOT_SET_TAX, new NeedNameDialog("claims:gui-enter-plot-tax", "/plot set fee ", "claims:gui-set-tax-button",
                    inputKind: EnumDialogInput.Decimal));
            // Host-disabled types are hidden here as well; the server still enforces the list in
            // Plot.setNewType, so this only keeps the player from picking a doomed option.
            Add(PLOT_SET_TYPE, new DropDownDialog("claims:gui-select-plot-type",
                    () => EnabledPlotTypes().Select(p => p.Key).ToArray(),
                    "/plot set type ", "claims:gui-set-type-button",
                    displayNamesProvider: () => EnabledPlotTypes().Select(p => Lang.Get(p.Value)).ToArray()));
            Add(PLOT_PERMISSIONS, new PermissionsDialog("claims:gui-plot-permissions-title", PermissionsScopes.Plot));

            // ---- inter-city plot market ----
            // Price and audience are one listing, so setting either resends both: entering a new
            // price must not silently widen an "allies only" offer to everyone.
            Add(PLOT_SET_CITY_PRICE_NEED_NUMBER, new NeedNameDialog("claims:gui-enter-plot-city-price", null,
                    "claims:gui-set-price-button", inputKind: EnumDialogInput.Integer,
                    commandBuilder: args => "/plot citysell " + args.Text + " " + CurrentAudienceArgs()));
            Add(PLOT_SET_CITY_AUDIENCE, new DropDownDialog("claims:gui-select-plot-city-audience",
                    () => MarketAudiences.Select(a => a.Key).ToArray(),
                    null, "claims:gui-set-button",
                    displayNamesProvider: () => MarketAudiences.Select(a => Lang.Get(a.Value)).ToArray(),
                    commandBuilder: args => "/plot citysell " + CurrentCityPrice() + " " + args.Text));
            // Naming one buyer is its own step: the audience dropdown has no way to ask for a city.
            Add(PLOT_SET_CITY_TARGET, new DropDownDialog("claims:gui-select-plot-city-target",
                    () => OtherCityNames(), null, "claims:gui-set-button",
                    commandBuilder: args => "/plot citysell " + CurrentCityPrice() + " city " + args.Text));
            // The price the player is looking at travels with the command: the seller may re-price
            // the offer between the browser drawing it and the button being pressed, and a purchase
            // must never be charged a price nobody agreed to.
            Add(PLOT_CITY_BUY_CONFIRM, new YesNoDialog("claims:gui-plot-city-buy-confirm",
                    args => string.Format("/plot citybuy {0} {1} {2}",
                        CurrentPlotX(), CurrentPlotZ(), CurrentCityPrice()),
                    textArgs: args => new object[] { CurrentCityPrice() }));
            // From the market tab the plot is named by its coordinates, not by where the player stands.
            // Pos carries the plot coordinates: X and Z, with Y unused.
            Add(PLOT_MARKET_BUY_CONFIRM, new YesNoDialog("claims:gui-plot-market-buy-confirm",
                    args => string.Format("/plot citybuy {0} {1} {2}", args.Pos.X, args.Pos.Z, args.Second),
                    textArgs: args => new object[] { args.First, args.Second }));

            // ---- land auction ----
            // Price plus a duration picked from the lengths the host allows. Increment, buyout and a
            // named buyer stay command-line options: a form with five fields is a command line with
            // extra steps.
            Add(PLOT_AUCTION_START, new AuctionStartDialog());
            Add(PLOT_AUCTION_CANCEL_CONFIRM, new YesNoDialog("claims:gui-auction-cancel-confirm", "/plot auctioncancel"));
            // Pos carries the lot's plot coordinates when the bid comes from the market tab; from the
            // plot page there are none and the lot is the one underfoot.
            Add(PLOT_AUCTION_BID, new NeedNameDialog("claims:gui-enter-auction-bid", null,
                    "claims:gui-auction-bid-button", inputKind: EnumDialogInput.Integer,
                    commandBuilder: args => args.Pos != null
                        ? string.Format("/plot bid {0} {1} {2}", args.Text, args.Pos.X, args.Pos.Z)
                        : "/plot bid " + args.Text));
            // A bankrupt settlement sold whole stands on no plot, so its lot is addressed by name.
            Add(PLOT_AUCTION_BID_CITY, new NeedNameDialog("claims:gui-enter-auction-bid", null,
                    "claims:gui-auction-bid-button", inputKind: EnumDialogInput.Integer,
                    commandBuilder: args => string.Format("/plot bidcity {0} {1}", args.First, args.Text)));

            // ---- ranks ----
            Add(CITY_RANK_CREATION_NEED_NAME, new NeedNameDialog("claims:gui-enter-rank-name", "/c rank create ", "claims:gui-add-button"));
            Add(CITY_RANK_DELETE_CONFIRM, new YesNoDialog("claims:gui-remove-rank-confirm",
                    args => "/c rank delete " + args.Selected,
                    textArgs: args => new object[] { args.Selected }));
            Add(CITY_RANK_REMOVE_CONFIRM, new YesNoDialog("claims:gui-strip-rank-from-player",
                    args => "/city rank remove " + args.First + " " + args.Second,
                    textArgs: args => new object[] { args.Second, args.First }));
            Add(CITY_RANK_ADD, new RankAddDialog());

            // ---- prison and summon ----
            Add(CITY_PRISON_REMOVE_CELL_CONFIRM, new YesNoDialog("claims:gui-prison-cell-remove-confirm",
                    args => string.Format("/c prison cremovecell {0} {1} {2}", args.Pos.X, args.Pos.Y, args.Pos.Z),
                    textArgs: args => new object[] { args.First }));
            Add(CITY_SUMMON_NEED_NAME, new NeedNameDialog("claims:gui-enter-summon-point-name", null, "claims:gui-set-button",
                    commandBuilder: args => string.Format("/city summon set cname {0} {1} {2} {3}",
                        args.Pos.X, args.Pos.Y, args.Pos.Z, args.Text)));

            // ---- plots groups ----
            Add(CITY_PLOTSGROUP_ADD_NEW_NEED_NAME, new NeedNameDialog("claims:gui-enter-plotsgroup-name", "/c plotsgroup create ", "claims:gui-add-button"));
            Add(ADD_PLOTSGROUP_MEMBER_NEED_NAME, new NeedNameDialog("claims:gui-enter-player-name", null, "claims:gui-invite-button",
                    commandBuilder: args => string.Format("/c plotsgroup add {0} {1}", SelectedGroupName(), args.Text)));
            Add(REMOVE_PLOTSGROUP_MEMBER_SELECT, new SelectThenConfirmDialog("claims:gui-kick-from-plotsgroup-title",
                    () => SelectedGroup()?.PlayersNames.ToArray() ?? new string[0],
                    "claims:gui-kick-button", REMOVE_PLOTSGROUP_MEMBER_CONFIRM,
                    textArgs: () => new object[] { SelectedGroupName() }));
            Add(REMOVE_PLOTSGROUP_MEMBER_CONFIRM, new YesNoDialog("claims:gui-kick-player-from-plotsgroup",
                    args => string.Format("/c plotsgroup kick {0} {1}", SelectedGroupName(), args.Text),
                    textArgs: args => new object[] { SelectedGroupName(), args.Text }));
            Add(CITY_PLOTSGROUP_REMOVE_SELECT, new SelectThenConfirmDialog("claims:gui-select-plotsgroup-to-remove",
                    () => P.PlotsGroups, "claims:gui-remove-button", CITY_PLOTSGROUP_REMOVE_CONFIRM));
            // Unlike the other plots group dialogs this one identifies the group by the name picked
            // in the preceding select dialog, not by the guid the page put in Selected.
            Add(CITY_PLOTSGROUP_REMOVE_CONFIRM, new YesNoDialog("claims:gui-remove-plotsgroup-confirm",
                    args => string.Format("/c plotsgroup delete {0}", args.Text),
                    textArgs: args => new object[] { args.Text }));
            Add(CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM, new YesNoDialog("claims:gui-claim-plot-plotsgroup",
                    args => string.Format("/c plotsgroup plotadd {0}", SelectedGroupName()),
                    textArgs: args => new object[] { SelectedGroupName() }));
            Add(CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM, new YesNoDialog("claims:gui-unclaim-plot-plotsgroup",
                    args => string.Format("/c plotsgroup plotremove {0}", SelectedGroupName()),
                    textArgs: args => new object[] { SelectedGroupName() }));
            Add(CITY_PLOTSGROUP_PERMISSIONS, new PermissionsDialog("claims:gui-plotsgroup-permissions-title",
                    PermissionsScopes.PlotsGroup(SelectedGroupName, () => SelectedGroup()?.PermsHandler)));

            // ---- alliance ----
            Add(NEW_ALLIANCE_NEED_NAME, new NeedNameDialog("claims:gui-enter-alliance-name", "/a create ", "claims:gui-create-button"));
            Add(SELECT_NEW_ALLIANCE_NAME, new NeedNameDialog("claims:gui-enter-alliance-name", "/a set name ", "claims:gui-set-button"));
            Add(ALLIANCE_PREFIX_NEED_NAME, new NeedNameDialog("claims:gui-enter-alliance-prefix", "/a set prefix ", "claims:gui-set-button"));
            Add(INVITE_TO_ALLIANCE_NEED_NAME, new NeedNameDialog("claims:gui-enter-alliance-name", "/a invite ", "claims:gui-add-button"));
            Add(KICK_FROM_ALLIANCE_NEED_NAME, new NeedNameDialog("claims:gui-enter-alliance-name", "/a kick ", "claims:gui-remove-button"));
            Add(LEAVE_ALLIANCE_CONFIRM, new YesNoDialog("claims:gui-leave-alliance-confirm-button", "/alliance leave"));
            Add(ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME, new NeedNameDialog("claims:name_of_alliance_to_send_conflict_letter",
                    "/a conflict declare ", "claims:gui-confirm-button"));
            Add(ALLIANCE_SEND_PEACE_OFFER_CONFIRM, new PeaceOfferDialog());

            // ---- treasury ----
            Add(CITY_SET_FEE, new NeedNameDialog("claims:gui-enter-city-fee", "/c set fee ", "claims:gui-set-button",
                    inputKind: EnumDialogInput.Integer));
            Add(CITY_WITHDRAW, new NeedNameDialog("claims:gui-enter-city-withdraw-amount", "/city withdraw ", "claims:gui-city-withdraw-button",
                    inputKind: EnumDialogInput.Integer));
            Add(CITY_DEPOSIT_CONFIRM, new YesNoDialog("claims:gui-city-deposit-confirm", "/city deposit"));

            // ---- diplomacy ----
            Add(ALLIANCE_SEND_NEW_UNION_LETTER_NEED_NAME, new NeedNameDialog("claims:gui-enter-alliance-name", "/a union declare ", "claims:gui-confirm-button"));
            Add(ALLIANCE_CANCEL_UNION_SELECT, new DropDownDialog("claims:gui-select-union-to-leave-title",
                    () => claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Allies.ToArray() ?? new string[0],
                    "/a union revoke ", "claims:gui-confirm-button"));
            Add(CITY_SEND_NEW_CONFLICT_LETTER_NEED_NAME, new DeclareConflictDialog("/c war declare "));
            Add(CITY_SEND_NAP_OFFER_NEED_NAME, new DeclareConflictDialog("/city war nap offer ", "claims:name_of_target_to_send_nap"));
            Add(ALLIANCE_SEND_NAP_OFFER_NEED_NAME, new DeclareConflictDialog("/a conflict nap offer ", "claims:name_of_target_to_send_nap"));
            Add(SEND_ULTIMATUM, new UltimatumDialog());

            // There is no alliance uninvite dialog: the server only has /city uninvite, so the button
            // that used to open one has been dropped along with its enum value.
        }

        public void Add(EnumUpperWindowSelectedState state, CANGuiDialogPanel dialog) => dialogs[state] = dialog;

        public bool TryGet(EnumUpperWindowSelectedState state, out CANGuiDialogPanel dialog)
            => dialogs.TryGetValue(state, out dialog);
    }
}
