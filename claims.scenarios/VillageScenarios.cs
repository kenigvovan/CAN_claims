using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.events;
using claims.src.part;
using claims.src;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Raid window, cooldowns after a fall and the upgrade to a city.
/// Cases VR, VC, VU.
/// </summary>
[Trait("Category", "E2E")]
public class VillageScenarios : ClaimsScenarioBase
{
    private const int GraceDays = 7;
    private const int Duration = 3600;

    protected override void ResetConfig()
    {
        base.ResetConfig();
        Mod.config.VILLAGE_RAIDABLE = true;
        Mod.config.VILLAGE_RAID_GRACE_DAYS = GraceDays;
        Mod.config.VILLAGE_RAID_DURATION_SECONDS = Duration;
        Mod.config.VILLAGE_ANCHOR_BREAKS = 50;
        Mod.config.VILLAGE_UPGRADE_MIN_CITIZENS = 1;
    }

    private static long DayLength => Mod.config.MOD_DAY_DURATION_IN_SECONDS > 0 ? Mod.config.MOD_DAY_DURATION_IN_SECONDS : 86400;

    /// <summary>
    /// Backdates the founding so the village is <paramref name="ageSeconds"/> old. Set and read in
    /// the same tick, so the clock does not move between them.
    /// </summary>
    private static void SetAge(City village, long ageSeconds) =>
        village.TimeStampCreated = TimeFunctions.getEpochSeconds() - ageSeconds;

    /// <summary>Past the grace period and <paramref name="into"/> seconds into the day's window.</summary>
    private static void SetIntoWindow(City village, long into) =>
        SetAge(village, GraceDays * DayLength + into);

    /// <summary>A block of the main plot away from the anchor (which stands where the head founded).</summary>
    private BlockPos SomeBlock(int area) => PlotCenter(area).AddCopy(-5, 0, -5);

    /// <summary>What the window callback does when it opens or closes: drops cached rights.</summary>
    private static void DropCachedRights(ITestPlayer player) => Info(player).PlayerCache.Reset();

    // VR1, R13
    [AtlasScenario]
    public async Task Window_Should_OpenExactlyWhenTheGracePeriodEnds()
    {
        (_, City village, _) = await GivenVillage();

        SetAge(village, GraceDays * DayLength - 1);
        bool beforeGrace = VillageRaidHelper.IsRaidWindowOpen(village);
        SetAge(village, GraceDays * DayLength);
        bool atGrace = VillageRaidHelper.IsRaidWindowOpen(village);

        Assert.False(beforeGrace, "open a second before the grace period ends");
        Assert.True(atGrace, "closed when the grace period ends");
    }

    // VR2, R13
    [AtlasScenario]
    public async Task Window_Should_CloseExactlyAfterItsDuration()
    {
        (_, City village, _) = await GivenVillage();

        SetIntoWindow(village, Duration - 1);
        bool lastSecond = VillageRaidHelper.IsRaidWindowOpen(village);
        SetIntoWindow(village, Duration);
        bool afterEnd = VillageRaidHelper.IsRaidWindowOpen(village);

        Assert.True(lastSecond, "closed in the last second of the window");
        Assert.False(afterEnd, "still open after the window's duration");
    }

    // VR3, R13
    [AtlasScenario]
    public async Task Outsider_Should_BuildOnlyInsideTheWindow()
    {
        (_, City village, int area) = await GivenVillage();
        ITestPlayer raider = await JoinAt("raider" + area, area);
        var block = new BlockSelection { Position = SomeBlock(area) };

        SetIntoWindow(village, Duration); // closed
        DropCachedRights(raider);
        bool outside = OnBlockAction.canBlockDestroy(raider.Player, block, out _);
        SetIntoWindow(village, 10);
        DropCachedRights(raider);
        bool inside = OnBlockAction.canBlockDestroy(raider.Player, block, out _);

        Assert.False(outside, "an outsider built on the village outside the raid window");
        Assert.True(inside, "an outsider could not build inside the raid window");
    }

    // VR4, R13
    [AtlasScenario]
    public async Task Citizen_Should_KeepTheirUsualRights_InsideTheWindow()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        ITestPlayer citizen = await JoinAt("citizen" + area, area);
        await AddCitizen(village, head, citizen);
        var block = new BlockSelection { Position = SomeBlock(area) };

        SetIntoWindow(village, Duration); // closed
        DropCachedRights(citizen);
        bool outside = OnBlockAction.canBlockDestroy(citizen.Player, block, out _);
        SetIntoWindow(village, 10);
        DropCachedRights(citizen);
        bool inside = OnBlockAction.canBlockDestroy(citizen.Player, block, out _);

        Assert.Equal(outside, inside);
    }

    // VR6, R13
    [AtlasScenario]
    public async Task Window_Should_NeverOpen_When_RaidsAreOff()
    {
        (_, City village, _) = await GivenVillage();
        Mod.config.VILLAGE_RAIDABLE = false;

        SetIntoWindow(village, 10);

        Assert.False(VillageRaidHelper.IsRaidWindowOpen(village));
    }

    // VR5, R13
    [AtlasScenario]
    public async Task Village_Should_FallOnTheLastAnchorHit()
    {
        (_, City village, int area) = await GivenVillage();
        Mod.config.VILLAGE_ANCHOR_BREAKS = 3;
        Assert.True(village.TryGetVillageMain(out _, out PlotDescVillage desc) && desc.AnchorPos != null, "precondition: the village has an anchor");
        var anchor = new BlockSelection { Position = new BlockPos(desc.AnchorPos.X, desc.AnchorPos.Y, desc.AnchorPos.Z) };
        ITestPlayer raider = await JoinAt("raider" + area, area);
        SetIntoWindow(village, 10);

        OnBlockAction.canBlockDestroy(raider.Player, anchor, out _);
        OnBlockAction.canBlockDestroy(raider.Player, anchor, out _);
        Assert.Contains(village, Mod.dataStorage.getCitiesList());

        bool breaks = OnBlockAction.canBlockDestroy(raider.Player, anchor, out _);

        Assert.True(breaks, "the last hit did not break the anchor");
        Assert.DoesNotContain(village, Mod.dataStorage.getCitiesList());
    }

    // VR7, R13
    [AtlasScenario]
    public async Task Abandon_Should_BeRefused_InsideTheWindow()
    {
        (ITestPlayer head, City village, _) = await GivenVillage();
        SetIntoWindow(village, 10);

        await head.ExecuteCommand("/village abandon");
        await head.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Contains(village, Mod.dataStorage.getCitiesList());
    }

    // VR8, R13, A25
    [AtlasScenario]
    public async Task Upgrade_Should_BeRefused_InsideTheWindow()
    {
        (ITestPlayer head, City village, _) = await GivenVillage();
        SetIntoWindow(village, 10);

        await head.ExecuteCommand("/village upgrade");

        Assert.True(village.IsVillage(), "the village escaped the raid by becoming a city");
    }

    // VU1, R13
    [AtlasScenario]
    public async Task Upgrade_Should_NeedTheMinimumOfCitizens()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        Mod.config.VILLAGE_UPGRADE_MIN_CITIZENS = 2;

        await head.ExecuteCommand("/village upgrade");
        Assert.True(village.IsVillage(), "upgraded with one citizen short");

        ITestPlayer citizen = await JoinAt("citizen" + area, area);
        await AddCitizen(village, head, citizen);
        await head.ExecuteCommand("/village upgrade");
        Assert.False(village.IsVillage(), "not upgraded with exactly the minimum");
    }

    // VU2, R13
    [AtlasScenario]
    public async Task Upgrade_Should_FollowTheHeadsMoney_AtThePriceBoundary()
    {
        (ITestPlayer head, City village, _) = await GivenVillage();
        Mod.config.VILLAGE_UPGRADE_COST = 150;
        FakeMoneyProvider money = UseFakeEconomy();

        money.SetBalance(Account(head), 149.99m);
        await head.ExecuteCommand("/village upgrade");
        Assert.True(village.IsVillage(), "upgraded a coin short of the price");

        money.SetBalance(Account(head), 150m);
        await head.ExecuteCommand("/village upgrade");
        Assert.False(village.IsVillage(), "not upgraded with exactly the price");
        Assert.Equal(0m, money.GetBalance(Account(head)));
    }

    // VU3, R13
    [AtlasScenario]
    public async Task Upgrade_Should_LeaveNothingOfTheVillageBehind()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();

        await head.ExecuteCommand("/village upgrade");

        Assert.False(village.IsVillage(), "precondition: upgraded");
        await GoToPlot(head, area);
        Assert.Equal(PlotType.DEFAULT, PlotAt(head).Type);
        Assert.IsNotType<PlotDescVillage>(PlotAt(head).PlotDesc);
        var missing = RightsHandler.GetPermsByGroup("MAYOR").Where(p => !Info(head).PlayerPermissionsHandler.HasPermission(p)).ToList();
        Assert.True(missing.Count == 0, "the new mayor lacks: " + string.Join(", ", missing));
    }

    // VU4, R13, R27
    [AtlasScenario]
    public async Task Upgrade_Should_ChangeNothing_When_TheWithdrawalFails()
    {
        (ITestPlayer head, City village, _) = await GivenVillage();
        Mod.config.VILLAGE_UPGRADE_COST = 150;
        FakeMoneyProvider money = UseFakeEconomy();
        money.SetBalance(Account(head), 500m);
        money.FailNext(FakeMoneyOp.Withdraw, Account(head));

        await head.ExecuteCommand("/village upgrade");

        Assert.True(village.IsVillage(), "upgraded without the money taken");
        Assert.Equal(500m, money.GetBalance(Account(head)));
    }

    // VU5, R10 H2
    [AtlasScenario]
    public async Task Unclaim_Should_RefundNothing_For_APlotTheVillageGotForFree()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        await GoToPlot(head, area, 1, 0);
        await head.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(village, head), "precondition: the village claimed a plot");
        await head.ExecuteCommand("/village upgrade");
        Assert.False(village.IsVillage(), "precondition: the village became a city");
        FakeMoneyProvider money = UseFakeEconomy();
        EnableRefund(50);

        await head.ExecuteCommand("/city unclaim");

        Assert.False(ClaimedByAnyone(head), "precondition: the plot was unclaimed");
        Assert.Equal(0m, money.GetBalance(village.MoneyAccountName));
    }

    // VC1, R13
    [AtlasScenario]
    public async Task Founding_Should_WaitOutTheCooldown_AfterAVillageFell()
    {
        ResetConfig();
        int area = NewArea();
        ITestPlayer survivor = await JoinAt("survivor" + area, area);
        Vec2i here = PlotPosition.fromXZ(survivor.Position.X, survivor.Position.Z).getPos();
        long cooldown = Mod.config.VILLAGE_REFOUND_COOLDOWN_HOURS * 3600L;
        Info(survivor).VillageAbandoned = false;
        AddCleanup(() => Info(survivor).VillageLostAt = 0);

        Info(survivor).VillageLostAt = TimeFunctions.getEpochSeconds() - cooldown + 1;
        bool lastSecond = VillageCooldownHelper.CanFoundHere(Info(survivor), here, out _, out _);
        Info(survivor).VillageLostAt = TimeFunctions.getEpochSeconds() - cooldown;
        bool atEnd = VillageCooldownHelper.CanFoundHere(Info(survivor), here, out _, out _);

        Assert.False(lastSecond, "founded a second before the cooldown ended");
        Assert.True(atEnd, "still blocked when the cooldown ended");
    }

    // VC2, R13
    [AtlasScenario]
    public async Task Abandon_Should_PutOnlyTheHeadOnTheShorterCooldown()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        ITestPlayer citizen = await JoinAt("citizen" + area, area);
        await AddCitizen(village, head, citizen);
        AddCleanup(() => Info(head).VillageLostAt = 0);

        await head.ExecuteCommand("/village abandon");
        await Agree(head);
        await World.Until(() => !Mod.dataStorage.getCitiesList().Contains(village), "the village to be abandoned", AgreeTimeoutTicks);

        PlayerInfo h = Info(head);
        Assert.Equal(h.VillageLostAt + Mod.config.VILLAGE_ABANDON_COOLDOWN_HOURS * 3600L, VillageCooldownHelper.RefoundUntil(h));
        Assert.Equal(0L, VillageCooldownHelper.RefoundUntil(Info(citizen)));
    }

    // VC3, R13
    [AtlasScenario]
    public async Task Abandon_Should_NotShortenAPenaltyTheHeadAlreadyHas()
    {
        (ITestPlayer head, _, _) = await GivenVillage();
        PlayerInfo h = Info(head);
        h.VillageLostAt = TimeFunctions.getEpochSeconds(); // an earlier village fell just now
        h.VillageAbandoned = false;
        AddCleanup(() => h.VillageLostAt = 0);
        long fallUntil = VillageCooldownHelper.RefoundUntil(h);

        await head.ExecuteCommand("/village abandon");
        await Agree(head);
        await World.Until(() => !h.hasCity(), "the village to be abandoned", AgreeTimeoutTicks);

        Assert.True(VillageCooldownHelper.RefoundUntil(h) >= fallUntil, "abandoning shortened the penalty for the fall");
    }

    // VC5, R13
    [AtlasScenario]
    public async Task Player_Should_JoinACity_DuringTheCooldown()
    {
        CityScene s = await GivenCity();
        ITestPlayer survivor = await JoinAt("survivor" + s.Area, s.Area);
        Info(survivor).VillageLostAt = TimeFunctions.getEpochSeconds();
        AddCleanup(() => Info(survivor).VillageLostAt = 0);

        await AddCitizen(s.City, s.Mayor, survivor); // asserts the join
    }

    // VC4, R13
    [AtlasScenario]
    public async Task Ruins_Should_BlockFoundingUpToTheirRadius()
    {
        ResetConfig();
        int area = NewArea();
        ITestPlayer founder = await JoinAt("founder" + area, area);
        Vec2i ruin = PlotPosition.fromXZ(founder.Position.X, founder.Position.Z).getPos();
        Mod.dataStorage.VillageRuins[ruin] = new VillageRuin(TimeFunctions.getEpochSeconds(), false);
        AddCleanup(() => Mod.dataStorage.VillageRuins.Remove(ruin));
        int radius = Mod.config.VILLAGE_RUIN_RADIUS_PLOTS;

        bool atRadius = VillageCooldownHelper.CanFoundHere(Info(founder), new Vec2i(ruin.X + radius, ruin.Y), out _, out _);
        bool pastRadius = VillageCooldownHelper.CanFoundHere(Info(founder), new Vec2i(ruin.X + radius + 1, ruin.Y), out _, out _);

        Assert.False(atRadius, "founded right at the edge of the ruins");
        Assert.True(pastRadius, "blocked one plot past the ruins' radius");
    }
}
