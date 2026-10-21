using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Vintagestory.API.Common;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// R02 (P0): is the build alive at all? Run before anything else; if this fails, the rest of the
/// suite says nothing. Entry criterion 2.
/// </summary>
[Trait("Category", "Smoke")]
public class SmokeScenarios : ClaimsScenarioBase
{
    // R02: the server boots with the mod and logs no error while doing so.
    // The mod logs through the shared api.Logger without a prefix, so its errors cannot be told
    // apart from the engine's (Source "unknown"): any boot error fails here, whoever logged it.
    [AtlasScenario]
    public async Task Server_Should_BootWithoutErrors()
    {
        var errors = World.BootDiagnostics
            .Where(e => e.Tick is null && e.Level >= EnumLogType.Error)
            .Select(e => $"[{e.DescribeSource()}] {e.Message}")
            .ToList();

        Assert.True(errors.Count == 0, "errors while booting:\n" + string.Join("\n", errors));
        Assert.NotNull(Mod.config);
        Assert.NotNull(Mod.dataStorage);
        await World.Ticks(1);
    }

    // R22 and every other harmony patch (fire, water, falling blocks, bombs, damage, commands):
    // each one matches game code that an update can change. A patch that no longer fits is only
    // logged and its protection silently stops - this turns those warnings into a failure on the
    // first run on a new game version.
    [AtlasScenario]
    public async Task HarmonyPatches_Should_AllApply()
    {
        string[] markers = { "patch skipped", "patch of", "transpiler skipped", "transpiler found no injection point" };
        var warnings = World.BootDiagnostics
            .Where(e => markers.Any(m => e.Message.Contains(m)))
            .Select(e => e.Message)
            .ToList();

        Assert.True(warnings.Count == 0, "patches not applied: " + string.Join("; ", warnings));
        await World.Ticks(1);
    }

    // R02: the core flow works end to end - a city is founded, a citizen joins and buys a plot.
    [AtlasScenario]
    public async Task CoreFlow_Should_Work_When_CityIsFoundedAndPlotBought()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];

        await GivenOwnedPlot(s, citizen);

        Assert.True(Owns(citizen), "the citizen must own the plot");
        AssertInvariants();
    }
}
