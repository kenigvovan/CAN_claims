using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.commands;
using claims.src.events;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.subplots;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Sub-plots on a live server: marked and made by command, judged by the same checks the game
/// runs, sent to the client, sold, taxed, and cut when the city loses the ground under them.
/// Every sub-plot here is cut out of plot (1, 0) of the scene, where citizens may build.
/// </summary>
[Trait("Category", "E2E")]
public class SubPlotScenarios : ClaimsScenarioBase
{
    protected override void ResetConfig()
    {
        base.ResetConfig();
        Mod.config.SUBPLOT_MAX_BOXES = 8;
        Mod.config.MAX_SUBPLOT_TAX = 50;
        Mod.config.SUBPLOT_TAX_RAISE_DELAY_HOURS = 24;
    }

    /// <summary>A city with one citizen, who may build anywhere on plot (1, 0).</summary>
    private async Task<(CityScene Scene, ITestPlayer Citizen, BlockPos Center)> GivenCityWhereCitizensBuild()
    {
        CityScene s = await GivenCity("citizen");
        await s.Mayor.ExecuteCommand("/plot set p citizen build on");
        BlockPos center = PlotCenter(s.Area, 1, 0);
        Assert.True(MayBuild(s.Citizens[0], center), "precondition: citizens may build on the plot");
        return (s, s.Citizens[0], center);
    }

    /// <summary>Marks the two corners as the wand or /subplot pos1|pos2 would and runs /subplot create.</summary>
    private async Task<SubPlot> GivenSubPlot(CityScene s, string name, BlockPos a, BlockPos b)
    {
        SubPlotCommand.SetCorner(s.Mayor.Player, 0, a);
        SubPlotCommand.SetCorner(s.Mayor.Player, 1, b);
        CommandResult created = await s.Mayor.ExecuteCommand($"/subplot create {name}");
        SubPlot? subPlot = SubPlotEditor.FindByName(s.City, name);
        Assert.True(subPlot != null, $"/subplot create {name} made nothing: {created.Message}");
        return subPlot!;
    }

    private Task<SubPlot> GivenFlat(CityScene s, BlockPos center) =>
        GivenSubPlot(s, "flat", center.AddCopy(-3, 0, -3), center.AddCopy(3, 3, 3));

    private static bool MayBuild(ITestPlayer player, BlockPos pos) =>
        OnBlockAction.canBlockDestroy(player.Player, new BlockSelection { Position = pos }, out _);

    [AtlasScenario]
    public async Task SubPlot_Should_ShutCitizensOut_Inside_ButNotOutside()
    {
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();

        await GivenFlat(s, center);

        // new sub-plots are private; the rest of the chunk keeps its rules
        Assert.False(MayBuild(citizen, center), "a citizen can still build inside a new sub-plot");
        Assert.True(MayBuild(citizen, center.AddCopy(6, 0, 0)), "a citizen lost building outside the sub-plot");
        Assert.True(MayBuild(s.Mayor, center), "the mayor cannot build inside the city's own sub-plot");
        AssertInvariants();
    }

    [AtlasScenario]
    public async Task ClientPacket_Should_CarryTheSubPlot_WithTheViewersRights()
    {
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();
        await GivenFlat(s, center);
        // Test players join in creative, and a creative player is sent every right on purpose
        citizen.Player.WorldData.CurrentGameMode = EnumGameMode.Survival;
        citizen.Player.BroadcastPlayerData();

        var info = PlotStateHandling.BuildSavedPlotInfo(PlotAt(citizen), citizen.Player, Info(citizen));

        var piece = Assert.Single(info.clientInnerClaims ?? new());
        Assert.Equal("flat", piece.Name);
        Assert.True(piece.Contains(center.X, center.Y, center.Z), "the piece sent does not hold the sub-plot's block");
        Assert.False(piece.May(1), "the client would let the citizen start breaking a block inside");
        Assert.True(info.buildFlag, "the chunk's own flag must still say the citizen may build");
    }

    [AtlasScenario]
    public async Task Citizen_Should_Build_AfterBuyingTheSubPlot()
    {
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();
        SubPlot flat = await GivenFlat(s, center);
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(citizen), 100);
        decimal treasuryBefore = money.GetBalance(s.City.MoneyAccountName);
        await s.Mayor.ExecuteCommand("/subplot fs 40 flat");

        CommandResult ask = await citizen.ExecuteCommand("/subplot buy flat");
        Assert.True(ask.Ok, "/subplot buy: " + ask.Message);
        await Agree(citizen);
        await World.Until(() => Info(citizen).Equals(flat.Owner), "the sub-plot to be bought after /agree", AgreeTimeoutTicks);

        Assert.Equal(60m, money.GetBalance(Account(citizen)));
        Assert.Equal(treasuryBefore + 40m, money.GetBalance(s.City.MoneyAccountName));
        Assert.True(MayBuild(citizen, center), "the owner cannot build in their own sub-plot");
        Assert.False(flat.IsForSale, "a bought sub-plot is still for sale");
    }

    [AtlasScenario]
    public async Task CityUnclaim_Should_BeRefused_While_ASubPlotReachesIn()
    {
        var (s, _, center) = await GivenCityWhereCitizensBuild();
        await GivenFlat(s, center);

        await s.Mayor.ExecuteCommand("/city unclaim");

        Assert.True(ClaimedBy(s.City, s.Mayor), "the city unclaimed ground a sub-plot stands on");
        Assert.NotNull(SubPlotEditor.FindByName(s.City, "flat"));
    }

    [AtlasScenario]
    public async Task LostChunk_Should_BeCutOutOfTheSubPlot_AndTheRestKept()
    {
        // a sub-plot over plots (1, 0) and (2, 0)
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();
        await GoToPlot(s.Mayor, s.Area, 2, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(s.City, s.Mayor), "precondition: plot (2, 0) is the city's");
        SubPlot flat = await GivenSubPlot(s, "flat", center.AddCopy(4, 0, -2), center.AddCopy(12, 3, 2));
        Assert.Equal(2, flat.Chunks(PlotPosition.plotSize).Count);
        Plot lost = PlotAt(s.Mayor);
        CityScene enemy = await GivenCity();

        // plot (2, 0) changes hands, as a capture does
        PlotTransferHelper.Transfer(lost, s.City, enemy.City, markCaptured: true);

        Assert.True(Mod.dataStorage.TryGetSubPlot(flat.Guid, out _), "the sub-plot went with the chunk it only partly stood on");
        Assert.Single(flat.Chunks(PlotPosition.plotSize));
        Assert.Empty(Mod.dataStorage.SubPlotsInChunk(lost.getPos()));
        Assert.False(MayBuild(citizen, center.AddCopy(5, 0, 0)), "the part left in our chunk stopped protecting");
    }

    [AtlasScenario]
    public async Task UnacceptedTaxRaise_Should_GiveTheSubPlotBack_WhenTheWaitIsOver()
    {
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();
        SubPlot flat = await GivenFlat(s, center);
        await s.Mayor.ExecuteCommand($"/subplot setowner {citizen.Player.PlayerName} flat");
        Assert.True(Info(citizen).Equals(flat.Owner), "precondition: the citizen owns the sub-plot");

        await s.Mayor.ExecuteCommand("/subplot tax 10 flat");
        Assert.True(flat.HasPendingTax, "a raise on an owned sub-plot applied without the owner's say");
        // Nobody waits a day in a test: the deadline is moved to the past.
        flat.PendingTaxAt = TimeFunctions.getEpochSeconds() - 1;
        await RunHour(s.Mayor);

        Assert.Null(flat.Owner);
        Assert.Equal(10, flat.Tax);
        Assert.False(MayBuild(citizen, center), "the former owner can still build in the sub-plot");
    }

    [AtlasScenario]
    public async Task AcceptedTaxRaise_Should_Apply_AndTheOwnerKeepsTheSubPlot()
    {
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();
        SubPlot flat = await GivenFlat(s, center);
        await s.Mayor.ExecuteCommand($"/subplot setowner {citizen.Player.PlayerName} flat");
        await s.Mayor.ExecuteCommand("/subplot tax 10 flat");

        CommandResult accept = await citizen.ExecuteCommand("/subplot accepttax flat");
        Assert.True(accept.Ok, "/subplot accepttax: " + accept.Message);
        flat.PendingTaxAt = TimeFunctions.getEpochSeconds() - 1;
        await RunHour(s.Mayor);

        Assert.True(Info(citizen).Equals(flat.Owner), "the owner lost a sub-plot whose tax they accepted");
        Assert.Equal(10, flat.Tax);
        Assert.False(flat.HasPendingTax);
    }

    [AtlasScenario]
    public async Task CitizenLeavingTheCity_Should_GiveTheirSubPlotBack()
    {
        var (s, citizen, center) = await GivenCityWhereCitizensBuild();
        SubPlot flat = await GivenFlat(s, center);
        await s.Mayor.ExecuteCommand($"/subplot setowner {citizen.Player.PlayerName} flat");

        await citizen.ExecuteCommand("/city leave");
        await World.Until(() => !Info(citizen).hasCity(), "the citizen to leave the city");

        Assert.Null(flat.Owner);
        AssertInvariants();
    }
}
