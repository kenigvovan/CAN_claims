using System;
using System.Collections.Generic;
using claims.src.part.structure.conflict;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientConflictCellElement
    {
        public string Name { get; set; }
        public string FirstPartyName { get; set; }
        public string FirstPartyGuid { get; set; }
        public WarTargetType FirstPartyType { get; set; }
        public string SecondPartyName { get; set; }
        public string SecondPartyGuid { get; set; }
        public WarTargetType SecondPartyType { get; set; }
        public string StartedByPartyName { get; set; }
        public ConflictState State { get; set; }
        public string Guid { get; set; }
        public int MinimumDaysBetweenBattles { get; set; } = 6;
        public DateTime LastBattleDateStart { get; set; } = DateTime.UnixEpoch;
        public DateTime LastBattleDateEnd { get; set; } = DateTime.UnixEpoch;
        public DateTime NextBattleDateStart { get; set; } = DateTime.UnixEpoch;
        public DateTime NextBattleDateEnd { get; set; } = DateTime.UnixEpoch;
        public List<SelectedWarRange> WarRanges { get; set; } = new List<SelectedWarRange>();
        public List<SelectedWarRange> FirstWarRanges { get; set; } = new List<SelectedWarRange>();
        public List<SelectedWarRange> SecondWarRanges { get; set; } = new List<SelectedWarRange>();
        public long TimeStampCreated { get; set; }
        public bool ActiveWarTime { get; set; }
        public ClientConflictCellElement(string name, string firstAllianceName, string firstPartyGuid, WarTargetType firstPartyType,
                                         string secondAllianceName, string secondPartyGuid, WarTargetType secondPartyType,
                                         string startedByAllianceName, ConflictState state, string guid, int minimumDaysBetweenBattles,
                                         DateTime lastBattleDateStart, DateTime lastBattleDateEnd, DateTime nextBattleDateStart, DateTime nextBattleDateEnd, List<SelectedWarRange> warRanges, List<SelectedWarRange> firtstWarRanges,
                                         List<SelectedWarRange> secondWarRanges, long timeStampCreated, bool activeWarTime = false)
        {
            Name = name;
            FirstPartyName = firstAllianceName;
            FirstPartyGuid = firstPartyGuid;
            FirstPartyType = firstPartyType;
            SecondPartyName = secondAllianceName;
            SecondPartyGuid = secondPartyGuid;
            SecondPartyType = secondPartyType;
            StartedByPartyName = startedByAllianceName;
            State = state;
            Guid = guid;
            WarRanges = warRanges ?? new List<SelectedWarRange>();
            FirstWarRanges = firtstWarRanges ?? new List<SelectedWarRange>();
            SecondWarRanges = secondWarRanges ?? new List<SelectedWarRange>();
            TimeStampCreated = timeStampCreated;
            MinimumDaysBetweenBattles = minimumDaysBetweenBattles;
            LastBattleDateStart = lastBattleDateStart;
            LastBattleDateEnd = lastBattleDateEnd;
            NextBattleDateStart = nextBattleDateStart;
            NextBattleDateEnd = nextBattleDateEnd;
            ActiveWarTime = activeWarTime;
        }
    }
}
