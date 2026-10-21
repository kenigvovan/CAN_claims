using Atlas.XUnit;
using claims.src;
using claims.src.rights;

namespace claims.scenarios;

/// <summary>
/// A server that ran an older build: its claims_permissions.json predates the newer permissions.
/// The groups are read from that file, not from the code, so a new permission reaches them only
/// through RightsHandler.permissionMigrations (R21).
/// </summary>
[Trait("Category", "E2E")]
[AtlasDataFiles("fixtures/OldPermissions", TargetPath = "ModConfig")]
public class PermissionsMigrationScenarios : AtlasScenarioBase
{
    // R21
    [AtlasScenario]
    public async Task OldPermissionsFile_Should_GetEveryNewPermissionOfItsGroups()
    {
        // Every permission appended after the base set, by the group the code gives it to.
        var expected = new (string Group, EnumPlayerPermissions Perm)[]
        {
            ("MAYOR", EnumPlayerPermissions.CITY_SET_EMBLEM),
            ("MAYOR", EnumPlayerPermissions.CITY_SELL_PLOT_TO_CITY),
            ("MAYOR", EnumPlayerPermissions.CITY_BUY_PLOT_FROM_CITY),
            ("MAYOR", EnumPlayerPermissions.CITY_PLOTSGROUP_SET_FEE),
            ("MAYOR", EnumPlayerPermissions.CITY_SET_MOBSPAWN),
            ("MAYOR", EnumPlayerPermissions.CITY_PLOTSGROUP_SET_MOBSPAWN),
            ("MAYOR", EnumPlayerPermissions.CITY_SET_NEUTRAL),
            ("LEADER", EnumPlayerPermissions.ALLIANCE_SET_EMBLEM),
            ("MAYOR", EnumPlayerPermissions.CITY_SUBPLOT_MANAGE),
        };

        var missing = expected
            .Where(e => !(RightsHandler.GetPermsByGroup(e.Group)?.Contains(e.Perm) ?? false))
            .Select(e => e.Group + "." + e.Perm)
            .ToList();

        Assert.True(missing.Count == 0, "not granted to an old permissions file: " + string.Join(", ", missing));
        await World.Ticks(1);
    }
}
