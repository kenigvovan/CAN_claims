using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.part.structure;
using claims.src.rights;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// City ranks and what they may grant.
/// </summary>
[Trait("Category", "E2E")]
public class RanksScenarios : ClaimsScenarioBase
{
    // RR-O1, RR1: the mayor of a member city must not get the alliance's money through a city rank
    [AtlasScenario]
    public async Task CityRank_Should_NotGrantAlliancePermissions()
    {
        // city A leads an alliance with 1000 in it, city B is a member (seeded)
        CityScene leader = await GivenCity();
        CityScene member = await GivenCity();
        await leader.Mayor.ExecuteCommand($"/alliance create Ally{CityName(leader.Area)}");
        await Agree(leader.Mayor);
        await World.Until(() => leader.City.HasAlliance(), "the alliance to be founded", AgreeTimeoutTicks);
        Alliance alliance = leader.City.Alliance;
        alliance.Cities.Add(member.City);
        member.City.Alliance = alliance;
        Assert.False(Info(member.Mayor).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.ALLIANCE_WITHDRAW_MONEY),
            "precondition: the member's mayor cannot withdraw from the alliance");

        // a city rank with an alliance permission, given to oneself
        string self = member.Mayor.Player.PlayerName;
        await member.Mayor.ExecuteCommand("/city rank create treasurer");
        await member.Mayor.ExecuteCommand("/city rank addperm treasurer ALLIANCE_WITHDRAW_MONEY");
        await member.Mayor.ExecuteCommand($"/city rank add treasurer {self}");

        Assert.False(Info(member.Mayor).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.ALLIANCE_WITHDRAW_MONEY),
            "a city rank gave the member's mayor the right to empty the alliance treasury");
    }

    // RR-O2, RR1: whoever may edit ranks still cannot hand out more than they have
    [AtlasScenario]
    public async Task RankEditor_Should_NotGrantPermissionsTheyDoNotHave()
    {
        // the mayor makes a deputy who may edit ranks, but not take money
        CityScene s = await GivenCity("deputy");
        ITestPlayer deputy = s.Citizens[0];
        string name = deputy.Player.PlayerName;
        await s.Mayor.ExecuteCommand("/city rank create deputy");
        await s.Mayor.ExecuteCommand("/city rank addperm deputy CITY_ADD_PERMISSION_TO_RANK");
        await s.Mayor.ExecuteCommand($"/city rank add deputy {name}");
        Assert.True(Info(deputy).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK),
            "precondition: the deputy may edit ranks");
        Assert.False(Info(deputy).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "precondition: the deputy cannot take money");

        // the deputy adds the money right to their own rank
        await deputy.ExecuteCommand("/city rank addperm deputy CITY_WITHDRAW_MONEY");

        Assert.False(Info(deputy).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "the deputy gave themselves the right to empty the treasury");
    }

    // RR1 (decision A, 2026-10-15): handing out a ready-made rank follows the same rule as editing one
    [AtlasScenario]
    public async Task RankAssigner_Should_NotGiveARankWithRightsTheyDoNotHave()
    {
        // the mayor sets up a treasurer's rank and a deputy who may only hand out ranks
        CityScene s = await GivenCity("deputy");
        ITestPlayer deputy = s.Citizens[0];
        string name = deputy.Player.PlayerName;
        await s.Mayor.ExecuteCommand("/city rank create treasurer");
        await s.Mayor.ExecuteCommand("/city rank addperm treasurer CITY_WITHDRAW_MONEY");
        await s.Mayor.ExecuteCommand("/city rank create hr");
        await s.Mayor.ExecuteCommand("/city rank addperm hr CITY_SET_RANK");
        await s.Mayor.ExecuteCommand($"/city rank add hr {name}");
        Assert.True(Info(deputy).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_SET_RANK),
            "precondition: the deputy may hand out ranks");
        Assert.False(Info(deputy).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "precondition: the deputy cannot take money");

        // the deputy gives themselves the treasurer's rank
        CommandResult result = await deputy.ExecuteCommand($"/city rank add treasurer {name}");

        Assert.False(result.Ok, "the refusal must be reported as an error: " + result.Message);
        Assert.False(Info(deputy).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "the deputy took the treasurer's rank and its money right");
    }

    // The same rule must not stop the mayor, who holds every city right
    [AtlasScenario]
    public async Task Mayor_Should_GiveARankWithCityRights()
    {
        CityScene s = await GivenCity("treasurer");
        ITestPlayer treasurer = s.Citizens[0];
        await s.Mayor.ExecuteCommand("/city rank create treasurer");
        await s.Mayor.ExecuteCommand("/city rank addperm treasurer CITY_WITHDRAW_MONEY");

        await s.Mayor.ExecuteCommand($"/city rank add treasurer {treasurer.Player.PlayerName}");

        Assert.True(Info(treasurer).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "the mayor could not hand out a rank with a city right");
    }

    // A deleted rank takes its rights with it, and its name does not come back to life
    [AtlasScenario]
    public async Task DeletedRank_Should_TakeItsRightsAway()
    {
        CityScene s = await GivenCity("treasurer");
        ITestPlayer treasurer = s.Citizens[0];
        await s.Mayor.ExecuteCommand("/city rank create treasurer");
        await s.Mayor.ExecuteCommand("/city rank addperm treasurer CITY_WITHDRAW_MONEY");
        await s.Mayor.ExecuteCommand($"/city rank add treasurer {treasurer.Player.PlayerName}");
        Assert.True(Info(treasurer).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "precondition: the treasurer may take money");

        await s.Mayor.ExecuteCommand("/city rank delete treasurer");

        Assert.False(Info(treasurer).PlayerPermissionsHandler.HasPermission(EnumPlayerPermissions.CITY_WITHDRAW_MONEY),
            "the rank is gone, its right stayed");
        Assert.DoesNotContain("treasurer", Info(treasurer).getCityTitles());
    }

    // RR-O3: a number is not a permission
    [AtlasScenario]
    public async Task AddPerm_Should_RejectNumbersThatAreNoPermission()
    {
        CityScene s = await GivenCity();
        await s.Mayor.ExecuteCommand("/city rank create odd");

        await s.Mayor.ExecuteCommand("/city rank addperm odd 99999");

        Assert.All(s.City.CustomCityRanks["odd"].Permissions,
            p => Assert.True(Enum.IsDefined(p), "the rank holds a value that is no permission: " + (int)p));
    }
}
