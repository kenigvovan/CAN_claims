using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.part;
using claims.src.part.structure.plots;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Hourly upkeep of villages (VillageSupplyHelper).
/// </summary>
[Trait("Category", "E2E")]
public class VillageSupplyScenarios : ClaimsScenarioBase
{
    // V1: boundary of VILLAGE_DECAY_HOURS - one empty hour short of the limit survives, the limit starves
    [AtlasScenario]
    public async Task Village_Should_StarveExactlyAtTheDecayLimit()
    {
        // an empty granary, decay two hours short of the limit
        ResetConfig();
        Mod.config.VILLAGE_ENABLED = true;
        Mod.config.VILLAGE_CREATE_COST = 0;
        int area = NewArea();
        ITestPlayer head = await JoinAt("head" + area, area);
        await head.ExecuteCommand($"/village create {CityName(area)}");
        await Agree(head);
        await World.Until(() => Info(head).hasCity(), "the village to be founded", AgreeTimeoutTicks);
        City village = Info(head).City;
        Assert.True(village.TryGetVillageMain(out _, out PlotDescVillage desc), "precondition: the village has its main plot");
        int limit = Mod.config.VILLAGE_DECAY_HOURS;
        desc.SupplyHours = 0;
        desc.DecayHours = limit - 2;

        // limit - 1 -> still standing
        await RunHour(head);
        Assert.Equal(limit - 1, desc.DecayHours);
        Assert.Contains(village, Mod.dataStorage.getCitiesList());

        // the limit -> gone
        await RunHour(head);
        Assert.DoesNotContain(village, Mod.dataStorage.getCitiesList());
    }
}
