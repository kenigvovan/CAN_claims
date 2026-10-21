using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.events;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Who may hurt whom on claimed land (OnPVP.canPVPAttackHere).
/// </summary>
[Trait("Category", "E2E")]
public class PvpScenarios : ClaimsScenarioBase
{
    // R18, D3: enemies may fight on claimed land at any time, not only during a battle
    [AtlasScenario]
    public async Task Pvp_Should_BeAllowed_When_CitiesAreEnemiesOutsideABattle()
    {
        // hostile cities (seeded, no conflict and no battle running)
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        theirs.City.HostileCities.Add(ours.City);
        ours.City.HostileCities.Add(theirs.City);
        await GoToPlot(theirs.Mayor, ours.Area, 1, 0);

        // the defender stands on our own plot
        Assert.True(OnPVP.canPVPAttackHere(theirs.Mayor.Player, ours.Mayor.Player),
            "an enemy could not attack outside a battle (D3: they may at any time)");
    }

    // FL1
    [AtlasScenario]
    public async Task Pvp_Should_BeDenied_When_PlotHasNoPvpFlagAndPlayersAreNotEnemies()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);

        Assert.False(OnPVP.canPVPAttackHere(stranger.Player, s.Citizens[0].Player),
            "a stranger could attack a citizen on a peaceful plot");
        await World.Ticks(1);
    }

    // FL2
    [AtlasScenario]
    public async Task Pvp_Should_BeAllowed_When_PlotPvpFlagIsOn()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        await s.Mayor.ExecuteCommand("/plot set pvp on");
        Assert.True(PlotAt(s.Citizens[0]).getPermsHandler().pvpFlag, "precondition: the plot's pvp flag is on");

        Assert.True(OnPVP.canPVPAttackHere(stranger.Player, s.Citizens[0].Player),
            "the pvp flag did not open the plot to fighting");
    }

    // FL3
    [AtlasScenario]
    public async Task Pvp_Should_BeDenied_When_WorldForbidsIt_EvenForEnemies()
    {
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        ours.City.HostileCities.Add(theirs.City);
        theirs.City.HostileCities.Add(ours.City);
        var world = Mod.dataStorage.getWorldInfo();
        world.pvpForbidden = true;
        try
        {
            Assert.False(OnPVP.canPVPAttackHere(theirs.Mayor.Player, ours.Mayor.Player),
                "pvp went through although the world forbids it");
        }
        finally
        {
            world.pvpForbidden = false;
        }
        await World.Ticks(1);
    }

    // R18: peace ends the hostility
    [AtlasScenario]
    public async Task Pvp_Should_BeDenied_When_WarEndedInPeace()
    {
        // a war between the cities, the enemy may attack
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        AllowAdminCommands(ours.Mayor);
        string warArgs = $"{ours.City.GetPartName()} {theirs.City.GetPartName()}";
        await ours.Mayor.ExecuteCommand("/cadmin startwar " + warArgs);
        Assert.True(OnPVP.canPVPAttackHere(theirs.Mayor.Player, ours.Mayor.Player), "precondition: enemies at war");

        await ours.Mayor.ExecuteCommand("/cadmin endwar " + warArgs);

        Assert.False(OnPVP.canPVPAttackHere(theirs.Mayor.Player, ours.Mayor.Player),
            "the cities are still enemies after the peace");
    }
}
