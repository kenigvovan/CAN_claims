using claims.src.rights;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace claims.src
{
    public class Config: IConfig
    {
        //ECONOMY
        public double NEW_CITY_COST = 150;
        public double CITY_NAME_CHANGE_COST = 20;
        public bool DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE = true;
        public bool DELETE_CITY_IF_DOESN_PAY_FEE = false;

        public double CITY_BASE_CARE = 2;
        public double CITY_MAX_DEBT = 1000;

        public double NEUTRAL_ALLANCE_PAYMENT = 50;
        public bool ADDITIONAL_COST_OF_NO_PVP_PLOT = true;

        public double PLOT_CLAIM_PRICE = 5;
        public double MAX_CITY_FEE = 50;
        public double SUMMON_PAYMENT = 5;
        public double NEW_ALLIANCE_COST { get; set; } = 300;
        public double ALLIANCE_RENAME_COST { get; set; } = 50;
        public double ALLIANCE_MAX_FEE { get; set; } = 50;
        public double ALLIANCE_BASE_CARE { get; set; } = 50;
        //DATABASE
        public string PATH_TO_DB_AND_JSON_FILES = "";
        public string DB_NAME = "claims.db";
        public string MANUALLY_BACKUP_FILE_NAME = "backup_manually_claims.db";
        public string DAILY_BACKUP_FILE_NAME = "backup_daily_claims.db";
        public string HOURLY_BACKUP_FILE_NAME = "backup_hourly_claims.db";
        public string PERMS_FILE_NAME = "claims_permissions.json";
        public HashSet<string> DAYTIME_MAKE_BACKUP = new HashSet<string>();
        public int BACKUP_CHECK_TIMER_SECONDS = 10;
        public string BACKUP_FOLDER_NAME_IN_DATA_FOLDER = "claims_backups";
        public string FULL_BACKUP_FOLDER = "";
        //CHAT
        public bool USE_MOD_CHAT_WINDOW = true;
        public string CHAT_WINDOW_NAME = "claims";
        public string PREFIX_COLOR_PLAYER = "#00FFFF";
        public string NAME_COLOR_PLAYER = "#FFFFFF";
        public string POSTFIX_COLOR_PLAYER = "#1F920E";
        public string CITY_COLOR_NAME = "#755985";
        public string ALLIANCE_COLOR_NAME = "#218fdc";
        public int MAX_CITIZEN_TITLE_LENGTH = 16;
        public double LOCAL_CHAT_DISTANCE = 100;
        public int ALLIANCE_PREFIX_LENGTH = 3;
        public bool SHOW_CITY_NAME_IN_CHAT = true;
        public bool SHOW_ALLIANCE_PREFIX_IN_CHAT = true;

        //INVITATIONS
        public int MAX_SENT_INVITATIONS_CITY = 20;
        public int MAX_SENT_INVITATIONS_VILLAGE = 10;
        public bool NEED_AGREE_FOR_CONFLICT { get; set; } = true;
        public int DELAY_FOR_CONFLICT_ACTIVATED { get; set; } = 300;

        public int MAX_RECEIVED_INVITATIONS_CITY = 10;
        public int MAX_RECEIVED_INVITATIONS_PLAYER = 10;

        public int MAX_SENT_INVITATIONS_ALLIANCE = 10;
        public int HOUR_TIMEOUT_INVITATION_TO_ALLIANCE { get; set; } = 2;

        public bool NEED_AGREE_FOR_WAR_RANGES = true;
        public int MIN_WARRANGE_DURATION_MINUTES = 60;
        public int WARRANGE_PER_ALLIANCE = 2;
        public int MIN_RANGE_CELL_DURATION_MINUTES = 30;

        //PLOTGROUPS
        public int PLOT_GROUP_INVITATION_TIMEOUT = 2;
        public int MAX_PLOTS_GROUP_PER_CITY = 5;

        //TIME
        public int HOUR_NEW_DAY_START = 43200;
        public int HOUR_TIMEOUT_INVITATION_CITY = 2;
        public int SECONDS_SUMMON_TIME = 10;
        public int SECONDS_SUMMON_COOLDOWN = 10;
        public bool PVP_DURING_PART_OF_THE_DAY = true;
        public float PVP_TIME_START = 19;
        public float PVP_TIME_END = 6;
        public long MOD_DAY_DURATION_IN_SECONDS = 86400;
        public int SECONDS_ALLIANCE_RENAME_COOLDOWN { get; set; } = 10;
        public int CHECK_FOR_WAR_TO_START_EVERY_N_SECONDS = 600;
        public int CHECK_FOR_WAR_TO_START_CALLBACK_EVERY_N_SECONDS = 960;
        public int FLAG_CAPTURE_DURATION_SECONDS = 60;
        //WAR
        public int MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE = 2;
        public int FLAG_REINFORCEMENT_AMOUNT = 10;
        public int MINIMUM_DAYS_BETWEEN_BATTLES { get; set; } = 3;
        //PATCHES
        public bool FALLING_BLOCKS_TO_CITY_PLOTS_PATCH = true;
        public bool WATER_FLOW_CITY_PLOTS_PATCH = true;

        //DISTANCE
        public int MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY = 3;
        public bool CAPTURED_PLOTS_DO_NOT_BLOCK_CLAIMS = true;
        public int MAX_OUTPOST_DISTANCE_FROM_CITY = 1250;
        public int MIN_OUTPOST_DISTANCE_FROM_CITY = 0;
        //AGREEMENT
        public int AGREEMENT_TIMEOUT_SECONDS = 120;
        public string AGREEMENT_COMMAND { get; set; } = "agree";

        //MOVEMENT
        public int DELTA_TIME_PLAYER_POSITION_CHECK_CLIENT = 500;
        public int DELTA_TIME_PLAYER_POSITION_CHECK = 500;
        public bool PLAYER_MOVEMENT_CANCEL_TELEPORTATION = true;

        //PRISON
        public HashSet<string> BLOCKED_COMMANDS_PRISON = new HashSet<string> ();
        public double RANSOM_FOR_NO_CITIZEN = 2;
        public double RANSOM_FOR_CITIZEN = 10;
        public double RANSOM_FOR_MAYOR = 20;
        public double RANSOM_FOR_LEADER = 30;
        public double RANSOM_FOR_CHIEF = 5;

        //DEFENCE
        public HashSet<string> PROTECTED_MOB_TYPES = new HashSet<string>();

        //PLOTS COST
        public double DEFAULT_PLOT_COST = 1;
        public double OUTPOST_PLOT_COST = 150;
        public double TOURNAMENT_PLOT_COST = 3;
        public double CAMP_PLOT_COST = 4;
        public double TEMPLE_PLOT_COST = 5;
        public double FARM_PLOT_COST = 6;
        public double SUMMON_PLOT_COST = 7;
        public double EMBASSY_PLOT_COST = 8;
        public double TAVERN_PLOT_COST = 9;
        public double PLOT_NO_PVP_FLAG_COST = 3;
        public double MAIN_CITYPLOT_COST = 3;
        public double PRISON_PLOT_COST = 3;
        public double EXTRA_PLOT_COST = 30;

     
        //STRINGS
        public int MAX_LENGTH_CITY_INV_MSG = 100;
        public int MAX_LENGTH_CITY_NAME = 40;

        //PRISON
        public int MAX_CELLS_PER_PRISON = 4;

        //SUMMON
        public int SUMMON_MIN_PLAYERS = 0;
        public int SUMMON_HOR_RANGE = 10;
        public int SUMMON_VER_RANGE = 10;
        public bool SUMMON_ALLOWED = true;

        //GENERAL
        public int [] PLOT_BORDERS_COLOR_WILD_PLOT = new int[] { 64, 255, 255, 0 };
        public int [] PLOT_BORDERS_COLOR_OUR_CITY_PLOT = new int[] { 143, 5, 146, 0 };
        public int [] PLOT_BORDERS_COLOR_OTHER_PLOT = new int[] { 16, 49, 158, 0 };
        public int PLOT_SIZE = 16;
        public int MAP_ZONE_SIZE = 512;
        public HashSet<string> BLOCKED_NAMES = new HashSet<string> { };
        public HashSet<string> CITY_PLOTS_COLOR_AVAILABLE_COLORS_GUI = new HashSet<string>();
        public HashSet<string> ALWAYS_ACCESS_BLOCKS = new HashSet<string> { "canmailbox:CANBlockGenericTypedContainer",
            "canmarket:BlockCANMarket", "canmarket:BlockCANMarketSingle", "canmarket:BlockCANStall",
             "vinconomy:BlockVLiquidContainer","vinconomy:BlockVDualLiquidContainer", "vinconomy:BlockVContainer",
            "vinconomy:BlockVPurchaseContainer", "vinconomy:BlockVGacha", "vinconomy:BlockVClothingDisplay"};
        public HashSet<Type> blockTypesAccess = new HashSet<Type>();

        public int[] PLOT_COLORS;

        public int SEND_CITY_UPDATES_EVERY_N_SECONDS = 60;
        public int CHECK_FOR_PACKETS_TO_SEND_EVERY_N_SECONDS = 10;

        //INNER CLAIM
        public int MAX_NUMBER_INNER_CLAIM_PER_TAVERN = 3;

        //CLAIMSEXT
        public bool NEW_CITY_ONLY_BY_ITEM = false;
        public int MAX_NUMBER_TAVERN_PER_CITY = 3;
        public bool SEND_CITY_BANKS_COORDS = true;
        public int ZONE_PLOTS_LENGTH = 32;
        public int ZONE_BLOCKS_LENGTH = 512;

        public int AREA_REGION_SIZE = 512;
        public int AREA_MAP_SIZE = 2000;

        public string SELECTED_ECONOMY_HANDLER = "";
        public string CITY_ACCOUNT_STRING_PREFIX = "#city_";
        public string ALLIANCE_ACCOUNT_STRING_PREFIX = "#alliance_";

        public System.Collections.Generic.OrderedDictionary<double, string> COINS_VALUES_TO_CODE = new();
        public System.Collections.Generic.OrderedDictionary<int, double> ID_TO_COINS_VALUES = new();

        public bool VERBOSE_LOGGING = true;
        public bool SEND_ANNOUNCEMENTS_PLOT_IN_UNDER_ATTACK = true;
        public bool SEND_COORDS_OF_PLOT_IN_UNDER_ATTACK = true;
        public bool SEND_ANNOUNCEMENTS_PLOT_WAS_CAPTURED = true;
        public bool SEND_COORDS_OF_PLOT_WAS_CAPTURED = true;
        public bool GUI_SHOW_DEBT = true;
        public CITY_AREA_VISIBILITY CITY_AREA_VISIBILITY_STATE = CITY_AREA_VISIBILITY.ALL;
        public HashSet<EnumPlayerPermissions> AVAILABLE_CITY_PERMISSIONS = new() {
        };
        public HashSet<string> ROLE_CODES_WITH_ADMIN_RIGHTS = new HashSet<string>();
        public bool CLAIM_LIMITERS_ENABLED = false;
        public Dictionary<string, Dictionary<string, object>> CLAIM_LIMITERS = new Dictionary<string, Dictionary<string, object>>();
        public static void AddDefaultValues()
        {
            claims.config.DAYTIME_MAKE_BACKUP = new HashSet<string> { "6:00", "12:00", "18:00", "0:00" };
            claims.config.PROTECTED_MOB_TYPES = new HashSet<string>{"Bighorn lamb",
            "Bighorn ewe", "Bighorn ram", "Rooster", "Chick", "Hen", "Sow", "Boar", "Piglet" };
            claims.config.CITY_PLOTS_COLOR_AVAILABLE_COLORS_GUI = new HashSet<string> { "white", "blue", "red", "orange", "black", "aqua", "yellow", "cyan", "pink", "gold", "indigo", "ivory", "lime", "green", "red", "purple", "silver",
        "violet"};
            claims.config.BLOCKED_COMMANDS_PRISON = new HashSet<string> { "summon" };
            claims.config.AVAILABLE_CITY_PERMISSIONS = new() { EnumPlayerPermissions.CITY_CLAIM_PLOT,
            EnumPlayerPermissions.CITY_UNCLAIM_PLOT,
            EnumPlayerPermissions.CITY_BUY_EXTRA_PLOT,
            EnumPlayerPermissions.CITY_BUY_OUTPOST,

            EnumPlayerPermissions.CITY_INVITE,
            EnumPlayerPermissions.CITY_KICK,
            EnumPlayerPermissions.CITY_UNINVITE,
            EnumPlayerPermissions.SHOW_INVITES_SENT,

            EnumPlayerPermissions.CITY_SET_ALL,
            EnumPlayerPermissions.CITY_SET_NAME,
            EnumPlayerPermissions.CITY_SET_OPEN_STATE,
            EnumPlayerPermissions.CITY_SET_PVP,
            EnumPlayerPermissions.CITY_SET_FIRE,
            EnumPlayerPermissions.CITY_SET_BLAST,
            EnumPlayerPermissions.CITY_SET_GLOBAL_FEE,
            EnumPlayerPermissions.CITY_SET_DAILY_MSG,
            EnumPlayerPermissions.CITY_SET_PLOT_ACCESS_PERMISSIONS,
            EnumPlayerPermissions.CITY_SET_INV_MSG,

            EnumPlayerPermissions.CITY_INFO,
            EnumPlayerPermissions.CITY_HERE,

            EnumPlayerPermissions.CITY_SHOW_RANK_OTHERS,
            EnumPlayerPermissions.CITY_SET_RANK,
            EnumPlayerPermissions.CITY_REMOVE_RANK,

            EnumPlayerPermissions.CITY_PRISON_ALL,
            EnumPlayerPermissions.CITY_CRIMINAL_ALL,
            EnumPlayerPermissions.CITY_ADD_CRIMINAL,
            EnumPlayerPermissions.CITY_REMOVE_CRIMINAL,
            EnumPlayerPermissions.CITY_PRISON_ADD_CELL,
            EnumPlayerPermissions.CITY_PRISON_REMOVE_CELL,
            EnumPlayerPermissions.CITY_PRISON_LIST,

            EnumPlayerPermissions.CITY_SET_SUMMON,

            EnumPlayerPermissions.CITY_SET_OTHERS_PREFIX,

            EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE,
            EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE,
            EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLAYER,
            EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER,
            EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLOT,
            EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT,
            EnumPlayerPermissions.CITY_PLOTSGROUP_LIST,
            EnumPlayerPermissions.CITY_PLOTSGROUP_SET,
            EnumPlayerPermissions.CITY_PLOTSGROUP_SET_PVP,
            EnumPlayerPermissions.CITY_PLOTSGROUP_SET_FIRE,
            EnumPlayerPermissions.CITY_PLOTSGROUP_SET_BLAST,

            EnumPlayerPermissions.CITY_SET_PLOTS_COLOR,

            EnumPlayerPermissions.CITY_SEE_BALANCE,

            EnumPlayerPermissions.CITY_WITHDRAW_MONEY,
            EnumPlayerPermissions.CITY_CREATE_CITY_RANK,
            EnumPlayerPermissions.CITY_DELETE_CITY_RANK,
            EnumPlayerPermissions.CITY_SEE_CITY_RANKS,
            EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK,
            EnumPlayerPermissions.CITY_REMOVE_PERMISSION_FROM_RANK};
            claims.config.ROLE_CODES_WITH_ADMIN_RIGHTS = new HashSet<string> { "admin" };
            var c = new BlockPos(0, 0, 0);
            /* public Vec3i ToLocalPosition(ICoreAPI api)
    {
        return new Vec3i(X - api.World.DefaultSpawnPosition.XInt, Y, Z - api.World.DefaultSpawnPosition.ZInt);
    }
            c.ToLocalPosition*/
            claims.config.CLAIM_LIMITERS = new Dictionary<string, Dictionary<string, object>>()
            {
                { "NearClaimLimiter", new Dictionary<string, object>()
                    {
                        { "Zones", new List<(Vec2i, int)>()
                            {
                                (new Vec2i(0, 0), 500)
                            }
                        }
                    }
                },
                { "DistantClaimLimiter", new Dictionary<string, object>()
                    {
                        { "Zones", new List<(Vec2i, int)>()
                            {
                                (new Vec2i(0, 0), 1500)
                            }
                        }
                    }
                }
            };
        }
        public static void LoadConfig(ICoreAPI api)
        {
            try
            {
                claims.config = api.LoadModConfig<Config>( "claims.json");
                if (claims.config != null)
                {
                    api.StoreModConfig<Config>(claims.config, "claims.json");
                    return;
                }
                else
                {
                    claims.config = new Config();
                    AddDefaultValues();
                    api.StoreModConfig<Config>(claims.config, "claims.json");
                }
            }
            catch
            {
                if (claims.config == null)
                {
                    claims.config = new Config();
                    AddDefaultValues();
                    api.StoreModConfig<Config>(claims.config, "claims.json");
                    return;
                }
            }
        }
        public enum CITY_AREA_VISIBILITY
        {
            ALL, WITHOUT_BORDER, WITHOUT_INNER
        }
    }

    
}
