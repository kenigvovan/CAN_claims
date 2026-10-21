using System.Data;
using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// The database after real commands: what is stored must be what the server holds in memory.
/// Founding a city saves the city, its plots and its people several times within one 0.5s drain,
/// which is where the write queue loses saves. The queue rules themselves: claims.tests,
/// DatabaseQueueTests and DatabaseTablesTests.
/// </summary>
[Trait("Category", "E2E")]
public class DbConsistencyScenarios : ClaimsScenarioBase
{
    private static DataRow[] Rows(string sql, params (string Name, object Value)[] parameters) =>
        Database().readFromDatabase(sql, parameters.ToDictionary(p => p.Name, p => p.Value)).Rows.Cast<DataRow>().ToArray();

    private static DataRow[] PlayerRows(ITestPlayer player) =>
        Rows("SELECT * FROM PLAYERS WHERE uid=@uid", ("@uid", Info(player).Guid));

    private static DataRow[] PlotRows(Plot plot) =>
        Rows("SELECT * FROM PLOTS WHERE x=@x AND z=@z", ("@x", plot.getPos().X), ("@z", plot.getPos().Y));

    [AtlasScenario]
    public async Task JoinedPlayer_Should_BeStored()
    {
        ResetConfig();
        int area = NewArea();
        ITestPlayer player = await JoinAt("joined" + area, area);

        Database().saveEveryThing();

        DataRow row = Assert.Single(PlayerRows(player));
        Assert.Equal(Info(player).GetPartName(), row["name"].ToString());
    }

    [AtlasScenario]
    public async Task FoundedCity_Should_BeStoredAsItIsInMemory()
    {
        CityScene s = await GivenCity("citizen");

        Database().saveEveryThing();

        var broken = new List<string>();
        DataRow[] cityRows = Rows("SELECT * FROM CITIES WHERE guid=@guid", ("@guid", s.City.Guid));
        if (cityRows.Length != 1) broken.Add($"{cityRows.Length} city rows");
        else
        {
            if (cityRows[0]["name"].ToString() != s.City.GetPartName()) broken.Add("city name is " + cityRows[0]["name"]);
            if (cityRows[0]["mayor"].ToString() != s.City.getMayor().Guid) broken.Add("mayor is " + cityRows[0]["mayor"]);
        }
        foreach (ITestPlayer player in s.Citizens.Prepend(s.Mayor))
        {
            DataRow[] rows = PlayerRows(player);
            if (rows.Length != 1) broken.Add($"{player.Player.PlayerName}: {rows.Length} rows");
            else if (rows[0]["city"].ToString() != s.City.Guid) broken.Add($"{player.Player.PlayerName}: city is '{rows[0]["city"]}'");
        }
        foreach (Plot plot in s.City.getCityPlots())
        {
            DataRow[] rows = PlotRows(plot);
            if (rows.Length != 1) broken.Add($"plot {plot.getPos()}: {rows.Length} rows");
            else if (rows[0]["city"].ToString() != s.City.Guid) broken.Add($"plot {plot.getPos()}: city is '{rows[0]["city"]}'");
        }
        int storedPlots = Rows("SELECT * FROM PLOTS WHERE city=@guid", ("@guid", s.City.Guid)).Length;
        if (storedPlots != s.City.getCityPlots().Count)
            broken.Add($"{storedPlots} plot rows, the city holds {s.City.getCityPlots().Count}");

        Assert.True(broken.Count == 0, "the database differs from memory:\n" + string.Join("\n", broken));
    }

    // A refused command writes nothing: the citizen may not delete the city, its rows stay as they were.
    [AtlasScenario]
    public async Task RefusedCityDelete_Should_LeaveTheRowsAlone()
    {
        CityScene s = await GivenCity("citizen");
        Database().saveEveryThing();
        int plotsBefore = Rows("SELECT * FROM PLOTS WHERE city=@guid", ("@guid", s.City.Guid)).Length;

        CommandResult refused = await s.Citizens[0].ExecuteCommand("/city delete");
        await s.Citizens[0].ExecuteCommand("/agree");
        await World.Ticks(AgreeTimeoutTicks);
        Database().saveEveryThing();

        Assert.False(refused.Ok, "precondition: a citizen was allowed to delete the city");
        Assert.Single(Rows("SELECT * FROM CITIES WHERE guid=@guid", ("@guid", s.City.Guid)));
        Assert.Equal(plotsBefore, Rows("SELECT * FROM PLOTS WHERE city=@guid", ("@guid", s.City.Guid)).Length);
        Assert.Equal(s.City.Guid, Assert.Single(PlayerRows(s.Citizens[0]))["city"].ToString());
    }

    [AtlasScenario]
    public async Task DeletedCity_Should_LeaveNoRows()
    {
        CityScene s = await GivenCity("citizen");
        Database().saveEveryThing();

        await s.Mayor.ExecuteCommand("/city delete");
        await Agree(s.Mayor);
        await World.Until(() => !Mod.dataStorage.getCitiesList().Contains(s.City), "the city to be deleted", AgreeTimeoutTicks);
        Database().saveEveryThing();

        Assert.Empty(Rows("SELECT * FROM CITIES WHERE guid=@guid", ("@guid", s.City.Guid)));
        Assert.Empty(Rows("SELECT * FROM PLOTS WHERE city=@guid", ("@guid", s.City.Guid)));
        foreach (ITestPlayer player in s.Citizens.Prepend(s.Mayor))
            Assert.Equal("", Assert.Single(PlayerRows(player))["city"].ToString());
    }
}
