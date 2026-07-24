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

        // War settings — editable in-game via the admin War tab. Synced so the GUI
        // can render current values and /cadmin setcfg edits reach every client.
        [ProtoMember(48)]
        public bool WAR_FLAG_DEFENDER_INTERRUPT_ENABLED;
        [ProtoMember(49)]
        public int WAR_FLAG_DEFENDER_RADIUS;
        [ProtoMember(50)]
        public double WAR_FLAG_REGRESS_MULTIPLIER;

        [ProtoMember(51)]
        public bool WAR_SCORE_ENABLED;
        [ProtoMember(52)]
        public int WAR_SCORE_TO_WIN;
        [ProtoMember(53)]
        public int WAR_SCORE_PER_PLOT_CAPTURE;
        [ProtoMember(54)]
        public int WAR_SCORE_PER_KILL;
        [ProtoMember(55)]
        public int WAR_SCORE_PER_HOLD_TICK;
        [ProtoMember(56)]
        public int WAR_SCORE_HOLD_TICK_SECONDS;

        [ProtoMember(57)]
        public bool WAR_PILLAGE_ENABLED;
        [ProtoMember(58)]
        public double WAR_PILLAGE_PERCENT;

        [ProtoMember(59)]
        public bool WAR_CAMP_ENABLED;
        [ProtoMember(60)]
        public int WAR_MAX_CAMPS_PER_CONFLICT;

        [ProtoMember(61)]
        public bool WAR_RESPAWN_SAFEZONE_ENABLED;
        [ProtoMember(62)]
        public int WAR_RESPAWN_SAFEZONE_RADIUS;
        [ProtoMember(63)]
        public int WAR_RESPAWN_SAFEZONE_SECONDS;

        [ProtoMember(64)]
        public bool WAR_SIEGE_ENABLED;
        [ProtoMember(65)]
        public int WAR_SIEGE_RAM_TICK_SECONDS;
        [ProtoMember(66)]
        public int WAR_SIEGE_RAM_RANGE;
        [ProtoMember(67)]
        public double WAR_SIEGE_RAM_COST;

        // Pre-existing war tunables — now also editable in-game.
        [ProtoMember(68)]
        public int FLAG_CAPTURE_DURATION_SECONDS;
        [ProtoMember(69)]
        public int MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE;
        [ProtoMember(70)]
        public int FLAG_REINFORCEMENT_AMOUNT;
        [ProtoMember(71)]
        public int MINIMUM_DAYS_BETWEEN_BATTLES;

        // War diplomacy / economy (declaration, peace terms, bounties, report/HUD).
        [ProtoMember(72)]
        public double WAR_DECLARATION_COST;
        [ProtoMember(73)]
        public int WAR_REDECLARE_COOLDOWN_DAYS;
        [ProtoMember(74)]
        public bool WAR_REQUIRE_CASUS_BELLI;
        [ProtoMember(75)]
        public int WAR_CASUS_BELLI_GRACE_DAYS;

        [ProtoMember(76)]
        public bool WAR_PEACE_TERMS_ENABLED;
        [ProtoMember(77)]
        public double WAR_VASSAL_TRIBUTE;
        [ProtoMember(78)]
        public int WAR_VASSAL_DURATION_DAYS;

        [ProtoMember(79)]
        public bool WAR_BOUNTY_ENABLED;
        [ProtoMember(80)]
        public double WAR_BOUNTY_MIN;
        [ProtoMember(81)]
        public bool WAR_PLUNDER_ON_KILL_ENABLED;
        [ProtoMember(82)]
        public double WAR_PLUNDER_ON_KILL_PERCENT;

        [ProtoMember(83)]
        public bool WAR_REPORT_ENABLED;
        [ProtoMember(84)]
        public bool WAR_HUD_ENABLED;
        [ProtoMember(85)]
        public int WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY;
        [ProtoMember(86)]
        public int WAR_CAMP_ANCHOR_BREAKS;

        [ProtoMember(87)]
        public bool WAR_NAP_ENABLED;
        [ProtoMember(88)]
        public int WAR_NAP_DEFAULT_DAYS;
        [ProtoMember(89)]
        public int WAR_NAP_MAX_DAYS;
        [ProtoMember(90)]
        public double WAR_NAP_BREAK_PENALTY;
        [ProtoMember(91)]
        public int WAR_BATTLE_WARN_MINUTES;
        [ProtoMember(92)]
        public bool WAR_ULTIMATUM_ENABLED;
    }
}
