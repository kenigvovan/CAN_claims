using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src;
using claims.src.cityplotsgroups;
using Microsoft.Data.Sqlite;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Builds the "full world" database that PersistenceScenarios load (R03) and writes it into
/// claims.scenarios/fixtures/FullWorld/. Run by hand, once, and again only when the database
/// schema changes:
///   set CLAIMS_GENERATE_FIXTURE=1
///   dotnet test claims.scenarios --filter "Category=FixtureGen"
/// Without the variable it does nothing, so an ordinary run never overwrites the fixture.
/// What it builds is listed in FullWorld (below) - PersistenceScenarios expects exactly that.
/// </summary>
[Trait("Category", "FixtureGen")]
public class FullWorldFixtureGenerator : ClaimsScenarioBase
{
    [AtlasScenario]
    public async Task Generate()
    {
        if (Environment.GetEnvironmentVariable("CLAIMS_GENERATE_FIXTURE") != "1")
        {
            await World.Ticks(1);
            return;
        }

        ResetConfig();
        int area = NewArea();
        ITestPlayer mayor = await World.JoinPlayer(FullWorld.Mayor);
        ITestPlayer citizen = await World.JoinPlayer(FullWorld.Citizen);
        await GoToPlot(mayor, area);
        await GoToPlot(citizen, area);

        // City, citizen, an extra plot
        await FoundCity(mayor, FullWorld.City);
        var city = Info(mayor).City;
        await AddCitizen(city, mayor, citizen);
        await GoToPlot(mayor, area, 1, 0);
        await GoToPlot(citizen, area, 1, 0);
        await mayor.ExecuteCommand("/city claim");

        // A plot bought by the citizen, taxed
        await PutForSale(mayor, FullWorld.PlotPrice);
        await ClaimAndAgree(citizen);
        await mayor.ExecuteCommand($"/plot set fee {FullWorld.PlotTax}");

        // City settings and state
        await mayor.ExecuteCommand($"/city set fee {FullWorld.CityFee}");
        city.DebtBalance = FullWorld.Debt;

        // A plots group with the citizen in it
        await mayor.ExecuteCommand($"/city plotsgroup create {FullWorld.Group}");
        CityPlotsGroup group = city.getCityPlotsGroups().Single();
        group.PlayersList.Add(Info(citizen));

        // An alliance and a prison sentence
        await mayor.ExecuteCommand($"/alliance create {FullWorld.Alliance}");
        await Agree(mayor);
        await World.Until(() => city.HasAlliance(), "the alliance to be founded", AgreeTimeoutTicks);
        Info(citizen).PrisonHoursLeft = FullWorld.PrisonHours;

        // Write everything out, then copy the database next to the fixtures of the source tree
        var db = Database();
        city.saveToDatabase();
        group.saveToDatabase();
        Info(citizen).saveToDatabase();
        db.saveEveryThing();

        string bin = Path.GetDirectoryName(typeof(FullWorldFixtureGenerator).Assembly.Location)!;
        string target = Path.GetFullPath(Path.Combine(bin, "..", "..", "..", "fixtures", "FullWorld"));
        Directory.CreateDirectory(target);
        using var copy = new SqliteConnection("Data Source=" + Path.Combine(target, Mod.config.DB_NAME));
        db.getConnection().BackupDatabase(copy);
    }
}

/// <summary>What the fixture contains - shared by the generator and the checks.</summary>
public static class FullWorld
{
    public const string City = "PersistTown";
    public const string Mayor = "pmayor";
    public const string Citizen = "pcitizen";
    public const string Group = "pgroup";
    public const string Alliance = "PersistAlly";
    public const int CityPlots = 2;
    public const int PlotPrice = 40;
    public const int PlotTax = 5;
    public const int CityFee = 10;
    public const double Debt = 25;
    public const int PrisonHours = 5;
}
