using System.Collections.Generic;
using claims.src.auxialiry.innerclaims;
using ProtoBuf;

namespace claims.src.clientMapHandling
{
    [ProtoContract]
    public class SavedPlotInfo
    {
        //if not for sale then it is -1
        [ProtoMember(1)]
        public int price;
        [ProtoMember(2)]
        public bool PvPIsOn;
        [ProtoMember(3)]
        public bool buildFlag;
        [ProtoMember(4)]
        public bool useFlag;
        [ProtoMember(5)]
        public bool attackAnimalsFlag;
        [ProtoMember(6)]
        public string cityName;
        [ProtoMember(7)]
        public string plotName;
        [ProtoMember(8)]
        public string groupName;
        [ProtoMember(9)]
        public List<ClientInnerClaim> clientInnerClaims;
        [ProtoMember(10)]
        public string AllianceGuid;
        /// <summary>The plot's other flags, for the markers over it. Only pvp used to reach the client.</summary>
        [ProtoMember(11)]
        public bool FireIsOn;
        /// <summary>Blasts allowed - the stored meaning of the flag, not the "protection" the GUI switch shows.</summary>
        [ProtoMember(12)]
        public bool BlastIsOn;
        [ProtoMember(13)]
        public bool NoMobSpawn;
        /// <summary>
        /// What each group may do here, for the markers: bit group * 3 + type, groups in
        /// <see cref="RightsGroupOrder"/> order, types use / build / attack. The viewer's own rights
        /// are the three flags above; this is the plot's rule for everyone.
        /// </summary>
        [ProtoMember(14)]
        public int GroupRights;

        /// <summary>Order of the groups in <see cref="GroupRights"/>: friend, citizen, ally, stranger.</summary>
        public static readonly perms.PermGroup[] RightsGroupOrder =
        {
            perms.PermGroup.COMRADE, perms.PermGroup.CITIZEN, perms.PermGroup.ALLY, perms.PermGroup.STRANGER
        };

        public static int EncodeGroupRights(perms.PermsHandler handler)
        {
            int bits = 0;
            if (handler == null) return bits;
            for (int g = 0; g < RightsGroupOrder.Length; g++)
            {
                for (int t = 0; t < 3; t++)
                {
                    if (handler.getPerm(RightsGroupOrder[g], (perms.type.PermType)t)) bits |= 1 << (g * 3 + t);
                }
            }
            return bits;
        }

        public bool GroupMay(int group, int type) => (GroupRights & (1 << (group * 3 + type))) != 0;

        /// <summary>The pieces of sub-plots in this plot; tavern rooms left out.</summary>
        public IEnumerable<ClientInnerClaim> SubPlotPieces()
        {
            if (clientInnerClaims == null) yield break;
            foreach (ClientInnerClaim piece in clientInnerClaims)
            {
                if (piece != null && piece.IsSubPlot) yield return piece;
            }
        }

        /// <summary>The sub-plot piece holding this block, or null.</summary>
        public ClientInnerClaim SubPlotAt(int x, int y, int z)
        {
            foreach (ClientInnerClaim piece in SubPlotPieces())
            {
                if (piece.Contains(x, y, z)) return piece;
            }
            return null;
        }

        public SavedPlotInfo()
        {

        }
        public SavedPlotInfo(int price, bool pvPIsOn, bool buildFlag, bool useFlag, bool attackAnimalsFlag,
            string cityName, string plotName, string groupName, List<ClientInnerClaim> clientInnerClaims, string allianceGuid)
        {
            this.price = price;
            this.PvPIsOn = pvPIsOn;
            this.buildFlag = buildFlag;
            this.useFlag = useFlag;
            this.attackAnimalsFlag = attackAnimalsFlag;
            this.cityName = cityName;
            this.plotName = plotName;
            this.groupName = groupName;
            this.clientInnerClaims = clientInnerClaims;
            AllianceGuid = allianceGuid;
        }
    }
}
