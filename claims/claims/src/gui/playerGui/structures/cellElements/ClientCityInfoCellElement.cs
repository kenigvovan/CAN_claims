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
        /// <summary>Declared neutral - marked in the list, the way an alliance is.</summary>
        public bool Neutral { get; set; }
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

        /// <summary>
        /// Copies the city's current state in. Returns true when anything actually changed, which is
        /// what decides whether the row is worth sending: the city list has no dirty flag, the send
        /// timer compares the cached row against the city instead.
        /// </summary>
        public bool UpdateFrom(City city)
        {
            string allianceName = city.Alliance?.GetPartName() ?? "";
            string mayorName = city.getMayor()?.GetPartName() ?? "";
            string name = city.GetPartName();
            int citizens = city.getCityCitizens().Count;
            int plots = city.getCityPlots().Count;

            bool changed = Tier != city.Tier
                || AllianceName != allianceName
                || MayorName != mayorName
                || Name != name
                || InvMsg != city.invMsg
                || TimeStampCreated != city.TimeStampCreated
                || CitizensAmount != citizens
                || Open != city.openCity
                || Neutral != city.IsNeutral
                || ClaimedPlotsAmount != plots;

            if (!changed) return false;

            Tier = city.Tier;
            AllianceName = allianceName;
            MayorName = mayorName;
            Name = name;
            InvMsg = city.invMsg;
            TimeStampCreated = city.TimeStampCreated;
            CitizensAmount = citizens;
            Open = city.openCity;
            Neutral = city.IsNeutral;
            ClaimedPlotsAmount = plots;
            return true;
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
                city.Tier)
            {
                Neutral = city.IsNeutral
            };
        }
    }
}
