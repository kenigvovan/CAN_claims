using System.Data;
using System.Text.RegularExpressions;
using claims.src.database;

namespace claims.tests
{
    // The queue rules of DatabaseQueueTests, for every table in QuerryTemplates.ByTable: rows are
    // built from the table's own statements (key = the DELETE's WHERE, values for every INSERT
    // parameter), so no entity is needed and a new table is covered without touching this file.
    // Values are numbers on purpose: they store and read back the same under any column affinity.
    [Collection("ModStatics")]
    public class DatabaseTablesTests : DatabaseTestBase
    {
        public static TheoryData<string> Tables()
        {
            var data = new TheoryData<string>();
            foreach (string table in QuerryTemplates.ByTable.Keys) data.Add(table);
            return data;
        }

        private static string[] Params(string sql) =>
            Regex.Matches(sql, @"@(\w+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        private static string[] KeyParams(string table)
        {
            string delete = QuerryTemplates.ByTable[table].Delete;
            return Params(delete[delete.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase)..]);
        }

        private static object KeyValue(string param) => param switch
        {
            "x" => 1,
            "z" => 2,
            _ => "key-1"
        };

        /// <summary>The column the tests write and read back: the last non-key INSERT parameter.</summary>
        private static string Payload(string table)
        {
            string[] keys = KeyParams(table);
            return Params(QuerryTemplates.ByTable[table].Insert).Last(p => !keys.Contains(p));
        }

        private static Dictionary<string, object> Row(string table, int payload)
        {
            string[] keys = KeyParams(table);
            string payloadParam = Payload(table);
            return Params(QuerryTemplates.ByTable[table].Insert).ToDictionary(
                p => "@" + p,
                p => keys.Contains(p) ? KeyValue(p) : p == payloadParam ? payload : (object)0);
        }

        private static Dictionary<string, object> Key(string table) =>
            KeyParams(table).ToDictionary(p => "@" + p, KeyValue);

        private void Queue(string table, QuerryType type, int payload = 0) =>
            db.getQueue().Enqueue(new QuerryInfo(table, type, type == QuerryType.DELETE ? Key(table) : Row(table, payload)));

        private static string SelectByKey(string table) =>
            Regex.Replace(QuerryTemplates.ByTable[table].Delete, @"^DELETE\s+FROM", "SELECT * FROM", RegexOptions.IgnoreCase);

        private DataRow[] Rows(string table) => Select(SelectByKey(table), Key(table));

        /// <summary>A row where every non-key column gets its own value: baseValue + its position.</summary>
        private static Dictionary<string, object> RowWithDistinctValues(string table, int baseValue)
        {
            string[] keys = KeyParams(table);
            return Params(QuerryTemplates.ByTable[table].Insert)
                .Select((p, i) => (p, i))
                .ToDictionary(t => "@" + t.p, t => keys.Contains(t.p) ? KeyValue(t.p) : (object)(baseValue + t.i));
        }

        private string StoredPayload(string table) => Assert.Single(Rows(table))[Payload(table)].ToString()!;

        [Theory]
        [MemberData(nameof(Tables))]
        public void Update_Should_CreateTheRow_When_ItIsMissing(string table)
        {
            Queue(table, QuerryType.UPDATE, 101);
            db.saveEveryThing();

            Assert.Equal("101", StoredPayload(table));
            Assert.Equal(0, LoggedErrors());
        }

        [Theory]
        [MemberData(nameof(Tables))]
        public void Update_Should_ChangeTheRow_When_ItExists(string table)
        {
            Queue(table, QuerryType.INSERT, 101);
            db.saveEveryThing();

            Queue(table, QuerryType.UPDATE, 102);
            db.saveEveryThing();

            Assert.Equal("102", StoredPayload(table));
        }

        [Theory]
        [MemberData(nameof(Tables))]
        public void Delete_Should_RemoveTheRow(string table)
        {
            Queue(table, QuerryType.INSERT, 101);
            db.saveEveryThing();

            Queue(table, QuerryType.DELETE);
            db.saveEveryThing();

            Assert.Empty(Rows(table));
        }

        // Every table refuses a second row with the same key: the failure is logged, the first row stays.
        [Theory]
        [MemberData(nameof(Tables))]
        public void SecondInsert_Should_FailAndKeepTheFirstRow(string table)
        {
            Queue(table, QuerryType.INSERT, 101);
            Queue(table, QuerryType.INSERT, 102);
            db.saveEveryThing();

            Assert.Equal("101", StoredPayload(table));
            Assert.Equal(1, LoggedErrors());
        }

        [Theory]
        [MemberData(nameof(Tables))]
        public void TwoUpdatesOfANewRow_Should_LeaveExactlyOneRow(string table)
        {
            Queue(table, QuerryType.UPDATE, 101);
            Queue(table, QuerryType.UPDATE, 102);
            db.saveEveryThing();

            Assert.Single(Rows(table));
        }

        // ---- every column ------------------------------------------------------------------------

        // A column the INSERT writes but the UPDATE forgot would keep its first value forever.
        [Theory]
        [MemberData(nameof(Tables))]
        public void Statements_Should_WriteTheSameColumns_ForInsertAndUpdate(string table)
        {
            var (insert, update, _) = QuerryTemplates.ByTable[table];
            Assert.Equal(Params(insert).OrderBy(p => p), Params(update).OrderBy(p => p));
        }

        [Theory]
        [MemberData(nameof(Tables))]
        public void Update_Should_WriteEveryColumn(string table)
        {
            db.getQueue().Enqueue(new QuerryInfo(table, QuerryType.INSERT, RowWithDistinctValues(table, 1000)));
            db.saveEveryThing();

            Dictionary<string, object> expected = RowWithDistinctValues(table, 2000);
            db.getQueue().Enqueue(new QuerryInfo(table, QuerryType.UPDATE, expected));
            db.saveEveryThing();

            DataRow row = Assert.Single(Rows(table));
            string[] keys = KeyParams(table);
            string[] wrong = expected
                .Where(e => !keys.Contains(e.Key[1..]) && row[e.Key[1..]].ToString() != e.Value.ToString())
                .Select(e => $"{e.Key[1..]} = {row[e.Key[1..]]}, expected {e.Value}")
                .ToArray();
            Assert.True(wrong.Length == 0, "columns not written by the UPDATE:\n" + string.Join("\n", wrong));
        }

        // ---- second start ------------------------------------------------------------------------

        // A server restart opens the same file again: tables and column migrations run once more
        // over existing data and must neither fail nor touch it.
        [Fact]
        public void Rows_Should_SurviveReopeningTheDatabase()
        {
            foreach (string table in QuerryTemplates.ByTable.Keys) Queue(table, QuerryType.INSERT, 101);
            db.saveEveryThing();
            db.getConnection().Close();

            var reopened = new SQLiteDatabaseHanlder();
            try
            {
                string[] lost = QuerryTemplates.ByTable.Keys
                    .Where(t =>
                    {
                        DataRow[] rows = reopened.readFromDatabase(SelectByKey(t), Key(t)).Rows.Cast<DataRow>().ToArray();
                        return rows.Length != 1 || rows[0][Payload(t)].ToString() != "101";
                    })
                    .ToArray();
                Assert.True(lost.Length == 0, "rows lost or changed on reopening: " + string.Join(", ", lost));
                Assert.Equal(0, LoggedErrors());
                Assert.Equal(0, logger.Invocations.Count(i => i.Method.Name == nameof(Vintagestory.API.Common.ILogger.Warning)));
                using var version = new Microsoft.Data.Sqlite.SqliteCommand("PRAGMA user_version", reopened.getConnection());
                Assert.Equal(3L, Convert.ToInt64(version.ExecuteScalar()));
            }
            finally
            {
                reopened.getConnection().Dispose();
            }
        }

        // ---- schema vs statements ---------------------------------------------------------------

        private string[] PrimaryKey(string table) =>
            // Only name and pk: table_info's dflt_value mixes types across rows, which DataTable.Load rejects.
            Select($"SELECT name, pk FROM pragma_table_info('{table}')", new Dictionary<string, object>())
                .Where(r => Convert.ToInt64(r["pk"]) > 0)
                .Select(r => r["name"].ToString()!.ToLowerInvariant())
                .OrderBy(n => n).ToArray();

        public static TheoryData<string> TablesKeyedAsInSchema()
        {
            var data = new TheoryData<string>();
            foreach (string table in QuerryTemplates.ByTable.Keys.Where(t => t != "WORLDS")) data.Add(table);
            return data;
        }

        // The statements find a row by the table's primary key - what an ON CONFLICT clause would need.
        [Theory]
        [MemberData(nameof(TablesKeyedAsInSchema))]
        public void Statements_Should_FindTheRowByThePrimaryKey(string table)
        {
            Assert.Equal(PrimaryKey(table), KeyParams(table).Select(p => p.ToLowerInvariant()).OrderBy(p => p).ToArray());
        }

        // Current state: WORLDS is keyed by name in the schema but found by guid in its statements.
        [Fact]
        public void Worlds_Should_BeKeyedByNameInTheSchema_But_FoundByGuid()
        {
            Assert.Equal(new[] { "name" }, PrimaryKey("WORLDS"));
            Assert.Equal(new[] { "guid" }, KeyParams("WORLDS"));
        }
    }
}
