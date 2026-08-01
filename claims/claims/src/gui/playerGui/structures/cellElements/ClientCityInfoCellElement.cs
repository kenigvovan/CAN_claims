using claims.src.part;
using claims.src.part.structure;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientCityInfoCellElement
    {
        public int CitizensAmount { get; set; }
        public string MayorName { get; set; }
        public int ClaimedPlotsAmount { get; set; }
        public string AllianceName { get; set; }
        public long TimeStampCreated { get; set; }
        public string Name { get; set; }
        public bool Open { get; set; }
        public string InvMsg { get;set; }
        public string Guid { get; set; }
        /// <summary>Village or full city; drives the label in the settlement list.</summary>
        public CityTier Tier { get; set; } = CityTier.CITY;
        public ClientCityInfoCellElement(int citizensAmount, string mayorName, int claimedPlotsAmount,
                                         string allianceName, long timeStampCreated, string name, bool open, string invMsg, string guid,
                                         CityTier tier = CityTier.CITY)
        {
            Tier = tier;
            CitizensAmount = citizensAmount;
            MayorName = mayorName;
            ClaimedPlotsAmount = claimedPlotsAmount;
            AllianceName = allianceName;
            TimeStampCreated = timeStampCreated;
            Name = name;
            Open = open;
            InvMsg = invMsg;
            Guid = guid;
        }

        public void UpdateFrom(City city)
        {
            Tier = city.Tier;
            AllianceName = city.Alliance?.GetPartName() ?? "";
            MayorName = city.getMayor()?.GetPartName() ?? "";
            Name = city.GetPartName();
            InvMsg = city.invMsg;
            TimeStampCreated = city.TimeStampCreated;
            CitizensAmount = city.getCityCitizens().Count;
            Open = city.openCity;
            ClaimedPlotsAmount = city.getCityPlots().Count;
        }

        public static ClientCityInfoCellElement FromCity(City city)
        {
            return new ClientCityInfoCellElement(
                city.getCityCitizens().Count,
                city.getMayor()?.GetPartName() ?? "",
                city.getCityPlots().Count,
                city.Alliance?.GetPartName() ?? "",
                city.TimeStampCreated,
                city.GetPartName(),
                city.openCity,
                city.invMsg,
                city.Guid,
                city.Tier);
        }
    }
}
