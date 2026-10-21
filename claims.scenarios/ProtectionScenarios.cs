using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src;
using claims.src.events;
using claims.src.part.structure.conflict;
using Mod = global::claims.src.claims;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.scenarios;

/// <summary>
/// Block protection and the per-plot permission cache.
/// </summary>
[Trait("Category", "E2E")]
public class ProtectionScenarios : ClaimsScenarioBase
{
    // O1, PR1
    [AtlasScenario]
    public async Task Build_Should_BeDenied_When_PlayerWasJustRemovedFromFriends()
    {
        // friends may build on the owner's plot; the friend breaks a block there
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        ITestPlayer friend = await JoinAt("friend" + s.Area, s.Area);
        await owner.ExecuteCommand("/plot set p friend build on");
        await owner.ExecuteCommand($"/citizen friend add {friend.Player.PlayerName}");
        var block = new BlockSelection { Position = PlotCenter(s.Area, 1, 0) };
        Assert.True(OnBlockAction.canBlockDestroy(friend.Player, block, out _), "precondition: the friend may build");

        await owner.ExecuteCommand($"/citizen friend remove {friend.Player.PlayerName}");

        Assert.False(OnBlockAction.canBlockDestroy(friend.Player, block, out _),
            "a removed friend can still build: the old answer is cached");
    }

    /// <summary>
    /// The check the game runs for <paramref name="perm"/> (use / build / attack) on that block.
    /// Each has its own copy of the cache code in OnBlockAction, so each is tested.
    /// </summary>
    private static bool May(ITestPlayer player, string perm, BlockPos pos) => perm switch
    {
        "build" => OnBlockAction.canBlockDestroy(player.Player, new BlockSelection { Position = pos }, out _),
        "use" => OnBlockAction.canBlockUse(player.Player, new BlockSelection { Position = pos }),
        // creative mode skips the check altogether
        "attack" => player.Player.WorldData.CurrentGameMode != EnumGameMode.Creative
            ? OnBlockAction.canAttackAnimals(player.Player, pos.ToVec3d().Add(0.5, 0, 0.5))
            : throw new InvalidOperationException("precondition: the player is not in creative mode"),
        _ => throw new ArgumentOutOfRangeException(nameof(perm), perm, null),
    };

    /// <summary>A plot owned by a citizen where only friends have <paramref name="perm"/>, and a player who is no friend yet.</summary>
    private async Task<(ITestPlayer Owner, ITestPlayer Friend, BlockPos Pos)> GivenFriendsOnlyPlot(string perm)
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenOwnedPlot(s, owner);
        ITestPlayer friend = await JoinAt("friend" + s.Area, s.Area);
        // Test players join in creative, which skips the attack check altogether
        friend.Player.WorldData.CurrentGameMode = EnumGameMode.Survival;
        friend.Player.BroadcastPlayerData();
        await owner.ExecuteCommand($"/plot set p stranger {perm} off");
        await owner.ExecuteCommand($"/plot set p friend {perm} on");
        return (owner, friend, PlotCenter(s.Area, 1, 0));
    }

    // O1c, PR1
    [AtlasTheory]
    [InlineData("build")]
    [InlineData("use")]
    [InlineData("attack")]
    public async Task Rights_Should_BeGranted_When_PlayerWasJustAddedToFriends(string perm)
    {
        // the player tries before being a friend, so "no" is cached
        (ITestPlayer owner, ITestPlayer friend, BlockPos pos) = await GivenFriendsOnlyPlot(perm);
        Assert.False(May(friend, perm, pos), $"precondition: a stranger has no {perm}");

        await owner.ExecuteCommand($"/citizen friend add {friend.Player.PlayerName}");

        Assert.True(May(friend, perm, pos), $"a new friend still has no {perm}: the old answer is cached");
    }

    // O1d, PR1: add, remove, add again on the same plot - each step must hold at once
    [AtlasTheory]
    [InlineData("build")]
    [InlineData("use")]
    [InlineData("attack")]
    public async Task FriendRights_Should_FollowEveryChange_When_AddedRemovedAndAddedAgain(string perm)
    {
        (ITestPlayer owner, ITestPlayer friend, BlockPos pos) = await GivenFriendsOnlyPlot(perm);
        string name = friend.Player.PlayerName;
        var steps = new List<string>();

        await owner.ExecuteCommand($"/citizen friend add {name}");
        if (!May(friend, perm, pos)) steps.Add($"after add: no {perm}");
        await owner.ExecuteCommand($"/citizen friend remove {name}");
        if (May(friend, perm, pos)) steps.Add($"after remove: still {perm}");
        await owner.ExecuteCommand($"/citizen friend add {name}");
        if (!May(friend, perm, pos)) steps.Add($"after adding again: no {perm}");

        Assert.True(Info(owner).Friends.Contains(Info(friend)), "the player is not a friend after being added again");
        Assert.True(steps.Count == 0, "rights lag behind the friends list:\n" + string.Join("\n", steps));
    }

    // O2, PR1
    [AtlasScenario]
    public async Task Destroy_Should_BeDenied_When_WarJustEnded()
    {
        // a battle on, every plot in scope; the enemy mayor breaks a block on our plot
        CityScene ours = await GivenCity();
        CityScene theirs = await GivenCity();
        Mod.config.WAR_DESTRUCTION_SCOPE = Config.WAR_DESTRUCTION.ALL_PLOTS;
        AllowAdminCommands(ours.Mayor);
        string warArgs = $"{ours.City.GetPartName()} {theirs.City.GetPartName()}";
        await ours.Mayor.ExecuteCommand("/cadmin startwar " + warArgs);
        // startwar makes the cities hostile at once, but the battle opens in a callback 2 s later
        await World.Until(() => ConflictHandler.TryGetConflictWithSides(ours.City, theirs.City, out var c) && c.ActiveWarTime,
            "the forced battle has started");
        await GoToPlot(theirs.Mayor, ours.Area, 1, 0);
        var block = new BlockSelection { Position = PlotCenter(ours.Area, 1, 0) };
        Assert.True(OnBlockAction.canBlockDestroy(theirs.Mayor.Player, block, out _), "precondition: the enemy may destroy during the battle");

        await ours.Mayor.ExecuteCommand("/cadmin endwar " + warArgs);

        Assert.False(OnBlockAction.canBlockDestroy(theirs.Mayor.Player, block, out _),
            "the enemy can still destroy after the war: the old answer is cached");
    }
}
