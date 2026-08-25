using System;
using System.Collections.Generic;
using claims.src.part.structure.war;

namespace claims.src.part.structure.conflict
{
    public class Conflict : Part
    {
        public Conflict(string val, string guid) : base(val, guid)
        {
        }
        public IConflictParty First { get; set; }
        public IConflictParty Second { get; set; }
        public IConflictParty StartedBy { get; set; }
        public ConflictState State { get; set; }
        public List<SelectedWarRange> WarRanges { get; set; } = new List<SelectedWarRange>();
        public List<SelectedWarRange> FirstWarRanges { get; set; } = new List<SelectedWarRange>();
        public List<SelectedWarRange> SecondWarRanges { get; set; } = new List<SelectedWarRange>();
        public int MinimumDaysBetweenBattles { get; set; } = 6;
        public DateTime NextBattleDateStart { get; set; } = DateTime.UnixEpoch;
        public DateTime NextBattleDateEnd { get; set; } = DateTime.UnixEpoch;
        public DateTime LastBattleDateStart { get; set; } = DateTime.UnixEpoch;
        public DateTime LastBattleDateEnd { get; set; } = DateTime.UnixEpoch;
        public long TimeStampStarted { get; set; }
        /// <summary>
        /// The war this one was dragged into, empty for a war declared in its own right. An ally
        /// pulled in by a union fights its own conflict, and that conflict has no reason to outlive
        /// the one it was called to: peace between the main sides ends it too.
        /// </summary>
        public string ParentConflictGuid { get; set; } = "";
        public bool ActiveWarTime { get; set; } = false;
        // War score accumulated across all battle windows of this conflict.
        // When either side reaches config.WAR_SCORE_TO_WIN the conflict ends with that side winning.
        public int FirstScore { get; set; } = 0;
        public int SecondScore { get; set; } = 0;
        // After-action statistics (for the war report on conflict end).
        public int FirstPlotsCaptured { get; set; } = 0;
        public int SecondPlotsCaptured { get; set; } = 0;
        public int FirstKills { get; set; } = 0;
        public int SecondKills { get; set; } = 0;
        public long FirstPillaged { get; set; } = 0;
        public long SecondPillaged { get; set; } = 0;
        public override bool saveToDatabase(bool update = true)
        {
            claims.getModInstance().getDatabaseHandler().saveConflict(this, update);
            return true;
        }
        public bool MinimumDaysHasPassed()
        {
            if (LastBattleDateStart == DateTime.UnixEpoch)
            {
                return true;
            }
            TimeSpan timeSinceLastBattle = DateTime.Now - LastBattleDateStart;
            return timeSinceLastBattle.TotalDays >= MinimumDaysBetweenBattles;
        }
        public int MinutesTillMinPassed()
        {
            if (LastBattleDateStart == DateTime.UnixEpoch)
            {
                return 0;
            }
            TimeSpan timeSinceLastBattle = DateTime.Now - LastBattleDateStart;
            int minutesPassed = (int)timeSinceLastBattle.TotalMinutes;
            return Math.Max(0, MinimumDaysBetweenBattles * 24 * 60 - minutesPassed);
        }
        public void CalculateNextBattleDate()
        {
            DateTime min = DateTime.MaxValue;
            TimeSpan savedDuration = TimeSpan.Zero;
            foreach (var it in WarRanges)
            {
                if (GetNextDateForRange(it, out DateTime dateTime))
                {
                    if(min > dateTime)
                    {
                        min = dateTime;
                        savedDuration = it.Duration;
                    }
                }
            }
            if(min != DateTime.MaxValue)
            {
                NextBattleDateStart = min;
                NextBattleDateEnd = min + savedDuration;
            }
            else
            {
                NextBattleDateStart = DateTime.UnixEpoch;
                NextBattleDateEnd = DateTime.UnixEpoch;
            }
        }
        /// <summary>
        /// Resolves a weekday+time slot into the next real date it falls on. Weekdays are counted on
        /// the schedule clock (see <see cref="WarScheduleHelper"/>), while the result is server-local
        /// time because that is what every war timer compares against.
        /// </summary>
        public bool GetNextDateForRange(SelectedWarRange range, out DateTime dateTime)
        {
            if (!WarScheduleHelper.IsDayAllowed(range.StartDay))
            {
                dateTime = DateTime.UnixEpoch;
                return false;
            }
            DateTime startDate = WarScheduleHelper.NowInZone() + (LastBattleDateEnd == DateTime.UnixEpoch
                                                                ? TimeSpan.Zero
                                                                : TimeSpan.FromDays(MinimumDaysBetweenBattles));
            for (int i = 0; i < 8; i++)
            {
                DateTime candidate = startDate.AddDays(i);
                if (candidate.DayOfWeek == range.StartDay)
                {
                    dateTime = WarScheduleHelper.ToServerLocal(candidate.Date + range.StartTime);
                    if (dateTime < DateTime.Now)
                    {
                        continue;
                    }
                    return true;
                }
            }
            dateTime = DateTime.UnixEpoch;
            return false;
        }
    }
}
