using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.database;
using claims.src.part.structure;
using Microsoft.Data.Sqlite;

namespace claims.scenarios;

/// <summary>
/// Loading the database: one bad row must not take the rest down with it.
/// </summary>
[Trait("Category", "E2E")]
public class DbLoadScenarios : ClaimsScenarioBase
{
    // O2, DB1
    [AtlasScenario]
    public async Task LoadPlots_Should_SkipABadRowAndGoOn()
    {
        // a city with two plots; one row gets a type that is not a number
        CityScene s = await GivenCity();
        Plot good = PlotAt(s.Mayor);
        await GoToPlot(s.Mayor, s.Area);
        Plot bad = PlotAt(s.Mayor);
        SQLiteDatabaseHanlder db = Database();
        db.saveEveryThing();
        SetPlotTypeColumn(db, bad, "x");
        good.Price = 77; // in memory only: the load has to bring the stored -1 back

        Exception? error = Record.Exception(() => db.loadAllPlots());

        SetPlotTypeColumn(db, bad, ((int)bad.Type).ToString());
        Assert.True(error == null, "one bad row broke the whole load: " + error);
        Assert.Equal(-1, good.Price);
        await World.Ticks(1);
    }

    private static void SetPlotTypeColumn(SQLiteDatabaseHanlder db, Plot plot, string value)
    {
        using var cmd = new SqliteCommand("UPDATE PLOTS SET type=@t WHERE x=@x AND z=@z", db.getConnection());
        cmd.Parameters.AddWithValue("@t", value);
        cmd.Parameters.AddWithValue("@x", plot.getPos().X);
        cmd.Parameters.AddWithValue("@z", plot.getPos().Y);
        cmd.ExecuteNonQuery();
    }
}
