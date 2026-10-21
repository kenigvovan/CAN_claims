using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// /agree: what a confirmation still checks when it is finally given.
/// </summary>
[Trait("Category", "E2E")]
public class AgreeScenarios : ClaimsScenarioBase
{
    // O1, AG4
    [AtlasScenario]
    public async Task CityDelete_Should_BeRefused_When_PlayerIsNoLongerMayorAtAgree()
    {
        // the mayor asks to delete the city, then hands the city over
        CityScene s = await GivenCity("heir");
        ITestPlayer heir = s.Citizens[0];
        await s.Mayor.ExecuteCommand("/city delete");
        await s.Mayor.ExecuteCommand($"/city set mayor {heir.Player.PlayerName}");
        Assert.True(s.City.isMayor(Info(heir)), "precondition: the heir is the mayor now");

        // the former mayor confirms the old request
        await s.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Contains(s.City, Mod.dataStorage.getCitiesList());
    }

    // O4, AG4
    [AtlasScenario]
    public async Task AllianceCreate_Should_BeRefused_When_PlayerIsNoLongerMayorAtAgree()
    {
        // the mayor asks to found an alliance, then hands the city over
        CityScene s = await GivenCity("heir");
        ITestPlayer heir = s.Citizens[0];
        await s.Mayor.ExecuteCommand($"/alliance create Ally{CityName(s.Area)}");
        await s.Mayor.ExecuteCommand($"/city set mayor {heir.Player.PlayerName}");
        Assert.True(s.City.isMayor(Info(heir)), "precondition: the heir is the mayor now");

        await s.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.False(s.City.HasAlliance(), "a former mayor founded an alliance for the city");
    }

    // O4 (name), AG4: check at the command, use at /agree (TOCTOU)
    [AtlasScenario]
    public async Task AllianceCreate_Should_BeRefused_When_NameWasTakenWhileWaiting()
    {
        // two mayors ask for the same alliance name before either confirms
        CityScene first = await GivenCity();
        CityScene second = await GivenCity();
        string name = "Ally" + CityName(first.Area);
        await first.Mayor.ExecuteCommand($"/alliance create {name}");
        await second.Mayor.ExecuteCommand($"/alliance create {name}");
        await first.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);
        Assert.True(first.City.HasAlliance(), "precondition: the first alliance was founded");

        await second.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Single(Mod.dataStorage.getAllAlliances(), a => a.GetPartName() == name);
        Assert.False(second.City.HasAlliance(), "the second city founded an alliance with a taken name");
    }

    // O5, AG4: same TOCTOU as O4, for city names
    [AtlasScenario]
    public async Task CityCreate_Should_BeRefused_When_NameWasTakenWhileWaiting()
    {
        // two players far apart ask for the same city name before either confirms
        ResetConfig();
        int areaA = NewArea(), areaB = NewArea();
        string name = CityName(areaA);
        ITestPlayer first = await JoinAt("founderA" + areaA, areaA);
        ITestPlayer second = await JoinAt("founderB" + areaB, areaB);
        await first.ExecuteCommand($"/city create {name}");
        await second.ExecuteCommand($"/city create {name}");
        await first.ExecuteCommand("/agree");
        await World.Until(() => Info(first).hasCity(), "the first city to be founded", AgreeTimeoutTicks);

        await second.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Single(Mod.dataStorage.getCitiesList(), c => c.GetPartName() == name);
        Assert.False(Info(second).hasCity(), "the second player founded a city with a taken name");
    }

    // AG2
    [AtlasScenario]
    public async Task Agree_Should_DoNothing_When_ConfirmationTimedOut()
    {
        // a one-second timeout, a city founding left unconfirmed past it
        ResetConfig();
        Mod.config.AGREEMENT_TIMEOUT_SECONDS = 1;
        int area = NewArea();
        ITestPlayer founder = await JoinAt("founder" + area, area);
        await founder.ExecuteCommand($"/city create {CityName(area)}");
        await World.Ticks(60);

        await founder.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.False(Info(founder).hasCity(), "a timed-out confirmation founded the city");
    }
}
