using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Vintagestory.GameContent;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Whether a bomb may go off (harmony prefix on BlockEntityBomb.HasPermissionToUse). The test makes
/// a bomb object of its own and asks it: the server runs in this process, so the mod's patch
/// answers. Its blast radius stays 0, so only the plot the bomb stands on is looked at.
/// Default per A20: bombs work on claimed land.
/// </summary>
[Trait("Category", "E2E")]
public class BlastScenarios : ClaimsScenarioBase
{
    private BlockEntityBomb BombAt(int area, int dx, int dz) =>
        new BlockEntityBomb { Pos = PlotCenter(area, dx, dz) };

    // F4: no plot anywhere around - nothing for claims to protect
    [AtlasScenario]
    public async Task Bomb_Should_Work_InTheWilderness()
    {
        ResetConfig();
        int area = NewArea();

        Assert.True(BombAt(area, 0, 0).HasPermissionToUse(), "a bomb cannot go off in the wilderness");
        await World.Ticks(1);
    }

    // A20: a new city's plot lets bombs go off
    [AtlasScenario]
    public async Task Bomb_Should_Work_OnACityPlot_ByDefault()
    {
        CityScene s = await GivenCity();

        Assert.True(BombAt(s.Area, 1, 0).HasPermissionToUse(), "bombs do not work on claimed land by default (A20)");
    }

    // A20: switching blasts off on the plot and the city stops them
    [AtlasScenario]
    public async Task Bomb_Should_NotWork_When_BlastIsOffOnPlotAndCity()
    {
        CityScene s = await GivenCity();
        await s.Mayor.ExecuteCommand("/plot set blast off");
        await s.Mayor.ExecuteCommand("/city set blast off");
        Assert.False(PlotAt(s.Mayor).getPermsHandler().blastFlag || s.City.getPermsHandler().blastFlag,
            "precondition: blast is off on the plot and the city");

        Assert.False(BombAt(s.Area, 1, 0).HasPermissionToUse(), "a bomb went off where blasts are switched off");
    }

    // The world switch beats the plot's own setting
    [AtlasScenario]
    public async Task Bomb_Should_NotWork_When_WorldForbidsBlasts()
    {
        CityScene s = await GivenCity();
        var world = Mod.dataStorage.getWorldInfo();
        world.blastForbidden = true;
        try
        {
            Assert.False(BombAt(s.Area, 1, 0).HasPermissionToUse(), "a bomb went off although the world forbids blasts");
        }
        finally
        {
            world.blastForbidden = false;
        }
    }
}
