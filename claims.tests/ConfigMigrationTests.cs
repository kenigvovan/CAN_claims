using claims.src;
using claims.src.rights;

namespace claims.tests
{
    /// <summary>One-time changes to an existing claims.json: each runs once, and a fresh file needs none.</summary>
    public class ConfigMigrationTests
    {
        [Fact]
        public void OldFile_GetsTheSubPlotRight_InTheRankList()
        {
            var config = new Config { AVAILABLE_CITY_PERMISSIONS = new() { EnumPlayerPermissions.CITY_CLAIM_PLOT } };

            Assert.True(Config.ApplyConfigMigrations(config));

            Assert.Contains(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE, config.AVAILABLE_CITY_PERMISSIONS);
            Assert.Contains("subplot-manage-available", config.APPLIED_CONFIG_MIGRATIONS);
        }

        [Fact]
        public void RightTakenOutByTheHost_StaysOut()
        {
            var config = new Config { AVAILABLE_CITY_PERMISSIONS = new() { EnumPlayerPermissions.CITY_CLAIM_PLOT } };
            Config.ApplyConfigMigrations(config);
            config.AVAILABLE_CITY_PERMISSIONS.Remove(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);

            Assert.False(Config.ApplyConfigMigrations(config));

            Assert.DoesNotContain(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE, config.AVAILABLE_CITY_PERMISSIONS);
        }

        [Fact]
        public void FreshFile_HasNothingLeftToMigrate()
        {
            var config = new Config();
            Config.MarkConfigMigrationsApplied(config);

            Assert.False(Config.ApplyConfigMigrations(config));
            Assert.Empty(config.AVAILABLE_CITY_PERMISSIONS);
        }
    }
}
