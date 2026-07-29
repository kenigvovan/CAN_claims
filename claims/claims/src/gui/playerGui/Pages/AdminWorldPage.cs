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
        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var currentBounds = ctx.Current.BelowCopy(0, 10).WithFixedHeight(25);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            compo.AddStaticText(Lang.Get("claims:gui-admin-world-title"), ClaimsFonts.PageLabel, currentBounds);

            var world = AdminClientState.World;
            if (world == null)
            {
                // The reply to ADMIN_REQUEST_CITY_FLAGS has not arrived yet.
                compo.AddStaticText(Lang.Get("claims:gui-admin-world-loading"),
                    CairoFont.WhiteDetailText(), currentBounds.BelowCopy(0, 20));
                return;
            }

            currentBounds = currentBounds.BelowCopy(0, 5);

            currentBounds = AddFlagSection(compo, currentBounds, Lang.Get("claims:gui-admin-pvp"), "pvp",
                world.PvpEverywhere, v => world.PvpEverywhere = v,
                world.PvpForbidden, v => world.PvpForbidden = v);

            currentBounds = AddFlagSection(compo, currentBounds, Lang.Get("claims:gui-admin-fire-spread"), "fire",
                world.FireEverywhere, v => world.FireEverywhere = v,
                world.FireForbidden, v => world.FireForbidden = v);

            currentBounds = AddFlagSection(compo, currentBounds, Lang.Get("claims:gui-admin-explosions"), "blast",
                world.BlastEverywhere, v => world.BlastEverywhere = v,
                world.BlastForbidden, v => world.BlastForbidden = v);

            currentBounds = currentBounds.BelowCopy(0, 10);
            compo.AddStaticText(Lang.Get("claims:gui-admin-diagnostics"), ClaimsFonts.PageLabel, currentBounds);

            currentBounds = currentBounds.BelowCopy(0, 2);
            compo.AddStaticText(Lang.Get("claims:gui-admin-diagnostics-hint"),
                CairoFont.WhiteDetailText(), currentBounds);

            var triggers = new ButtonRow(compo, currentBounds.BelowCopy(0, 8), ctx.Line.fixedWidth);
            AddTrigger(triggers, "claims:gui-admin-force-nday", "/cadmin nday", "claims:gui-admin-nday-world-tooltip");
            AddTrigger(triggers, "claims:gui-admin-force-nhour", "/cadmin nhour", "claims:gui-admin-nhour-world-tooltip");
            AddTrigger(triggers, "claims:gui-admin-force-backup", "/cadmin backup", "claims:gui-admin-backup-world-tooltip");
        }

        /// <summary>
        /// One pair of mutually exclusive switches. "Everywhere" and "forbidden" contradict each
        /// other, so whichever is on locks the other out.
        /// </summary>
        private ElementBounds AddFlagSection(GuiComposer compo, ElementBounds bounds, string sectionName, string key,
            bool everywhere, Action<bool> setEverywhere,
            bool forbidden, Action<bool> setForbidden)
        {
            compo.AddStaticText(sectionName, CairoFont.WhiteDetailText(), bounds);

            var row = bounds.BelowCopy(0, 2).WithFixedHeight(25);

            var everywhereLabel = row.FlatCopy().WithFixedWidth(110);
            compo.AddStaticText(Lang.Get("claims:gui-admin-everywhere"), CairoFont.WhiteDetailText(), everywhereLabel);

            var everywhereSwitch = everywhereLabel.RightCopy(0, 0).WithFixedSize(25, 25);
            compo.AddSwitch((on) =>
            {
                setEverywhere(on);
                Send("/cadmin world set " + key + "ew " + (on ? "on" : "off"));
                Gui.BuildMainWindow();
            }, everywhereSwitch, key + "-everywhere");
            compo.GetSwitch(key + "-everywhere").SetValue(everywhere);
            compo.GetSwitch(key + "-everywhere").Enabled = !forbidden;

            var forbiddenLabel = everywhereSwitch.RightCopy(20, 0).WithFixedWidth(110);
            compo.AddStaticText(Lang.Get("claims:gui-admin-forbidden"), CairoFont.WhiteDetailText(), forbiddenLabel);

            var forbiddenSwitch = forbiddenLabel.RightCopy(0, 0).WithFixedSize(25, 25);
            compo.AddSwitch((on) =>
            {
                setForbidden(on);
                Send("/cadmin world set " + key + "fb " + (on ? "on" : "off"));
                Gui.BuildMainWindow();
            }, forbiddenSwitch, key + "-forbidden");
            compo.GetSwitch(key + "-forbidden").SetValue(forbidden);
            compo.GetSwitch(key + "-forbidden").Enabled = !everywhere;

            return row.BelowCopy(0, 10).WithFixedHeight(25);
        }

        private void AddTrigger(ButtonRow row, string labelLangKey, string command, string tooltipLangKey)
        {
            row.Add(Lang.Get(labelLangKey), () =>
            {
                Send(command);
                return true;
            }, Lang.Get(tooltipLangKey));
        }
    }
}
