using System.Collections.Generic;
using claims.src.part;
using claims.src.perms;

namespace claims.src
{
    public class CityPlotsGroup: Part
    {
        public List<PlayerInfo> PlayersList {  get; set; }
        //USED ONLY CITIZEN
        public PermsHandler PermsHandler {  get; set; }
        /// <summary>What members are charged daily right now. Only this is ever taken off anyone.</summary>
        public double PlotsGroupFee {  get; set; }

        /// <summary>
        /// A raise the mayor has announced but nobody is paying yet, -1 when there is none.
        ///
        /// A group holds other people's buildings, so a mayor who could raise the fee at will could
        /// name any price once the houses were up. A raise therefore only ever applies to members who
        /// said yes to it: it waits <see cref="PendingFeeAt"/> at the old price, and when the wait is
        /// over the members who agreed move to the new one and the rest leave the group. Nobody is
        /// charged a rate they never accepted - including a member who was offline throughout.
        /// </summary>
        public double PendingFee { get; set; } = -1;

        /// <summary>Unix seconds when the announced raise is settled. 0 when nothing is pending.</summary>
        public long PendingFeeAt { get; set; }

        /// <summary>Guids of members who accepted the pending raise.</summary>
        public HashSet<string> PendingFeeAccepted { get; } = new HashSet<string>();

        public City City {  get; set; }
        public CityPlotsGroup(string val, string guid) : base(val, guid)
        {
            PlayersList = new List<PlayerInfo>();
            PermsHandler = new PermsHandler();
            PlotsGroupFee = 0;
        }
        public bool HasFee()
        {
            return PlotsGroupFee > 0;
        }

        /// <summary>Whether a raise is announced and still waiting for its day.</summary>
        public bool HasPendingFee => PendingFee >= 0 && PendingFeeAt > 0;

        /// <summary>Drops the announced raise, leaving the fee members pay untouched.</summary>
        public void ClearPendingFee()
        {
            PendingFee = -1;
            PendingFeeAt = 0;
            PendingFeeAccepted.Clear();
        }
        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().saveCityPlotGroup(this, update);
        }
    }
}
