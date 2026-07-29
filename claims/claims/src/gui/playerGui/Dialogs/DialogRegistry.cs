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

        public DialogRegistry()
        {
            // ---- city ----
            Add(NEED_NAME, new NeedNameDialog("claims:gui-enter-new-city-name", "/city new ", "-->", closeAfter: false));
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
