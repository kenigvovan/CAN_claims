using Atlas.Api;
using Atlas.XUnit;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// A claims.json the server cannot parse. It used to be replaced with the defaults on boot, so an
/// admin's edit with one typo in it was silently thrown away - and the cooldowns went back to
/// 72 hours, which looked exactly like "the config change does nothing".
/// </summary>
[Trait("Category", "E2E")]
[AtlasDataFiles("fixtures/BrokenConfig", TargetPath = "ModConfig")]
public class ConfigLoadScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task BrokenConfigFile_Should_StayUntouched_When_ItCannotBeParsed()
    {
        // Not AppContext.BaseDirectory: Atlas points it at the game install once the server boots.
        string testBin = Path.GetDirectoryName(typeof(ConfigLoadScenarios).Assembly.Location)!;
        string seeded = File.ReadAllText(Path.Combine(testBin, "fixtures", "BrokenConfig", "claims.json"));
        string onServer = File.ReadAllText(Path.Combine(GamePaths.ModConfig, "claims.json"));

        Assert.Equal(seeded, onServer);
        Assert.Contains(World.BootDiagnostics, e =>
            e.Level == EnumLogType.Error && e.Message.Contains("claims.json could not be read"));
        // The server still runs, on the defaults.
        Assert.NotNull(Mod.config);
        await World.Ticks(1);
    }
}
