using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.part;
using claims.src.part.structure;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// The daily cycle: city upkeep, debt, citizens' fees.
/// </summary>
[Trait("Category", "E2E")]
public class DayCycleScenarios : ClaimsScenarioBase
{
    // B6, O4, A13
    [AtlasScenario]
    public async Task Upkeep_Should_PayWhatTreasuryHas_When_DebtIsBiggerThanTreasury()
    {
        // debt 100, treasury 50
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        double today = s.City.GetDayPaymentAmount();
        s.City.DebtBalance = 100;
        money.SetBalance(s.City.MoneyAccountName, 50);

        await RunDay(s.Mayor);

        Assert.Equal(0m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(100 + Math.Floor(today) - 50, s.City.DebtBalance, 2);
    }

    // B8
    [AtlasScenario]
    public async Task Upkeep_Should_ClearTheDebt_When_TreasuryHoldsExactlyTodayPlusDebt()
    {
        // debt 100, treasury exactly today + debt
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        decimal due = (decimal)Math.Floor(s.City.GetDayPaymentAmount()) + 100;
        s.City.DebtBalance = 100;
        money.SetBalance(s.City.MoneyAccountName, due);

        await RunDay(s.Mayor);

        Assert.Equal(0m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(0, s.City.DebtBalance, 2);
    }

    // B10, O3, A15
    [AtlasScenario]
    public async Task Upkeep_Should_PayTheRoundedDownAmount_When_TreasuryCoversIt()
    {
        // two plots at 0.45 make today end in .9; treasury = rounded-down today + 0.5
        CityScene s = await GivenCity();
        SetDefaultPlotCost(0.45);
        FakeMoneyProvider money = UseFakeEconomy();
        double today = s.City.GetDayPaymentAmount();
        Assert.True(today % 1 > 0.5, "precondition: today's upkeep has a fraction above .5, was " + today);
        money.SetBalance(s.City.MoneyAccountName, (decimal)Math.Floor(today) + 0.5m);

        await RunDay(s.Mayor);

        Assert.Equal(0.5m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(0, s.City.DebtBalance, 2);
    }

    // O1, A12: an exception at one city must not stop the others
    [AtlasScenario]
    public async Task Upkeep_Should_StillBeCollectedFromOtherCities_When_OneCityThrows()
    {
        // two cities with money; the one the cycle reaches first will throw
        CityScene a = await GivenCity();
        CityScene b = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(a.City.MoneyAccountName, 1000);
        money.SetBalance(b.City.MoneyAccountName, 1000);
        List<City> order = Mod.dataStorage.getCitiesList();
        bool aFirst = order.IndexOf(a.City) < order.IndexOf(b.City);
        CityScene failing = aFirst ? a : b, other = aFirst ? b : a;
        decimal otherToday = (decimal)Math.Floor(other.City.GetDayPaymentAmount());
        money.ThrowNext(FakeMoneyOp.Withdraw, failing.City.MoneyAccountName);

        await RunDay(a.Mayor);

        Assert.Equal(1000m - otherToday, money.GetBalance(other.City.MoneyAccountName));
    }

    // O2
    [AtlasScenario]
    public async Task Upkeep_Should_BecomeDebt_When_WithdrawFails()
    {
        // enough money, but the withdrawal fails
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        double today = Math.Floor(s.City.GetDayPaymentAmount());
        money.SetBalance(s.City.MoneyAccountName, 1000);
        money.FailNext(FakeMoneyOp.Withdraw, s.City.MoneyAccountName);

        await RunDay(s.Mayor);

        Assert.Equal(1000m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(today, s.City.DebtBalance, 2);
    }

    // O6, A14
    [AtlasScenario]
    public async Task Embassy_Should_BeLost_When_StrangerCannotPayItsTax()
    {
        // a stranger owns an embassy taxed 5 a day and has no money
        CityScene s = await GivenCity();
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        await GoToPlot(stranger, s.Area, 1, 0);
        await GivenPlotType(s.Mayor, "embassy", PlotType.EMBASSY);
        await GivenOwnedPlot(s, stranger);
        await s.Mayor.ExecuteCommand("/plot set fee 5");
        Assert.True(PlotAt(stranger).hasCutomTax(), "precondition: the embassy is taxed");
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(stranger), 0);

        await RunDay(s.Mayor);

        Assert.False(Owns(stranger), "a stranger who does not pay the tax keeps the embassy");
        AssertInvariants();
    }

    // O5
    [AtlasScenario]
    public async Task EmbassyTax_Should_BePaid_When_OwnerIsMayorOfAnotherCity()
    {
        // the mayor of another city owns our embassy, taxed 5 a day
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        ITestPlayer foreignMayor = theirs.Mayor;
        await GoToPlot(foreignMayor, ours.Area, 1, 0);
        await GivenPlotType(ours.Mayor, "embassy", PlotType.EMBASSY);
        await GivenOwnedPlot(ours, foreignMayor);
        await ours.Mayor.ExecuteCommand("/plot set fee 5");
        Assert.True(PlotAt(foreignMayor).hasCutomTax(), "precondition: the embassy is taxed");
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(foreignMayor), 100);

        await RunDay(ours.Mayor);

        Assert.Equal(95m, money.GetBalance(Account(foreignMayor)));
    }

    // DC7 (bankruptcy sale off: demolished)
    [AtlasScenario]
    public async Task City_Should_BeDemolished_When_DebtGoesOverMaxAndDeletingIsOn()
    {
        // deleting on, no money, debt already over the maximum
        CityScene s = await GivenCity();
        Mod.config.DELETE_CITY_IF_DOESN_PAY_FEE = true;
        FakeMoneyProvider money = UseFakeEconomy();
        s.City.DebtBalance = Mod.config.CITY_MAX_DEBT + 1;

        await RunDay(s.Mayor);

        Assert.DoesNotContain(s.City, Mod.dataStorage.getCitiesList());
        Assert.False(Info(s.Mayor).hasCity(), "the mayor must be left without a city");
    }

    // DC7 (boundary: debt ends exactly at the maximum)
    [AtlasScenario]
    public async Task City_Should_Survive_When_DebtEndsExactlyAtMax()
    {
        // today's upkeep brings the debt to exactly CITY_MAX_DEBT
        CityScene s = await GivenCity();
        Mod.config.DELETE_CITY_IF_DOESN_PAY_FEE = true;
        FakeMoneyProvider money = UseFakeEconomy();
        double today = s.City.GetDayPaymentAmount();
        Assert.True(today % 1 == 0, "precondition: today's upkeep is whole, was " + today);
        s.City.DebtBalance = Mod.config.CITY_MAX_DEBT - today;

        await RunDay(s.Mayor);

        Assert.Equal(Mod.config.CITY_MAX_DEBT, s.City.DebtBalance, 2);
        Assert.Contains(s.City, Mod.dataStorage.getCitiesList());
    }

    // DC4
    [AtlasScenario]
    public async Task CitizenFee_Should_BeCityFeePlusPlotTax_And_MayorShouldPayNothing()
    {
        // city fee 10, the citizen's plot taxed 5
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        await GivenOwnedPlot(s, citizen);
        await s.Mayor.ExecuteCommand("/plot set fee 5");
        await s.Mayor.ExecuteCommand("/city set fee 10");
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 100);
        money.SetBalance(Account(s.Mayor), 100);

        await RunDay(s.Mayor);

        Assert.Equal(85m, money.GetBalance(Account(citizen)));
        Assert.Equal(100m, money.GetBalance(Account(s.Mayor)));
    }

    // DC3: citizens pay before the city pays its upkeep
    [AtlasScenario]
    public async Task Upkeep_Should_BePaidFromTodaysFees_When_TreasuryWasEmpty()
    {
        // empty treasury, a citizen paying a fee of 50
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        await s.Mayor.ExecuteCommand("/city set fee 50");
        FakeMoneyProvider money = UseFakeEconomy();
        double today = Math.Floor(s.City.GetDayPaymentAmount());
        Assert.True(today <= 50, "precondition: the fee covers the upkeep, upkeep was " + today);
        money.SetBalance(Account(citizen), 100);

        await RunDay(s.Mayor);

        Assert.Equal(0, s.City.DebtBalance, 2);
        Assert.Equal(50m - (decimal)today, money.GetBalance(s.City.MoneyAccountName));
    }

    // DC8: a village pays no upkeep
    [AtlasScenario]
    public async Task Village_Should_GetNoDebt_When_DayPasses()
    {
        // a village with an empty treasury
        ResetConfig();
        Mod.config.VILLAGE_ENABLED = true;
        Mod.config.VILLAGE_CREATE_COST = 0;
        int area = NewArea();
        ITestPlayer head = await JoinAt("head" + area, area);
        await head.ExecuteCommand($"/village create {CityName(area)}");
        await Agree(head);
        await World.Until(() => Info(head).hasCity(), "the village to be founded", AgreeTimeoutTicks);
        City village = Info(head).City;
        Assert.True(village.IsVillage(), "precondition: it is a village");
        UseFakeEconomy();

        await RunDay(head);

        Assert.Equal(0, village.DebtBalance, 2);
        Assert.Contains(village, Mod.dataStorage.getCitiesList());
    }

    // DC5 (kicking on, which ClaimsScenarioBase sets - the mod's default is off)
    [AtlasScenario]
    public async Task Citizen_Should_BeKickedAndLosePlot_When_CannotPayCityFee()
    {
        // city fee 10, the citizen owns a plot and has no money
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        await GivenOwnedPlot(s, citizen);
        Plot plot = PlotAt(citizen);
        await s.Mayor.ExecuteCommand("/city set fee 10");
        Assert.Equal(10, s.City.fee);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 0);

        await RunDay(s.Mayor);

        Assert.False(Info(citizen).hasCity(), "a citizen who cannot pay the fee must be kicked");
        Assert.DoesNotContain(Info(citizen), s.City.getCityCitizens());
        Assert.False(plot.hasPlotOwner(), "the plot must go back to the city");
        // The mayor is shown this on their next login; before, only the city chat said it.
        Assert.Equal(new[] { Info(citizen).GetPartName() }, s.City.EvictedForUnpaidSince(0));
        AssertInvariants();
    }

    // DC5 (no kicking: loses the plots only)
    [AtlasScenario]
    public async Task Citizen_Should_StayButLosePlot_When_CannotPayAndKickingIsOff()
    {
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        Mod.config.DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE = false;
        await GivenOwnedPlot(s, citizen);
        Plot plot = PlotAt(citizen);
        await s.Mayor.ExecuteCommand("/city set fee 10");
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 0);

        await RunDay(s.Mayor);

        Assert.True(Info(citizen).hasCity(), "with kicking off the citizen must stay");
        Assert.False(plot.hasPlotOwner(), "the plot must go back to the city");
        Assert.False(Info(citizen).PlayerPlots.Contains(plot), "the plot stayed in the citizen's PlayerPlots");
        AssertInvariants();
    }
}
