using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Changing a plot's type: each type switched on from DEFAULT and back off, with what it
/// registers in the city. Cases PT1-PT7.
/// </summary>
[Trait("Category", "E2E")]
public class PlotTypeScenarios : ClaimsScenarioBase
{
    protected override void ResetConfig()
    {
        base.ResetConfig();
        Mod.config.DISABLED_PLOT_TYPES = new HashSet<string>();
        Mod.config.TEMPLE_RESPAWN_ENABLED = true;
        Mod.config.MAX_NUMBER_TAVERN_PER_CITY = 3;
    }

    /// <summary>Lets the city hold one more summon plot than it has; restored on Dispose.</summary>
    private void AllowOneMoreSummonPlot(City city)
    {
        CityLevelInfo level = Settings.getCityLevelInfo(city.getCityCitizens().Count);
        int previous = level.SummonPlots;
        AddCleanup(() => level.SummonPlots = previous);
        level.SummonPlots = city.summonPlots.Count + 1;
    }

    /// <summary>What the type left in the city: the lists the type's description adds itself to.</summary>
    private static List<string> Registrations(City city, Plot plot)
    {
        var found = new List<string>();
        if (city.summonPlots.Contains(plot)) found.Add("summon point");
        if (plot.Prison != null) found.Add("plot.Prison");
        if (city.getPrisons().Any(p => p.Plot == plot)) found.Add("city prison");
        return found;
    }

    private static readonly Dictionary<string, (PlotType Type, Type Desc, string[] Registers)> Types = new()
    {
        ["farm"] = (PlotType.FARM, typeof(PlotDesc), Array.Empty<string>()),
        ["orchard"] = (PlotType.ORCHARD, typeof(PlotDesc), Array.Empty<string>()),
        ["summon"] = (PlotType.SUMMON, typeof(PlotDescSummon), new[] { "summon point" }),
        ["embassy"] = (PlotType.EMBASSY, typeof(PlotDescEmbassy), Array.Empty<string>()),
        ["tavern"] = (PlotType.TAVERN, typeof(PlotDescTavern), Array.Empty<string>()),
        ["prison"] = (PlotType.PRISON, typeof(PlotDescPrison), new[] { "plot.Prison", "city prison" }),
        ["temple"] = (PlotType.TEMPLE, typeof(PlotDescTemple), Array.Empty<string>()),
    };

    // PT1, PT2
    [AtlasTheory]
    [InlineData("farm")]
    [InlineData("orchard")]
    [InlineData("summon")]
    [InlineData("embassy")]
    [InlineData("tavern")]
    [InlineData("prison")]
    [InlineData("temple")]
    public async Task Type_Should_RegisterAndUnregister(string code)
    {
        var (type, desc, registers) = Types[code];
        CityScene s = await GivenCity();
        AllowOneMoreSummonPlot(s.City);
        Plot plot = PlotAt(s.Mayor);
        Assert.Equal(PlotType.DEFAULT, plot.Type);

        await GivenPlotType(s.Mayor, code, type);

        Assert.IsType(desc, plot.PlotDesc);
        Assert.Equal(registers, Registrations(s.City, plot));

        await GivenPlotType(s.Mayor, "default", PlotType.DEFAULT);

        Assert.Empty(Registrations(s.City, plot));
        AssertInvariants();
    }

    // PT3
    [AtlasTheory]
    [InlineData("camp")]
    [InlineData("tournament")]
    [InlineData("VILLAGE_MAIN")]
    [InlineData("MAIN_CITY_PLOT")]
    [InlineData("nosuchtype")]
    public async Task Type_Should_BeRefused_When_NotSettableByPlayers(string code)
    {
        CityScene s = await GivenCity();

        await s.Mayor.ExecuteCommand($"/plot set type {code}");
        await s.Mayor.ExecuteCommand("/agree"); // in case a price made it ask

        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);
    }

    // PT4
    [AtlasTheory]
    [InlineData("farm", "listed")]
    [InlineData("temple", "respawn off")]
    public async Task Type_Should_BeRefused_When_TheHostSwitchedItOff(string code, string how)
    {
        CityScene s = await GivenCity();
        if (how == "listed") Mod.config.DISABLED_PLOT_TYPES.Add(code);
        else Mod.config.TEMPLE_RESPAWN_ENABLED = false;

        await s.Mayor.ExecuteCommand($"/plot set type {code}");
        await s.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);
    }

    // PT6
    [AtlasScenario]
    public async Task Village_Should_SetOnlyTheTypesAllowedToVillages()
    {
        (ITestPlayer head, City village, int area) = await GivenVillage();
        await GoToPlot(head, area, 1, 0);
        await head.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(village, head), "precondition: the village claimed a plot");
        Assert.DoesNotContain("tavern", Mod.config.VILLAGE_ALLOWED_PLOT_TYPES);

        await head.ExecuteCommand("/plot set type tavern");
        await head.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);
        Assert.Equal(PlotType.DEFAULT, PlotAt(head).Type);

        await GivenPlotType(head, "farm", PlotType.FARM); // an allowed type still goes through
    }

    // PT5
    [AtlasScenario]
    public async Task Type_Should_StayAsItWas_When_OverTheLimit()
    {
        CityScene s = await GivenCity();
        Mod.config.MAX_NUMBER_TAVERN_PER_CITY = 1;
        await GivenPlotType(s.Mayor, "tavern", PlotType.TAVERN);
        await GoToPlot(s.Mayor, s.Area, 2, 0);
        await s.Mayor.ExecuteCommand("/city claim");
        Assert.True(ClaimedBy(s.City, s.Mayor), "precondition: a second plot");

        await s.Mayor.ExecuteCommand("/plot set type tavern");
        await s.Mayor.ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);

        Assert.Equal(PlotType.DEFAULT, PlotAt(s.Mayor).Type);
    }

    // PT7, P1
    [AtlasScenario]
    public async Task EmbassyOfAForeigner_Should_KeepInvariants_When_RetypedByTheMayor()
    {
        CityScene s = await GivenCity("owner");
        ITestPlayer owner = s.Citizens[0];
        await GivenPlotType(s.Mayor, "embassy", PlotType.EMBASSY);
        await GivenOwnedPlot(s, owner);
        await owner.ExecuteCommand("/city leave");
        Assert.True(!Info(owner).hasCity() && Owns(owner), "precondition: a foreigner owns the embassy (P1)");

        await s.Mayor.ExecuteCommand("/plot set type default");

        AssertInvariants();
    }
}
