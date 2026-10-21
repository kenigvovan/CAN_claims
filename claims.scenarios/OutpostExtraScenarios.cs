using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;

namespace claims.scenarios;

/// <summary>
/// Outposts and extra plots against the rules an ordinary claim follows.
/// </summary>
[Trait("Category", "E2E")]
public class OutpostExtraScenarios : ClaimsScenarioBase
{
    // Control: the outpost honours forbidden areas - the rule the extra plot is compared with
    [AtlasScenario]
    public async Task Outpost_Should_BeRefused_In_ForbiddenArea()
    {
        CityScene s = await GivenCity();
        await GoToPlot(s.Mayor, s.Area, 5, 0);
        ForbidClaimingHere(s.Mayor);

        await s.Mayor.ExecuteCommand("/city outpost");

        Assert.False(ClaimedByAnyone(s.Mayor), "an outpost was claimed in a forbidden area");
    }

    // Without this a refusal for "no extra plots left" would pass the tests below for the wrong reason.
    private static void AssertCityMayBuyExtraPlots(CityScene s) =>
        Assert.True(Settings.getMaxNumberOfExtraChunksBought(s.City) > s.City.Extrachunksbought,
            "precondition: the city may still buy an extra plot");

    // EX-O1
    [AtlasScenario]
    public async Task ExtraPlot_Should_BeRefused_In_ForbiddenArea()
    {
        CityScene s = await GivenCity();
        AssertCityMayBuyExtraPlots(s);
        await GoToPlot(s.Mayor, s.Area, 2, 0); // next to the city, so only the forbidden area is in the way
        ForbidClaimingHere(s.Mayor);

        await s.Mayor.ExecuteCommand("/city extraplot");

        Assert.False(ClaimedByAnyone(s.Mayor), "an extra plot was claimed in a forbidden area");
    }

    // EX-O2, A21: an extra plot must touch the city, like an ordinary claim
    [AtlasScenario]
    public async Task ExtraPlot_Should_BeRefused_When_NotNextToTheCity()
    {
        CityScene s = await GivenCity();
        AssertCityMayBuyExtraPlots(s);
        await GoToPlot(s.Mayor, s.Area, 6, 0);

        await s.Mayor.ExecuteCommand("/city extraplot");

        Assert.False(ClaimedByAnyone(s.Mayor), "an extra plot was claimed away from the city");
    }
}
