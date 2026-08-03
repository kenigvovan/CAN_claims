using System.Collections.Generic;

namespace claims.src.gui.playerGui.structures
{
    public class AllianceInfo
    {
        public string Name { get; set; }
        public string LeaderName { get; set; }
        public long TimeStampCreated { get; set; }
        public string Prefix { get; set; }
        public List<string> Cities { get; set; } = new List<string>();
        public double Balance { get; set; }
        public string Guid { get; set; }
        public List<string> Hostiles { get; set; } = new();
        public List<string> Allies { get; set; } = new();
        /// <summary>Announced union breaks: ally name -> unix seconds when the union actually ends.</summary>
        public Dictionary<string, long> PendingUnionBreaks { get; set; } = new();
        /// <summary>Coat of arms as an EmblemHandler layer string; empty when the alliance has none.</summary>
        public string Emblem { get; set; } = "";
        public AllianceInfo(string name, string leaderName, long timeStampCreated, string prefix, List<string> cities, double balance, string guid, List<string> allies)
        {
            Name = name;
            LeaderName = leaderName;
            TimeStampCreated = timeStampCreated;
            Prefix = prefix;
            Cities = cities;
            Balance = balance;
            Guid = guid;
            Allies = allies;
        }
    }
}
