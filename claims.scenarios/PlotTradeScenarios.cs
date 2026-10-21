using System.Data;
using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.database;
using claims.src.part.structure;
using claims.src.rights;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Buying a plot from one's own city and giving it back: /plot claim, /plot unclaim, /plot fs.
/// Cases: section 6.
/// </summary>
[Trait("Category", "E2E")]
public class PlotTradeScenarios : ClaimsScenarioBase
{
    // TC05
    [AtlasScenario]
    public async Task Claim_Should_MoveMoneyAndOwnership_When_CitizenBuysPlotOfOwnCity()
    {
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(buyer), 100);
        decimal treasuryBefore = money.GetBalance(s.City.MoneyAccountName);
        decimal totalBefore = money.Total();

        CommandResult result = await ClaimAndAgree(buyer);

        Plot plot = PlotAt(buyer);
        Assert.True(result.Ok, "/plot claim: " + result.Message);
        Assert.True(Owns(buyer), "the buyer must own the plot");
        Assert.Equal(-1, plot.Price);
        Assert.Equal(40, plot.lastPaidPrice);
        Assert.Equal(60m, money.GetBalance(Account(buyer)));
        Assert.Equal(treasuryBefore + 40m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(totalBefore, money.Total());
        AssertInvariants();
    }

    // TC06
    [AtlasScenario]
    public async Task Claim_Should_LetAStrangerBuy_When_PlotIsAnEmbassy()
    {
        CityScene s = await GivenCity();
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        await GoToPlot(stranger, s.Area, 1, 0);
        await GivenPlotType(s.Mayor, "embassy", PlotType.EMBASSY);
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(stranger), 100);
        Assert.False(Info(stranger).hasCity(), "precondition: the stranger has no city");

        await ClaimAndAgree(stranger);

        Assert.True(Owns(stranger), "a stranger must be able to buy an embassy");
        Assert.Equal(60m, money.GetBalance(Account(stranger)));
        AssertInvariants();
    }

    // TC26, A6, P1
    [AtlasScenario]
    public async Task Embassy_Should_StayWithCitizen_When_OwnerLeavesTheCity()
    {
        // a citizen buys an embassy plot in their own city
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenPlotType(s.Mayor, "embassy", PlotType.EMBASSY);
        await GivenOwnedPlot(s, owner);

        await owner.ExecuteCommand("/city leave");

        Assert.False(Info(owner).hasCity(), "precondition: the owner left the city");
        Assert.True(Owns(owner), "an embassy stays with its owner after leaving the city (P1)");
        AssertInvariants();
    }

    // TC23 (E5)
    [AtlasScenario]
    public async Task Claim_Should_ChargeOnce_When_SentTwiceInARow()
    {
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(buyer), 100);

        await ClaimAndAgree(buyer);
        CommandResult second = await buyer.ExecuteCommand("/plot claim");

        Assert.True(Owns(buyer), "the buyer must own the plot");
        Assert.False(second.Ok, "the second /plot claim must be refused: " + second.Message);
        Assert.Equal(60m, money.GetBalance(Account(buyer)));
        AssertInvariants();
    }

    // TC20, R30, C8
    [AtlasScenario]
    public async Task Agree_Should_NotBuy_When_PriceChangedAfterTheOffer()
    {
        // the buyer is offered the plot for 40
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(buyer), 10000);
        await buyer.ExecuteCommand("/plot claim");

        // the mayor raises the price before the buyer agrees
        await PutForSale(s.Mayor, 9999);
        await buyer.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.False(PlotAt(buyer).hasPlotOwner(), "the buyer got the plot at a price they were not shown");
        Assert.Equal(10000m, money.GetBalance(Account(buyer)));
    }

    // TC29, C8
    [AtlasScenario]
    public async Task Agree_Should_NotBuy_When_PlotWasTakenOffSaleAfterTheOffer()
    {
        // the buyer is offered the plot for 40
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(buyer), 100);
        await buyer.ExecuteCommand("/plot claim");

        // the mayor takes it off sale before the buyer agrees
        await s.Mayor.ExecuteCommand("/plot nfs");
        await buyer.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.False(PlotAt(buyer).hasPlotOwner(), "the buyer got a plot that is no longer for sale");
        Assert.Equal(100m, money.GetBalance(Account(buyer)));
    }

    // TC22, E4 (database round trip; a real restart needs a class without joined players)
    [AtlasScenario]
    public async Task Purchase_Should_SurviveTheDatabase_When_SavedAndLoadedBack()
    {
        // the owner bought the plot for 40
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(owner), 100);
        await GivenOwnedPlot(s, owner, 40);
        Plot plot = PlotAt(owner);
        SQLiteDatabaseHanlder db = Database();
        db.saveEveryThing();

        // forget the purchase in memory, then load the row back
        DataRow row = Assert.Single(db.readFromDatabase("SELECT * FROM PLOTS WHERE x=@x AND z=@z",
            new Dictionary<string, object> { { "@x", plot.getPos().X }, { "@z", plot.getPos().Y } }).Rows.Cast<DataRow>());
        plot.setPlotOwner(null);
        Info(owner).PlayerPlots.Remove(plot);
        plot.lastPaidPrice = 0;
        Assert.True(db.loadPlot(row), "loadPlot failed");

        Assert.True(Owns(owner), "the owner did not survive the database");
        Assert.Contains(plot, Info(owner).PlayerPlots);
        Assert.Equal(40, plot.lastPaidPrice);
        Assert.Equal(-1, plot.Price);
        AssertInvariants();
    }

    // TC09, R27
    [AtlasScenario]
    public async Task Owner_Should_KeepPlot_When_PreviousOwnerLeavesCityAfterMayorUnclaimedIt()
    {
        CityScene s = await GivenCity("first", "second");
        ITestPlayer first = s.Citizens[0], second = s.Citizens[1];
        await GivenOwnedPlot(s, first);
        await s.Mayor.ExecuteCommand("/plot unclaim");
        await GivenOwnedPlot(s, second);

        await first.ExecuteCommand("/city leave");

        Assert.True(Owns(second), "second lost the plot when first left the city");
        AssertInvariants();
    }

    // TC04, R29
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_PlayerLacksPlotClaimPermission()
    {
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        await PutForSale(s.Mayor, 0);

        using (WithoutGroupPermission("DEFAULT", EnumPlayerPermissions.PLOT_CLAIM, buyer))
        {
            Assert.False(Info(buyer).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.PLOT_CLAIM),
                "precondition: buyer has no PLOT_CLAIM");

            await ClaimAndAgree(buyer);

            Assert.False(PlotAt(buyer).hasPlotOwner(), "a player without PLOT_CLAIM bought the plot");
        }
        AssertInvariants();
    }

    // TC04 (unclaim), R29
    [AtlasScenario]
    public async Task Unclaim_Should_BeRefused_When_PlayerLacksPlotUnclaimPermission()
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);

        using (WithoutGroupPermission("DEFAULT", EnumPlayerPermissions.PLOT_UNCLAIM, owner))
        {
            Assert.False(Info(owner).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.PLOT_UNCLAIM),
                "precondition: owner has no PLOT_UNCLAIM");

            await owner.ExecuteCommand("/plot unclaim");

            Assert.True(Owns(owner), "a player without PLOT_UNCLAIM gave the plot back");
        }
        AssertInvariants();
    }

    // TC08 (S2 + claim by someone else)
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_PlotAlreadyHasAnOwner()
    {
        // first owns the plot, the mayor prices it anyway (S3, allowed by P2)
        CityScene s = await GivenCity("first", "second");
        ITestPlayer first = s.Citizens[0], second = s.Citizens[1];
        await GivenOwnedPlot(s, first);
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(second), 100);

        CommandResult result = await ClaimAndAgree(second);

        Assert.False(result.Ok, "buying a plot that has an owner must be refused: " + result.Message);
        Assert.True(Owns(first), "first must still own the plot");
        Assert.Equal(100m, money.GetBalance(Account(second)));
        AssertInvariants();
    }

    // TC08 (S1 + unclaim by the mayor), A2
    [AtlasScenario]
    public async Task Unclaim_Should_BeRefused_When_PlotHasNoOwner()
    {
        // a city plot for sale that nobody has bought
        CityScene s = await GivenCity();
        await PutForSale(s.Mayor, 30);

        CommandResult result = await s.Mayor.ExecuteCommand("/plot unclaim");

        Assert.Equal(30, PlotAt(s.Mayor).Price);
        Assert.False(result.Ok, "unclaiming a plot nobody owns must be refused (A2): " + result.Message);
    }

    // TC07 (S3 -> S0, owner leaves the city), P1
    [AtlasScenario]
    public async Task Plot_Should_GoBackToCityNotForSale_When_OwnerOfPricedPlotLeavesTheCity()
    {
        // the owner's plot is priced by the mayor (S3)
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        await PutForSale(s.Mayor, 50);
        Plot plot = PlotAt(owner);

        await owner.ExecuteCommand("/city leave");

        Assert.False(plot.hasPlotOwner(), "the plot must go back to the city");
        Assert.Equal(-1, plot.Price);
        Assert.False(Info(owner).PlayerPlots.Contains(plot), "the plot stayed in the former owner's PlayerPlots");
        AssertInvariants();
    }

    // TC07 (S1 -> S1, new price), FS5
    [AtlasScenario]
    public async Task Claim_Should_ChargeTheNewPrice_When_PriceWasChanged()
    {
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 30);
        await PutForSale(s.Mayor, 50);
        money.SetBalance(Account(buyer), 100);

        await ClaimAndAgree(buyer);

        Assert.True(Owns(buyer), "the buyer must own the plot");
        Assert.Equal(50m, money.GetBalance(Account(buyer)));
        Assert.Equal(50, PlotAt(buyer).lastPaidPrice);
    }

    // TC07 (S1 -> S0, nfs), FS4
    [AtlasScenario]
    public async Task Claim_Should_BeRefused_When_PlotWasTakenOffSale()
    {
        // (S1 -> S0)
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        await PutForSale(s.Mayor, 30);
        await s.Mayor.ExecuteCommand("/plot nfs");
        Assert.Equal(-1, PlotAt(buyer).Price);

        CommandResult result = await ClaimAndAgree(buyer);

        Assert.False(result.Ok, "a plot taken off sale must not be bought: " + result.Message);
        Assert.False(PlotAt(buyer).hasPlotOwner(), "the plot must stay the city's");
    }

    // TC07 (S0 -> S1), FS2
    [AtlasScenario]
    public async Task ForSale_Should_PutCityPlotOnSale_When_PriceIsZeroOrMore()
    {
        // (S0)
        CityScene s = await GivenCity();
        Assert.False(PlotAt(s.Mayor).IsForSale, "precondition: a freshly claimed plot is not for sale");

        CommandResult result = await s.Mayor.ExecuteCommand("/plot fs 0");

        Assert.True(result.Ok, "/plot fs 0: " + result.Message);
        Assert.Equal(0, PlotAt(s.Mayor).Price);
        Assert.True(PlotAt(s.Mayor).IsForSale, "price 0 means free, not off sale (P3)");
    }

    // TC07 (S2 -> S0, owner leaves the city), P1, D2
    [AtlasScenario]
    public async Task Plot_Should_GoBackToCity_When_OwnerLeavesTheCity()
    {
        // (S2), the owner made it a tavern
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        await GivenPlotType(s.Mayor, "tavern", PlotType.TAVERN);
        Plot plot = PlotAt(owner);

        await owner.ExecuteCommand("/city leave");

        Assert.False(plot.hasPlotOwner(), "the plot must go back to the city");
        Assert.Equal(PlotType.DEFAULT, plot.Type);
        Assert.False(Info(owner).PlayerPlots.Contains(plot), "the plot stayed in the former owner's PlayerPlots");
        AssertInvariants();
    }

    // TC07 (S2 -> S3, fs on an owned plot), FS3, P2
    [AtlasScenario]
    public async Task ForSale_Should_KeepTheOwner_When_PlotIsOwned()
    {
        // (S2)
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);

        await s.Mayor.ExecuteCommand("/plot fs 50");

        Assert.Equal(50, PlotAt(owner).Price);
        Assert.True(Owns(owner), "fs must not touch the owner");
        AssertInvariants();
    }

    // TC07 (S3 -> S2, nfs), FS4
    [AtlasScenario]
    public async Task NotForSale_Should_KeepTheOwner_When_PlotWasPricedWhileOwned()
    {
        // (S3)
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        await PutForSale(s.Mayor, 50);

        await s.Mayor.ExecuteCommand("/plot nfs");

        Assert.Equal(-1, PlotAt(owner).Price);
        Assert.True(Owns(owner), "nfs must not touch the owner");
        AssertInvariants();
    }

    // TC07 (S2 -> S0 by the mayor), R09
    [AtlasScenario]
    public async Task Unclaim_Should_RemovePlotFromOwnersList_When_MayorUnclaimsIt()
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        Plot plot = PlotAt(owner);

        await s.Mayor.ExecuteCommand("/plot unclaim");

        Assert.False(plot.hasPlotOwner(), "the plot must have no owner after the mayor's unclaim");
        Assert.False(Info(owner).PlayerPlots.Contains(plot), "the plot stayed in the former owner's PlayerPlots");
        AssertInvariants();
    }

    // R27: the refund belongs to whoever loses the plot, not to the mayor who unclaimed it
    [AtlasScenario]
    public async Task Unclaim_Should_RefundTheOwner_When_MayorUnclaimsIt()
    {
        // the owner paid 40, refund 50%
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        EnableRefund(50);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(owner), 100);
        await GivenOwnedPlot(s, owner, 40);
        decimal mayorBefore = money.GetBalance(Account(s.Mayor));

        await s.Mayor.ExecuteCommand("/plot unclaim");

        Assert.False(PlotAt(owner).hasPlotOwner(), "precondition: the mayor unclaimed the plot");
        Assert.Equal(80m, money.GetBalance(Account(owner)));
        Assert.Equal(mayorBefore, money.GetBalance(Account(s.Mayor)));
    }

    // TC14 (Q6), R09
    [AtlasScenario]
    public async Task Unclaim_Should_RemoveEmbassyFromStrangersList_When_MayorUnclaimsIt()
    {
        // a stranger owns an embassy plot of the city
        CityScene s = await GivenCity();
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        await GoToPlot(stranger, s.Area, 1, 0);
        await GivenPlotType(s.Mayor, "embassy", PlotType.EMBASSY);
        await GivenOwnedPlot(s, stranger);
        Plot plot = PlotAt(stranger);

        await s.Mayor.ExecuteCommand("/plot unclaim");

        Assert.False(plot.hasPlotOwner(), "the plot must have no owner after the mayor's unclaim");
        Assert.False(Info(stranger).PlayerPlots.Contains(plot), "the embassy stayed in the stranger's PlayerPlots");
        AssertInvariants();
    }

    // TC13 (Q5), R28, A3
    [AtlasScenario]
    public async Task Unclaim_Should_ResetPlotPermissionsToCityDefaults_When_OwnerChangedThem()
    {
        // the owner lets strangers build on the plot
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        Plot plot = PlotAt(owner);
        await owner.ExecuteCommand("/plot set p stranger build on");
        Assert.True(plot.getPermsHandler().StrangerPerms[1], "precondition: strangers may build on the plot");

        await owner.ExecuteCommand("/plot unclaim");

        Assert.False(plot.hasPlotOwner(), "precondition: the plot is free");
        Assert.Equal(s.City.getPermsHandler().getStringForChat(), plot.getPermsHandler().getStringForChat());
    }

    // TC30, R27 (plot added to a group)
    [AtlasScenario]
    public async Task PlotsGroup_Should_RemovePlotFromOwnersList_When_OwnedPlotIsAddedToGroup()
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        Plot plot = PlotAt(owner);
        await s.Mayor.ExecuteCommand("/city plotsgroup create grp");

        await s.Mayor.ExecuteCommand("/city plotsgroup plotadd grp");

        Assert.True(plot.hasCityPlotsGroup(), "precondition: the plot is in the group");
        Assert.False(plot.hasPlotOwner(), "a plot in a group has no owner");
        Assert.False(Info(owner).PlayerPlots.Contains(plot), "the plot stayed in the former owner's PlayerPlots");
        AssertInvariants();
    }

    // TC10, R09 (O2)
    [AtlasScenario]
    public async Task Refund_Should_BePaidOnce_When_MayorUnclaimsAPlotItsOwnerAlreadyGaveBack()
    {
        // bought for 100, given back by the owner for a refund of 50
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        EnableRefund(50);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(owner), 100);
        await GivenOwnedPlot(s, owner, 100);
        await owner.ExecuteCommand("/plot unclaim");
        Assert.Equal(50m, money.GetBalance(s.City.MoneyAccountName));

        await s.Mayor.ExecuteCommand("/plot unclaim");

        Assert.Equal(50m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(0m, money.GetBalance(Account(s.Mayor)));
    }

    // TC15, R08
    [AtlasScenario]
    public async Task Unclaim_Should_FreeBoughtPlot_When_NoEconomyModIsInstalled()
    {
        // default noop economy
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        EnableRefund(50);
        await GivenOwnedPlot(s, owner, 10);
        Plot plot = PlotAt(owner);

        Exception? error = await Record.ExceptionAsync(() => owner.ExecuteCommand("/plot unclaim"));

        Assert.True(error == null, "/plot unclaim threw: " + error);
        Assert.False(plot.hasPlotOwner(), "the plot must be free after /plot unclaim");
        AssertInvariants();
    }

    // TC19, C5
    [AtlasScenario]
    public async Task Claim_Should_ChangeNothing_When_PaymentFails()
    {
        CityScene s = await GivenCity("buyer");
        ITestPlayer buyer = s.Citizens[0];
        FakeMoneyProvider money = UseFakeEconomy();
        await PutForSale(s.Mayor, 40);
        money.SetBalance(Account(buyer), 100);
        decimal treasuryBefore = money.GetBalance(s.City.MoneyAccountName);
        money.FailNext(FakeMoneyOp.Transfer, Account(buyer));

        await ClaimAndAgree(buyer);

        Plot plot = PlotAt(buyer);
        Assert.False(plot.hasPlotOwner(), "the plot changed hands although the payment failed");
        Assert.Equal(40, plot.Price);
        Assert.Equal(100m, money.GetBalance(Account(buyer)));
        Assert.Equal(treasuryBefore, money.GetBalance(s.City.MoneyAccountName));
        AssertInvariants();
    }

    // TC18, A4 (O5)
    [AtlasScenario]
    public async Task Unclaim_Should_ChangeNothing_When_RefundPaymentFails()
    {
        // bought for 100, the refund of 50 will fail
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        EnableRefund(50);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(owner), 100);
        await GivenOwnedPlot(s, owner, 100);
        money.FailNext(FakeMoneyOp.Transfer, s.City.MoneyAccountName);

        await owner.ExecuteCommand("/plot unclaim");

        Assert.True(Owns(owner), "the plot was taken although the refund was not paid");
        Assert.Equal(0m, money.GetBalance(Account(owner)));
        Assert.Equal(100m, money.GetBalance(s.City.MoneyAccountName));
        AssertInvariants();
    }

    // TC17, U5
    [AtlasScenario]
    public async Task Unclaim_Should_PayWhatTreasuryHas_When_OwnerAgreesToReducedRefund()
    {
        // refund would be 50, the treasury holds only 20
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        EnableRefund(50);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(owner), 100);
        await GivenOwnedPlot(s, owner, 100);
        Plot plot = PlotAt(owner);
        money.SetBalance(s.City.MoneyAccountName, 20);

        await owner.ExecuteCommand("/plot unclaim");
        Assert.True(Owns(owner), "the plot must wait for /agree");
        await Agree(owner);
        await World.Until(() => !plot.hasPlotOwner(), "the plot to be freed after /agree", AgreeTimeoutTicks);

        Assert.Equal(20m, money.GetBalance(Account(owner)));
        Assert.Equal(0m, money.GetBalance(s.City.MoneyAccountName));
        AssertInvariants();
    }

    // TC21, R05 (E2)
    [AtlasScenario]
    public async Task Agree_Should_NotTakeThePlot_When_ItChangedHandsWhileWaiting()
    {
        // first's reduced-refund unclaim waits for /agree
        CityScene s = await GivenCity("first", "second");
        ITestPlayer first = s.Citizens[0], second = s.Citizens[1];
        EnableRefund(50);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(first), 100);
        await GivenOwnedPlot(s, first, 100);
        money.SetBalance(s.City.MoneyAccountName, 20);
        await first.ExecuteCommand("/plot unclaim");

        // Meanwhile the plot goes to second. Refund off, so the mayor's unclaim needs no /agree.
        Mod.config.PLOT_UNCLAIM_REFUND_PERCENT = 0;
        await s.Mayor.ExecuteCommand("/plot unclaim");
        await GivenOwnedPlot(s, second);
        decimal firstBefore = money.GetBalance(Account(first));

        await first.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.True(Owns(second), "first's late /agree took second's plot");
        Assert.Equal(firstBefore, money.GetBalance(Account(first)));
    }

    // TC12, A8
    [AtlasScenario]
    public async Task Plot_Should_StayForSale_When_OwnerGivesBackAPlotTheMayorPricedForSale()
    {
        // first owns the plot, the mayor puts it up for 50 (S3)
        CityScene s = await GivenCity("first", "second");
        ITestPlayer first = s.Citizens[0], second = s.Citizens[1];
        FakeMoneyProvider money = UseFakeEconomy();
        await GivenOwnedPlot(s, first);
        await PutForSale(s.Mayor, 50);
        money.SetBalance(Account(second), 50);
        decimal treasuryBefore = money.GetBalance(s.City.MoneyAccountName);

        await first.ExecuteCommand("/plot unclaim");
        Assert.Equal(50, PlotAt(first).Price);
        await ClaimAndAgree(second);

        Assert.True(Owns(second), "second must have bought the plot");
        Assert.Equal(treasuryBefore + 50m, money.GetBalance(s.City.MoneyAccountName));
        Assert.Equal(0m, money.GetBalance(Account(first)));
        AssertInvariants();
    }

    // TC27, R31, A10
    [AtlasScenario]
    public async Task MainPlot_Should_KeepItsType_When_CitizenWhoBoughtItLeavesTheCity()
    {
        // a city from before A11 - its founding plot (0, 0) is MAIN_CITY_PLOT - and the
        // owner buys that plot
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GoToPlot(s.Mayor, s.Area);
        await GoToPlot(owner, s.Area);
        PlotAt(owner).Type = PlotType.MAIN_CITY_PLOT;
        await GivenOwnedPlot(s, owner);
        Plot mainPlot = PlotAt(owner);

        await owner.ExecuteCommand("/city leave");

        Assert.False(mainPlot.hasPlotOwner(), "the owner left the city, the plot goes back to it (P1)");
        Assert.Equal(PlotType.MAIN_CITY_PLOT, mainPlot.Type);
        AssertInvariants();
    }

    // TC28, A11
    [AtlasScenario]
    public async Task FoundingPlot_Should_BeOrdinaryAndStaySo_When_MayorTriesToMakeItMain()
    {
        CityScene s = await GivenCity();
        await GoToPlot(s.Mayor, s.Area);
        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);

        await s.Mayor.ExecuteCommand("/plot set type MAIN_CITY_PLOT");

        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);
    }

    // TC28 (admin), A11
    [AtlasScenario]
    public async Task MainPlotType_Should_StillBeSettableByAdmin()
    {
        CityScene s = await GivenCity();
        AllowAdminCommands(s.Mayor);

        await s.Mayor.ExecuteCommand("/cadmin plot type MAIN_CITY_PLOT");

        Assert.Equal(PlotType.MAIN_CITY_PLOT, PlotAt(s.Mayor).Type);
    }

    // R16: the type is set before the money is taken
    [AtlasScenario]
    public async Task SetType_Should_KeepTheOldType_When_PaymentFails()
    {
        // a paid type change confirmed with /agree, the withdrawal will fail
        CityScene s = await GivenCity();
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(s.City.MoneyAccountName, 100);
        await s.Mayor.ExecuteCommand("/plot set type farm");
        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);
        money.FailNext(FakeMoneyOp.Withdraw, s.City.MoneyAccountName);

        await s.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);
        Assert.Equal(100m, money.GetBalance(s.City.MoneyAccountName));
    }

    // TC24 (another city's plot), FS1, R24
    [AtlasScenario]
    public async Task ForSale_Should_BeRefusedAsError_When_PlotBelongsToAnotherCity()
    {
        // the mayor of another city stands on our plot
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        await PutForSale(ours.Mayor, 30);
        await GoToPlot(theirs.Mayor, ours.Area, 1, 0);

        CommandResult result = await theirs.Mayor.ExecuteCommand("/plot fs 1");

        Assert.Equal(30, PlotAt(ours.Mayor).Price);
        Assert.False(result.Ok, "a refusal must be reported as an error: " + result.Message);
    }

    // TC24 (no right), FS1
    [AtlasScenario]
    public async Task ForSale_Should_BeRefused_When_CitizenLacksPlotSetFsPermission()
    {
        // an ordinary citizen, rights of the DEFAULT group only
        CityScene s = await GivenCity("citizen");
        ITestPlayer citizen = s.Citizens[0];
        await PutForSale(s.Mayor, 30);
        Assert.False(Info(citizen).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.PLOT_SET_FS),
            "precondition: citizen has no PLOT_SET_FS");

        CommandResult result = await citizen.ExecuteCommand("/plot fs 1");

        Assert.Equal(30, PlotAt(citizen).Price);
        Assert.False(result.Ok, "a refusal must be reported as an error: " + result.Message);
    }

    // TC24 (negative price), FS2, R24
    [AtlasScenario]
    public async Task ForSale_Should_BeRefusedAsError_When_PriceIsNegative()
    {
        CityScene s = await GivenCity();
        await PutForSale(s.Mayor, 30);

        CommandResult result = await s.Mayor.ExecuteCommand("/plot fs -5");

        Assert.Equal(30, PlotAt(s.Mayor).Price);
        Assert.False(result.Ok, "a refusal must be reported as an error: " + result.Message);
    }
}
