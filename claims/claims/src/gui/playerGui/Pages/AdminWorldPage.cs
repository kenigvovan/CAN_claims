using System;
using claims.src.gui.playerGui.Widgets;
using claims.src.network.packets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// World-wide overrides: force PVP, fire spread or explosions everywhere, or forbid them
    /// outright, plus the day/hour/backup triggers.
    /// </summary>
    public sealed class AdminWorldPage : AdminPageBase
    {
        /// <summary>Caption line of one flag pair, and the row of switches under it.</summary>
        private const double FlagLabelHeight = 18;
        private const double FlagRowHeight = 25;
        private const double FlagSectionGap = 8;

        private const double FlagSectionHeight = FlagLabelHeight + FlagRowHeight + FlagSectionGap;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var column = ctx.Current.BelowCopy(0, 10);
            column.fixedWidth = ctx.Line.fixedWidth;
            column.WithAlignment(EnumDialogArea.LeftTop);

            var world = AdminClientState.World;
            if (world == null)
            {
                // The reply to ADMIN_REQUEST_CITY_FLAGS has not arrived yet.
                const double loadingHeight = 24;
                ElementBounds loadingInner = Card.Frame(compo, column, column.fixedY,
                    Card.HeaderHeight + loadingHeight + Card.Padding * 2,
                    Lang.Get("claims:gui-admin-world-title"));

                compo.AddStaticText(Lang.Get("claims:gui-admin-world-loading"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    loadingInner.FlatCopy().WithFixedHeight(loadingHeight), "admin-world-loading");
                return;
            }

            // --- the three world-wide overrides ---
            double flagsHeight = Card.HeaderHeight + FlagSectionHeight * 3 + Card.Padding * 2;
            ElementBounds inner = Card.Frame(compo, column, column.fixedY, flagsHeight,
                Lang.Get("claims:gui-admin-world-title"));

            double y = inner.fixedY;
            y = AddFlagSection(compo, inner, y, Lang.Get("claims:gui-admin-pvp"), "pvp",
                world.PvpEverywhere, v => world.PvpEverywhere = v,
                world.PvpForbidden, v => world.PvpForbidden = v);

            y = AddFlagSection(compo, inner, y, Lang.Get("claims:gui-admin-fire-spread"), "fire",
                world.FireEverywhere, v => world.FireEverywhere = v,
                world.FireForbidden, v => world.FireForbidden = v);

            AddFlagSection(compo, inner, y, Lang.Get("claims:gui-admin-explosions"), "blast",
                world.BlastEverywhere, v => world.BlastEverywhere = v,
                world.BlastForbidden, v => world.BlastForbidden = v);

            // --- one-off triggers ---
            string ndayCaption = Lang.Get("claims:gui-admin-force-nday");
            string nhourCaption = Lang.Get("claims:gui-admin-force-nhour");
            string backupCaption = Lang.Get("claims:gui-admin-force-backup");

            double innerWidth = column.fixedWidth - Card.Padding * 2;

            // Both measured rather than assumed: the hint wraps to two lines at this width, and the
            // three buttons wrap to two rows - a card framed for one of each clipped them both.
            string hint = Lang.Get("claims:gui-admin-diagnostics-hint");
            double hintHeight = TextHeight(hint, innerWidth) + 4;
            double triggerRowHeight = ButtonRow.HeightFor(innerWidth, 0, ndayCaption, nhourCaption, backupCaption);

            double diagnosticsHeight = Card.HeaderHeight + hintHeight + triggerRowHeight + Card.Padding * 2;

            ElementBounds diagnostics = Card.Frame(compo, column,
                column.fixedY + flagsHeight + Card.Gap, diagnosticsHeight,
                Lang.Get("claims:gui-admin-diagnostics"));

            compo.AddStaticText(hint,
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                diagnostics.FlatCopy().WithFixedHeight(hintHeight), "admin-diagnostics-hint");

            var triggerAnchor = diagnostics.FlatCopy().WithFixedHeight(ButtonRow.ButtonHeight);
            triggerAnchor.fixedY += hintHeight;

            var triggers = new ButtonRow(compo, triggerAnchor, diagnostics.fixedWidth);
            AddTrigger(triggers, ndayCaption, "/cadmin nday", "claims:gui-admin-nday-world-tooltip");
            AddTrigger(triggers, nhourCaption, "/cadmin nhour", "claims:gui-admin-nhour-world-tooltip");
            AddTrigger(triggers, backupCaption, "/cadmin backup", "claims:gui-admin-backup-world-tooltip");
        }

        /// <summary>
        /// One pair of mutually exclusive switches. "Everywhere" and "forbidden" contradict each
        /// other, so whichever is on locks the other out. Returns the y the next section starts at.
        /// </summary>
        private double AddFlagSection(GuiComposer compo, ElementBounds inner, double y, string sectionName, string key,
            bool everywhere, Action<bool> setEverywhere,
            bool forbidden, Action<bool> setForbidden)
        {
            var labelBounds = inner.FlatCopy().WithFixedHeight(FlagLabelHeight);
            labelBounds.fixedY = y;
            compo.AddStaticText(sectionName,
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Value), labelBounds, "admin-flag-" + key);

            var row = inner.FlatCopy().WithFixedSize(110, FlagRowHeight);
            row.fixedY = y + FlagLabelHeight;

            compo.AddStaticText(Lang.Get("claims:gui-admin-everywhere"),
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), row);

            var everywhereSwitch = row.RightCopy(0, 0).WithFixedSize(25, 25);
            compo.AddSwitch((on) =>
            {
                setEverywhere(on);
                Send("/cadmin world set " + key + "ew " + (on ? "on" : "off"));
                Gui.BuildMainWindow();
            }, everywhereSwitch, key + "-everywhere");
            compo.GetSwitch(key + "-everywhere").SetValue(everywhere);
            compo.GetSwitch(key + "-everywhere").Enabled = !forbidden;

            var forbiddenLabel = everywhereSwitch.RightCopy(20, 0).WithFixedSize(110, FlagRowHeight);
            compo.AddStaticText(Lang.Get("claims:gui-admin-forbidden"),
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), forbiddenLabel);

            var forbiddenSwitch = forbiddenLabel.RightCopy(0, 0).WithFixedSize(25, 25);
            compo.AddSwitch((on) =>
            {
                setForbidden(on);
                Send("/cadmin world set " + key + "fb " + (on ? "on" : "off"));
                Gui.BuildMainWindow();
            }, forbiddenSwitch, key + "-forbidden");
            compo.GetSwitch(key + "-forbidden").SetValue(forbidden);
            compo.GetSwitch(key + "-forbidden").Enabled = !everywhere;

            return y + FlagSectionHeight;
        }

        private void AddTrigger(ButtonRow row, string caption, string command, string tooltipLangKey)
        {
            row.Add(caption, () =>
            {
                Send(command);
                return true;
            }, Lang.Get(tooltipLangKey));
        }
    }
}
