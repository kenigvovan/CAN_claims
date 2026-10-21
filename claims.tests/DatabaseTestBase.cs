using System.Data;
using claims.src;
using claims.src.database;
using Microsoft.Data.Sqlite;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.tests
{
    /// <summary>
    /// A real SQLiteDatabaseHanlder on a temp file, no server: claims.sapi is a mock whose logger
    /// counts errors. The drain timer is never fired - tests flush with saveEveryThing().
    /// </summary>
    public abstract class DatabaseTestBase : IDisposable
    {
        private readonly string dir = Path.Combine(Path.GetTempPath(), "claims-db-tests", Guid.NewGuid().ToString("N"));
        private readonly ICoreServerAPI previousSapi = claims.src.claims.sapi;
        private readonly Config previousConfig = claims.src.claims.config;
        protected readonly Mock<ILogger> logger = new();
        protected readonly SQLiteDatabaseHanlder db;

        protected DatabaseTestBase()
        {
            Directory.CreateDirectory(dir);
            var world = new Mock<IServerWorldAccessor> { DefaultValue = DefaultValue.Mock };
            world.Setup(w => w.Logger).Returns(logger.Object);
            var sapi = new Mock<ICoreServerAPI> { DefaultValue = DefaultValue.Mock };
            sapi.Setup(a => a.World).Returns(world.Object);
            sapi.Setup(a => a.Logger).Returns(logger.Object);
            claims.src.claims.sapi = sapi.Object;
            claims.src.claims.config = new Config { PATH_TO_DB_AND_JSON_FILES = dir, DB_NAME = "test.db" };
            claims.src.perms.PermsHandler.initDicts();

            db = new SQLiteDatabaseHanlder();
        }

        public void Dispose()
        {
            db.getConnection().Dispose();
            SqliteConnection.ClearAllPools();
            claims.src.claims.sapi = previousSapi;
            claims.src.claims.config = previousConfig;
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }

        protected DataRow[] Select(string sql, Dictionary<string, object> parameters) =>
            db.readFromDatabase(sql, parameters).Rows.Cast<DataRow>().ToArray();

        protected int LoggedErrors() =>
            logger.Invocations.Count(i => i.Method.Name == nameof(ILogger.Error));
    }
}
