using claims.src;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;

namespace claims.tests
{
    // Cutting battle windows down to the allowed days.
    [Collection("ModStatics")]
    public class WarScheduleTests
    {
        public WarScheduleTests()
        {
            claims.src.claims.config = new Config { MIN_WARRANGE_DURATION_MINUTES = 60 };
        }

        private static SelectedWarRange Range(DayOfWeek day, int startHour, int hours) =>
            new(day, day, TimeSpan.FromHours(startHour), TimeSpan.FromHours(hours), "");

        private static void AllowOnly(params DayOfWeek[] days) =>
            claims.src.claims.config.WAR_ALLOWED_BATTLE_DAYS = days.ToList();

        // WS1: no restriction, nothing changes
        [Fact]
        public void Filter_Should_KeepEverything_When_NoDayIsRestricted()
        {
            var ranges = new List<SelectedWarRange> { Range(DayOfWeek.Tuesday, 20, 3) };

            Assert.Same(ranges, WarScheduleHelper.FilterToAllowedDays(ranges));
        }

        // WS2: a window running past midnight keeps only its allowed part
        [Fact]
        public void Filter_Should_KeepTheSaturdayPart_When_WindowRunsIntoSunday()
        {
            AllowOnly(DayOfWeek.Saturday);

            var result = WarScheduleHelper.FilterToAllowedDays(new() { Range(DayOfWeek.Saturday, 22, 5) });

            SelectedWarRange kept = Assert.Single(result);
            Assert.Equal(DayOfWeek.Saturday, kept.StartDay);
            Assert.Equal(TimeSpan.FromHours(22), kept.StartTime);
            Assert.Equal(TimeSpan.FromHours(2), kept.Duration);
        }

        // WS3: the week wraps from Saturday to Sunday
        [Fact]
        public void Filter_Should_KeepTheSundayPart_When_WindowStartsLateOnSaturday()
        {
            AllowOnly(DayOfWeek.Sunday);

            var result = WarScheduleHelper.FilterToAllowedDays(new() { Range(DayOfWeek.Saturday, 22, 5) });

            SelectedWarRange kept = Assert.Single(result);
            Assert.Equal(DayOfWeek.Sunday, kept.StartDay);
            Assert.Equal(TimeSpan.Zero, kept.StartTime);
            Assert.Equal(TimeSpan.FromHours(3), kept.Duration);
        }

        // WS4: boundary of MIN_WARRANGE_DURATION_MINUTES (60): exactly 60 stays, 59 goes
        [Theory]
        [InlineData(23 * 60, true)]       // Saturday 23:00 -> 60 min left of Saturday
        [InlineData(23 * 60 + 1, false)]  // Saturday 23:01 -> 59 min
        public void Filter_Should_DropPiecesShorterThanMinimum(int startMinute, bool kept)
        {
            AllowOnly(DayOfWeek.Saturday);
            var range = new SelectedWarRange(DayOfWeek.Saturday, DayOfWeek.Sunday,
                TimeSpan.FromMinutes(startMinute), TimeSpan.FromHours(3), "");

            var result = WarScheduleHelper.FilterToAllowedDays(new() { range });

            Assert.Equal(kept ? 1 : 0, result.Count);
        }

        // WS5: a list with repeats is still a restriction (error guessing)
        [Fact]
        public void Filter_Should_StillRestrict_When_AllowedDaysRepeatSevenTimes()
        {
            AllowOnly(Enumerable.Repeat(DayOfWeek.Saturday, 7).ToArray());

            var result = WarScheduleHelper.FilterToAllowedDays(new() { Range(DayOfWeek.Tuesday, 20, 3) });

            Assert.Empty(result);
        }
    }
}
