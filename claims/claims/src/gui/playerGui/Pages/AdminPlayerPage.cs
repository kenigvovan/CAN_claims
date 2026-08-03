using System;
using System.Linq;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.Widgets;
using claims.src.network.packets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Diagnostics for a named player, plus admin overrides on the plot the admin is standing on -
    /// the server resolves "this plot" from the caller's position.
    ///
    /// Laid out in grids rather than one control per line: the stacked version ran a hundred pixels
    /// past the window bottom. The explanatory hints live in the section titles' tooltips.
    /// </summary>
    public sealed class AdminPlayerPage : AdminPageBase
    {
        private const double InputHeight = 28;
        private const double RowGap = 6;

        /// <summary>One line of a switch grid. The grids run one entry per row inside a column.</summary>
        private const double GridRowHeight = 31;

        private static readonly string[] PermTokens = { "use", "build", "attack" };
        private static readonly string[] PermLangKeys =
        {
            "claims:gui-admin-perm-use", "claims:gui-admin-perm-build", "claims:gui-admin-perm-attack"
        };

        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var anchor = ctx.Current.BelowCopy(0, 5);
            anchor.fixedWidth = ctx.Line.fixedWidth;
            anchor.WithAlignment(EnumDialogArea.LeftTop);

            // Two columns: stacked, the four cards added up to more than the window is tall and the
            // permissions ran off the bottom. Each column's grids drop to one entry per row to fit.
            double columnWidth = (ctx.Line.fixedWidth - Card.ColumnGap) / 2;

            var left = anchor.FlatCopy();
            left.fixedWidth = columnWidth;

            var right = anchor.FlatCopy();
            right.fixedWidth = columnWidth;
            right.fixedX += columnWidth + Card.ColumnGap;

            double leftY = BuildDiagnosticsCard(compo, left, left.fixedY);

            var plot = Player.CurrentPlotInfo;
            if (plot?.PermsHandler == null)
            {
                BuildPlotCard(compo, left, leftY, null);
                return;
            }

            BuildPlotCard(compo, left, leftY, plot);

            double rightY = BuildPlotSettingsCard(compo, right, right.fixedY, plot);
            BuildPermissionsCard(compo, right, rightY, plot);
        }

        /// <summary>Asking the server what it knows about one player.</summary>
        private double BuildDiagnosticsCard(GuiComposer compo, ElementBounds column, double y)
        {
            double height = Card.HeaderHeight + InputHeight + Card.Padding * 2;

            ElementBounds inner = Card.Frame(compo, column, y, height,
                Lang.Get("claims:gui-admin-player-diag"));

            var nameBounds = inner.FlatCopy().WithFixedSize(180, InputHeight);
            compo.AddTextInput(nameBounds, v => Admin.SelectedPlayer = v, null, "admin-diag-player");
            compo.GetTextInput("admin-diag-player").SetValue(Admin.SelectedPlayer);
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-player-diag-hint"), nameBounds, "tip-admin-diag");

            string caption = Lang.Get("claims:gui-admin-diag");
            var buttonBounds = nameBounds.RightCopy(10).WithFixedSize(ButtonWidth(caption), InputHeight);
            compo.AddButton(caption, new ActionConsumable(() =>
            {
                Send("/cadmin diag " + Admin.SelectedPlayer);
                return true;
            }), buttonBounds, EnumButtonStyle.Normal);
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-diag-tooltip"), buttonBounds, "tip-admin-diag-run");

            return y + height + Card.Gap;
        }

        /// <summary>
        /// The plot underfoot: the refresh that fetches it, and its three flags. Passing a null plot
        /// draws the card explaining that no plot data has arrived.
        /// </summary>
        private double BuildPlotCard(GuiComposer compo, ElementBounds column, double y, CurrentPlotInfo plot)
        {
            string refreshCaption = Lang.Get("claims:gui-admin-refresh-plot");

            int flagRows = plot == null ? 1 : 3;
            double body = InputHeight + RowGap + flagRows * GridRowHeight;
            double height = Card.HeaderHeight + body + Card.Padding * 2;

            ElementBounds inner = Card.Frame(compo, column, y, height,
                Lang.Get("claims:gui-admin-plot-at-position"));

            // The plot only arrives with a packet, so without this the page can sit on data from
            // wherever the admin stood last.
            var refreshBounds = inner.FlatCopy().WithFixedSize(ButtonWidth(refreshCaption), InputHeight);
            compo.AddButton(refreshCaption, new ActionConsumable(() =>
            {
                claims.clientChannel.SendPacket(new SavedPlotsPacket
                {
                    type = PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST
                });
                return true;
            }), refreshBounds, EnumButtonStyle.Normal);
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-refresh-plot-tooltip"), refreshBounds, "tip-admin-refresh-plot");

            double gridY = inner.fixedY + InputHeight + RowGap;

            if (plot == null)
            {
                var noneBounds = inner.FlatCopy().WithFixedHeight(GridRowHeight);
                noneBounds.fixedY = gridY;
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-plot-data"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), noneBounds, "admin-no-plot-data");

                return y + height + Card.Gap;
            }

            var flagDefs = new (string Key, string Label, bool Value, Action<bool> Apply, string TooltipKey)[]
            {
                ("pvp", Lang.Get("claims:gui-admin-flag-pvp"), plot.PermsHandler.pvpFlag,
                    v => plot.PermsHandler.pvpFlag = v, "claims:gui-admin-plot-pvp-tooltip"),
                ("fire", Lang.Get("claims:gui-admin-flag-fire"), plot.PermsHandler.fireFlag,
                    v => plot.PermsHandler.fireFlag = v, "claims:gui-admin-plot-fire-tooltip"),
                ("blast", Lang.Get("claims:gui-admin-flag-blast"), plot.PermsHandler.blastFlag,
                    v => plot.PermsHandler.blastFlag = v, "claims:gui-admin-plot-blast-tooltip"),
            };

            for (int i = 0; i < flagDefs.Length; i++)
            {
                var def = flagDefs[i];

                var labelBounds = inner.FlatCopy().WithFixedSize(inner.fixedWidth - 40, 25);
                labelBounds.fixedY = gridY + i * GridRowHeight;
                compo.AddStaticText(def.Label,
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), labelBounds, "admin-plotflag-" + def.Key);

                var switchBounds = labelBounds.RightCopy(5, 0).WithFixedSize(25, 25);
                string switchKey = "admin-plot-" + def.Key;
                compo.AddSwitch((on) =>
                {
                    def.Apply(on);
                    Send("/cadmin plot set " + def.Key + (on ? " on" : " off"));
                }, switchBounds, switchKey);
                compo.GetSwitch(switchKey).SetValue(def.Value);
                Tooltip.Add(compo, Lang.Get(def.TooltipKey), switchBounds, "tip-" + switchKey);
            }

            return y + height + Card.Gap;
        }

        /// <summary>What the plot costs and what it is: fee, sale price, type.</summary>
        private double BuildPlotSettingsCard(GuiComposer compo, ElementBounds column, double y, CurrentPlotInfo plot)
        {
            const double numberInput = 70;

            string feeCaption = Lang.Get("claims:gui-admin-tax-fee");
            string priceCaption = Lang.Get("claims:gui-admin-fs-price");

            double innerWidth = column.fixedWidth - Card.Padding * 2;

            // Fee and sale price share one row when both fit - both are short numbers.
            bool priceFitsOnSameRow = numberInput + 8 + ButtonWidth(feeCaption) + 16
                                    + numberInput + 8 + ButtonWidth(priceCaption) <= innerWidth;
            double numbersHeight = priceFitsOnSameRow ? InputHeight : InputHeight * 2 + RowGap;

            double body = numbersHeight + RowGap + InputHeight;
            double height = Card.HeaderHeight + body + Card.Padding * 2;

            ElementBounds inner = Card.Frame(compo, column, y, height,
                Lang.Get("claims:gui-admin-section-plot-settings"));

            var feeInput = inner.FlatCopy().WithFixedSize(numberInput, InputHeight);
            compo.AddTextInput(feeInput, v => Admin.CityFee = v, null, "admin-plot-fee");
            compo.GetTextInput("admin-plot-fee").SetValue(Admin.CityFee ?? "");

            var feeButton = AddCommand(compo, feeInput.RightCopy(8).WithFixedHeight(InputHeight), feeCaption,
                () => "/cadmin plot fee " + Admin.CityFee, "claims:gui-admin-tax-fee-tooltip");

            var priceInput = priceFitsOnSameRow
                ? feeButton.RightCopy(16).WithFixedSize(numberInput, InputHeight)
                : feeInput.BelowCopy(0, RowGap).WithFixedSize(numberInput, InputHeight);
            compo.AddTextInput(priceInput, v => Admin.BonusClaims = v, null, "admin-plot-fs");
            compo.GetTextInput("admin-plot-fs").SetValue(Admin.BonusClaims ?? "");

            AddCommand(compo, priceInput.RightCopy(8).WithFixedHeight(InputHeight), priceCaption,
                () => "/cadmin plot fs " + Admin.BonusClaims, "claims:gui-admin-fs-price-tooltip");

            var typeBounds = inner.FlatCopy().WithFixedSize(180, InputHeight);
            typeBounds.fixedY = inner.fixedY + numbersHeight + RowGap;

            string[] typeNames = Enum.GetNames(typeof(PlotType));
            string[] typeLabels = typeNames.Select(n => n.ToLowerInvariant()).ToArray();
            int selectedType = Array.IndexOf(typeNames, plot.PlotType.ToString());

            compo.AddDropDown(typeLabels, typeLabels, selectedType < 0 ? 0 : selectedType, (code, on) =>
            {
                if (!on) return;
                // Written locally too, so the dropdown keeps the picked entry until the server
                // sends the plot back.
                plot.PlotType = (PlotType)Enum.Parse(typeof(PlotType), code, true);
                Send("/cadmin plot type " + code);
            }, typeBounds, "admin-plot-type");
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-plot-type-tooltip"), typeBounds, "tip-admin-plot-type");

            return y + height + Card.Gap;
        }

        /// <summary>Who may do what on the plot, one group per row.</summary>
        private void BuildPermissionsCard(GuiComposer compo, ElementBounds column, double y, CurrentPlotInfo plot)
        {
            var groupDefs = new (string Label, string Cmd, bool[] Perms)[]
            {
                (Lang.Get("claims:gui-admin-group-citizen"), "citizen", plot.PermsHandler.CitizenPerms),
                (Lang.Get("claims:gui-admin-group-stranger"), "stranger", plot.PermsHandler.StrangerPerms),
                (Lang.Get("claims:gui-admin-group-ally"), "ally", plot.PermsHandler.AlliancePerms),
                (Lang.Get("claims:gui-admin-group-friend"), "friend", plot.PermsHandler.ComradePerms),
            };

            double height = Card.HeaderHeight + groupDefs.Length * GridRowHeight + Card.Padding * 2;

            ElementBounds inner = Card.Frame(compo, column, y, height,
                Lang.Get("claims:gui-admin-permissions"));

            Tooltip.Add(compo, Lang.Get("claims:gui-admin-permissions-help"),
                inner.FlatCopy().WithFixedSize(inner.fixedWidth, 12), "tip-admin-perms");

            for (int i = 0; i < groupDefs.Length; i++)
            {
                var cell = inner.FlatCopy().WithFixedSize(90, 25);
                cell.fixedY = inner.fixedY + i * GridRowHeight;

                AddPermGroup(compo, cell, groupDefs[i].Label, groupDefs[i].Cmd, groupDefs[i].Perms);
            }
        }

        /// <summary>One group's use/build/attack switches, label first, on a single line.</summary>
        private void AddPermGroup(GuiComposer compo, ElementBounds cell, string groupLabel, string groupCmd, bool[] perms)
        {
            if (perms == null) return;

            compo.AddStaticText(groupLabel,
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), cell, "admin-permgroup-" + groupCmd);

            ElementBounds switchBounds = cell;
            for (int i = 0; i < perms.Length && i < PermTokens.Length; i++)
            {
                int index = i;
                switchBounds = switchBounds.RightCopy(i == 0 ? 0 : 12, 0).WithFixedSize(25, 25);

                string key = "admin-perm-" + groupCmd + "-" + PermTokens[i];
                var target = switchBounds;
                compo.AddSwitch((on) =>
                {
                    perms[index] = on;
                    Send("/cadmin plot set permissions " + groupCmd + " " + PermTokens[index] + (on ? " on" : " off"));
                }, target, key);
                compo.GetSwitch(key).SetValue(perms[i]);
                Tooltip.Add(compo, Lang.Get(PermLangKeys[i]), target, "tip-" + key);
            }
        }

        /// <summary>
        /// A command button sized to its own caption - a fixed width clipped the longer translations
        /// and made neighbouring captions overlap. Returns the bounds actually used.
        /// </summary>
        private ElementBounds AddCommand(GuiComposer compo, ElementBounds bounds, string caption, Func<string> command, string tooltipKey)
        {
            var target = bounds.FlatCopy().WithFixedWidth(ButtonWidth(caption));

            compo.AddButton(caption, new ActionConsumable(() =>
            {
                Send(command());
                return true;
            }), target, EnumButtonStyle.Normal);

            if (tooltipKey != null)
            {
                Tooltip.Add(compo, Lang.Get(tooltipKey), target, "tip-cmd-" + caption);
            }

            return target;
        }
    }
}
