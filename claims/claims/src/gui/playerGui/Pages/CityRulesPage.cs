using claims.src.gui.playerGui.Dialogs;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The city's default rules - what every plot of it starts with: its flags on the left, who may
    /// do what on the right. They used to sit in a side window only the mayor could open; every
    /// citizen may read them here, and whoever lacks the right gets the switches read-only.
    /// </summary>
    public sealed class CityRulesPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo?.PermsHandler != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var perms = Player.PlayerPermissions;
            bool canSetRules = perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOT_ACCESS_PERMISSIONS)
                            || perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL);

            Columns(CardAnchor(ctx), out ElementBounds left, out ElementBounds right);
            PermissionsScope scope = PermissionsScopes.City;
            // Each flag by its own right, as /city set checks it; CITY_SET_ALL covers them all.
            bool canSetAll = perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL);
            double leftEnd = PermTable.Flags(ctx, left, left.fixedY, "cityperm", scope, flag =>
            {
                if (canSetAll) return true;
                var flagRight = PermissionsScopes.FlagPermission(flag, rights.EnumPlayerPermissions.CITY_SET_PVP,
                    rights.EnumPlayerPermissions.CITY_SET_FIRE, rights.EnumPlayerPermissions.CITY_SET_BLAST,
                    rights.EnumPlayerPermissions.CITY_SET_MOBSPAWN);
                return flagRight.HasValue && perms.HasPermission(flagRight.Value);
            });
            PermTable.Rights(ctx, right, right.fixedY, "cityperm", scope, canSetRules);

            // What these are: the defaults of plots without rules of their own.
            Card.Note(ctx.Compo, left, leftEnd, null, Lang.Get("claims:gui-city-rules-hint"));

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City),
                    Lang.Get("claims:gui-nav-back")));
        }
    }
}
