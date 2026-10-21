using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Raising the fee of a plots group.
/// </summary>
[Trait("Category", "E2E")]
public class PlotsGroupFeeScenarios : ClaimsScenarioBase
{
    // G2
    [AtlasScenario]
    public async Task Raise_Should_KeepWhoAgreedAndDropTheRest_When_WaitIsOver()
    {
        // two members (seeded - joining a group is its own flow), a raise to 10
        CityScene s = await GivenCity("agrees", "refuses");
        ITestPlayer agrees = s.Citizens[0], refuses = s.Citizens[1];
        Mod.config.PLOTSGROUP_FEE_RAISE_DELAY_HOURS = 24;
        await s.Mayor.ExecuteCommand("/city plotsgroup create grp");
        CityPlotsGroup group = Assert.Single(s.City.getCityPlotsGroups());
        group.PlayersList.Add(Info(agrees));
        group.PlayersList.Add(Info(refuses));
        await s.Mayor.ExecuteCommand("/city plotsgroup set fee grp 10");
        Assert.True(group.HasPendingFee, "precondition: the raise is only announced");
        await agrees.ExecuteCommand("/city plotsgroup acceptfee grp");
        group.PendingFeeAt = TimeFunctions.getEpochSeconds() - 1; // the 24 hours are over

        await RunHour(s.Mayor);

        Assert.Equal(10, group.PlotsGroupFee);
        Assert.Contains(Info(agrees), group.PlayersList);
        Assert.DoesNotContain(Info(refuses), group.PlayersList);
    }
}
