using System.Collections.Generic;
using claims.src.rights;
using ProtoBuf;
using Vintagestory.API.Datastructures;
using static claims.src.Config;

namespace claims.src.network.packets
{
    [ProtoContract]
    public class ConfigUpdateValuesPacket
    {
        [ProtoMember(1)]
        public double NewCityCost;
        [ProtoMember(2)]
        public double NewPlotClaimCost;
        [ProtoMember(3)]
        public System.Collections.Generic.OrderedDictionary<double, string> COINS_VALUES_TO_CODE;
        [ProtoMember(4)]
        public System.Collections.Generic.OrderedDictionary<int, double> ID_TO_COINS_VALUES;
        [ProtoMember(5)]
        public double CITY_NAME_CHANGE_COST;
        [ProtoMember(6)]
        public double CITY_BASE_CARE;
        [ProtoMember(7)]
        public int[] PLOTS_COLORS;
        [ProtoMember(8)]
        public bool NO_ACCESS_WITH_FOR_NOT_CLAIMED_AREA;
        [ProtoMember(9)]
        public double NewAllianceCost;
        [ProtoMember(10)]
        public double SummonPayment;
        [ProtoMember(11)]
        public HashSet<int> POSSIBLE_USED_BLOCKS_IN_WILDERNESS = new HashSet<int>();
        [ProtoMember(12)]
        public HashSet<int> POSSIBLE_BUILD_ITEMS_IN_WILDERNESS = new HashSet<int>();
        [ProtoMember(13)]
        public HashSet<string> ALWAYS_ACCESS_BLOCKS = new HashSet<string>();
        [ProtoMember(14)]
        public HashSet<EnumPlayerPermissions> AVAILABLE_CITY_PERMISSIONS;
        [ProtoMember(15)]
        public string SELECTED_ECONOMY_HANDLER;
        [ProtoMember(16)]
        public bool GUI_SHOW_DEBT;
        [ProtoMember(17)]
        public CITY_AREA_VISIBILITY CITY_AREA_VISIBILITY_STATE;
    }
}
