using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace claims.scenarios;

/// <summary>
/// What the hourly timer does: prison sentences.
/// </summary>
[Trait("Category", "E2E")]
public class HourTimerScenarios : ClaimsScenarioBase
{
    // H2
    [AtlasScenario]
    public async Task PrisonHours_Should_DropByOne_When_HourPasses()
    {
        // a prisoner with 3 hours left, set directly - jailing is its own area
        ResetConfig();
        int area = NewArea();
        ITestPlayer prisoner = await JoinAt("prisoner" + area, area);
        Info(prisoner).PrisonHoursLeft = 3;

        await RunHour(prisoner);

        Assert.Equal(2, Info(prisoner).PrisonHoursLeft);
    }

    // H2 (release, online)
    [AtlasScenario]
    public async Task Prisoner_Should_BeReleasedToWorldSpawn_When_LastHourPasses()
    {
        // one hour left, standing far from the world spawn
        ResetConfig();
        int area = NewArea();
        ITestPlayer prisoner = await JoinAt("prisoner" + area, area);
        await GoToPlot(prisoner, area, 5, 0);
        BlockPos spawn = World.Api.World.DefaultSpawnPosition.AsBlockPos;
        Assert.True(prisoner.Position.DistanceTo(spawn) > 50, "precondition: the prisoner is far from the world spawn");
        Info(prisoner).PrisonHoursLeft = 1;

        await RunHour(prisoner);

        Assert.Equal(0, Info(prisoner).PrisonHoursLeft);
        Assert.Null(Info(prisoner).PrisonedIn);
        await World.Until(() => prisoner.Position.DistanceTo(spawn) < 3,
            "the released prisoner to be at the world spawn", 100);
    }

    // O4, A17
    [AtlasScenario]
    public async Task Release_Should_KeepPlayersOwnSpawnPoint()
    {
        // the prisoner has their own spawn point (a bed, say) far from the world spawn
        ResetConfig();
        int area = NewArea();
        ITestPlayer prisoner = await JoinAt("prisoner" + area, area);
        BlockPos bed = PlotCenter(area, 5, 0);
        prisoner.Player.SetSpawnPosition(new PlayerSpawnPos(bed.X, bed.Y, bed.Z));
        Info(prisoner).PrisonHoursLeft = 1;

        await RunHour(prisoner);

        FuzzyEntityPos spawn = prisoner.Player.GetSpawnPosition(false);
        Assert.True(spawn.AsBlockPos.DistanceTo(bed) < 3,
            $"the release overwrote the player's own spawn point: now {spawn.AsBlockPos}, was {bed}");
    }
}
