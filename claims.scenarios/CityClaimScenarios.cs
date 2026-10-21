using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.events;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.rights;
using Vintagestory.API.Common;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// /city claim and /city unclaim. Rules CC/CU.
/// </summary>
[Trait("Category", "E2E")]
public class CityClaimScenarios : ClaimsScenarioBase
{
    // CC1, R12
    [AtlasScenario]
    public async Task Claim_Should_TakeThePlotAndCharge_When_NextToTheCity()
    {
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        decimal price = (decimal)Mod.config.PLOT_CLAIM_PRICE;
        money.SetBalance(s.City.MoneyAccountName, 100);
        await GoToPlot(s.Mayor, s.Area, 2, 0);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.True(ClaimedBy(s.City, s.Mayor), "the plot was not claimed");
        Assert.Equal(100m - price, money.GetBalance(s.City.MoneyAccountName));
        AssertInvariants();
    }

    // CC1, R12
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_NotNextToTheCity()
    {
        CityScene s = await GivenCity();
        await GoToPlot(s.Mayor, s.Area, 4, 0);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.False(ClaimedBy(s.City, s.Mayor), "a plot away from the city was claimed");
    }

    // CC2, R12
    [AtlasScenario]
    public async Task Claim_Should_FollowTheTreasury_AtThePriceBoundary()
    {
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        decimal price = (decimal)Mod.config.PLOT_CLAIM_PRICE;

        money.SetBalance(s.City.MoneyAccountName, price - 0.01m);
        await GoToPlot(s.Mayor, s.Area, 2, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.False(ClaimedBy(s.City, s.Mayor), "claimed with a coin short of the price");

        money.SetBalance(s.City.MoneyAccountName, price);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(s.City, s.Mayor), "not claimed with exactly the price");
        Assert.Equal(0m, money.GetBalance(s.City.MoneyAccountName));
    }

    // CC3, R12
    [AtlasScenario]
    public async Task Unclaim_Should_BeRefused_When_ItIsTheLastPlot()
    {
        CityScene s = await GivenCity();
        await s.Mayor.ExecuteCommand("/city unclaim"); // (1, 0) goes back, (0, 0) is left
        Assert.Single(s.City.getCityPlots());
        await GoToPlot(s.Mayor, s.Area);

        await s.Mayor.ExecuteCommand("/city unclaim");

        Assert.Single(s.City.getCityPlots());
        Assert.Contains(s.City, Mod.dataStorage.getCitiesList());
    }

    /// <summary>A second city in the scene's area, founded on plot (dx, 0).</summary>
    private async Task<(ITestPlayer Mayor, City City)> NeighbourCity(CityScene s, int dx)
    {
        ITestPlayer mayor = await JoinAt("neighbour" + s.Area, s.Area);
        await GoToPlot(mayor, s.Area, dx, 0);
        City city = await FoundCity(mayor, CityName(s.Area) + "x");
        return (mayor, city);
    }

    // CC1c, R12
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_OnlyDiagonalToTheCity()
    {
        CityScene s = await GivenCity();
        await GoToPlot(s.Mayor, s.Area, 2, 1);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.False(ClaimedByAnyone(s.Mayor), "a plot touching the city only by a corner was claimed");
    }

    // CC4, R12
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_On_AnotherCitysPlot()
    {
        CityScene s = await GivenCity();
        (_, City other) = await NeighbourCity(s, 5);
        await GoToPlot(s.Mayor, s.Area, 5, 0);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.True(ClaimedBy(other, s.Mayor), "the plot left the city that owned it");
    }

    // CC6, R12, R27
    [AtlasScenario]
    public async Task Claim_Should_ChangeNothing_When_TheWithdrawalFails()
    {
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(s.City.MoneyAccountName, 100);
        money.FailNext(FakeMoneyOp.Withdraw, s.City.MoneyAccountName);
        await GoToPlot(s.Mayor, s.Area, 2, 0);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.False(ClaimedByAnyone(s.Mayor), "the plot was claimed without the money taken");
        Assert.Equal(100m, money.GetBalance(s.City.MoneyAccountName));
    }

    // CC7, R12
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_In_ForbiddenArea()
    {
        CityScene s = await GivenCity();
        await GoToPlot(s.Mayor, s.Area, 2, 0);
        ForbidClaimingHere(s.Mayor);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.False(ClaimedByAnyone(s.Mayor), "a plot in a forbidden area was claimed");
    }

    // CC9, R12
    [AtlasScenario]
    public async Task Claim_Should_FollowTheDistanceToAnotherCity_AtTheBoundary()
    {
        CityScene s = await GivenCity();
        await NeighbourCity(s, 5);
        int min = Mod.config.MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY;

        await GoToPlot(s.Mayor, s.Area, 5 - min, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(s.City, s.Mayor), "not claimed at exactly the minimum distance");

        await GoToPlot(s.Mayor, s.Area, 5 - min + 1, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.False(ClaimedByAnyone(s.Mayor), "claimed a plot closer than the minimum distance");
    }

    // CC10, R12
    [AtlasScenario]
    public async Task Claim_Should_SayNotNextToTheCity_When_AlsoShortOfMoney()
    {
        CityScene s = await GivenCity();
        UseFakeEconomy(); // empty treasury
        await GoToPlot(s.Mayor, s.Area, 4, 0);

        CommandResult result = await s.Mayor.ExecuteCommand("/city claim");

        Assert.Equal("claims:should_be_on_the_border_with_another_claimed_plot", result.Raw.StatusMessage);
    }

    // CC5, R12
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_CitizenHasNoRight()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        Assert.False(Info(citizen).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_CLAIM_PLOT),
            "precondition: an ordinary citizen has no CITY_CLAIM_PLOT");
        await GoToPlot(citizen, s.Area, 2, 0);

        CommandResult result = await citizen.ExecuteCommand("/city claim");

        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.False(ClaimedByAnyone(citizen), "a citizen without the right claimed a plot");
    }

    // CC8, R12
    [AtlasScenario]
    public async Task Claim_Should_StopAtThePlotLimit()
    {
        CityScene s = await GivenCity();
        CityLevelInfo level = Settings.getCityLevelInfo(s.City.getCityCitizens().Count);
        int previous = level.AmountOfPlots;
        AddCleanup(() => level.AmountOfPlots = previous);
        level.AmountOfPlots += s.City.getCityPlots().Count + 1 - Settings.getMaxNumberOfPlotForCity(s.City); // one plot left

        await GoToPlot(s.Mayor, s.Area, 2, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(s.City, s.Mayor), "not claimed at limit - 1");

        await GoToPlot(s.Mayor, s.Area, 3, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.False(ClaimedByAnyone(s.Mayor), "claimed past the limit");
    }

    // CC8b, R12: the city shrank below what it holds (a citizen left, an admin took bonus plots away)
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_TheCityIsAlreadyOverTheLimit()
    {
        CityScene s = await GivenCity();
        CityLevelInfo level = Settings.getCityLevelInfo(s.City.getCityCitizens().Count);
        int previous = level.AmountOfPlots;
        AddCleanup(() => level.AmountOfPlots = previous);
        level.AmountOfPlots += s.City.getCityPlots().Count - 1 - Settings.getMaxNumberOfPlotForCity(s.City); // one plot over

        await GoToPlot(s.Mayor, s.Area, 2, 0);
        await s.Mayor.ExecuteCommand("/city claim");

        Assert.False(ClaimedByAnyone(s.Mayor), "claimed while already over the limit");
        Assert.Equal(2, s.City.getCityPlots().Count);
    }

    // CC13, R12
    [AtlasScenario]
    public async Task Claim_Should_GiveThePlotTheCitysPermissions()
    {
        CityScene s = await GivenCity();
        await s.Mayor.ExecuteCommand("/city set p stranger build on");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        await GoToPlot(s.Mayor, s.Area, 2, 0);

        await s.Mayor.ExecuteCommand("/city claim");

        Assert.True(ClaimedBy(s.City, s.Mayor), "precondition: the plot was claimed");
        await GoToPlot(stranger, s.Area, 2, 0);
        var block = new BlockSelection { Position = PlotCenter(s.Area, 2, 0) };
        Assert.True(OnBlockAction.canBlockDestroy(stranger.Player, block, out _),
            "the new plot did not get the city's permissions");
    }

    // CC11, R12
    [AtlasScenario]
    public async Task Claim_Should_BeFree_When_ItIsAVillage()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        Assert.True(Settings.getMaxNumberOfPlotForCity(village) >= 2, "precondition: the village may hold two plots");
        FakeMoneyProvider money = UseFakeEconomy(); // empty treasury
        await GoToPlot(head, area, 1, 0);

        await head.ExecuteCommand("/city claim");

        Assert.True(ClaimedBy(village, head), "the village could not claim with an empty treasury");
        Assert.Empty(money.Log);
    }

    // CU2, R12
    [AtlasScenario]
    public async Task Unclaim_Should_BeRefused_On_TheVillageMainPlot()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        Assert.True(Settings.getMaxNumberOfPlotForCity(village) >= 2, "precondition: the village may hold two plots");
        await GoToPlot(head, area, 1, 0);
        await head.ExecuteCommand("/city claim");
        Assert.Equal(2, village.getCityPlots().Count); // otherwise "last plot" would refuse first
        await GoToPlot(head, area);
        Assert.Equal(PlotType.VILLAGE_MAIN, PlotAt(head).Type);

        await head.ExecuteCommand("/city unclaim");

        Assert.True(ClaimedBy(village, head), "the village main plot was unclaimed");
    }

    // CU1, R12
    [AtlasScenario]
    public async Task Unclaim_Should_BeRefused_On_AnotherCitysPlot()
    {
        CityScene s = await GivenCity();
        (_, City other) = await NeighbourCity(s, 5);
        await GoToPlot(s.Mayor, s.Area, 5, 0);

        await s.Mayor.ExecuteCommand("/city unclaim");

        Assert.True(ClaimedBy(other, s.Mayor), "the plot left the city that owned it");
    }

    // CU4, R12
    [AtlasScenario]
    public async Task Unclaim_Should_FreeTheSlot_When_ItIsAnExtraPlot()
    {
        CityScene s = await GivenCity();
        await GoToPlot(s.Mayor, s.Area, 2, 0);
        await s.Mayor.ExecuteCommand("/city extraplot");
        Assert.True(PlotAt(s.Mayor).extraBought, "precondition: an extra plot was bought");
        int bought = s.City.Extrachunksbought;

        await s.Mayor.ExecuteCommand("/city unclaim");

        Assert.Equal(bought - 1, s.City.Extrachunksbought);
    }

    // CU5, A22, R12
    [AtlasScenario]
    public async Task Unclaim_Should_RefundTheCitizen_When_TheyBoughtThePlot()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 100);
        EnableRefund(50);
        await GivenOwnedPlot(s, citizen, 40);

        await s.Mayor.ExecuteCommand("/city unclaim");

        Assert.False(ClaimedByAnyone(s.Mayor), "precondition: the city gave the plot up");
        Assert.True(money.GetBalance(Account(citizen)) > 60m, "the citizen lost the plot and got nothing back");
        AssertInvariants();
    }

    // CU5: the treasury pays what it holds and goes no lower
    [AtlasScenario]
    public async Task Unclaim_Should_RefundOnlyWhatTheTreasuryHolds_When_TheCitizenBoughtThePlot()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 100);
        EnableRefund(50);
        await GivenOwnedPlot(s, citizen, 40);
        money.SetBalance(s.City.MoneyAccountName, 5); // the refund would be 20
        // Keeps the city's own claim refund out of the treasury; ResetConfig does not restore it.
        var claimPrice = Mod.config.PLOT_CLAIM_PRICE;
        Mod.config.PLOT_CLAIM_PRICE = 0;
        try
        {
            await s.Mayor.ExecuteCommand("/city unclaim");
        }
        finally
        {
            Mod.config.PLOT_CLAIM_PRICE = claimPrice;
        }

        Assert.False(ClaimedByAnyone(s.Mayor), "precondition: the city gave the plot up");
        Assert.Equal(65m, money.GetBalance(Account(citizen)));
        Assert.Equal(0m, money.GetBalance(s.City.MoneyAccountName));
    }

    // CU5: with the refund off the citizen gets nothing back, as before
    [AtlasScenario]
    public async Task Unclaim_Should_RefundNothing_When_RefundIsOff()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 100);
        await GivenOwnedPlot(s, citizen, 40);

        await s.Mayor.ExecuteCommand("/city unclaim");

        Assert.False(ClaimedByAnyone(s.Mayor), "precondition: the city gave the plot up");
        Assert.Equal(60m, money.GetBalance(Account(citizen)));
    }
}
