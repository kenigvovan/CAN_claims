using Atlas.XUnit;
using claims.src.part;
using claims.src.part.structure;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// R03: a full world survives loading and a real server restart. The data comes from the
/// fixture FullWorldFixtureGenerator writes; no player joins here, which is what lets Atlas
/// restart the class host. Both scenarios only read, so their order does not matter (ADR 0010).
/// The expectations are written out (FullWorld), not "same as before": a value that is saved and
/// loaded back equally wrong would pass a before/after comparison.
/// </summary>
[Trait("Category", "E2E")]
[AtlasDataFiles("fixtures/FullWorld", TargetPath = "ModConfig")]
public class PersistenceScenarios : AtlasScenarioBase
{
    // R03, R04: the stored world is loaded completely at boot
    [AtlasScenario]
    public async Task FullWorld_Should_BeLoaded_When_ServerBoots()
    {
        AssertFullWorld();
        await World.Ticks(1);
    }

    // R03, invariant 7: and is still the same after the server was shut down and booted again
    [AtlasScenario(RestartWorld = true)]
    public async Task FullWorld_Should_StayTheSame_When_ServerRestarts()
    {
        AssertFullWorld();
        await World.Ticks(1);
    }

    private static void AssertFullWorld()
    {
        var broken = new List<string>();

        if (!Mod.dataStorage.GetCityByName(FullWorld.City, out City city))
        {
            Assert.Fail("city " + FullWorld.City + " was not loaded");
        }
        if (city.getMayor()?.GetPartName() != FullWorld.Mayor) broken.Add("mayor is " + city.getMayor()?.GetPartName());
        if (city.fee != FullWorld.CityFee) broken.Add("city fee is " + city.fee);
        if (Math.Abs(city.DebtBalance - FullWorld.Debt) > 0.001) broken.Add("debt is " + city.DebtBalance);
        // CC12: the founding plot and the one /city claim took
        if (city.getCityPlots().Count != FullWorld.CityPlots) broken.Add("city has " + city.getCityPlots().Count + " plots");
        foreach (Plot plot in city.getCityPlots().Where(p => !city.Equals(p.getCity())))
            broken.Add("plot " + plot.getPos() + " does not point to the city");

        if (!Mod.dataStorage.getPlayerByName(FullWorld.Citizen, out PlayerInfo citizen))
        {
            Assert.Fail("player " + FullWorld.Citizen + " was not loaded");
        }
        if (!city.Equals(citizen.City)) broken.Add("citizen's city is " + citizen.City?.GetPartName());
        // The hourly timer may tick once between boot and this check.
        if (citizen.PrisonHoursLeft < FullWorld.PrisonHours - 1 || citizen.PrisonHoursLeft > FullWorld.PrisonHours)
            broken.Add("prison hours are " + citizen.PrisonHoursLeft);

        Plot? bought = citizen.PlayerPlots.SingleOrDefault();
        if (bought == null) broken.Add("citizen owns " + citizen.PlayerPlots.Count + " plots, expected 1");
        else
        {
            if (!citizen.Equals(bought.getPlotOwner())) broken.Add("the bought plot's owner is " + bought.getPlotOwner()?.GetPartName());
            if (bought.lastPaidPrice != FullWorld.PlotPrice) broken.Add("lastPaidPrice is " + bought.lastPaidPrice);
            if (bought.getCustomTax() != FullWorld.PlotTax) broken.Add("plot tax is " + bought.getCustomTax());
            if (bought.Price != -1) broken.Add("the bought plot is for sale at " + bought.Price);
        }

        var group = city.getCityPlotsGroups().SingleOrDefault(g => g.GetPartName() == FullWorld.Group);
        if (group == null) broken.Add("plots group " + FullWorld.Group + " is missing");
        else if (!group.PlayersList.Contains(citizen)) broken.Add("the citizen is not in the plots group");

        if (!Mod.dataStorage.GetAllianceByName(FullWorld.Alliance, out Alliance alliance)) broken.Add("alliance is missing");
        else if (!alliance.Cities.Contains(city)) broken.Add("the city is not in the alliance");

        Assert.True(broken.Count == 0, "the world was not loaded as stored:\n" + string.Join("\n", broken));
    }
}
