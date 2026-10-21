using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// R12, the regression core: rules LC1-LC5.
/// </summary>
[Trait("Category", "E2E")]
public class CityLifecycleScenarios : ClaimsScenarioBase
{
    // LC1, R12
    [AtlasScenario]
    public async Task Player_Should_BecomeCitizen_When_AcceptingAnInvitation()
    {
        CityScene s = await GivenCity();
        ITestPlayer newcomer = await JoinAt("newcomer" + s.Area, s.Area);
        await s.Mayor.ExecuteCommand($"/city invite {newcomer.Player.PlayerName}");

        await newcomer.ExecuteCommand("/accept");
        await World.Until(() => Info(newcomer).hasCity(), "the newcomer to join", AgreeTimeoutTicks);

        Assert.True(s.City.Equals(Info(newcomer).City), "the newcomer joined another city");
        AssertInvariants();
    }

    // LC2, R12
    [AtlasScenario]
    public async Task Citizen_Should_LoseCityAndPlot_When_Kicked()
    {
        // a citizen owning a plot
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        await GivenOwnedPlot(s, citizen);
        var plot = PlotAt(citizen);

        await s.Mayor.ExecuteCommand($"/city kick {citizen.Player.PlayerName}");

        Assert.False(Info(citizen).hasCity(), "the kicked citizen is still in the city");
        Assert.DoesNotContain(Info(citizen), s.City.getPlayerInfos());
        Assert.False(plot.hasPlotOwner(), "the kicked citizen still owns the plot");
        AssertInvariants();
    }

    // LC3, R12
    [AtlasScenario]
    public async Task Citizen_Should_BeOutOfTheCity_When_Leaving()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];

        await citizen.ExecuteCommand("/city leave");

        Assert.False(Info(citizen).hasCity(), "the citizen is still in the city");
        Assert.DoesNotContain(Info(citizen), s.City.getPlayerInfos());
        AssertInvariants();
    }

    // LC4, R12
    [AtlasScenario]
    public async Task Mayor_Should_NotBeAbleToLeave()
    {
        CityScene s = await GivenCity();

        await s.Mayor.ExecuteCommand("/city leave");

        Assert.True(s.City.isMayor(Info(s.Mayor)), "the mayor left the city without a mayor");
        AssertInvariants();
    }

    // LC5, R12
    [AtlasScenario]
    public async Task City_Should_BeGoneWithItsLand_When_MayorDeletesItAndAgrees()
    {
        // a city with a citizen who owns a plot
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        await GivenOwnedPlot(s, citizen);
        var plots = s.City.getCityPlots().ToList();

        await s.Mayor.ExecuteCommand("/city delete");
        await Agree(s.Mayor);
        await World.Until(() => !Mod.dataStorage.getCitiesList().Contains(s.City), "the city to be deleted", AgreeTimeoutTicks);

        // nobody lives there and the land is free again
        Assert.False(Info(s.Mayor).hasCity(), "the mayor still has the city");
        Assert.False(Info(citizen).hasCity(), "the citizen still has the city");
        Assert.Empty(Info(citizen).PlayerPlots);
        Assert.All(plots, p => Assert.False(Mod.dataStorage.GetPlot(p.plotPosition, out _), "a plot of the deleted city is still claimed"));
    }
}
