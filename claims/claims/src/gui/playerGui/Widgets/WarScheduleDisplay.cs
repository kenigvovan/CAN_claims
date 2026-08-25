using System;
using claims.src.part.structure.war;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>
    /// Says which clock the war schedule grid is drawn in, and what a slot in it comes to on the
    /// player's own clock.
    ///
    /// The grid carries no time zone of its own: a slot is a weekday plus a time, and the server
    /// reads both on the schedule clock (its own zone, or WAR_SCHEDULE_TIMEZONE). A player in
    /// another zone would otherwise read "Saturday 20:00" as their Saturday evening and turn up for
    /// a battle that already happened - or on the wrong day entirely, since crossing midnight moves
    /// the weekday too.
    /// </summary>
    public static class WarScheduleDisplay
    {
        private const int MinutesPerDay = 24 * 60;
        private const int MinutesPerWeek = 7 * MinutesPerDay;

        /// <summary>How far the player's clock is from the schedule clock, in minutes.</summary>
        public static int OffsetFromScheduleMinutes()
        {
            int local = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
            return local - (claims.config?.WAR_SCHEDULE_UTC_OFFSET_MINUTES ?? local);
        }

        /// <summary>Whether the player is on the same clock as the schedule - then no note is needed.</summary>
        public static bool SameAsPlayer => OffsetFromScheduleMinutes() == 0;

        /// <summary>"UTC+2" / "UTC-5:30" for the clock the grid is drawn in.</summary>
        public static string ScheduleZoneLabel()
        {
            return FormatUtcOffset(claims.config?.WAR_SCHEDULE_UTC_OFFSET_MINUTES ?? 0);
        }

        /// <summary>
        /// Heading over the grid: which clock its slots are in, and - when the grid is showing fewer
        /// than seven days - which days those are, so the missing rows are explained rather than
        /// looking like a bug.
        /// </summary>
        public static string GridHeading()
        {
            string clock = SameAsPlayer
                ? Lang.Get("claims:gui-warrange-clock-same", ScheduleZoneLabel())
                : Lang.Get("claims:gui-warrange-clock-server", ScheduleZoneLabel());

            if (!WarScheduleHelper.HasDayRestriction) return clock;

            return clock + "  |  " + Lang.Get("claims:gui-warrange-days-only",
                WarScheduleHelper.AllowedDaysText());
        }

        /// <summary>
        /// A slot spelled out on both clocks: "Sat 20:00 - 20:30 (server) = Sat 13:00 yours".
        /// Falls back to the plain server-side label when the player is on the same clock.
        /// </summary>
        public static string SlotLabel(DayOfWeek day, int slot, string serverPart)
        {
            if (SameAsPlayer) return DayShort(day) + " " + serverPart;

            int offset = OffsetFromScheduleMinutes();
            int localMinutes = (int)day * MinutesPerDay + slot * 30 + offset;
            localMinutes = ((localMinutes % MinutesPerWeek) + MinutesPerWeek) % MinutesPerWeek;

            var localDay = (DayOfWeek)(localMinutes / MinutesPerDay);
            int minuteOfDay = localMinutes % MinutesPerDay;

            return Lang.Get("claims:gui-warrange-slot-both",
                DayShort(day) + " " + serverPart,
                DayShort(localDay) + " " + string.Format("{0:00}:{1:00}", minuteOfDay / 60, minuteOfDay % 60));
        }

        private static string DayShort(DayOfWeek day)
            => Lang.Get("claims:gui_day_short_" + day.ToString().ToLowerInvariant());

        private static string FormatUtcOffset(int minutes)
        {
            string sign = minutes < 0 ? "-" : "+";
            int abs = Math.Abs(minutes);
            // Half-hour and 45-minute zones exist; only spell the minutes out when there are any.
            return abs % 60 == 0
                ? "UTC" + sign + (abs / 60)
                : "UTC" + sign + (abs / 60) + ":" + string.Format("{0:00}", abs % 60);
        }
    }
}
