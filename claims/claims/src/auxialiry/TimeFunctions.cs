using System;
using Vintagestory.API.Config;

namespace claims.src.auxialiry
{
    public static class TimeFunctions
    {
        public static readonly long secondsInAnHour = 3600;
        //public static DateTime start =  new DateTime(1970, 1, 1);
        public static long getEpochSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static long DayLength => claims.config.MOD_DAY_DURATION_IN_SECONDS > 0
            ? Math.Max(60, claims.config.MOD_DAY_DURATION_IN_SECONDS)
            : 86400;

        // Cycles are counted from the Unix epoch shifted by HOUR_NEW_DAY_START, not from UTC midnight.
        public static long GetCurrentDayStart(long now)
        {
            long day = DayLength;
            long sinceAnchor = now - claims.config.HOUR_NEW_DAY_START;
            long cycles = sinceAnchor >= 0 ? sinceAnchor / day : (sinceAnchor - day + 1) / day;
            return claims.config.HOUR_NEW_DAY_START + cycles * day;
        }
        public static long getSecondsBeforeNextDayStart()
        {
            long now = getEpochSeconds();
            return GetCurrentDayStart(now) + DayLength - now;
        }
        public static long getSecondsBeforeNextHourStart()
        {
            return secondsInAnHour - (getEpochSeconds() % secondsInAnHour);
        }
        public static string getDateFromEpochSeconds(long date)
        {
            DateTimeOffset dateTimeOffSet = DateTimeOffset.FromUnixTimeSeconds(date);
            DateTime datTime = dateTimeOffSet.DateTime;
            return datTime.ToString("dd/MM/yyyy");
        }
        public static string getDateFromEpochSecondsWithHoursMinutes(long date, bool toLocal = true)
        {
            DateTimeOffset dateTimeOffSet = DateTimeOffset.FromUnixTimeSeconds(date);
            if (toLocal)
            {
                dateTimeOffSet = dateTimeOffSet.ToLocalTime();
            }
            DateTime datTime = dateTimeOffSet.DateTime;
            return datTime.ToString("dd/MM/yyyy HH:mm");
        }
        public static string getHourFromEpochSeconds(long date)
        {
            DateTimeOffset dateTimeOffSet = DateTimeOffset.FromUnixTimeSeconds(date);
            DateTime datTime = dateTimeOffSet.DateTime;
            return datTime.ToString("T");
        }

        /// <summary>
        /// Unix seconds of a battle date, 0 for "no such battle". A plain (DateTimeOffset) cast
        /// throws for DateTime.MinValue in any zone east of UTC - and MinValue is what the CONFLICTS
        /// date columns hold for rows written before those columns had a value.
        /// </summary>
        public static long ToEpochSecondsSafe(DateTime date)
        {
            return date <= DateTime.UnixEpoch ? 0 : ((DateTimeOffset)date.ToUniversalTime()).ToUnixTimeSeconds();
        }

        /// <summary>A battle date for the GUI: the date itself, or a dash when there was none.</summary>
        public static string FormatBattleDate(DateTime date)
        {
            return date <= DateTime.UnixEpoch
                ? Lang.Get("claims:gui_battle_date_none")
                : getDateFromEpochSecondsWithHoursMinutes(ToEpochSecondsSafe(date));
        }
    }
}
