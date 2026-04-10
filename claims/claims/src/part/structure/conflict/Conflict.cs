using System;
using System.Collections.Generic;

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
        public bool ActiveWarTime { get; set; } = false;
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
            }
        }
        public bool GetNextDateForRange(SelectedWarRange range, out DateTime dateTime)
        {
            DateTime startDate = DateTime.Now + (LastBattleDateEnd == DateTime.UnixEpoch 
                                                                ? TimeSpan.Zero 
                                                                : TimeSpan.FromDays(MinimumDaysBetweenBattles));
            if (LastBattleDateStart == DateTime.UnixEpoch)
            {
                for (int i = 0; i < 8; i++)
                {
                    DateTime candidate = startDate.AddDays(i);
                    if (candidate.DayOfWeek == range.StartDay)
                    {
                        dateTime = candidate.Date + range.StartTime;
                        if(dateTime < DateTime.Now)
                        {
                            continue;
                        }
                        return true;
                    }
                }
            }
            dateTime = DateTime.UnixEpoch;
            return false;
        }
    }
}
