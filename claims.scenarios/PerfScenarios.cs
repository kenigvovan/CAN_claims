using System.Globalization;
using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Vintagestory.API.MathTools;

namespace claims.scenarios;

/// <summary>
/// R26: how much the mod slows the server down. The same workload runs on a vanilla server
/// (PerfVanillaScenarios) and on one with the mod (PerfModScenarios); each writes its numbers to
/// bin/perf/, and the mod run compares itself with the vanilla numbers when they are there.
/// Run on purpose and more than once - one window is noise:
///   dotnet test claims.scenarios --filter "Category=Perf"
/// Oracle (Z7 assumption): p95 of a tick with the mod at most 20 % above vanilla.
/// </summary>
public static class PerfWorkload
{
    public const int Ticks = 600;
    public const double AllowedSlowdown = 1.2;

    /// <summary>Three players walking back and forth across plot borders while the ticks are measured.</summary>
    public static async Task<TickMeasurement> Run(IWorldSession world, IReadOnlyList<ITestPlayer> walkers)
    {
        var walks = new List<Task>();
        foreach (ITestPlayer p in walkers)
        {
            BlockPos start = p.Position.Copy();
            walks.Add(WalkBackAndForth(p, start, start.AddCopy(64, 0, 0)));
        }
        TickMeasurement m = await world.MeasureTicks(Ticks);
        await Task.WhenAll(walks);
        return m;
    }

    private static async Task WalkBackAndForth(ITestPlayer p, BlockPos a, BlockPos b)
    {
        for (int i = 0; i < 3; i++)
        {
            await p.WalkTo(b);
            await p.WalkTo(a);
        }
    }

    public static string ResultPath(string name) =>
        Path.Combine(Path.GetDirectoryName(typeof(PerfWorkload).Assembly.Location)!, "perf", name + ".txt");

    public static void Save(string name, TickMeasurement m)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath(name))!);
        File.WriteAllText(ResultPath(name), string.Join(";",
            m.BusyTime.MedianMs, m.BusyTime.P95Ms,
            m.BusyTime.MeanMicroseconds.ToString(CultureInfo.InvariantCulture), m.AllocatedBytes));
    }

    public static bool TryLoad(string name, out long p95Ms, out double meanUs)
    {
        p95Ms = 0; meanUs = 0;
        if (!File.Exists(ResultPath(name))) return false;
        string[] parts = File.ReadAllText(ResultPath(name)).Split(';');
        p95Ms = long.Parse(parts[1], CultureInfo.InvariantCulture);
        meanUs = double.Parse(parts[2], CultureInfo.InvariantCulture);
        return true;
    }
}

[Trait("Category", "Perf")]
[AtlasWorld(ExcludeAssemblyMods = true)]
public class PerfVanillaScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task Vanilla_Baseline()
    {
        var walkers = new List<ITestPlayer>();
        for (int i = 0; i < 3; i++) walkers.Add(await World.JoinPlayer("walker" + i));

        TickMeasurement m = await PerfWorkload.Run(World, walkers);

        PerfWorkload.Save("vanilla", m);
    }
}

[Trait("Category", "Perf")]
public class PerfModScenarios : ClaimsScenarioBase
{
    [AtlasScenario]
    public async Task Mod_Should_NotSlowTheTickDownMoreThanAllowed()
    {
        // a city with a row of plots the walkers cross
        CityScene s = await GivenCity("walkera", "walkerb");
        for (int dx = 2; dx <= 4; dx++)
        {
            await GoToPlot(s.Mayor, s.Area, dx, 0);
            await s.Mayor.ExecuteCommand("/city claim");
        }
        var walkers = new List<ITestPlayer> { s.Mayor, s.Citizens[0], s.Citizens[1] };
        foreach (ITestPlayer w in walkers) await GoToPlot(w, s.Area, 0, 0);

        TickMeasurement m = await PerfWorkload.Run(World, walkers);
        PerfWorkload.Save("mod", m);

        // only when the vanilla numbers exist - classes run in no guaranteed order
        if (!PerfWorkload.TryLoad("vanilla", out long vanillaP95, out double vanillaMeanUs)) return;
        // Ticks are timed in whole milliseconds, so a slack of 1 ms keeps 0 ms vs 1 ms from failing.
        Assert.True(m.BusyTime.P95Ms <= vanillaP95 * PerfWorkload.AllowedSlowdown + 1,
            $"p95 {m.BusyTime.P95Ms} ms vs vanilla {vanillaP95} ms");
        Assert.True(m.BusyTime.MeanMicroseconds <= vanillaMeanUs * PerfWorkload.AllowedSlowdown + 1000,
            $"mean {m.BusyTime.MeanMicroseconds:0} us vs vanilla {vanillaMeanUs:0} us");
    }
}
