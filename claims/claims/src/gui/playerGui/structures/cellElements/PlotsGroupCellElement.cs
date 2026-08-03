using System.Collections.Generic;
using claims.src.perms;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class PlotsGroupCellElement
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public string CityName { get; set; }
        public List<string> PlayersNames { get; set; }
        public PermsHandler PermsHandler { get; set; }
        public double PlotsGroupFee { get; set; }

        /// <summary>How many plots the group holds. Its card is the only place this is visible.</summary>
        public int PlotsCount { get; set; }

        /// <summary>An announced raise nobody pays yet, -1 when there is none.</summary>
        public double PendingFee { get; set; } = -1;
        /// <summary>Unix seconds when the announced raise is settled. 0 when nothing is pending.</summary>
        public long PendingFeeAt { get; set; }
        /// <summary>
        /// Guids of the members who accepted the raise. A guid rather than a name: the page has to
        /// tell whether the player reading it has accepted, and it knows its own uid, not its name
        /// as the server spells it.
        /// </summary>
        public List<string> PendingFeeAccepted { get; set; } = new List<string>();

        public PlotsGroupCellElement(string guid, string name, string cityName, List<string> playersNames, PermsHandler permsHandler, double plotsGroupFee)
        {
            Guid = guid;
            Name = name;
            CityName = cityName;
            PlayersNames = playersNames;
            PermsHandler = permsHandler;
            PlotsGroupFee = plotsGroupFee;
        }

        public bool HasPendingFee => PendingFee >= 0 && PendingFeeAt > 0;

        public bool AcceptedBy(string playerUid) =>
            PendingFeeAccepted != null && PendingFeeAccepted.Contains(playerUid);
    }
}
