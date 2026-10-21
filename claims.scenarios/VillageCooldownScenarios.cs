using System.Data;
using Atlas.Api;
using Atlas.XUnit;
using claims.src.auxialiry;
using claims.src.database;
using claims.src.part;
using claims.src.part.structure;
using Microsoft.Data.Sqlite;
using Vintagestory.API.MathTools;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// The refounding cooldown after a village is gone. The bug: the cooldown was stored as a ready
/// end time, so lowering VILLAGE_*_COOLDOWN_HOURS in the config did nothing for players who
/// already carried one. Now the moment of the loss is stored and the length is read from the
/// current config on every check.
///
/// All scenarios share one server, and the mod's state is not rolled back between them. So each
/// one sets the config it needs itself, joins its own players and works in its own area.
/// </summary>
[Trait("Category", "E2E")]
public class VillageCooldownScenarios : AtlasScenarioBase
{
    // Far enough apart that the distance rules and the ruin radius never reach a neighbour.
    private const int AreaSpacingBlocks = 1600;

    [AtlasScenario]
    public async Task Refound_Should_BeBlocked_When_VillageWasJustAbandoned()
    {
        ResetConfig();
        ITestPlayer mayor = await JoinAt("abandonA", area: 0);

        await FoundAndAgree(mayor, "village", "Ashford");
        await AbandonAndAgree(mayor);

        CommandResult refound = await mayor.ExecuteCommand("/village create Ashford");

        Assert.False(refound.Ok, "refounding right after abandoning must be refused: " + refound.Message);
        Assert.False(Info(mayor).hasCity());
    }

    [AtlasScenario]
    public async Task Refound_Should_Succeed_When_AbandonCooldownIsLoweredToZeroAfterwards()
    {
        ResetConfig();
        ITestPlayer mayor = await JoinAt("abandonB", area: 1);

        await FoundAndAgree(mayor, "village", "Brookvale");
        await AbandonAndAgree(mayor);
        Assert.False((await mayor.ExecuteCommand("/village create Brookvale")).Ok);

        // The penalty was handed out under 24 h; lowering the config must reach it.
        Mod.config.VILLAGE_ABANDON_COOLDOWN_HOURS = 0;

        await FoundAndAgree(mayor, "village", "Brookvale");
        Assert.True(Info(mayor).hasCity());
        Assert.True(Info(mayor).City.IsVillage());
    }

    // The path a server admin takes: /cadmin setcfg, and refounding as a city rather than a
    // village - the founding checks are shared, so a city was blocked as well.
    [AtlasScenario]
    public async Task CityRefound_Should_Succeed_When_CooldownsAreLoweredThroughSetcfg()
    {
        ResetConfig();
        ITestPlayer mayor = await JoinAt("deletedC", area: 2);
        AllowAdminCommands(mayor);

        await FoundAndAgree(mayor, "village", "Cinderhill");
        // Deleting a village counts as losing it: the long cooldown for the player and the ruins.
        await DeleteAndAgree(mayor);
        Assert.False((await mayor.ExecuteCommand("/city create Cinderhill")).Ok);
        Assert.False((await mayor.ExecuteCommand("/village create Cinderhill")).Ok);

        CommandResult setRefound = await mayor.ExecuteCommand("/cadmin setcfg village_refound_cooldown_hours 0");
        CommandResult setSite = await mayor.ExecuteCommand("/cadmin setcfg village_site_cooldown_hours 0");
        Assert.True(setRefound.Ok, setRefound.Message);
        Assert.True(setSite.Ok, setSite.Message);

        await FoundAndAgree(mayor, "city", "Cinderhill");
        Assert.True(Info(mayor).hasCity());
        Assert.False(Info(mayor).City.IsVillage());
    }

    [AtlasScenario]
    public async Task RemainingHours_Should_FollowTheConfig_When_ItChangesAfterTheLoss()
    {
        ResetConfig();
        ITestPlayer mayor = await JoinAt("hoursD", area: 3);

        await FoundAndAgree(mayor, "village", "Dunmere");
        Vec2i site = PlotOf(mayor);
        await AbandonAndAgree(mayor);

        Assert.Equal(24, HoursLeft(mayor, site));

        Mod.config.VILLAGE_ABANDON_COOLDOWN_HOURS = 1;
        Assert.Equal(1, HoursLeft(mayor, site));

        // Both ways: raising it lengthens a cooldown already running.
        Mod.config.VILLAGE_ABANDON_COOLDOWN_HOURS = 48;
        Assert.Equal(48, HoursLeft(mayor, site));

        Mod.config.VILLAGE_ABANDON_COOLDOWN_HOURS = 0;
        Assert.Equal(0, HoursLeft(mayor, site));
    }

    // The ruins block the spot for anyone, also for a player who never lived in the village.
    [AtlasScenario]
    public async Task Ruins_Should_BlockAStranger_Until_SiteCooldownIsLowered()
    {
        ResetConfig();
        ITestPlayer mayor = await JoinAt("ownerE", area: 4);
        await FoundAndAgree(mayor, "village", "Elmwick");
        await DeleteAndAgree(mayor);

        ITestPlayer stranger = await JoinAt("strangerE", area: 4);
        Assert.False(Info(stranger).hasCity());

        CommandResult blocked = await stranger.ExecuteCommand("/village create Fernhollow");
        Assert.False(blocked.Ok, "the ruins must block a stranger too: " + blocked.Message);

        Mod.config.VILLAGE_SITE_COOLDOWN_HOURS = 0;

        await FoundAndAgree(stranger, "village", "Fernhollow");
        Assert.True(Info(stranger).hasCity());
    }

    // The cooldown is stored in new columns; a typo in the SQL templates would break saving
    // players altogether, so the round trip through the database is checked here.
    [AtlasScenario]
    public async Task Cooldown_Should_SurviveTheDatabase_When_SavedAndLoadedBack()
    {
        ResetConfig();
        ITestPlayer mayor = await JoinAt("dbF", area: 5);

        await FoundAndAgree(mayor, "village", "Glenrock");
        Vec2i site = PlotOf(mayor);
        long before = TimeFunctions.getEpochSeconds();
        await AbandonAndAgree(mayor);
        long after = TimeFunctions.getEpochSeconds();

        SQLiteDatabaseHanlder db = Database();
        db.saveEveryThing();

        DataRow row = Assert.Single(db.readFromDatabase("SELECT * FROM PLAYERS WHERE uid=@uid",
            new Dictionary<string, object> { { "@uid", mayor.Player.PlayerUID } }).Rows.Cast<DataRow>());
        long storedLostAt = long.Parse(row["villagelostat"].ToString()!);
        Assert.InRange(storedLostAt, before, after);
        Assert.Equal("1", row["villageabandoned"].ToString());

        DataRow ruin = Assert.Single(db.readFromDatabase("SELECT * FROM VILLAGERUINS WHERE x=@x AND z=@z",
            new Dictionary<string, object> { { "@x", site.X }, { "@z", site.Y } }).Rows.Cast<DataRow>());
        Assert.InRange(long.Parse(ruin["since"].ToString()!), before, after);
        Assert.Equal("1", ruin["abandoned"].ToString());

        // Loading the row back restores what the restart would.
        PlayerInfo info = Info(mayor);
        info.VillageLostAt = 0;
        info.VillageAbandoned = false;
        Assert.True(db.loadPlayerInfo(row));
        Assert.Equal(storedLostAt, info.VillageLostAt);
        Assert.True(info.VillageAbandoned);
    }

    // Rows written by an older build carry only an end time that the config cannot change.
    // They are dropped on load - otherwise servers that already hit the bug stay stuck.
    [AtlasScenario]
    public async Task LegacyRuin_Should_BeDropped_When_LoadedFromAnOlderBuild()
    {
        ResetConfig();
        ITestPlayer player = await JoinAt("legacyG", area: 6);
        Vec2i legacy = PlotOf(player);
        Vec2i current = new Vec2i(legacy.X + 10, legacy.Y);

        SQLiteDatabaseHanlder db = Database();
        db.saveEveryThing();
        using (var cmd = new SqliteCommand(
            "INSERT INTO VILLAGERUINS (x, z, since, abandoned) VALUES (@x, @z, 0, 0), (@cx, @cz, @since, 0)",
            db.getConnection()))
        {
            cmd.Parameters.AddWithValue("@x", legacy.X);
            cmd.Parameters.AddWithValue("@z", legacy.Y);
            cmd.Parameters.AddWithValue("@cx", current.X);
            cmd.Parameters.AddWithValue("@cz", current.Y);
            cmd.Parameters.AddWithValue("@since", TimeFunctions.getEpochSeconds());
            cmd.ExecuteNonQuery();
        }

        Assert.True(db.loadVillageRuins());

        Assert.False(Mod.dataStorage.VillageRuins.ContainsKey(legacy));
        Assert.True(Mod.dataStorage.VillageRuins.ContainsKey(current));
        Assert.True(VillageCooldownHelper.CanFoundHere(Info(player), legacy, out _, out _));

        db.saveEveryThing();
        Assert.Empty(db.readFromDatabase("SELECT * FROM VILLAGERUINS WHERE x=@x AND z=@z",
            new Dictionary<string, object> { { "@x", legacy.X }, { "@z", legacy.Y } }).Rows);
        await World.Ticks(1);
    }

    // ------------------------------------------------------------------------------------------

    private static void ResetConfig()
    {
        var c = Mod.config;
        c.VILLAGE_ENABLED = true;
        c.VILLAGE_CREATE_COST = 0;
        c.CLAIM_LIMITERS_ENABLED = false;
        c.VILLAGE_REFOUND_COOLDOWN_HOURS = 72;
        c.VILLAGE_SITE_COOLDOWN_HOURS = 72;
        c.VILLAGE_ABANDON_COOLDOWN_HOURS = 24;
        c.VILLAGE_RUIN_RADIUS_PLOTS = 1;
    }

    private async Task<ITestPlayer> JoinAt(string name, int area)
    {
        ITestPlayer player = await World.JoinPlayer(name);
        await player.TeleportTo(World.Spawn.Offset(area * AreaSpacingBlocks, 0, 0));
        return player;
    }

    private static void AllowAdminCommands(ITestPlayer player)
    {
        Mod.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Add(player.Player.Role.Code);
    }

    // Ticks to wait for an agreed action: /agree runs it in a callback on a later tick.
    private const int AgreeTimeoutTicks = 20;

    private async Task FoundAndAgree(ITestPlayer player, string kind, string name)
    {
        CommandResult result = await player.ExecuteCommand($"/{kind} create {name}");
        Assert.True(result.Ok, $"/{kind} create {name}: {result.Message}");
        await Agree(player);
        await World.Until(() => Info(player).hasCity(), $"{name} to be founded after /agree", AgreeTimeoutTicks);
    }

    private async Task AbandonAndAgree(ITestPlayer player)
    {
        CommandResult result = await player.ExecuteCommand("/village abandon");
        Assert.True(result.Ok, "/village abandon: " + result.Message);
        await Agree(player);
        await World.Until(() => !Info(player).hasCity(), "the village to be gone after /agree", AgreeTimeoutTicks);
    }

    private async Task DeleteAndAgree(ITestPlayer player)
    {
        CommandResult result = await player.ExecuteCommand("/city delete");
        Assert.True(result.Ok, "/city delete: " + result.Message);
        await Agree(player);
        await World.Until(() => !Info(player).hasCity(), "the settlement to be gone after /agree", AgreeTimeoutTicks);
    }

    private static async Task Agree(ITestPlayer player)
    {
        CommandResult agree = await player.ExecuteCommand("/agree");
        Assert.True(agree.Ok, "/agree: " + agree.Message);
    }

    private static PlayerInfo Info(ITestPlayer player)
    {
        Assert.True(Mod.dataStorage.GetPlayerByUid(player.Player.PlayerUID, out PlayerInfo info),
            "no PlayerInfo for " + player.Player.PlayerName);
        return info;
    }

    private static Vec2i PlotOf(ITestPlayer player)
    {
        BlockPos pos = player.Position;
        return PlotPosition.fromXZ(pos.X, pos.Z).getPos();
    }

    private static int HoursLeft(ITestPlayer player, Vec2i site)
    {
        VillageCooldownHelper.CanFoundHere(Info(player), site, out _, out int hoursLeft);
        return hoursLeft;
    }

    private static SQLiteDatabaseHanlder Database()
    {
        return Assert.IsType<SQLiteDatabaseHanlder>(Mod.getModInstance().getDatabaseHandler());
    }
}
