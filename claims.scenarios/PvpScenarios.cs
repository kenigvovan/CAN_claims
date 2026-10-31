using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.events;
using claims.src.clientMapHandling;
using claims.src.network.packets;
using claims.src.part.structure;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
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

    // FL4: "pvp everywhere" opens a peaceful city plot to anyone, no war needed
    [AtlasScenario]
    public async Task Pvp_Should_BeAllowed_InCity_When_WorldPvpEverywhereIsOn()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        Assert.False(OnPVP.canPVPAttackHere(stranger.Player, s.Citizens[0].Player),
            "precondition: the plot is peaceful while the world flag is off");

        var world = Mod.dataStorage.getWorldInfo();
        AllowAdminCommands(s.Mayor);
        try
        {
            CommandResult set = await s.Mayor.ExecuteCommand("/cadmin world set pvpew on");
            Assert.True(set.Ok, "/cadmin world set pvpew on: " + set.Message);
            Assert.True(world.pvpEverywhere, "the command did not turn the world flag on: " + set.Message);

            Assert.True(OnPVP.canPVPAttackHere(stranger.Player, s.Citizens[0].Player),
                "pvp everywhere did not override the city's peaceful plot");
        }
        finally
        {
            world.pvpEverywhere = false;
        }
        await World.Ticks(1);
    }

    // FL5: what the banner shows a stranger is what the server lets them do, under every world override
    [AtlasScenario]
    public async Task PvpShown_Should_MatchTheServer_UnderEveryWorldOverride()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        ITestPlayer victim = s.Citizens[0];
        var world = Mod.dataStorage.getWorldInfo();
        try
        {
            foreach (bool plotPvp in new[] { false, true })
            {
                PlotAt(victim).getPermsHandler().pvpFlag = plotPvp;
                foreach (var (everywhere, forbidden) in new[] { (false, false), (true, false), (false, true) })
                {
                    world.pvpEverywhere = everywhere;
                    world.pvpForbidden = forbidden;

                    var info = PlotStateHandling.BuildSavedPlotInfo(PlotAt(victim), stranger.Player, Info(stranger));
                    bool shown = AdminWorldFlags.EffectivePvp(AdminWorldFlags.From(world), info.PvPIsOn);
                    bool server = OnPVP.canPVPAttackHere(stranger.Player, victim.Player);

                    Assert.True(shown == server,
                        $"plot pvp {plotPvp}, everywhere {everywhere}, forbidden {forbidden}: shown {shown}, server {server}");
                }
            }
        }
        finally
        {
            world.pvpEverywhere = false;
            world.pvpForbidden = false;
        }
        await World.Ticks(1);
    }

    // FL6: the city's pvp flag opens its plots, and the plot sent to the client says so
    [AtlasScenario]
    public async Task PlotSentToClient_Should_ShowPvp_When_OnlyTheCityFlagIsOn()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        ITestPlayer victim = s.Citizens[0];

        CommandResult set = await s.Mayor.ExecuteCommand("/city set pvp on");
        Assert.True(s.City.getPermsHandler().pvpFlag, "precondition: /city set pvp on: " + set.Message);
        Assert.False(PlotAt(victim).getPermsHandler().pvpFlag, "precondition: the plot's own flag stays off");

        var info = PlotStateHandling.BuildSavedPlotInfo(PlotAt(victim), stranger.Player, Info(stranger));

        Assert.True(OnPVP.canPVPAttackHere(stranger.Player, victim.Player), "precondition: the city flag lets the stranger attack");
        Assert.True(info.PvPIsOn, "the client would show a peaceful plot in a city with pvp on");
    }

    // ---- what reaches the client -----------------------------------------------------------

    private const int PacketTimeoutTicks = 100;

    private static IEnumerable<SavedPlotsPacket> Received(ITestPlayer player, PacketsContentEnum type) =>
        player.Client.Packets<SavedPlotsPacket>("claimsExt").Where(p => p.type == type);

    private static AdminWorldFlags? LastWorldFlags(ITestPlayer player)
    {
        SavedPlotsPacket? last = Received(player, PacketsContentEnum.WORLD_FLAGS).LastOrDefault();
        return last == null ? null : JsonConvert.DeserializeObject<AdminWorldFlags>(last.data);
    }

    /// <summary>The player's copy of the plot as the last batched update carried it; null when none did.</summary>
    private static SavedPlotInfo? LastUpdateOf(ITestPlayer player, Vec2i plotPos)
    {
        SavedPlotInfo? found = null;
        foreach (SavedPlotsPacket p in Received(player, PacketsContentEnum.SERVER_UPDATE_COLLECTED_PLOTS))
        {
            var plots = JsonConvert.DeserializeObject<List<Tuple<Vec2i, SavedPlotInfo>>>(p.data) ?? new();
            foreach (var (pos, info) in plots)
                if (pos.X == plotPos.X && pos.Y == plotPos.Y) found = info;
        }
        return found;
    }

    /// <summary>What the client mod does when it sees a zone: asks for its plots and their updates.</summary>
    private static void SubscribeToZoneOf(ITestPlayer player, Plot plot)
    {
        int len = Mod.config.ZONE_PLOTS_LENGTH;
        var zone = new Vec2i(plot.getPos().X / len, plot.getPos().Y / len);
        Mod.serverPlayerMovementListener.SyncSubscriptionsAndSendSnapshot(player.Player, Info(player), new[] { zone });
    }

    // FL7: the overrides reach a player as soon as they join
    [AtlasScenario]
    public async Task WorldFlags_Should_BeSent_OnJoin()
    {
        int area = NewArea();
        var world = Mod.dataStorage.getWorldInfo();
        world.pvpEverywhere = true;
        try
        {
            ITestPlayer player = await JoinAt("joiner" + area, area);
            await World.Until(() => LastWorldFlags(player) != null, "WORLD_FLAGS after the join", PacketTimeoutTicks);

            Assert.True(LastWorldFlags(player)!.PvpEverywhere, "the joining player was sent the flags without pvp everywhere");
        }
        finally
        {
            world.pvpEverywhere = false;
        }
    }

    // FL8: /cadmin world set reaches everyone online, not only the admin who ran it
    [AtlasScenario]
    public async Task WorldFlags_Should_BeBroadcast_When_AdminChangesThem()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        AllowAdminCommands(s.Mayor);
        var world = Mod.dataStorage.getWorldInfo();
        try
        {
            stranger.Client.Clear();
            await s.Mayor.ExecuteCommand("/cadmin world set pvpew on");
            await World.Until(() => LastWorldFlags(stranger)?.PvpEverywhere == true,
                "WORLD_FLAGS with pvp everywhere to the other player", PacketTimeoutTicks);

            stranger.Client.Clear();
            await s.Mayor.ExecuteCommand("/cadmin world set pvpew off");
            await World.Until(() => LastWorldFlags(stranger) is { PvpEverywhere: false },
                "WORLD_FLAGS without pvp everywhere after switching it off", PacketTimeoutTicks);
        }
        finally
        {
            world.pvpEverywhere = false;
        }
    }

    // FL9: the city's pvp switch resends its plots to whoever watches them
    [AtlasScenario]
    public async Task CityPvpFlag_Should_ResendItsPlots_ToSubscribers()
    {
        CityScene s = await GivenCity("victim");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        Plot plot = PlotAt(s.Citizens[0]);
        SubscribeToZoneOf(stranger, plot);
        stranger.Client.Clear();

        await s.Mayor.ExecuteCommand("/city set pvp on");
        await World.Until(() => LastUpdateOf(stranger, plot.getPos())?.PvPIsOn == true,
            "the city's plot resent with pvp on", PacketTimeoutTicks);

        stranger.Client.Clear();
        await s.Mayor.ExecuteCommand("/city set pvp off");
        await World.Until(() => LastUpdateOf(stranger, plot.getPos()) is { PvPIsOn: false },
            "the city's plot resent with pvp off", PacketTimeoutTicks);
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
