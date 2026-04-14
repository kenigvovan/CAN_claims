using System.Collections.Generic;
using System.Linq;
using claims.src.part.structure;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientAllianceInfoCellElement
    {
        public string Name { get; set; }
        public string LeaderName { get; set; }
        public string Guid { get; set; }
        public int CitiesCount { get; set; }
        public List<string> CitiesNames { get; set; }
        public long TimeStampCreated { get; set; }
        public bool Neutral { get; set; }
        public ClientAllianceInfoCellElement(string name, string leaderName, string guid,
            int citiesCount, List<string> citiesNames, long timeStampCreated, bool neutral)
        {
            Name = name;
            LeaderName = leaderName;
            Guid = guid;
            CitiesCount = citiesCount;
            CitiesNames = citiesNames;
            TimeStampCreated = timeStampCreated;
            Neutral = neutral;
        }

        public void UpdateFrom(Alliance alliance)
        {
            Name = alliance.GetPartName();
            LeaderName = alliance.Leader?.GetPartName() ?? "";
            CitiesCount = alliance.Cities.Count;
            CitiesNames = alliance.Cities.Select(c => c.GetPartName()).ToList();
            TimeStampCreated = alliance.TimeStampCreated;
            Neutral = alliance.Neutral;
        }

        public static ClientAllianceInfoCellElement FromAlliance(Alliance alliance)
        {
            return new ClientAllianceInfoCellElement(
                alliance.GetPartName(),
                alliance.Leader?.GetPartName() ?? "",
                alliance.Guid,
                alliance.Cities.Count,
                alliance.Cities.Select(c => c.GetPartName()).ToList(),
                alliance.TimeStampCreated,
                alliance.Neutral);
        }
    }
}
