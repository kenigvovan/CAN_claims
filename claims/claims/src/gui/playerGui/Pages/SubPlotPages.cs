using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.Dialogs;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.rights;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The sub-plots of our city: what they cost the player, the tools to cut new ones, and the list,
    /// each row opening its own page.
    /// </summary>
    public sealed class SubPlotsPage : CANGuiPage
    {
        private const int MaxRowsShown = 9;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var subPlots = Player.CityInfo.SubPlotCells ?? new List<SubPlotCellElement>();
            bool canManage = Player.PlayerPermissions.HasPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);
            string me = claims.capi?.World?.Player?.PlayerName ?? "";
            var mine = subPlots.Where(s => s.OwnerName == me).ToList();

            ElementBounds column = CardAnchor(ctx);
            var summary = new List<CardRow>
            {
                new CardRow { Label = Lang.Get("claims:gui-subplot-label-count"), Value = subPlots.Count.ToString(), Key = "spCount" }
            };
            if (mine.Count > 0)
            {
                summary.Add(new CardRow { Label = Lang.Get("claims:gui-subplot-label-yours"), Value = mine.Count.ToString(), Key = "spMine" });
                summary.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-subplot-label-you-pay"),
                    Value = SubPlotInfoPage.Number(mine.Sum(s => s.Tax)),
                    Key = "spOwed"
                });
            }
            // Only when an answer is owed: a raise not yet accepted on one of the player's own.
            if (mine.Any(s => s.HasPendingTax && !s.PendingTaxAccepted))
            {
                summary.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-subplot-label-tax-pending"),
                    Value = Lang.Get("claims:gui-subplot-tax-pending-yours"),
                    ValueColor = ClaimsColors.Warning,
                    Key = "spPending"
                });
            }

            double y = Card.RowsWithActions(compo, column, column.fixedY, Lang.Get("claims:gui-subplot-title"), summary, slot =>
            {
                if (!canManage) return;
                var actions = new ActionRow(compo, slot);
                actions.Add("claims:large-paint-brush", "subplotWand",
                    on => { if (on) ClientChat.Send("/subplot wand"); },
                    Lang.Get("claims:gui-subplot-get-wand"));
                actions.Add("plus", "subplotCreate",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_CREATE_NEED_NAME); },
                    Lang.Get("claims:gui-subplot-create"));
                actions.Add("claims:cancel", "subplotClearSel",
                    on => { if (on) ClientChat.Send("/subplot clearsel"); },
                    Lang.Get("claims:gui-subplot-clear-selection"));
            });

            var items = subPlots.Select(s => new ItemRow
            {
                Label = s.Name,
                Value = Summary(s),
                ValueColor = s.IsForSale ? ClaimsColors.Success : null,
                Key = "subplot-" + s.Guid,
                Buttons = new List<RowButton>
                {
                    new RowButton("claims:open-book", () =>
                    {
                        State.DialogArgs.Selected = s.Guid;
                        GoTo(EnumSelectedTab.SubPlotInfoPage);
                    }, Lang.Get("claims:gui-subplot-open"))
                }
            }).ToList();

            // With no sub-plots the list's empty text says what they are and how to make one.
            Card.Items(compo, column, y, Lang.Get("claims:gui-subplot-section-list") + " (" + subPlots.Count + ")",
                Card.Limit(items, MaxRowsShown), Lang.Get(canManage ? "claims:gui-subplot-description-manager" : "claims:gui-subplot-description"),
                null);
        }

        /// <summary>"Bob · tax 5" or "the city's · for sale 100".</summary>
        private static string Summary(SubPlotCellElement s)
        {
            var parts = new List<string> { s.OwnerName.Length > 0 ? s.OwnerName : Lang.Get("claims:banner-subplot-city") };
            string offer = subplots.SubPlotText.Offer(s.Price, s.Tax);
            if (offer != null) parts.Add(offer);
            // A rent already names the tax.
            if (s.Tax > 0 && s.Price != 0) parts.Add(Lang.Get("claims:gui-subplot-tax", SubPlotInfoPage.Number(s.Tax)));
            return string.Join(" · ", parts);
        }
    }

    /// <summary>
    /// One sub-plot: what it is and costs on the left with the city's and the owner's actions, who
    /// shares it and what each group may do inside on the right.
    /// </summary>
    public sealed class SubPlotInfoPage : CANGuiPage
    {
        private SubPlotCellElement Selected()
            => Player.CityInfo?.SubPlotCells?.FirstOrDefault(s => s.Guid == State.DialogArgs.Selected);

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null && Selected() != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            SubPlotCellElement cell = Selected();
            var perms = Player.PlayerPermissions;
            string me = claims.capi?.World?.Player?.PlayerName ?? "";
            bool canManage = perms.HasPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);
            bool isOwner = cell.OwnerName.Length > 0 && cell.OwnerName == me;
            // Members and rules are the owner's to set as well; shape, hand-out and sale the city's.
            bool canEdit = canManage || isOwner;

            Columns(CardAnchor(ctx), out ElementBounds left, out ElementBounds right);

            var rows = new List<CardRow>
            {
                new CardRow { Label = Lang.Get("claims:gui-subplot-label-name"), Value = cell.Name, Key = "spName" },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-subplot-label-owner"),
                    Value = cell.OwnerName.Length > 0 ? cell.OwnerName : Lang.Get("claims:banner-subplot-city"),
                    ValueColor = cell.OwnerName.Length > 0 ? null : ClaimsColors.Label,
                    Key = "spOwner"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-subplot-label-price"),
                    Value = subplots.SubPlotText.Offer(cell.Price, cell.Tax) ?? Lang.Get("claims:gui-not-for-sale"),
                    ValueColor = cell.IsForSale ? ClaimsColors.Success : ClaimsColors.Label,
                    Key = "spPrice"
                },
                new CardRow { Label = Lang.Get("claims:gui-subplot-label-tax"), Value = Number(cell.Tax), Key = "spTax" },
            };
            if (cell.HasPendingTax)
            {
                long secondsLeft = cell.PendingTaxAt - TimeFunctions.getEpochSeconds();
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-subplot-label-tax-pending"),
                    Value = Number(cell.PendingTax) + " ("
                        + (secondsLeft > 0
                            ? Lang.Get("claims:gui-auction-left-value", secondsLeft / 3600, (secondsLeft % 3600) / 60)
                            : Lang.Get("claims:gui-plotsgroup-fee-pending-due"))
                        + ")",
                    ValueColor = cell.PendingTaxAccepted ? ClaimsColors.Success : ClaimsColors.Warning,
                    Tooltip = Lang.Get(cell.PendingTaxAccepted ? "claims:gui-subplot-tax-accepted-tooltip" : "claims:gui-subplot-tax-pending-tooltip"),
                    Key = "spTaxPending"
                });
            }
            if (cell.GroupName.Length > 0)
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-subplot-label-group"),
                    Value = StringFunctions.replaceUnderscore(cell.GroupName),
                    Tooltip = Lang.Get("claims:gui-subplot-group-tooltip"),
                    Key = "spGroup"
                });
            }
            rows.Add(new CardRow
            {
                Label = Lang.Get("claims:gui-subplot-label-size"),
                Value = Lang.Get("claims:gui-subplot-size-value", cell.Boxes, cell.Volume),
                Key = "spSize"
            });
            rows.Add(new CardRow
            {
                Label = Lang.Get("claims:gui-subplot-label-where"),
                Value = cell.X + " " + cell.Y + " " + cell.Z,
                Tooltip = Lang.Get("claims:gui-subplot-where-tooltip", cell.Chunks),
                Key = "spWhere"
            });

            double y = Card.RowsWithActions(compo, left, left.fixedY, Lang.Get("claims:gui-subplot-section-subplot"), rows, slot =>
            {
                var actions = new ActionRow(compo, slot);
                if (canManage)
                {
                    actions.Add("claims:queen-crown", "spSetOwner",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_SET_OWNER_SELECT); },
                        Lang.Get("claims:gui-subplot-set-owner"));
                    if (cell.OwnerName.Length > 0)
                    {
                        actions.Add("claims:thumb-down", "spClearOwner",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_CLEAR_OWNER_CONFIRM); },
                            Lang.Get("claims:gui-subplot-clear-owner"));
                    }
                    else
                    {
                        actions.Add("claims:price-tag", "spSetPrice",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_SET_PRICE_NEED_NUMBER); },
                            Lang.Get("claims:gui-subplot-set-price"));
                        if (cell.IsForSale)
                        {
                            actions.Add("line", "spNotForSale",
                                on => { if (on) ClientChat.Send(ClientCommandText.Join("/subplot nfs", cell.Name)); },
                                Lang.Get("claims:gui-subplot-not-for-sale"));
                        }
                    }
                    if (cell.GroupName.Length > 0)
                    {
                        actions.Add("claims:huts-village", "spUngroup",
                            on => { if (on) ClientChat.Send(ClientCommandText.Join("/subplot ungroup", cell.Name)); },
                            Lang.Get("claims:gui-subplot-ungroup"));
                    }
                    else if (Player.CityInfo.PlotsGroupCells?.Count > 0)
                    {
                        actions.Add("claims:huts-village", "spSetGroup",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_SET_GROUP_SELECT); },
                            Lang.Get("claims:gui-subplot-set-group"));
                    }
                    actions.Add("claims:receive-money", "spSetTax",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_SET_TAX_NEED_NUMBER); },
                        Lang.Get("claims:gui-subplot-set-tax"));
                    actions.Add("claims:cancel", "spDelete",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_DELETE_CONFIRM); },
                        Lang.Get("claims:gui-subplot-delete"));
                }
                if (cell.IsForSale && cell.OwnerName.Length == 0)
                {
                    actions.Add("claims:check-mark", "spBuy",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_BUY_CONFIRM); },
                        Lang.Get("claims:gui-subplot-buy"));
                }
                if (isOwner)
                {
                    if (cell.HasPendingTax && !cell.PendingTaxAccepted)
                    {
                        actions.Add("claims:contract", "spAcceptTax",
                            on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_ACCEPT_TAX_CONFIRM); },
                            Lang.Get("claims:gui-subplot-accept-tax"));
                    }
                    actions.Add("claims:exit-door", "spGiveBack",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_GIVE_BACK_CONFIRM); },
                        Lang.Get("claims:gui-subplot-give-back"));
                }
            });

            // --- who shares it, each with its own ✕ ---
            var memberItems = cell.MembersNames
                .Select(name => new ItemRow
                {
                    Label = StringFunctions.replaceUnderscore(name),
                    Key = "spmember-" + name,
                    Buttons = canEdit
                        ? new List<RowButton>
                        {
                            new RowButton("claims:cancel",
                                () => OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_REMOVE_MEMBER_CONFIRM, a => a.Text = name),
                                Lang.Get("claims:gui-subplot-remove-member"))
                        }
                        : new List<RowButton>()
                })
                .ToList();
            // The rights grid follows in this column; more rows push it onto the navigation row.
            const int MaxMembersShown = 3;
            double rightY = Card.Items(compo, right, right.fixedY,
                Lang.Get("claims:gui-subplot-section-members") + " (" + cell.MembersNames.Count + ")",
                Card.Limit(memberItems, MaxMembersShown), Lang.Get("claims:gui-list-empty"), slot =>
            {
                if (!canEdit) return;
                var actions = new ActionRow(compo, slot);
                actions.Add("plus", "spAddMember",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_ADD_MEMBER_NEED_NAME); },
                    Lang.Get("claims:gui-subplot-add-member"));
                actions.Add("claims:private", "spPreset",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SUBPLOT_PRESET_SELECT); },
                    Lang.Get("claims:gui-subplot-preset"));
            });

            // --- what each group may do inside ---
            var scope = PermissionsScopes.SubPlot(() => cell.Name, () => cell.PermsHandler);
            PermTable.Rights(ctx, right, rightY, "subplotperm", scope, canEdit);

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.SubPlots),
                    Lang.Get("claims:gui-nav-back")));
        }

        /// <summary>Money as shown: whole values without ".0".</summary>
        public static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
