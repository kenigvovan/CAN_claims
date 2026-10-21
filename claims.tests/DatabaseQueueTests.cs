using System.Data;
using claims.src.database;
using claims.src.part;

namespace claims.tests
{
    // How saves reach the database: saveToDatabase() only queues a row snapshot, the 0.5s drain
    // (saveEveryThing here) writes the queue in one transaction. These pin the current behaviour
    // before the queue is reworked, through the real save path of one entity (PLAYERS).
    // The same rules for every table: DatabaseTablesTests.
    [Collection("ModStatics")]
    public class DatabaseQueueTests : DatabaseTestBase
    {
        private static PlayerInfo Player(string afterName = "v1", string uid = "uid-1") =>
            new PlayerInfo("player", uid) { AfterName = afterName };

        private DataRow[] Rows(string uid = "uid-1") =>
            Select("SELECT * FROM PLAYERS WHERE uid=@uid", new Dictionary<string, object> { { "@uid", uid } });

        private string StoredAfterName(string uid = "uid-1") => Assert.Single(Rows(uid))["aftername"].ToString()!;

        // Write-behind: nothing reaches the file until the drain.
        [Fact]
        public void Save_Should_NotWrite_Before_TheFlush()
        {
            db.savePlayerInfo(Player());

            Assert.Empty(Rows());
        }

        // The default save is an UPDATE; with no row yet it falls back to an INSERT.
        [Fact]
        public void Save_Should_CreateTheRow_When_ItIsNotInTheDatabaseYet()
        {
            db.savePlayerInfo(Player("v1"));
            db.saveEveryThing();

            Assert.Equal("v1", StoredAfterName());
            Assert.Equal(0, LoggedErrors());
        }

        [Fact]
        public void SaveAsNew_Should_CreateTheRow()
        {
            db.savePlayerInfo(Player("v1"), update: false);
            db.saveEveryThing();

            Assert.Equal("v1", StoredAfterName());
        }

        [Fact]
        public void Save_Should_UpdateTheRow_When_ItExists()
        {
            db.savePlayerInfo(Player("v1"));
            db.saveEveryThing();

            db.savePlayerInfo(Player("v2"));
            db.saveEveryThing();

            Assert.Equal("v2", StoredAfterName());
            Assert.Equal(0, LoggedErrors());
        }

        [Fact]
        public void Saves_Should_KeepTheLatest_When_TheRowExists()
        {
            db.savePlayerInfo(Player("v1"));
            db.saveEveryThing();

            db.savePlayerInfo(Player("v2"));
            db.savePlayerInfo(Player("v3"));
            db.saveEveryThing();

            Assert.Equal("v3", StoredAfterName());
        }

        // An explicit INSERT is written in place, so an UPDATE behind it in the same drain finds the row.
        [Fact]
        public void SaveAsNewThenSave_Should_KeepTheLatest_When_InOneFlush()
        {
            db.savePlayerInfo(Player("v1"), update: false);
            db.savePlayerInfo(Player("v2"));
            db.saveEveryThing();

            Assert.Equal("v2", StoredAfterName());
            Assert.Equal(0, LoggedErrors());
        }

        [Fact]
        public void Delete_Should_RemoveTheRow()
        {
            db.savePlayerInfo(Player());
            db.saveEveryThing();

            db.deleteFromDatabasePlayerInfo(Player());
            db.saveEveryThing();

            Assert.Empty(Rows());
        }

        [Fact]
        public void SaveThenDelete_Should_RemoveTheRow_When_ItExists()
        {
            db.savePlayerInfo(Player("v1"));
            db.saveEveryThing();

            db.savePlayerInfo(Player("v2"));
            db.deleteFromDatabasePlayerInfo(Player());
            db.saveEveryThing();

            Assert.Empty(Rows());
        }

        [Fact]
        public void Save_Should_CreateTheRowAgain_When_ItWasDeleted()
        {
            db.savePlayerInfo(Player("v1"));
            db.saveEveryThing();
            db.deleteFromDatabasePlayerInfo(Player());
            db.saveEveryThing();

            db.savePlayerInfo(Player("v2"));
            db.saveEveryThing();

            Assert.Equal("v2", StoredAfterName());
        }

        // Rows are keyed: saving one player never touches another.
        [Fact]
        public void Save_Should_TouchOnlyItsOwnRow()
        {
            db.savePlayerInfo(Player("a", "uid-a"));
            db.savePlayerInfo(Player("b", "uid-b"));
            db.saveEveryThing();

            db.savePlayerInfo(Player("a2", "uid-a"));
            db.saveEveryThing();

            Assert.Equal("a2", StoredAfterName("uid-a"));
            Assert.Equal("b", StoredAfterName("uid-b"));
        }

        // A failing statement is logged and skipped; the rest of the drain is still written.
        [Fact]
        public void FailedQuery_Should_NotStopTheRestOfTheFlush()
        {
            db.savePlayerInfo(Player("a", "uid-a"), update: false);
            db.savePlayerInfo(Player("a", "uid-a"), update: false); // UNIQUE violation
            db.savePlayerInfo(Player("b", "uid-b"));
            db.saveEveryThing();

            Assert.Single(Rows("uid-a"));
            Assert.Equal("b", StoredAfterName("uid-b"));
            Assert.Equal(1, LoggedErrors());
        }

        [Fact]
        public void Flush_Should_EmptyTheQueue()
        {
            db.savePlayerInfo(Player());
            db.saveEveryThing();

            Assert.True(db.getQueue().IsEmpty);
        }

        // Two saves of a new row before the drain: both UPDATEs miss, both are re-queued as INSERTs.
        // The row is created once either way - that part holds now and must keep holding.
        [Fact]
        public void TwoSavesOfANewRow_Should_LeaveExactlyOneRow()
        {
            db.savePlayerInfo(Player("v1"));
            db.savePlayerInfo(Player("v2"));
            db.saveEveryThing();

            Assert.Single(Rows());
        }

        // ---- broken input: logged and skipped, the rest of the drain still written --------------

        /// <summary>Queues a save of A, lets <paramref name="breakIt"/> spoil it, then queues a good save of B.</summary>
        private void QueueBrokenThenGood(Action<QuerryInfo> breakIt)
        {
            db.savePlayerInfo(Player("a", "uid-a"), update: false);
            Assert.True(db.getQueue().TryPeek(out QuerryInfo? broken), "precondition: the save was queued");
            breakIt(broken!);
            db.savePlayerInfo(Player("b", "uid-b"));
        }

        [Fact]
        public void UnknownTable_Should_BeLoggedAndSkipped()
        {
            QueueBrokenThenGood(q => q.targetTable = "NO_SUCH_TABLE");
            db.saveEveryThing();

            Assert.Equal("b", StoredAfterName("uid-b"));
            Assert.Equal(1, LoggedErrors());
        }

        [Fact]
        public void MissingParameter_Should_BeLoggedAndSkipped()
        {
            QueueBrokenThenGood(q => q.parameters.Remove("@bounties"));
            db.saveEveryThing();

            Assert.Empty(Rows("uid-a"));
            Assert.Equal("b", StoredAfterName("uid-b"));
            Assert.Equal(1, LoggedErrors());
        }

        // A null field (a string nobody set) is refused by Microsoft.Data.Sqlite, not stored as NULL.
        [Fact]
        public void NullValue_Should_BeLoggedAndSkipped()
        {
            QueueBrokenThenGood(q => q.parameters["@aftername"] = null!);
            db.saveEveryThing();

            Assert.Empty(Rows("uid-a"));
            Assert.Equal("b", StoredAfterName("uid-b"));
            Assert.Equal(1, LoggedErrors());
        }

        [Fact]
        public void Delete_Should_DoNothing_When_TheRowIsMissing()
        {
            db.deleteFromDatabasePlayerInfo(Player());
            db.saveEveryThing();

            Assert.Empty(Rows());
            Assert.Equal(0, LoggedErrors());
        }

        [Fact]
        public void Flush_Should_DoNothing_When_TheQueueIsEmpty()
        {
            db.saveEveryThing();

            Assert.Equal(0, LoggedErrors());
        }

        // A damaged file does not take the server down at startup: the failure is logged instead.
        [Fact]
        public void Open_Should_LogAndNotThrow_When_TheFileIsNotADatabase()
        {
            string damaged = Path.Combine(claims.src.claims.config.PATH_TO_DB_AND_JSON_FILES, "damaged.db");
            File.WriteAllText(damaged, new string('x', 4096));
            claims.src.claims.config.DB_NAME = "damaged.db";

            claims.src.database.SQLiteDatabaseHanlder? opened = null;
            Exception? error = Record.Exception(() => opened = new claims.src.database.SQLiteDatabaseHanlder());
            opened?.getConnection().Dispose();

            Assert.Null(error);
            Assert.True(LoggedErrors() > 0, "the damaged database was opened without a word");
        }

        // ---- fixed queue bugs, and the one change left for the UPSERT rework ---------------------

        // Was: both UPDATEs missed and were re-queued as INSERTs; v1 won, v2 failed on the key.
        [Fact]
        public void TwoSavesOfANewRow_Should_KeepTheLatest()
        {
            db.savePlayerInfo(Player("v1"));
            db.savePlayerInfo(Player("v2"));
            db.saveEveryThing();

            Assert.Equal("v2", StoredAfterName());
            Assert.Equal(0, LoggedErrors());
        }

        // Now: the explicit INSERT fails on the primary key and is dropped; the row keeps v1.
        [Fact(Skip = "current: save(false) of an existing row is lost; changes with the UPSERT rework")]
        public void SaveAsNew_Should_UpdateTheRow_When_ItExists()
        {
            db.savePlayerInfo(Player("v1"));
            db.saveEveryThing();

            db.savePlayerInfo(Player("v2"), update: false);
            db.saveEveryThing();

            Assert.Equal("v2", StoredAfterName());
            Assert.Equal(0, LoggedErrors());
        }

        // Was: the UPDATE missed and was re-queued as an INSERT behind the DELETE, so the row
        // deleted in the same drain came back.
        [Fact]
        public void SaveThenDelete_Should_LeaveNoRow_When_TheRowIsNew()
        {
            db.savePlayerInfo(Player());
            db.deleteFromDatabasePlayerInfo(Player());
            db.saveEveryThing();

            Assert.Empty(Rows());
        }
    }
}
