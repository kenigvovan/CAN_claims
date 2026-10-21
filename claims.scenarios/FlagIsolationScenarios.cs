using Atlas.Api;
using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using claims.src.perms;
using claims.src.perms.type;

namespace claims.scenarios;

/// <summary>
/// Changing one of a plot's 16 values (4 flags, 4 groups x 3 rights) must leave the others alone.
/// Cases FI3-FI5.
/// </summary>
[Trait("Category", "E2E")]
public class FlagIsolationScenarios : ClaimsScenarioBase
{
    private static readonly string[] Groups = { "citizen", "stranger", "ally", "friend" };
    private static readonly string[] Types = { "use", "build", "attack" };
    private static readonly string[] Flags = { "pvp", "fire", "blast", "nomobspawn" };

    /// <summary>All 16 values of the plot the player stands on, by name.</summary>
    private static Dictionary<string, bool> Snapshot(ITestPlayer player)
    {
        PermsHandler h = PlotAt(player).getPermsHandler();
        var s = new Dictionary<string, bool>
        {
            ["pvp"] = h.pvpFlag,
            ["fire"] = h.fireFlag,
            ["blast"] = h.blastFlag,
            ["nomobspawn"] = h.noMobSpawnFlag,
        };
        bool[][] byGroup = { h.CitizenPerms, h.StrangerPerms, h.AlliancePerms, h.ComradePerms };
        for (int g = 0; g < Groups.Length; g++)
            for (int t = 0; t < Types.Length; t++)
                s[Groups[g] + ":" + Types[t]] = byGroup[g][t];
        return s;
    }

    private static string[] Changed(Dictionary<string, bool> before, Dictionary<string, bool> after) =>
        before.Keys.Where(k => before[k] != after[k]).ToArray();

    // FI3, R35
    [AtlasTheory]
    [InlineData("/plot set p stranger attack on", "stranger:attack")]
    [InlineData("/plot set pvp on", "pvp")]
    [InlineData("/plot set fire on", "fire")]
    [InlineData("/plot set blast off", "blast")]
    [InlineData("/plot set nomobspawn on", "nomobspawn")]
    public async Task Command_Should_ChangeOnlyItsOwnValue(string command, string value)
    {
        CityScene s = await GivenCity();
        Dictionary<string, bool> before = Snapshot(s.Mayor);

        await s.Mayor.ExecuteCommand(command);

        Assert.Equal(new[] { value }, Changed(before, Snapshot(s.Mayor)));
    }

    // FI4, R35
    [AtlasScenario]
    public async Task Permission_Should_StayAsItWas_When_TheStateIsMistyped()
    {
        CityScene s = await GivenCity();
        await s.Mayor.ExecuteCommand("/plot set p stranger attack on");
        Assert.True(Snapshot(s.Mayor)["stranger:attack"], "precondition: strangers may attack animals");
        Dictionary<string, bool> before = Snapshot(s.Mayor);

        await s.Mayor.ExecuteCommand("/plot set p stranger attack onn");

        Assert.Empty(Changed(before, Snapshot(s.Mayor)));
    }

    // FI4 for the city's own defaults: /city set p reported success and turned the right off
    [AtlasScenario]
    public async Task CityPermission_Should_StayAsItWas_When_TheStateIsMistyped()
    {
        CityScene s = await GivenCity();
        await s.Mayor.ExecuteCommand("/city set p stranger attack on");
        Assert.True(s.City.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.ATTACK_ANIMALS_PERM),
            "precondition: /city set p with a valid state works");

        CommandResult mistyped = await s.Mayor.ExecuteCommand("/city set p stranger attack onn");

        Assert.False(mistyped.Ok, "a mistyped state was reported as done");
        Assert.True(s.City.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.ATTACK_ANIMALS_PERM),
            "a mistyped state changed the city's right");
    }

    // FI5, R35, A24
    [AtlasScenario]
    public async Task Flags_Should_StayOnThePlot_When_ACitizenBuysIt()
    {
        CityScene s = await GivenCity("buyer");
        await s.Mayor.ExecuteCommand("/plot set nomobspawn on");
        await s.Mayor.ExecuteCommand("/plot set blast off");
        Dictionary<string, bool> before = Snapshot(s.Mayor);
        Assert.True(before["nomobspawn"] && !before["blast"], "precondition: no mobs and no blasts on the plot");

        await GivenOwnedPlot(s, s.Citizens[0]);

        Dictionary<string, bool> after = Snapshot(s.Mayor);
        Assert.Empty(Changed(before, after).Where(Flags.Contains));
    }
}
