using claims.src;
using claims.src.auxialiry;

namespace claims.tests
{
    // When a billing day starts. Cases: DC1.
    [Collection("ModStatics")]
    public class TimeFunctionsTests
    {
        private const long Day = 86400;
        private const long Offset = 43200; // 12:00 UTC

        public TimeFunctionsTests()
        {
            claims.src.claims.config = new Config
            {
                MOD_DAY_DURATION_IN_SECONDS = Day,
                HOUR_NEW_DAY_START = (int)Offset
            };
        }

        // DC1
        [Theory]
        [InlineData(Offset - 1, Offset - Day)]       // a second before the day starts
        [InlineData(Offset, Offset)]                 // the first second of the day
        [InlineData(Offset + Day - 1, Offset)]       // the last second of the day
        [InlineData(Offset + Day, Offset + Day)]     // the next day
        [InlineData(0L, Offset - Day)]              // before the offset: the negative branch
        public void DayStart_Should_BeTheLastOffsetPlusWholeDays(long now, long expected)
        {
            Assert.Equal(expected, TimeFunctions.GetCurrentDayStart(now));
        }

        // DC1: a day is never shorter than 60 s
        [Fact]
        public void DayLength_Should_BeAtLeastAMinute()
        {
            claims.src.claims.config.MOD_DAY_DURATION_IN_SECONDS = 30;

            Assert.Equal(60, TimeFunctions.DayLength);
        }
    }
}
