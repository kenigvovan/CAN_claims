using System;
using System.Collections.Generic;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.perms;

namespace claims.src.gui.playerGui.structures
{
    public class CityInfo
    {
        public string Name { get; set; }
        public string MayorName { get; set; }
        public long TimeStampCreated { get; set; }
        public List<string> PlayersNames;
        public Dictionary<string, int> MaxCountPlots { get; set; } = new();
        public int CountPlots { get; set; }
        public string Prefix { get; set; }
        public string AfterName { get; set; }
        public HashSet<string> CityTitles { get; set; } = new();
        public List<CityRankCellElement> CityRanks { get; set; } = new();
        //public List<RankCellElement> CitizensRanks { get; set; } = new();
        //public HashSet<string> PossibleCityRanks { get; set; }
        public int PlotsColor;
        public double CityBalance { get; set; }
        public double CityDebt { get; set; }
        public double CityDayPayment { get; set; }
        public int CityFee { get; set; }
        public List<string> Criminals = new();
        public PermsHandler PermsHandler { get; set; } = new();
        public List<PrisonCellElement> PrisonCells { get; set; } = new();
        public List<SummonCellElement> SummonCells { get; set; } = new();
        public List<PlotsGroupCellElement> PlotsGroupCells { get; set; } = new();   
        public List<ClientToAllianceInvitationCellElement> ClientToAllianceInvitations { get; set; } = new();
        public List<ClientConflictCellElement> ClientConflictCellElements { get; set; } = new();
        public List<ClientConflictLetterCellElement> ClientConflictLetterCellElements { get; set; } = new();
        public List<ClientWarRangeCellElement> ClientWarRangeCellElements { get; set; } = new();
        public List<ClientTwoWarRangesCellElement> ClientTwoWarRangesCellElement { get; set; } = new();
        public List<ClientUnionLetterCellElement> ClientUnionLetterCellElements { get; set; } = new();
        public CityInfo()
        {
            Name = "";
            //PossibleCityRanks = new HashSet<string>();
            this.ClientWarRangeCellElements = CreateDefaultWarRangeForWeek();
            this.ClientTwoWarRangesCellElement = CreateDefaultTwoWarRangesForWeek();
        }
        public CityInfo(string cityName, string mayorName, long timeStampCreated, List<string> citizens, Dictionary<string, int> maxCountPlots, int countPlots,
            string prefix, string afterName, HashSet<string> cityTitles, int plotsColor, double cityBalance, List<string> criminals)
        {
            Name = cityName;
            MayorName = mayorName;
            TimeStampCreated = timeStampCreated;
            PlayersNames = citizens;
            MaxCountPlots = maxCountPlots;
            CountPlots = countPlots;
            Prefix = prefix;
            AfterName = afterName;
            CityTitles = cityTitles;
            PlotsColor = plotsColor;
            this.CityBalance = cityBalance;
            Criminals = criminals;
            this.ClientWarRangeCellElements = CreateDefaultWarRangeForWeek();
        }
        public static List<ClientWarRangeCellElement> CreateDefaultWarRangeForWeek()
        {
            var warRanges = new List<ClientWarRangeCellElement>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                bool[] defaultRange = new bool[48];
                warRanges.Add(new ClientWarRangeCellElement(day, defaultRange));
            }
            return warRanges;
        }
        public static List<ClientTwoWarRangesCellElement> CreateDefaultTwoWarRangesForWeek()
        {
            var warRanges = new List<ClientTwoWarRangesCellElement>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                bool[] defaultRange = new bool[48];
                bool[] defaultRange2 = new bool[48];
                warRanges.Add(new ClientTwoWarRangesCellElement(day, defaultRange, defaultRange2));
            }
            return warRanges;
        }
    }
}
