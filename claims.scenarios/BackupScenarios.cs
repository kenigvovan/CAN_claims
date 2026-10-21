using Atlas.XUnit;
using claims.scenarios.Infrastructure;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Config;
using Mod = global::claims.src.claims;

namespace claims.scenarios;

/// <summary>
/// Database backups - the last line against losing data.
/// </summary>
[Trait("Category", "E2E")]
public class BackupScenarios : ClaimsScenarioBase
{
    // BK1: a backup is a full, readable copy of the database
    [AtlasScenario]
    public async Task Backup_Should_HoldTheWholeDatabase()
    {
        CityScene s = await GivenCity();
        var db = Database();
        db.saveEveryThing();
        string name = "atlas_backup_" + s.Area;

        Assert.True(db.makeBackup(name), "makeBackup reported a failure");

        // the city is in the copy
        string path = Path.Combine(GamePaths.DataPath, Mod.config.BACKUP_FOLDER_NAME_IN_DATA_FOLDER, name);
        Assert.True(File.Exists(path), "no backup file at " + path);
        Assert.Equal(1, CountCities(path, s.City.GetPartName()));
    }

    // BK-O1: FULL_BACKUP_FOLDER is where the admin looks for backups
    [AtlasScenario]
    public async Task Backup_Should_GoToFullBackupFolder_When_ItIsSet()
    {
        CityScene s = await GivenCity();
        var db = Database();
        string folder = Path.Combine(Path.GetTempPath(), "claims_backup_test_" + s.Area);
        Directory.CreateDirectory(folder);
        string before = Mod.config.FULL_BACKUP_FOLDER;
        Mod.config.FULL_BACKUP_FOLDER = folder;
        try
        {
            db.makeBackup("atlas_full_folder");

            Assert.True(File.Exists(Path.Combine(folder, "atlas_full_folder")),
                "the backup did not go to FULL_BACKUP_FOLDER");
        }
        finally
        {
            Mod.config.FULL_BACKUP_FOLDER = before;
        }
    }

    private static int CountCities(string dbPath, string cityName)
    {
        using var connection = new SqliteConnection("Data Source=" + dbPath);
        connection.Open();
        using var cmd = new SqliteCommand("SELECT COUNT(*) FROM CITIES WHERE name=@n", connection);
        cmd.Parameters.AddWithValue("@n", cityName);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
