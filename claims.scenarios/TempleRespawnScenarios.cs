using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.harmony;
using claims.src.part.structure;
using Vintagestory.API.MathTools;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// R22: where a citizen respawns. The injection itself is checked in SmokeScenarios; here the
/// decision is called directly (harmonyPatches.ReviveReplace is public), since a test player
/// cannot be made to press "respawn".
/// </summary>
[Trait("Category", "E2E")]
public class TempleRespawnScenarios : ClaimsScenarioBase
{
    // R22: a citizen respawns at the city's temple
    [AtlasScenario]
    public async Task Citizen_Should_RespawnAtTheTemple_When_CityHasOne()
    {
        // a temple point (seeded - building a temple is its own flow), the citizen far away
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        Mod.config.TEMPLE_RESPAWN_ENABLED = true;
        BlockPos temple = PlotCenter(s.Area, 1, 0);
        s.City.TempleRespawnPoints[new Vec2i(temple.X / Mod.config.PLOT_SIZE, temple.Z / Mod.config.PLOT_SIZE)]
            = new Vec3i(temple.X, temple.Y, temple.Z);
        await GoToPlot(citizen, s.Area, 6, 0);

        bool handled = harmonyPatches.ReviveReplace(null!, citizen.Player);

        Assert.True(handled, "the city did not take over the respawn");
        await World.Until(() => citizen.Position.DistanceTo(temple) < 4, "the citizen to arrive at the temple", 100);
    }

    // R22: a prisoner is left to the vanilla respawn
    [AtlasScenario]
    public async Task Prisoner_Should_NotRespawnAtTheTemple()
    {
        CityScene s = await GivenCity("prisoner");
        ITestPlayer prisoner = s.Citizens[0];
        Mod.config.TEMPLE_RESPAWN_ENABLED = true;
        BlockPos temple = PlotCenter(s.Area, 1, 0);
        s.City.TempleRespawnPoints[new Vec2i(temple.X / Mod.config.PLOT_SIZE, temple.Z / Mod.config.PLOT_SIZE)]
            = new Vec3i(temple.X, temple.Y, temple.Z);
        // "Prisoned" is PrisonedIn != null, not the hours left.
        Info(prisoner).PrisonedIn = new Prison("cell", "prison-guid");
        Info(prisoner).PrisonHoursLeft = 3;

        Assert.False(harmonyPatches.ReviveReplace(null!, prisoner.Player), "a prisoner escaped to the temple");
        await World.Ticks(1);
    }
}
