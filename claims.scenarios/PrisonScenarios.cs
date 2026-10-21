using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.delayed.teleportation;
using claims.src.events;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Getting into prison by being killed on a city's land, serving, escaping, cells.
/// Cases PZ1-PZ9, SU8.
/// </summary>
[Trait("Category", "E2E")]
public class PrisonScenarios : ClaimsScenarioBase
{
    protected override void ResetConfig()
    {
        base.ResetConfig();
        Mod.config.MAX_CELLS_PER_PRISON = 4;
        Mod.config.SUMMON_ALLOWED = true;
        Mod.config.SUMMON_MIN_PLAYERS = 0;
        Mod.config.SECONDS_SUMMON_TIME = 60;
        Mod.config.PLAYER_MOVEMENT_CANCEL_TELEPORTATION = false;
    }

    /// <summary>Makes the mayor's plot (1, 0) a prison; it comes with one cell where the mayor stands.</summary>
    private async Task<Prison> GivenPrison(CityScene s)
    {
        await GivenPlotType(s.Mayor, "prison", PlotType.PRISON);
        Prison prison = PlotAt(s.Mayor).Prison;
        Assert.True(prison != null && prison.getPrisonCells().Count == 1, "precondition: a prison with one cell");
        return prison;
    }

    /// <summary>The death event as the game raises it, without a fight.</summary>
    private static void Kill(ITestPlayer killer, ITestPlayer victim) =>
        OnPlayerDeath.Event_OnPlayerDeath(victim.Player,
            new DamageSource { Source = EnumDamageSource.Player, SourceEntity = killer.Player.Entity });

    private static bool InACell(Prison prison, ITestPlayer player) =>
        prison.TryGetCellInWhichPlayer(Info(player), out _);

    // PZ1, R38
    [AtlasScenario]
    public async Task Stranger_Should_GoToPrison_When_KilledByACitizenOnTheCitysLand()
    {
        CityScene s = await GivenCity();
        Prison prison = await GivenPrison(s);
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);

        Kill(s.Mayor, stranger);

        Assert.Same(prison, Info(stranger).PrisonedIn);
        Assert.True(InACell(prison, stranger), "the prisoner is in no cell");
    }

    // PZ2, R38, A27
    [AtlasScenario]
    public async Task Sentence_Should_HaveAnEnd()
    {
        CityScene s = await GivenCity();
        await GivenPrison(s);
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);

        Kill(s.Mayor, stranger);

        Assert.True(Info(stranger).isPrisoned(), "precondition: imprisoned");
        Assert.True(Info(stranger).PrisonHoursLeft > 0, "no sentence length: the hour timer will never let the prisoner out");
    }

    // PZ3, R38
    [AtlasScenario]
    public async Task Prisoner_Should_BeReleased_When_TheLastHourPasses()
    {
        CityScene s = await GivenCity();
        Prison prison = await GivenPrison(s);
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        Kill(s.Mayor, stranger);
        Info(stranger).PrisonHoursLeft = 2;

        await RunHour(s.Mayor);
        Assert.Equal(1, Info(stranger).PrisonHoursLeft);
        Assert.True(Info(stranger).isPrisoned(), "released an hour early");

        await RunHour(s.Mayor);
        Assert.False(Info(stranger).isPrisoned(), "not released after the last hour");
        Assert.False(InACell(prison, stranger), "released but still listed in a cell");
    }

    // PZ4, R38
    [AtlasScenario]
    public async Task Nobody_Should_GoToPrison_OffTheKillersLand()
    {
        CityScene s = await GivenCity();
        await GivenPrison(s);
        CityScene other = await GivenCity();
        ITestPlayer inTheWild = await JoinAt("wild" + s.Area, s.Area);
        await GoToPlot(inTheWild, s.Area, 6, 0);
        ITestPlayer onOtherLand = await JoinAt("otherland" + s.Area, other.Area);

        Kill(s.Mayor, inTheWild);
        Kill(s.Mayor, onOtherLand);

        Assert.False(Info(inTheWild).isPrisoned(), "imprisoned for a death in the wild");
        Assert.False(Info(onOtherLand).isPrisoned(), "imprisoned for a death on another city's land");
    }

    // PZ5, R38, A28
    [AtlasScenario]
    public async Task Citizen_Should_NotGoToTheirOwnCitysPrison()
    {
        CityScene s = await GivenCity("citizen");
        await GivenPrison(s);

        Kill(s.Mayor, s.Citizens[0]);

        Assert.False(Info(s.Citizens[0]).isPrisoned(), "a citizen was put in their own city's prison");
    }

    // PZ6, R38
    [AtlasScenario]
    public async Task Prisoner_Should_BeFree_When_EscapingToTheWild()
    {
        CityScene s = await GivenCity();
        Prison prison = await GivenPrison(s);
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        Kill(s.Mayor, stranger);
        Assert.True(Info(stranger).isPrisoned(), "precondition: imprisoned");

        await GoToPlot(stranger, s.Area, 6, 0);

        await World.Until(() => !Info(stranger).isPrisoned(), "the escape to be noticed", 150);
        Assert.False(InACell(prison, stranger), "escaped but still listed in a cell");
    }

    // PZ7, R38, A17
    [AtlasScenario]
    public async Task Release_Should_GiveBackTheSpawnTheyHadBefore()
    {
        CityScene s = await GivenCity();
        await GivenPrison(s);
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        BlockPos home = PlotCenter(s.Area, 8, 0);
        stranger.Player.SetSpawnPosition(new PlayerSpawnPos(home.X, home.Y, home.Z));
        Kill(s.Mayor, stranger);
        Info(stranger).PrisonHoursLeft = 1;

        await RunHour(s.Mayor);

        Assert.False(Info(stranger).isPrisoned(), "precondition: released");
        var spawn = stranger.Player.GetSpawnPosition(false);
        Assert.True(Math.Abs(spawn.X - home.X) <= 2 && Math.Abs(spawn.Z - home.Z) <= 2,
            $"the spawn is ({spawn.X}, {spawn.Z}), not the player's own ({home.X}, {home.Z})");
    }

    // PZ8, R38
    [AtlasScenario]
    public async Task Cells_Should_StopAtTheLimit()
    {
        CityScene s = await GivenCity();
        Prison prison = await GivenPrison(s);
        Mod.config.MAX_CELLS_PER_PRISON = 2;

        await s.Mayor.ExecuteCommand("/city prison addcell");
        Assert.Equal(2, prison.getPrisonCells().Count);

        await s.Mayor.ExecuteCommand("/city prison addcell");

        Assert.Equal(2, prison.getPrisonCells().Count);
    }

    /// <summary>
    /// A prisoner with a city of their own and a summon point there: <paramref name="theirs"/>'s
    /// mayor, imprisoned in <paramref name="ours"/>, standing on the prison plot (1, 0).
    /// </summary>
    private async Task<string> GivenPrisonerWithASummonPoint(CityScene ours, CityScene theirs)
    {
        await GivenPrison(ours);
        CityLevelInfo level = Settings.getCityLevelInfo(theirs.City.getCityCitizens().Count);
        int previous = level.SummonPlots;
        AddCleanup(() => level.SummonPlots = previous);
        level.SummonPlots = theirs.City.summonPlots.Count + 1;
        await GivenPlotType(theirs.Mayor, "summon", PlotType.SUMMON);
        string name = Assert.IsType<PlotDescSummon>(PlotAt(theirs.Mayor).PlotDesc).Name;

        await GoToPlot(theirs.Mayor, ours.Area, 1, 0);
        Kill(ours.Mayor, theirs.Mayor);
        Assert.True(Info(theirs.Mayor).isPrisoned(), "precondition: imprisoned");
        Assert.Contains("summon", Settings.blockedCommandsForPrison);
        AddCleanup(() => TeleportationHandler.removeTeleportation(Info(theirs.Mayor)));
        return name;
    }

    // SU8, R36, R38
    [AtlasScenario]
    public async Task Prisoner_Should_NotSummonAway_FromThePrisonPlot()
    {
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        string name = await GivenPrisonerWithASummonPoint(ours, theirs);

        await theirs.Mayor.ExecuteCommand($"/city summon use {name}");

        Assert.False(TeleportationHandler.hasTeleportation(Info(theirs.Mayor)), "a prisoner summoned himself out of the prison plot");
    }

    // PZ9, R36, R38
    [AtlasScenario]
    public async Task Prisoner_Should_NotSummonAway_FromTheNextPlotOfTheCity()
    {
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        string name = await GivenPrisonerWithASummonPoint(ours, theirs);
        await GoToPlot(theirs.Mayor, ours.Area); // the city's founding plot, next to the prison
        await World.Ticks(40);
        Assert.True(Info(theirs.Mayor).isPrisoned(), "precondition: stepping onto the city's own plot is not an escape");

        await theirs.Mayor.ExecuteCommand($"/city summon use {name}");

        Assert.False(TeleportationHandler.hasTeleportation(Info(theirs.Mayor)), "a prisoner one plot from the prison summoned himself home");
    }

    // The chat path goes through the Harmony prefix on ChatCommandApi.Execute (ExecuteCommand skips it).
    // A blocked command must not reach its handler at all: the handler's own refusal must not show up.
    [AtlasScenario]
    public async Task PrisonFilter_Should_StopABlockedCommand_BeforeItsHandler()
    {
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        string name = await GivenPrisonerWithASummonPoint(ours, theirs);
        theirs.Mayor.Client.Clear();

        await theirs.Mayor.Say($"/city summon use {name}");
        await World.Ticks(5);

        Assert.False(TeleportationHandler.hasTeleportation(Info(theirs.Mayor)), "a prisoner summoned himself through chat");
        string handlerReply = Vintagestory.API.Config.Lang.Get("claims:you_are_prisoned");
        Assert.DoesNotContain(theirs.Mayor.Client.ChatLines(), line => line.Contains(handlerReply));
    }
}
