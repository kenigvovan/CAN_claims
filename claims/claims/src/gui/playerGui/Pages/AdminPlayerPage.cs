using System;
using System.Linq;
using claims.src.part.structure;
using claims.src.part.structure.plots;
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
        private static readonly string[] PermTokens = { "use", "build", "attack" };
        private static readonly string[] PermLangKeys =
        {
            "claims:gui-admin-perm-use", "claims:gui-admin-perm-build", "claims:gui-admin-perm-attack"
        };

        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var currentBounds = ctx.Current.BelowCopy(0, 5);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            compo.AddStaticText(Lang.Get("claims:gui-admin-player-diag"), ClaimsFonts.PageLabel, currentBounds);
            compo.AddHoverText(Lang.Get("claims:gui-admin-player-diag-hint"), CairoFont.SmallButtonText(), 300, currentBounds);

            var nameBounds = currentBounds.BelowCopy(0, 8).WithFixedSize(180, 28);
            compo.AddTextInput(nameBounds, v => Admin.SelectedPlayer = v, null, "admin-diag-player");
            compo.GetTextInput("admin-diag-player").SetValue(Admin.SelectedPlayer);

            AddCommand(compo, nameBounds.RightCopy(10).WithFixedHeight(28), "claims:gui-admin-diag",
                () => "/cadmin diag " + Admin.SelectedPlayer, "claims:gui-admin-diag-tooltip");

            // --- the plot underfoot ---
            var plotBounds = nameBounds.BelowCopy(0, 12);
            string refreshCaption = Lang.Get("claims:gui-admin-refresh-plot");
            double refreshWidth = ButtonWidth(refreshCaption);
            plotBounds.fixedWidth = ctx.Line.fixedWidth - refreshWidth - 10;
            compo.AddStaticText(Lang.Get("claims:gui-admin-plot-at-position"), ClaimsFonts.PageLabel, plotBounds);
            compo.AddHoverText(Lang.Get("claims:gui-admin-plot-at-position-hint"), CairoFont.SmallButtonText(), 300, plotBounds);

            // The plot only arrives with a packet, so without this the page can sit on data from
            // wherever the admin stood last.
            var refreshBounds = plotBounds.RightCopy(10).WithFixedSize(refreshWidth, 28);
            compo.AddButton(refreshCaption, new ActionConsumable(() =>
            {
                claims.clientChannel.SendPacket(new SavedPlotsPacket
                {
                    type = PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST
                });
                return true;
            }), refreshBounds, EnumButtonStyle.Normal);
            compo.AddHoverText(Lang.Get("claims:gui-admin-refresh-plot-tooltip"), CairoFont.SmallButtonText(), 250, refreshBounds);

            var plot = Player.CurrentPlotInfo;
            if (plot?.PermsHandler == null)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-plot-data"), CairoFont.WhiteDetailText(),
                    plotBounds.BelowCopy(0, 8).WithFixedWidth(ctx.Line.fixedWidth));
                return;
            }

            // --- plot flags, one grid ---
            var flagDefs = new (string Key, string Label, bool Value, Action<bool> Apply, string TooltipKey)[]
            {
                ("pvp", Lang.Get("claims:gui-admin-flag-pvp"), plot.PermsHandler.pvpFlag,
                    v => plot.PermsHandler.pvpFlag = v, "claims:gui-admin-plot-pvp-tooltip"),
                ("fire", Lang.Get("claims:gui-admin-flag-fire"), plot.PermsHandler.fireFlag,
                    v => plot.PermsHandler.fireFlag = v, "claims:gui-admin-plot-fire-tooltip"),
                ("blast", Lang.Get("claims:gui-admin-flag-blast"), plot.PermsHandler.blastFlag,
                    v => plot.PermsHandler.blastFlag = v, "claims:gui-admin-plot-blast-tooltip"),
            };

            var flagGridStart = plotBounds.BelowCopy(0, 8).WithFixedSize(120, 25);
            int flagRows = (flagDefs.Length + 1) / 2;
            for (int i = 0; i < flagDefs.Length; i++)
            {
                var def = flagDefs[i];

                var labelBounds = flagGridStart.FlatCopy();
                labelBounds.fixedX += (i % 2) * 220;
                labelBounds.fixedY += (i / 2) * 31;
                compo.AddStaticText(def.Label, CairoFont.WhiteDetailText(), labelBounds);

                var switchBounds = labelBounds.RightCopy(5, 0).WithFixedSize(25, 25);
                string switchKey = "admin-plot-" + def.Key;
                compo.AddSwitch((on) =>
                {
                    def.Apply(on);
                    Send("/cadmin plot set " + def.Key + (on ? " on" : " off"));
                }, switchBounds, switchKey);
                compo.GetSwitch(switchKey).SetValue(def.Value);
                compo.AddHoverText(Lang.Get(def.TooltipKey), CairoFont.SmallButtonText(), 250, switchBounds);
            }

            var flagAnchor = flagGridStart.FlatCopy();
            flagAnchor.fixedY += (flagRows - 1) * 31;

            // --- fee and sale price, one row: both are short numbers ---
            var feeInput = flagAnchor.BelowCopy(0, 10).WithFixedSize(70, 28);
            compo.AddTextInput(feeInput, v => Admin.CityFee = v, null, "admin-plot-fee");
            compo.GetTextInput("admin-plot-fee").SetValue(Admin.CityFee ?? "");
            var feeButton = AddCommand(compo, feeInput.RightCopy(8).WithFixedHeight(28), "claims:gui-admin-tax-fee",
                () => "/cadmin plot fee " + Admin.CityFee, "claims:gui-admin-tax-fee-tooltip");

            var priceInput = feeButton.RightCopy(16).WithFixedSize(70, 28);
            compo.AddTextInput(priceInput, v => Admin.BonusClaims = v, null, "admin-plot-fs");
            compo.GetTextInput("admin-plot-fs").SetValue(Admin.BonusClaims ?? "");
            AddCommand(compo, priceInput.RightCopy(8).WithFixedHeight(28), "claims:gui-admin-fs-price",
                () => "/cadmin plot fs " + Admin.BonusClaims, "claims:gui-admin-fs-price-tooltip");

            // --- plot type ---
            var typeBounds = feeInput.BelowCopy(0, 10).WithFixedSize(180, 28);
            string[] typeNames = System.Enum.GetNames(typeof(PlotType));
            string[] typeLabels = typeNames.Select(n => n.ToLowerInvariant()).ToArray();
            int selectedType = System.Array.IndexOf(typeNames, plot.PlotType.ToString());

            compo.AddDropDown(typeLabels, typeLabels, selectedType < 0 ? 0 : selectedType, (code, on) =>
            {
                if (!on) return;
                // Written locally too, so the dropdown keeps the picked entry until the server
                // sends the plot back.
                plot.PlotType = (PlotType)System.Enum.Parse(typeof(PlotType), code, true);
                Send("/cadmin plot type " + code);
            }, typeBounds, "admin-plot-type");
            compo.AddHoverText(Lang.Get("claims:gui-admin-plot-type-tooltip"), CairoFont.SmallButtonText(), 250, typeBounds);

            // --- per-group permissions, two groups per row ---
            var permBounds = typeBounds.BelowCopy(0, 12);
            permBounds.fixedWidth = ctx.Line.fixedWidth;
            compo.AddStaticText(Lang.Get("claims:gui-admin-permissions"), ClaimsFonts.PageLabel, permBounds);
            compo.AddHoverText(Lang.Get("claims:gui-admin-permissions-help"), CairoFont.SmallButtonText(), 300, permBounds);

            var groupDefs = new (string Label, string Cmd, bool[] Perms)[]
            {
                (Lang.Get("claims:gui-admin-group-citizen"), "citizen", plot.PermsHandler.CitizenPerms),
                (Lang.Get("claims:gui-admin-group-stranger"), "stranger", plot.PermsHandler.StrangerPerms),
                (Lang.Get("claims:gui-admin-group-ally"), "ally", plot.PermsHandler.AlliancePerms),
                (Lang.Get("claims:gui-admin-group-friend"), "friend", plot.PermsHandler.ComradePerms),
            };

            var permGridStart = permBounds.BelowCopy(0, 8).WithFixedSize(90, 25);
            for (int i = 0; i < groupDefs.Length; i++)
            {
                var cell = permGridStart.FlatCopy();
                cell.fixedX += (i % 2) * 240;
                cell.fixedY += (i / 2) * 31;
                AddPermGroup(compo, cell, groupDefs[i].Label, groupDefs[i].Cmd, groupDefs[i].Perms);
            }
        }

        /// <summary>One group's use/build/attack switches, label first, on a single line.</summary>
        private void AddPermGroup(GuiComposer compo, ElementBounds cell, string groupLabel, string groupCmd, bool[] perms)
        {
            if (perms == null) return;

            compo.AddStaticText(groupLabel, CairoFont.WhiteDetailText(), cell);

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
                compo.AddHoverText(Lang.Get(PermLangKeys[i]), CairoFont.SmallButtonText(), 120, target);
            }
        }

        /// <summary>
        /// A command button sized to its own caption - a fixed width clipped the longer translations
        /// and made neighbouring captions overlap. Returns the bounds actually used.
        /// </summary>
        private ElementBounds AddCommand(GuiComposer compo, ElementBounds bounds, string labelKey, Func<string> command, string tooltipKey)
        {
            string caption = Lang.Get(labelKey);
            var target = bounds.FlatCopy().WithFixedWidth(ButtonWidth(caption));

            compo.AddButton(caption, new ActionConsumable(() =>
            {
                Send(command());
                return true;
            }), target, EnumButtonStyle.Normal);

            if (tooltipKey != null)
            {
                compo.AddHoverText(Lang.Get(tooltipKey), CairoFont.SmallButtonText(), 250, target);
            }

            return target;
        }
    }
}
