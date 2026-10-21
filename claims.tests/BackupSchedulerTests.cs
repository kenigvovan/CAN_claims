using claims.src;
using claims.src.timers;

namespace claims.tests
{
    // When backups are taken.
    [Collection("ModStatics")]
    public class BackupSchedulerTests
    {
        // BK-O2: one bad time in the config must not stop the scheduler
        [Fact]
        public void Tick_Should_NotThrow_When_ConfiguredTimeIsInvalid()
        {
            claims.src.claims.config = new Config { DAYTIME_MAKE_BACKUP = new HashSet<string> { "25:00" } };

            Exception? error = Record.Exception(() => new BackupScheduler(null!).Tick());

            Assert.True(error == null, "an invalid backup time breaks the scheduler: " + error?.Message);
        }

        [Theory]
        [InlineData("6:00", 6, 0)]
        [InlineData("00:00", 0, 0)]
        [InlineData("23:59", 23, 59)]
        public void Time_Accepted(string value, int hour, int minute)
        {
            Assert.True(BackupScheduler.TryParseTime(value, out int h, out int m));
            Assert.Equal((hour, minute), (h, m));
        }

        [Theory]
        [InlineData("25:00")]
        [InlineData("12:60")]
        [InlineData("-1:00")]
        [InlineData("12")]
        [InlineData("12:00:00")]
        [InlineData("noon")]
        [InlineData("")]
        [InlineData(null)]
        public void Time_Refused(string? value)
        {
            Assert.False(BackupScheduler.TryParseTime(value!, out _, out _));
        }

        [Fact]
        public void LatestSlot_IsTheLastPassedOneToday()
        {
            var now = new DateTime(2026, 10, 21, 13, 0, 0);

            Assert.Equal(new DateTime(2026, 10, 21, 12, 0, 0),
                BackupScheduler.LatestSlot(now, new[] { (6, 0), (12, 0), (18, 0) }));
        }

        [Fact]
        public void LatestSlot_JustAfterMidnight_IsYesterdaysLateSlot()
        {
            // 23:59 checked a few seconds after midnight used to be skipped.
            var now = new DateTime(2026, 10, 22, 0, 0, 5);

            Assert.Equal(new DateTime(2026, 10, 21, 23, 59, 0),
                BackupScheduler.LatestSlot(now, new[] { (23, 59) }));
        }

        [Fact]
        public void LatestSlot_NoSlots_IsNone()
        {
            Assert.Null(BackupScheduler.LatestSlot(DateTime.Now, Array.Empty<(int, int)>()));
        }

        [Fact]
        public void Names_SortByTime()
        {
            string earlier = BackupScheduler.ScheduledBackupName(new DateTime(2026, 9, 30, 23, 0, 0));
            string later = BackupScheduler.ScheduledBackupName(new DateTime(2026, 10, 1, 6, 0, 0));

            Assert.True(string.CompareOrdinal(earlier, later) < 0, earlier + " sorts after " + later);
        }

        [Fact]
        public void Prune_KeepsTheNewest_AndTouchesOnlyScheduledBackups()
        {
            var names = new List<string>
            {
                BackupScheduler.ScheduledBackupName(new DateTime(2026, 10, 3, 0, 0, 0)),
                BackupScheduler.ScheduledBackupName(new DateTime(2026, 10, 1, 0, 0, 0)),
                BackupScheduler.ScheduledBackupName(new DateTime(2026, 10, 2, 0, 0, 0)),
                "backup_manually_claims.db",
                "claims_notadate.db",
                "35_12_06_01_10_2026", // the old, unsortable name
            };

            List<string> pruned = BackupScheduler.FilesToPrune(names, 2);

            Assert.Equal(new[] { BackupScheduler.ScheduledBackupName(new DateTime(2026, 10, 1, 0, 0, 0)) }, pruned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Prune_NothingWhenKeepingAllOrUnderTheLimit(int keep)
        {
            var names = new[] { BackupScheduler.ScheduledBackupName(new DateTime(2026, 10, 1, 0, 0, 0)) };

            Assert.Empty(BackupScheduler.FilesToPrune(names, keep));
        }
    }
}
