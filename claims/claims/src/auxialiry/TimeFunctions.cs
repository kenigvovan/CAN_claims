using System;

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
    }
}
