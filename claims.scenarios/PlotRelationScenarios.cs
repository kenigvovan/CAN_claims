using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src;
using claims.src.events;
using Vintagestory.API.Common;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Which role a player has on a plot when several fit at once.
/// Cases RL1-RL6.
/// </summary>
[Trait("Category", "E2E")]
public class PlotRelationScenarios : ClaimsScenarioBase
{
    /// <summary>
    /// Whether the player may break a block on plot (dx, 0). Touches the city's main plot first,
    /// so the answer is computed now and not read from the per-plot cache (O1, O2).
    /// </summary>
    private async Task<bool> CanDestroy(ITestPlayer player, int area, int dx)
    {
        OnBlockAction.canBlockDestroy(player.Player, new BlockSelection { Position = PlotCenter(area) }, out _);
        await GoToPlot(player, area, dx, 0);
        return OnBlockAction.canBlockDestroy(player.Player, new BlockSelection { Position = PlotCenter(area, dx, 0) }, out _);
    }

    /// <summary>/citizen friend add, checked by the list: the command reports refusals as successes (R24).</summary>
    private static async Task AddFriend(ITestPlayer who, ITestPlayer friend)
    {
        await who.ExecuteCommand($"/citizen friend add {friend.Player.PlayerName}");
        Assert.True(Info(who).Friends.Contains(Info(friend)), $"precondition: {friend.Player.PlayerName} is a friend of {who.Player.PlayerName}");
    }

    // RL4a, R34, A23
    [AtlasScenario]
    public async Task Citizen_Should_KeepCitizenRights_When_AlsoTheOwnersFriend()
    {
        CityScene s = await GivenCity("owner", "neighbour");
        ITestPlayer owner = s.Citizens[0];
        ITestPlayer neighbour = s.Citizens[1];
        await GivenOwnedPlot(s, owner);
        await owner.ExecuteCommand("/plot set p citizen build on");
        await owner.ExecuteCommand("/plot set p friend build off");
        Assert.True(await CanDestroy(neighbour, s.Area, 1), "precondition: citizens may build here");

        await AddFriend(owner, neighbour);

        Assert.True(await CanDestroy(neighbour, s.Area, 1), "becoming the owner's friend took the citizen's rights away");
    }

    // RL4a the other way round: the friend's rights still add to the citizen's
    [AtlasScenario]
    public async Task Citizen_Should_GetFriendRights_When_AlsoTheOwnersFriend()
    {
        CityScene s = await GivenCity("owner", "neighbour");
        ITestPlayer owner = s.Citizens[0];
        ITestPlayer neighbour = s.Citizens[1];
        await GivenOwnedPlot(s, owner);
        await owner.ExecuteCommand("/plot set p citizen build off");
        await owner.ExecuteCommand("/plot set p friend build on");
        Assert.False(await CanDestroy(neighbour, s.Area, 1), "precondition: citizens may not build here");

        await AddFriend(owner, neighbour);

        Assert.True(await CanDestroy(neighbour, s.Area, 1), "the owner's friend could not build with the friend's rights");
    }

    // RL4a for strangers: friendship never takes away what everyone may do
    [AtlasScenario]
    public async Task Stranger_Should_KeepStrangerRights_When_AlsoTheOwnersFriend()
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        await owner.ExecuteCommand("/plot set p stranger build on");
        await owner.ExecuteCommand("/plot set p friend build off");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        Assert.True(await CanDestroy(stranger, s.Area, 1), "precondition: strangers may build here");

        await AddFriend(owner, stranger);

        Assert.True(await CanDestroy(stranger, s.Area, 1), "becoming the owner's friend took the stranger's rights away");
    }

    // RL3a, R34
    [AtlasScenario]
    public async Task Enemy_Should_FollowWarRules_When_AlsoTheOwnersFriend()
    {
        CityScene ours = await GivenCity("owner");
        ITestPlayer owner = ours.Citizens[0];
        await GivenOwnedPlot(ours, owner);
        await owner.ExecuteCommand("/plot set p friend build on");
        CityScene theirs = await GivenCity();
        await AddFriend(owner, theirs.Mayor);
        Assert.True(await CanDestroy(theirs.Mayor, ours.Area, 1), "precondition: the friend may build in peace");
        Mod.config.WAR_DESTRUCTION_SCOPE = Config.WAR_DESTRUCTION.FLAG_PLOTS; // no flag planted: war allows nothing here

        AllowAdminCommands(ours.Mayor);
        await ours.Mayor.ExecuteCommand($"/cadmin startwar {ours.City.GetPartName()} {theirs.City.GetPartName()}");

        Assert.False(await CanDestroy(theirs.Mayor, ours.Area, 1), "an enemy built under the friend's peacetime rights");
    }

    // RL5, R34
    [AtlasScenario]
    public async Task Friendship_Should_NotCount_On_APlotWithoutOwner()
    {
        CityScene s = await GivenCity();
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);
        await s.Mayor.ExecuteCommand("/plot set p stranger build off");
        await s.Mayor.ExecuteCommand("/plot set p friend build on");

        await AddFriend(s.Mayor, stranger);

        Assert.False(await CanDestroy(stranger, s.Area, 1), "the mayor's friend built on a city plot nobody owns");
    }

    // RL6, R34
    [AtlasScenario]
    public async Task Friendship_Should_CountOnlyFromTheOwnersSide()
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        await owner.ExecuteCommand("/plot set p stranger build off");
        await owner.ExecuteCommand("/plot set p friend build on");
        ITestPlayer stranger = await JoinAt("stranger" + s.Area, s.Area);

        await AddFriend(stranger, owner);

        Assert.False(await CanDestroy(stranger, s.Area, 1), "listing the owner as a friend gave the stranger the friend's rights");
    }
}
