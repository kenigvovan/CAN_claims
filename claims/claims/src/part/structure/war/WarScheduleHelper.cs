using System;
using System.Collections.Generic;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// The weekly battle schedule: which days of the week battles may fall on, and which time zone
    /// those days are counted in.
    ///
    /// A <see cref="SelectedWarRange"/> is a day-of-week plus a start time, and everything that
    /// resolves one into a real date goes through <see cref="DateTime.Now"/> - the server machine's
    /// own clock. That makes "Saturday" mean whatever the host's OS says, which silently changes if
    /// the server moves to a host in another zone. WAR_SCHEDULE_TIMEZONE pins it down; left empty it
    /// stays the server's own zone, so existing worlds keep behaving exactly as before.
    /// </summary>
    public static class WarScheduleHelper
    {
        private const int MinutesPerDay = 24 * 60;
        private const int MinutesPerWeek = 7 * MinutesPerDay;

        private static string cachedZoneId;
        private static TimeZoneInfo cachedZone;

        /// <summary>
        /// Zone the weekly grid is read in. Falls back to the server's own zone when the config is
        /// empty or names a zone this machine does not know.
        /// </summary>
        public static TimeZoneInfo Zone
        {
            get
            {
                string id = claims.config?.WAR_SCHEDULE_TIMEZONE ?? "";
                if (cachedZone != null && cachedZoneId == id) return cachedZone;

                cachedZoneId = id;
                cachedZone = TimeZoneInfo.Local;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    try
                    {
                        cachedZone = TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
                    }
                    catch (Exception)
                    {
                        // Windows and Linux use different zone ids, and a typo here would otherwise
                        // move every battle by hours without a word. Say so and keep the local zone.
                        claims.sapi?.World?.Logger?.Warning(
                            "[claims] WAR_SCHEDULE_TIMEZONE '{0}' is not a time zone this machine knows, using the server zone instead.", id);
                    }
                }
                return cachedZone;
            }
        }

        /// <summary>Current time expressed in the schedule zone.</summary>
        public static DateTime NowInZone()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, Zone);
        }

        /// <summary>
        /// A wall-clock moment in the schedule zone, turned back into server-local time - the clock
        /// every war timer compares against.
        /// </summary>
        public static DateTime ToServerLocal(DateTime zoneTime)
        {
            DateTime unspecified = DateTime.SpecifyKind(zoneTime, DateTimeKind.Unspecified);
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone), TimeZoneInfo.Local);
            }
            catch (ArgumentException)
            {
                // The hour that daylight saving skips does not exist in this zone; take the next one.
                return ToServerLocal(unspecified.AddHours(1));
            }
        }

        /// <summary>
        /// How far the schedule clock currently sits from UTC, in minutes. Read now rather than
        /// stored, so it follows daylight saving; it is re-sent with every config packet.
        /// </summary>
        public static int CurrentUtcOffsetMinutes()
        {
            return (int)Zone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
        }

        /// <summary>Whether the admin restricted battles to certain days at all.</summary>
        public static bool HasDayRestriction
        {
            get
            {
                var days = claims.config?.WAR_ALLOWED_BATTLE_DAYS;
                return days != null && days.Count > 0 && days.Count < 7;
            }
        }

        public static bool IsDayAllowed(DayOfWeek day)
        {
            if (!HasDayRestriction) return true;
            return claims.config.WAR_ALLOWED_BATTLE_DAYS.Contains(day);
        }

        /// <summary>Allowed days as a readable list, for chat messages and the GUI.</summary>
        public static string AllowedDaysText()
        {
            if (!HasDayRestriction) return "";
            var names = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                var day = (DayOfWeek)i;
                if (IsDayAllowed(day)) names.Add(Lang.Get("claims:gui_day_short_" + day.ToString().ToLower()));
            }
            return string.Join(", ", names);
        }

        /// <summary>
        /// Cuts everything outside the allowed days out of a schedule, splitting ranges that run
        /// across the boundary instead of dropping them whole - a Saturday 22:00 window five hours
        /// long stays as its Saturday part rather than vanishing. Pieces shorter than
        /// MIN_WARRANGE_DURATION_MINUTES are dropped: a ten-minute battle is not worth showing up for.
        /// </summary>
        public static List<SelectedWarRange> FilterToAllowedDays(List<SelectedWarRange> ranges)
        {
            if (ranges == null) return new List<SelectedWarRange>();
            if (!HasDayRestriction) return ranges;

            int minDuration = Math.Max(1, claims.config.MIN_WARRANGE_DURATION_MINUTES);
            var result = new List<SelectedWarRange>();

            foreach (var range in ranges)
            {
                int cursor = (int)range.StartDay * MinutesPerDay + (int)range.StartTime.TotalMinutes;
                int remaining = (int)range.Duration.TotalMinutes;
                if (remaining <= 0) continue;

                int segmentStart = -1;
                int segmentLength = 0;

                while (remaining > 0)
                {
                    int normalized = ((cursor % MinutesPerWeek) + MinutesPerWeek) % MinutesPerWeek;
                    int chunk = Math.Min(MinutesPerDay - normalized % MinutesPerDay, remaining);

                    if (IsDayAllowed((DayOfWeek)(normalized / MinutesPerDay)))
                    {
                        if (segmentStart < 0) segmentStart = normalized;
                        segmentLength += chunk;
                    }
                    else
                    {
                        Emit(result, range, segmentStart, segmentLength, minDuration);
                        segmentStart = -1;
                        segmentLength = 0;
                    }

                    cursor += chunk;
                    remaining -= chunk;
                }
                Emit(result, range, segmentStart, segmentLength, minDuration);
            }
            return result;
        }

        /// <summary>
        /// Re-applies the restriction to schedules that were agreed before it existed. Without this
        /// an admin turning on weekend-only fighting would change nothing for wars already running -
        /// their Tuesday windows would keep firing. A conflict left with no usable window at all goes
        /// back to CREATED so the two sides negotiate again; a battle already under way is left to
        /// finish. Returns how many conflicts were altered.
        /// </summary>
        public static int ReapplyToExistingConflicts()
        {
            int changed = 0;
            foreach (var conflict in claims.dataStorage.conflicts)
            {
                if (conflict.ActiveWarTime) continue;

                int before = conflict.WarRanges.Count + conflict.FirstWarRanges.Count + conflict.SecondWarRanges.Count;
                conflict.WarRanges = FilterToAllowedDays(conflict.WarRanges);
                conflict.FirstWarRanges = FilterToAllowedDays(conflict.FirstWarRanges);
                conflict.SecondWarRanges = FilterToAllowedDays(conflict.SecondWarRanges);
                int after = conflict.WarRanges.Count + conflict.FirstWarRanges.Count + conflict.SecondWarRanges.Count;

                if (conflict.WarRanges.Count == 0 && conflict.State == ConflictState.ACTIVE)
                {
                    conflict.State = ConflictState.CREATED;
                    conflict.NextBattleDateStart = DateTime.UnixEpoch;
                    conflict.NextBattleDateEnd = DateTime.UnixEpoch;
                }
                else
                {
                    conflict.CalculateNextBattleDate();
                }

                if (before != after) changed++;
                conflict.saveToDatabase();
            }
            return changed;
        }

        private static void Emit(List<SelectedWarRange> into, SelectedWarRange source, int start, int length, int minDuration)
        {
            if (start < 0 || length < minDuration) return;
            int end = (start + length) % MinutesPerWeek;
            into.Add(new SelectedWarRange(
                (DayOfWeek)(start / MinutesPerDay),
                (DayOfWeek)(end / MinutesPerDay),
                TimeSpan.FromMinutes(start % MinutesPerDay),
                TimeSpan.FromMinutes(length),
                source.SuggestedAllianceGuid));
        }
    }
}
