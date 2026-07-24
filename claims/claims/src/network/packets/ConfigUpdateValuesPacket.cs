using System.Collections.Generic;
using claims.src.economy;
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
        public System.Collections.Generic.OrderedDictionary<decimal, string> COINS_VALUES_TO_CODE;
        [ProtoMember(4)]
        public System.Collections.Generic.OrderedDictionary<int, decimal> ID_TO_COINS_VALUES;
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
        [ProtoMember(18)]
        public bool SHOW_BALANCE_HUD_DEFAULT;

        // Plot type costs (feed PlotInfo.dictPlotTypes on the client)
        [ProtoMember(19)]
        public double DEFAULT_PLOT_COST;
        [ProtoMember(20)]
        public double TOURNAMENT_PLOT_COST;
        [ProtoMember(21)]
        public double CAMP_PLOT_COST;
        [ProtoMember(22)]
        public double TEMPLE_PLOT_COST;
        [ProtoMember(23)]
        public double FARM_PLOT_COST;
        [ProtoMember(24)]
        public double SUMMON_PLOT_COST;
        [ProtoMember(25)]
        public double EMBASSY_PLOT_COST;
        [ProtoMember(26)]
        public double TAVERN_PLOT_COST;
        [ProtoMember(27)]
        public double MAIN_CITYPLOT_COST;
        [ProtoMember(28)]
        public double PRISON_PLOT_COST;

        // Extra plot costs
        [ProtoMember(29)]
        public double OUTPOST_PLOT_COST;
        [ProtoMember(30)]
        public double EXTRA_PLOT_COST;
        [ProtoMember(31)]
        public double PLOT_NO_PVP_FLAG_COST;

        // Ransom costs
        [ProtoMember(32)]
        public double RANSOM_FOR_NO_CITIZEN;
        [ProtoMember(33)]
        public double RANSOM_FOR_CITIZEN;
        [ProtoMember(34)]
        public double RANSOM_FOR_MAYOR;
        [ProtoMember(35)]
        public double RANSOM_FOR_LEADER;
        [ProtoMember(36)]
        public double RANSOM_FOR_CHIEF;

        // Alliance economy
        [ProtoMember(37)]
        public double ALLIANCE_RENAME_COST;
        [ProtoMember(38)]
        public double ALLIANCE_BASE_CARE;
        [ProtoMember(39)]
        public double ALLIANCE_MAX_FEE;
        [ProtoMember(40)]
        public double NEUTRAL_ALLANCE_PAYMENT;

        // City limits
        [ProtoMember(41)]
        public double MAX_CITY_FEE;
        [ProtoMember(42)]
        public double CITY_MAX_DEBT;

        // War range grid (client maps server ranges into slots with this)
        [ProtoMember(43)]
        public int MIN_RANGE_CELL_DURATION_MINUTES;

        // Full coin denomination list. Replaces COINS_VALUES_TO_CODE for the
        // prices GUI: preserves every configured coin, including coins that
        // share a value but differ by attributes.
        [ProtoMember(44)]
        public List<CoinDenominationData> COIN_DENOMINATIONS;

        // Plot types the host disabled; client hides them from the change-type GUI.
        [ProtoMember(45)]
        public HashSet<string> DISABLED_PLOT_TYPES = new HashSet<string>();

        // World grid geometry — the client must use the server's values, its local
        // config may disagree. 0 means "sent by an older server": keep local value.
        [ProtoMember(46)]
        public int PLOT_SIZE;
        [ProtoMember(47)]
        public int ZONE_PLOTS_LENGTH;
    }
}
