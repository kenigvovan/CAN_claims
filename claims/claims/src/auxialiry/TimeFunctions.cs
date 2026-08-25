using System;
using Vintagestory.API.Config;

namespace claims.src.auxialiry
{
    public static class TimeFunctions
    {
        public static readonly long secondsInADay = claims.config.MOD_DAY_DURATION_IN_SECONDS;
        public static readonly long secondsInAnHour = 3600;
        static readonly long secondsStartsNewDay = claims.config.HOUR_NEW_DAY_START;
        //public static DateTime start =  new DateTime(1970, 1, 1);
        public static long getEpochSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        public static long getSecondsBeforeNextDayStart()
        {
            long PHTOD = getEpochSeconds() % 86400;
            if (PHTOD < claims.config.HOUR_NEW_DAY_START)
            {
                long timeToDS = claims.config.HOUR_NEW_DAY_START - PHTOD;
                long possibleFullDays = (long)Math.Floor((double)timeToDS / secondsInADay);
                if (possibleFullDays > 0)
                {
                    return timeToDS - possibleFullDays * secondsInADay;
                }
                else
                {
                    return timeToDS;
                }
            }
            else
            {
                long timePassedAfterDS = PHTOD - claims.config.HOUR_NEW_DAY_START;
                long fullDays = (long)Math.Floor((double)timePassedAfterDS/ secondsInADay);
                if(fullDays == 0)
                {
                    return secondsInADay - timePassedAfterDS;
                }
                else
                {
                    return ((fullDays + 1) * secondsInADay) - timePassedAfterDS;
                }
            }          
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
