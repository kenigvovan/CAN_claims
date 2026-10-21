using Atlas.Api;
using Atlas.XUnit;
using claims.src;
using claims.src.auxialiry;
using claims.src.database;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.rights;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Mod = global::claims.src.claims;

namespace claims.scenarios.Infrastructure;

/// <summary>
/// Shared ground for scenarios of the mod.
///
/// One server per class, and the mod's state is not rolled back between scenarios. So every
/// scenario takes its own area of the map (<see cref="NewArea"/>), joins its own players, and
/// <see cref="AssertInvariants"/> looks only at the players and cities this scenario created -
/// a scenario that leaves the world broken on purpose must not fail the next one.
/// </summary>
public abstract class ClaimsScenarioBase : AtlasScenarioBase, IDisposable
{
    // 20 plots of 16 blocks: well past MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY.
    private const int AreaSpacingBlocks = 320;
    // /agree runs the action in a callback on a later tick.
    protected const int AgreeTimeoutTicks = 20;

    private static int lastArea = -1;

    // Players of finished scenarios, per class. The server holds 16 clients, so they are
    // disconnected before the next join; Dispose runs off the game thread and cannot do it.
    // Keyed by the server API, not by World: Atlas makes a new session per scenario.
    private static readonly Dictionary<Type, (ICoreServerAPI Server, List<ITestPlayer> Players)> leftOnline = new();

    private readonly List<ITestPlayer> players = new();
    private readonly List<City> cities = new();
    private readonly List<Action> cleanups = new();

    /// <summary>Config every scenario starts from. Override to add, call base first.</summary>
    protected virtual void ResetConfig()
    {
        var c = Mod.config;
        c.NEW_CITY_COST = 0;
        c.CLAIM_LIMITERS_ENABLED = false;
        c.VILLAGE_ENABLED = false;
        c.PLOT_UNCLAIM_REFUND_PERCENT = 0;
        c.DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE = true;
        c.DELETE_CITY_IF_DOESN_PAY_FEE = false;
        c.ADDITIONAL_COST_OF_NO_PVP_PLOT = false;
        c.AGREEMENT_TIMEOUT_SECONDS = 120;
        c.PLOTSGROUP_FEE_RAISE_DELAY_HOURS = 24;
        c.WAR_DESTRUCTION_SCOPE = Config.WAR_DESTRUCTION.BORDER_PLOTS;
        c.MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY = 3;
    }

    protected static int NewArea() => Interlocked.Increment(ref lastArea);

    protected static string CityName(int area) =>
        "Town" + new string(area.ToString().Select(d => (char)('a' + (d - '0'))).ToArray());

    /// <summary>
    /// The middle of a plot, counted in plots from the area's own plot: (0, 0) is where
    /// <see cref="JoinAt"/> puts players, (1, 0) its neighbour to the east. The middle, not
    /// the corner, so a player never stands on a border.
    /// </summary>
    protected BlockPos PlotCenter(int area, int dx = 0, int dz = 0)
    {
        int size = Mod.config.PLOT_SIZE;
        BlockPos origin = World.Spawn.Offset(area * AreaSpacingBlocks, 0, 0);
        int plotX = origin.X / size + dx;
        int plotZ = origin.Z / size + dz;
        return new BlockPos(plotX * size + size / 2, origin.Y, plotZ * size + size / 2);
    }

    protected Task GoToPlot(ITestPlayer player, int area, int dx = 0, int dz = 0) =>
        player.TeleportTo(PlotCenter(area, dx, dz));

    // ---- economy ---------------------------------------------------------------------------

    /// <summary>Replaces the economy for this scenario; the previous one comes back on Dispose.</summary>
    protected FakeMoneyProvider UseFakeEconomy()
    {
        IMoneyProvider previous = Mod.economyProvider;
        var fake = new FakeMoneyProvider();
        Mod.economyProvider = fake;
        cleanups.Add(() => Mod.economyProvider = previous);
        return fake;
    }

    // ---- players and cities ----------------------------------------------------------------

    protected async Task<ITestPlayer> JoinAt(string name, int area)
    {
        await DisconnectEarlierPlayers();
        // A new player is spawned at a random spot within spawnRadius. When that spot's chunk is
        // not loaded the engine places them only after loading it, and the test client's join
        // request can arrive first: HandleRequestJoin throws a NullReferenceException and the
        // join never completes (seen as "Until predicate still false" in JoinPlayerCore). Players
        // are teleported to their plot right after, so the random spot is of no use here.
        World.Api.World.Config.SetString("spawnRadius", "0");
        ITestPlayer player = await World.JoinPlayer(name);
        await GoToPlot(player, area);
        players.Add(player);
        return player;
    }

    private async Task DisconnectEarlierPlayers()
    {
        List<ITestPlayer> stale;
        lock (leftOnline)
        {
            if (!leftOnline.Remove(GetType(), out var left)) return;
            // A restarted host has a new API; its old players are gone already.
            if (!ReferenceEquals(left.Server, World.Api)) return;
            stale = left.Players;
        }

        foreach (ITestPlayer p in stale)
        {
            if (p.IsConnected) p.Player.Disconnect();
        }
        await World.Until(() => stale.All(p => !p.IsConnected), "players of earlier scenarios to leave", 100);
    }

    protected static PlayerInfo Info(ITestPlayer player)
    {
        Assert.True(Mod.dataStorage.GetPlayerByUid(player.Player.PlayerUID, out PlayerInfo info),
            "no PlayerInfo for " + player.Player.PlayerName);
        return info;
    }

    protected async Task<City> FoundCity(ITestPlayer mayor, string name)
    {
        CommandResult result = await mayor.ExecuteCommand($"/city create {name}");
        Assert.True(result.Ok, $"/city create {name}: {result.Message}");
        await Agree(mayor);
        await World.Until(() => Info(mayor).hasCity(), $"{name} to be founded after /agree", AgreeTimeoutTicks);
        City city = Info(mayor).City;
        cities.Add(city);
        return city;
    }

    protected async Task AddCitizen(City city, ITestPlayer mayor, ITestPlayer player)
    {
        await mayor.ExecuteCommand("/city set open on");
        Assert.True(city.openCity, $"{city.GetPartName()} must be open for /city join");
        await player.ExecuteCommand($"/city join {city.GetPartName()}");
        Assert.True(city.Equals(Info(player).City), $"{player.Player.PlayerName} must have joined {city.GetPartName()}");
    }

    /// <summary>
    /// What most scenarios start from. Everyone stands on an ordinary plot of the city, (1, 0),
    /// not on the founding plot: that one is still special in villages and in older cities.
    /// </summary>
    protected sealed record CityScene(int Area, ITestPlayer Mayor, City City, IReadOnlyList<ITestPlayer> Citizens);

    /// <summary>
    /// Resets the config, takes a new area, founds a city there, adds one citizen per name (the
    /// area number is appended, player names must be unique per class) and claims plot (1, 0).
    /// The city gets 8 bonus plots, so the plot limit never refuses a claim the scenario is about.
    /// Runs on the economy in place: call <see cref="UseFakeEconomy"/> after it, the fake's
    /// empty treasury could not pay for the claim.
    /// </summary>
    protected async Task<CityScene> GivenCity(params string[] citizenNames)
    {
        ResetConfig();
        int area = NewArea();
        ITestPlayer mayor = await JoinAt("mayor" + area, area);
        City city = await FoundCity(mayor, CityName(area));
        // A level 1 city may hold 2 plots, both taken below; room for the scenario's own claims.
        city.setBonusPlots(8);
        var citizens = new List<ITestPlayer>();
        foreach (string name in citizenNames)
        {
            ITestPlayer citizen = await JoinAt(name + area, area);
            await AddCitizen(city, mayor, citizen);
            citizens.Add(citizen);
        }

        await GoToPlot(mayor, area, 1, 0);
        CommandResult claim = await mayor.ExecuteCommand("/city claim");
        Assert.True(PlotAt(mayor).Type == PlotType.DEFAULT && city.Equals(PlotAt(mayor).getCity()),
            "precondition: the city claimed an ordinary plot next to its main one: " + claim.Message);
        foreach (ITestPlayer citizen in citizens) await GoToPlot(citizen, area, 1, 0);

        return new CityScene(area, mayor, city, citizens);
    }

    /// <summary>Resets the config and founds a village on plot (0, 0) of a new area; its head stands there.</summary>
    protected async Task<(ITestPlayer Head, City Village, int Area)> GivenVillage()
    {
        ResetConfig();
        Mod.config.VILLAGE_ENABLED = true;
        Mod.config.VILLAGE_CREATE_COST = 0;
        int area = NewArea();
        ITestPlayer head = await JoinAt("head" + area, area);
        await head.ExecuteCommand($"/village create {CityName(area)}");
        await Agree(head);
        await World.Until(() => Info(head).hasCity(), "the village to be founded", AgreeTimeoutTicks);
        City village = Info(head).City;
        Assert.True(village.IsVillage(), "precondition: a village was founded");
        cities.Add(village);
        return (head, village, area);
    }

    /// <summary>/plot claim and, if it made an offer, /agree (C8). Returns the claim's result.</summary>
    protected async Task<CommandResult> ClaimAndAgree(ITestPlayer buyer)
    {
        CommandResult offer = await buyer.ExecuteCommand("/plot claim");
        if (offer.Ok)
        {
            await buyer.ExecuteCommand("/agree");
            await World.Ticks(AgreeTimeoutTicks);
        }
        return offer;
    }

    /// <summary>The mayor puts the plot up for <paramref name="price"/> and the player buys it.</summary>
    protected async Task GivenOwnedPlot(CityScene scene, ITestPlayer buyer, int price = 0)
    {
        await PutForSale(scene.Mayor, price);
        await ClaimAndAgree(buyer);
        Assert.True(Owns(buyer), $"precondition: {buyer.Player.PlayerName} owns the plot");
    }

    /// <summary>
    /// <c>/plot set type</c> on the plot the player stands on, through the /agree a paid type
    /// asks for.
    /// </summary>
    protected async Task GivenPlotType(ITestPlayer by, string typeCode, PlotType expected)
    {
        await by.ExecuteCommand($"/plot set type {typeCode}");
        if (PlotAt(by).Type != expected)
        {
            await Agree(by);
            await World.Until(() => PlotAt(by).Type == expected, $"the plot to become {expected}", AgreeTimeoutTicks);
        }
    }

    /// <summary>Personal unclaim refund on, without the waiting period.</summary>
    protected static void EnableRefund(int percent)
    {
        Mod.config.PLOT_UNCLAIM_REFUND_PERCENT = percent;
        Mod.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS = 0;
    }

    protected static string Account(ITestPlayer player) => Info(player).MoneyAccountName;

    protected static async Task Agree(ITestPlayer player)
    {
        CommandResult agree = await player.ExecuteCommand("/agree");
        Assert.True(agree.Ok, "/agree: " + agree.Message);
    }

    // ---- plots -----------------------------------------------------------------------------

    protected static Plot PlotAt(ITestPlayer player)
    {
        BlockPos pos = player.Position;
        Assert.True(Mod.dataStorage.GetPlot(PlotPosition.fromXZ(pos.X, pos.Z), out Plot plot),
            $"no plot where {player.Player.PlayerName} stands");
        return plot;
    }

    /// <summary>Whether the player owns the plot they stand on.</summary>
    protected static bool Owns(ITestPlayer player) => Info(player).Equals(PlotAt(player).getPlotOwner());

    /// <summary>
    /// <c>/plot fs</c> on the plot the player stands on. Checked by the price, not by the result:
    /// fs reports its refusals as successes (R24).
    /// </summary>
    protected static async Task PutForSale(ITestPlayer by, int price)
    {
        CommandResult result = await by.ExecuteCommand($"/plot fs {price}");
        Assert.True(PlotAt(by).Price == price, $"/plot fs {price} did not set the price: {result.Message}");
    }

    // ---- rights ----------------------------------------------------------------------------

    /// <summary>
    /// Takes a right away from a permission group (as an admin would in claims_permissions.json)
    /// and reapplies the rights of <paramref name="affected"/>. Disposing puts it back.
    /// </summary>
    protected IDisposable WithoutGroupPermission(string group, EnumPlayerPermissions permission, params ITestPlayer[] affected)
    {
        HashSet<EnumPlayerPermissions> groupPerms = RightsHandler.GetPermsByGroup(group);
        Assert.True(groupPerms != null, $"no permission group {group}");
        bool removed = groupPerms!.Remove(permission);
        Reapply();

        var scope = new Scope(() =>
        {
            if (removed) groupPerms.Add(permission);
            Reapply();
        });
        cleanups.Add(scope.Dispose);
        return scope;

        void Reapply()
        {
            foreach (ITestPlayer p in affected) RightsHandler.reapplyRights(Info(p));
        }
    }

    /// <summary>A forbidden area covering one plot - what an admin's claim limiter does around spawn.</summary>
    private sealed class ForbidPlot(PlotPosition forbidden) : claims.src.auxialiry.ClaimLimiter.ClaimLimiter
    {
        public override bool CanClaimHere(PlayerInfo playerInfo, PlotPosition plotPosition) =>
            !plotPosition.Equals(forbidden);
    }

    /// <summary>Makes the plot the player stands on a forbidden area until Dispose.</summary>
    protected void ForbidClaimingHere(ITestPlayer player)
    {
        BlockPos pos = player.Position;
        var limiter = new ForbidPlot(PlotPosition.fromXZ(pos.X, pos.Z));
        Mod.dataStorage.ClaimLimiters.Add(limiter);
        Mod.config.CLAIM_LIMITERS_ENABLED = true;
        cleanups.Add(() => Mod.dataStorage.ClaimLimiters.Remove(limiter));
    }

    /// <summary>Whether the plot the player stands on belongs to the city.</summary>
    protected static bool ClaimedBy(City city, ITestPlayer player)
    {
        BlockPos pos = player.Position;
        return Mod.dataStorage.GetPlot(PlotPosition.fromXZ(pos.X, pos.Z), out Plot plot) && city.Equals(plot.getCity());
    }

    /// <summary>Whether anybody claimed the plot the player stands on.</summary>
    protected static bool ClaimedByAnyone(ITestPlayer player)
    {
        BlockPos pos = player.Position;
        return Mod.dataStorage.GetPlot(PlotPosition.fromXZ(pos.X, pos.Z), out _);
    }

    /// <summary>Runs on Dispose, after the scenario - for test state the base class does not know about.</summary>
    protected void AddCleanup(Action cleanup) => cleanups.Add(cleanup);

    /// <summary>Lets the player run /cadmin: adds their role to ROLE_CODES_WITH_ADMIN_RIGHTS until Dispose.</summary>
    protected void AllowAdminCommands(ITestPlayer player)
    {
        string role = player.Player.Role.Code;
        if (Mod.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Contains(role)) return;
        Mod.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Add(role);
        cleanups.Add(() => Mod.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Remove(role));
    }

    /// <summary>
    /// /cadmin nhour: runs the hourly timer on the next tick. Note O2: every call
    /// also starts another hourly chain, which nobody waits an hour in a test to see.
    /// </summary>
    protected async Task RunHour(ITestPlayer admin)
    {
        AllowAdminCommands(admin);
        await admin.ExecuteCommand("/cadmin nhour");
        await World.Ticks(5);
    }

    /// <summary>Daily upkeep of a DEFAULT plot, as /cadmin setcfg plot_cost_default does; restored on Dispose.</summary>
    protected void SetDefaultPlotCost(double cost)
    {
        double previous = Mod.config.DEFAULT_PLOT_COST;
        Mod.config.DEFAULT_PLOT_COST = cost;
        PlotInfo.initDicts();
        cleanups.Add(() =>
        {
            Mod.config.DEFAULT_PLOT_COST = previous;
            PlotInfo.initDicts();
        });
    }

    /// <summary>/cadmin nday: runs the daily cycle for every city, on the next tick.</summary>
    protected async Task RunDay(ITestPlayer admin)
    {
        AllowAdminCommands(admin);
        await admin.ExecuteCommand("/cadmin nday");
        await World.Ticks(5);
    }

    protected static SQLiteDatabaseHanlder Database() =>
        Assert.IsType<SQLiteDatabaseHanlder>(Mod.getModInstance().getDatabaseHandler());

    // ---- invariants ------------------------------------------------------------------------

    /// <summary>
    /// Invariants 1-4 and 6, for the cities and players of
    /// this scenario. Reports every broken one at once.
    /// </summary>
    protected void AssertInvariants()
    {
        var broken = new List<string>();
        var plots = new HashSet<Plot>();

        foreach (City city in cities.Where(c => Mod.dataStorage.getCitiesList().Contains(c)))
        {
            string name = city.GetPartName();
            foreach (Plot plot in city.getCityPlots())
            {
                plots.Add(plot);
                if (!city.Equals(plot.getCity()))
                    broken.Add($"inv1: {name} lists plot {plot.getPos()}, which points to {plot.getCity()?.GetPartName() ?? "no city"}");
            }

            PlayerInfo mayor = city.getMayor();
            if (mayor == null || !city.getPlayerInfos().Contains(mayor))
                broken.Add($"inv3: mayor of {name} ({mayor?.GetPartName() ?? "none"}) is not its citizen");
            foreach (PlayerInfo citizen in city.getPlayerInfos())
            {
                if (!city.Equals(citizen.City))
                    broken.Add($"inv3: {name} lists {citizen.GetPartName()}, whose city is {citizen.City?.GetPartName() ?? "none"}");
            }
        }

        foreach (ITestPlayer p in players)
        {
            PlayerInfo info = Info(p);
            foreach (Plot plot in info.PlayerPlots)
            {
                plots.Add(plot);
                if (!info.Equals(plot.getPlotOwner()))
                    broken.Add($"inv2: plot {plot.getPos()} is in PlayerPlots of {info.GetPartName()}, but its owner is {plot.getPlotOwner()?.GetPartName() ?? "nobody"}");
            }
        }

        foreach (Plot plot in plots)
        {
            City? city = plot.getCity();
            if (city != null && !city.getCityPlots().Contains(plot))
                broken.Add($"inv1: plot {plot.getPos()} points to {city.GetPartName()}, which does not list it");

            PlayerInfo? owner = plot.getPlotOwner();
            if (owner != null)
            {
                if (!owner.PlayerPlots.Contains(plot))
                    broken.Add($"inv2: owner {owner.GetPartName()} of plot {plot.getPos()} does not have it in PlayerPlots");
                if (plot.Type != PlotType.EMBASSY && (city == null || !city.Equals(owner.City)))
                    broken.Add($"inv4: owner {owner.GetPartName()} of plot {plot.getPos()} is not a citizen of {city?.GetPartName() ?? "no city"}");
            }

            if (plot.Price < -1)
                broken.Add($"inv6: plot {plot.getPos()} has price {plot.Price}");
        }

        Assert.True(broken.Count == 0, "invariants broken:\n" + string.Join("\n", broken));
    }

    // ---------------------------------------------------------------------------------------

    public void Dispose()
    {
        // Last in, first out: a later change may build on an earlier one.
        for (int i = cleanups.Count - 1; i >= 0; i--) cleanups[i]();
        cleanups.Clear();

        if (players.Count > 0)
        {
            lock (leftOnline)
            {
                if (leftOnline.TryGetValue(GetType(), out var left) && ReferenceEquals(left.Server, World.Api))
                    left.Players.AddRange(players);
                else
                    leftOnline[GetType()] = (World.Api, new List<ITestPlayer>(players));
            }
        }
        GC.SuppressFinalize(this);
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        private Action? onDispose = onDispose;

        public void Dispose()
        {
            onDispose?.Invoke();
            onDispose = null;
        }
    }
}
