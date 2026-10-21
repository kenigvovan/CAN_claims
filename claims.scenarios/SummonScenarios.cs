using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.delayed.teleportation;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.MathTools;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// /city summon use: paid up front, teleported later by the 1-second timer.
/// Cases SU1-SU11.
/// </summary>
[Trait("Category", "E2E")]
public class SummonScenarios : ClaimsScenarioBase
{
    private const decimal Payment = 5;
    // The teleport timer runs every second; a few of them.
    private const int TeleportTicks = 150;

    protected override void ResetConfig()
    {
        base.ResetConfig();
        var c = Mod.config;
        c.SUMMON_ALLOWED = true;
        c.SUMMON_PAYMENT = (double)Payment;
        c.SUMMON_MIN_PLAYERS = 0;
        c.SECONDS_SUMMON_TIME = 0;
        c.SECONDS_SUMMON_COOLDOWN = 600;
        c.PLAYER_MOVEMENT_CANCEL_TELEPORTATION = false;
    }

    /// <summary>
    /// Makes the mayor's plot (1, 0) a summon plot, sends the summoner off to the wild at (5, 0)
    /// and gives them 100 on a fake economy. Returns the point's name and position.
    /// </summary>
    private async Task<(string Name, Vec3d Point, FakeMoneyProvider Money)> GivenSummonPoint(CityScene s, ITestPlayer summoner)
    {
        CityLevelInfo level = Settings.getCityLevelInfo(s.City.getCityCitizens().Count);
        int previous = level.SummonPlots;
        AddCleanup(() => level.SummonPlots = previous);
        level.SummonPlots = s.City.summonPlots.Count + 1;

        await GivenPlotType(s.Mayor, "summon", PlotType.SUMMON);
        var desc = Assert.IsType<PlotDescSummon>(PlotAt(s.Mayor).PlotDesc);

        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(summoner), 100);
        await GoToPlot(summoner, s.Area, 5, 0);
        return (desc.Name, desc.SummonPoint.Clone(), money);
    }

    private static bool At(ITestPlayer player, Vec3d point) =>
        Math.Abs(player.Position.X + 0.5 - point.X) <= 1 && Math.Abs(player.Position.Z + 0.5 - point.Z) <= 1;

    private static bool Pending(ITestPlayer player) => TeleportationHandler.hasTeleportation(Info(player));

    // SU1, R36
    [AtlasScenario]
    public async Task Summon_Should_TeleportAndCharge()
    {
        CityScene s = await GivenCity();
        (string name, Vec3d point, FakeMoneyProvider money) = await GivenSummonPoint(s, s.Mayor);

        await s.Mayor.ExecuteCommand($"/city summon use {name}");

        await World.Until(() => At(s.Mayor, point), "the mayor to arrive at the summon point", TeleportTicks);
        Assert.Equal(100m - Payment, money.GetBalance(Account(s.Mayor)));
    }

    // SU2, R36
    [AtlasScenario]
    public async Task Summon_Should_FollowTheMoney_AtThePriceBoundary()
    {
        CityScene s = await GivenCity();
        (string name, _, FakeMoneyProvider money) = await GivenSummonPoint(s, s.Mayor);

        money.SetBalance(Account(s.Mayor), Payment - 0.01m);
        await s.Mayor.ExecuteCommand($"/city summon use {name}");
        Assert.False(Pending(s.Mayor), "summoned a coin short of the price");
        Assert.Equal(Payment - 0.01m, money.GetBalance(Account(s.Mayor)));

        money.SetBalance(Account(s.Mayor), Payment);
        Mod.config.SECONDS_SUMMON_TIME = 60; // keep it pending to look at it
        await s.Mayor.ExecuteCommand($"/city summon use {name}");
        Assert.True(Pending(s.Mayor), "not summoned with exactly the price");
        Assert.Equal(0m, money.GetBalance(Account(s.Mayor)));
        TeleportationHandler.removeTeleportation(Info(s.Mayor));
    }

    // SU3, R36
    [AtlasScenario]
    public async Task Summon_Should_BeRefused_DuringTheCooldown()
    {
        CityScene s = await GivenCity();
        (string name, Vec3d point, _) = await GivenSummonPoint(s, s.Mayor);
        await s.Mayor.ExecuteCommand($"/city summon use {name}");
        await World.Until(() => At(s.Mayor, point), "the first summon", TeleportTicks);
        await GoToPlot(s.Mayor, s.Area, 5, 0);

        await s.Mayor.ExecuteCommand($"/city summon use {name}");

        Assert.False(Pending(s.Mayor), "summoned again during the cooldown");
    }

    // SU5, R36, R27
    [AtlasScenario]
    public async Task Summon_Should_NotHappen_When_TheWithdrawalFails()
    {
        CityScene s = await GivenCity();
        (string name, _, FakeMoneyProvider money) = await GivenSummonPoint(s, s.Mayor);
        money.FailNext(FakeMoneyOp.Withdraw, Account(s.Mayor));

        await s.Mayor.ExecuteCommand($"/city summon use {name}");

        Assert.False(Pending(s.Mayor), "a teleport was booked without the money taken");
    }

    // SU6, R36, A26
    [AtlasScenario]
    public async Task Payment_Should_BeReturned_When_TheSummonIsCancelled()
    {
        CityScene s = await GivenCity();
        (string name, _, FakeMoneyProvider money) = await GivenSummonPoint(s, s.Mayor);
        Mod.config.SECONDS_SUMMON_TIME = 60;
        await s.Mayor.ExecuteCommand($"/city summon use {name}");
        Assert.True(Pending(s.Mayor), "precondition: the summon is waiting");

        TeleportationHandler.removeTeleportation(Info(s.Mayor)); // what moving away or logging out does

        Assert.Equal(100m, money.GetBalance(Account(s.Mayor)));
    }

    // SU7, R36
    [AtlasScenario]
    public async Task SecondSummon_Should_NotChargeAgain_While_TheFirstIsWaiting()
    {
        CityScene s = await GivenCity();
        (string name, _, FakeMoneyProvider money) = await GivenSummonPoint(s, s.Mayor);
        Mod.config.SECONDS_SUMMON_TIME = 60;
        await s.Mayor.ExecuteCommand($"/city summon use {name}");
        Assert.True(Pending(s.Mayor), "precondition: the summon is waiting");

        await s.Mayor.ExecuteCommand($"/city summon use {name}");

        Assert.Equal(100m - Payment, money.GetBalance(Account(s.Mayor)));
        TeleportationHandler.removeTeleportation(Info(s.Mayor));
    }

    // SU10, R36, A26
    [AtlasTheory]
    [InlineData("kick")]
    [InlineData("leave")]
    public async Task Summon_Should_NotHappen_When_ThePlayerLeftTheCityMeanwhile(string how)
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        (string name, Vec3d point, _) = await GivenSummonPoint(s, citizen);
        Mod.config.SECONDS_SUMMON_TIME = 2;
        await citizen.ExecuteCommand($"/city summon use {name}");
        Assert.True(Pending(citizen), "precondition: the summon is waiting");

        if (how == "kick") await s.Mayor.ExecuteCommand($"/city kick {citizen.Player.PlayerName}");
        else await citizen.ExecuteCommand("/city leave");
        Assert.False(Info(citizen).hasCity(), $"precondition: the citizen is out of the city ({how})");
        await World.Until(() => !Pending(citizen), "the summon to run out", TeleportTicks);

        Assert.False(At(citizen, point), $"a player out of the city ({how}) was still teleported into it");
    }

    // SU12, R36
    [AtlasScenario]
    public async Task Summon_Should_NotReach_AnotherCitysPoint_ByItsName()
    {
        CityScene theirs = await GivenCity(); // before the fake economy: its claim needs a treasury
        CityScene ours = await GivenCity();
        (string name, _, FakeMoneyProvider money) = await GivenSummonPoint(ours, ours.Mayor);
        money.SetBalance(Account(theirs.Mayor), 100); // not refused for want of money

        await theirs.Mayor.ExecuteCommand($"/city summon use {name}");

        Assert.False(Pending(theirs.Mayor), "a foreign mayor booked a teleport to our summon point");
    }

    // SU14, R36
    [AtlasTheory]
    [InlineData("unclaim")]
    [InlineData("transfer")]
    public async Task SummonPoint_Should_BeGone_When_ThePlotLeavesTheCity(string how)
    {
        CityScene other = await GivenCity(); // before the fake economy: its claim needs a treasury
        CityScene s = await GivenCity();
        (string name, _, _) = await GivenSummonPoint(s, s.Mayor);
        await GoToPlot(s.Mayor, s.Area, 1, 0);
        Plot plot = PlotAt(s.Mayor);

        // war captures, cessions and market sales all go through PlotTransferHelper
        if (how == "unclaim") await s.Mayor.ExecuteCommand("/city unclaim");
        else PlotTransferHelper.Transfer(plot, s.City, other.City, markCaptured: false);
        Assert.False(s.City.getCityPlots().Contains(plot), $"precondition: the plot left the city ({how})");

        Assert.DoesNotContain(plot, s.City.summonPlots);
        Assert.DoesNotContain(plot, other.City.summonPlots);
        await GoToPlot(s.Mayor, s.Area, 5, 0);
        await s.Mayor.ExecuteCommand($"/city summon use {name}");
        Assert.False(Pending(s.Mayor), $"the old summon point still works after {how}");
    }

    // SU13, R36
    [AtlasScenario]
    public async Task Summon_Should_BeRefused_AfterLeavingTheCity()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        (string name, _, _) = await GivenSummonPoint(s, citizen);
        await citizen.ExecuteCommand("/city leave");
        Assert.False(Info(citizen).hasCity(), "precondition: the citizen left");

        await citizen.ExecuteCommand($"/city summon use {name}");

        Assert.False(Pending(citizen), "a former citizen booked a teleport into the city");
    }
}
