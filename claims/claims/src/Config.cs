using claims.src.economy;
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
        // When true: players are auto-added to the mod chat group on join and mod
        // notifications go to that group's dedicated chat tab.
        // When false: players are NOT added to the group (so they won't leak their
        // presence through group-based features of other mods, e.g. Canhideplayerpins
        // "ShowGroupPlayers") and notifications fall back to the general chat.
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

        //WAR - where enemies may build/destroy during an active battle.
        //Enemy war camps are always breakable regardless of this setting.
        public WAR_DESTRUCTION WAR_DESTRUCTION_SCOPE = WAR_DESTRUCTION.BORDER_PLOTS;

        //WAR - flag capture defender interruption
        public bool WAR_FLAG_DEFENDER_INTERRUPT_ENABLED = true;
        public int WAR_FLAG_DEFENDER_RADIUS = 12;
        public double WAR_FLAG_REGRESS_MULTIPLIER = 1.0;

        //WAR - war score (threshold = victory)
        public bool WAR_SCORE_ENABLED = true;
        public int WAR_SCORE_TO_WIN = 100;
        public int WAR_SCORE_PER_PLOT_CAPTURE = 25;
        public int WAR_SCORE_PER_KILL = 5;
        public int WAR_SCORE_PER_HOLD_TICK = 1;
        public int WAR_SCORE_HOLD_TICK_SECONDS = 60;

        //WAR - treasury pillage on plot capture
        public bool WAR_PILLAGE_ENABLED = true;
        public double WAR_PILLAGE_PERCENT = 20;

        //WAR - military camp
        public bool WAR_CAMP_ENABLED = true;
        public int WAR_MAX_CAMPS_PER_CONFLICT = 2;
        // Min distance (in plots) from other cities' plots for placing a war camp. 0 = no limit.
        public int WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY = 2;
        // How many times the anchor block must be broken before the camp is destroyed.
        public int WAR_CAMP_ANCHOR_BREAKS = 5;
        // Whether camp plots count as city territory for claim distance checks. Camps can be placed
        // anywhere and vanish with the war, so by default they neither block other players' claims
        // nor new cities - otherwise camping next to someone would freeze their expansion.
        public bool WAR_CAMP_PLOTS_BLOCK_CLAIMS = false;
        // Whether camps are torn down when the battle window closes. False keeps them until the
        // whole war ends, which leaves an enemy owned plot standing during peacetime.
        public bool WAR_CAMP_REMOVE_AFTER_BATTLE = true;
        // Teleport to your city's war camp (/city war camptp). Channelled like a summon: moving
        // cancels it, and it puts the player on a cooldown afterwards.
        public bool WAR_CAMP_TP_ENABLED = true;
        public int WAR_CAMP_TP_CAST_SECONDS = 10;
        public int WAR_CAMP_TP_COOLDOWN_SECONDS = 300;

        //WAR - respawn safe zone (radius + timer after death)
        public bool WAR_RESPAWN_SAFEZONE_ENABLED = true;
        public int WAR_RESPAWN_SAFEZONE_RADIUS = 20;
        public int WAR_RESPAWN_SAFEZONE_SECONDS = 15;

        //WAR - siege ram
        public bool WAR_SIEGE_ENABLED = true;
        public int WAR_SIEGE_RAM_TICK_SECONDS = 20;
        public int WAR_SIEGE_RAM_RANGE = 6;
        public double WAR_SIEGE_RAM_COST = 50;

        //WAR - declaration cost / cooldown / casus belli
        public double WAR_DECLARATION_COST = 100;
        public int WAR_REDECLARE_COOLDOWN_DAYS = 3;
        public bool WAR_REQUIRE_CASUS_BELLI = false;
        public int WAR_CASUS_BELLI_GRACE_DAYS = 7;

        //WAR - peace terms / vassalage
        public bool WAR_PEACE_TERMS_ENABLED = true;
        //Daily tribute owed by a subdued side. By default it is the total for the whole side and
        //gets split between its cities; set WAR_VASSAL_TRIBUTE_PER_CITY to charge every city in full.
        public double WAR_VASSAL_TRIBUTE = 20;
        public bool WAR_VASSAL_TRIBUTE_PER_CITY = false;
        public int WAR_VASSAL_DURATION_DAYS = 14;

        //UNION - leaving an alliance union. A one-sided exit is a denunciation: it is announced now
        //and only takes effect after UNION_BREAK_DELAY_DAYS, during which war on the soon-to-be ex-ally
        //stays blocked. Both sides can instead agree to dissolve it at once (no delay, no cooldowns).
        public int UNION_BREAK_DELAY_DAYS = 3;
        //After a one-sided break took effect: days before war may be declared on the former ally.
        public int UNION_BREAK_WAR_COOLDOWN_DAYS = 3;
        //After a one-sided break took effect: days before a union with them may be signed again.
        public int UNION_REFORM_COOLDOWN_DAYS = 3;
        //Block leaving a union while both sides share a running conflict (no abandoning an ally mid-war).
        public bool UNION_BREAK_BLOCKED_IN_SHARED_WAR = true;

        //WAR - non-aggression pacts
        public bool WAR_NAP_ENABLED = true;
        public int WAR_NAP_DEFAULT_DAYS = 14;
        public int WAR_NAP_MAX_DAYS = 90;
        public double WAR_NAP_BREAK_PENALTY = 200;

        //WAR - battle window pre-notification
        public int WAR_BATTLE_WARN_MINUTES = 10;

        //WAR - ultimatums (a peacetime demand; refusal/expiry grants a free, justified war)
        public bool WAR_ULTIMATUM_ENABLED = true;
        //How long the target has to comply, in real hours. Ignoring it counts as a refusal, so this
        //must be long enough for an offline mayor to log in - the shared letter delay (5 minutes)
        //would hand out free wars to anyone who happened to be away.
        public int WAR_ULTIMATUM_EXPIRE_HOURS = 48;

        //WAR - bounties / plunder on kill
        public bool WAR_BOUNTY_ENABLED = true;
        public double WAR_BOUNTY_MIN = 10;
        public bool WAR_PLUNDER_ON_KILL_ENABLED = true;
        public double WAR_PLUNDER_ON_KILL_PERCENT = 5;

        //WAR - after-action report / HUD
        public bool WAR_REPORT_ENABLED = true;
        public bool WAR_HUD_ENABLED = true;
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

        // Plot types players are not allowed to set (codes match PlotInfo.nameToPlotType,
        // e.g. "temple", "summon", "tavern", "farm"). Empty = everything allowed.
        // Admins bypass this via /cadmin. Synced to clients to hide disabled types in the GUI.
        public HashSet<string> DISABLED_PLOT_TYPES = new HashSet<string>();

        //PLOTS COST
        public double DEFAULT_PLOT_COST = 1;
        public double OUTPOST_PLOT_COST = 150;
        public double TOURNAMENT_PLOT_COST = 3;
        public double CAMP_PLOT_COST = 4;
        public double TEMPLE_PLOT_COST = 5;
        public double FARM_PLOT_COST = 6;
        public double ORCHARD_PLOT_COST = 6;
        public double SUMMON_PLOT_COST = 7;
        public double EMBASSY_PLOT_COST = 8;
        public double TAVERN_PLOT_COST = 9;
        public double PLOT_NO_PVP_FLAG_COST = 3;
        public double MAIN_CITYPLOT_COST = 3;
        public double PRISON_PLOT_COST = 3;
        public double EXTRA_PLOT_COST = 30;

        //REFUND ON UNCLAIM
        public int PLOT_UNCLAIM_REFUND_PERCENT = 0;
        public int PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS = 300;

        //FARM PLOTS ONLY CROP GROWTH
        public bool CROPS_ONLY_ON_FARM_PLOTS = false;

        //ORCHARD PLOTS ONLY FRUIT TREE FRUITING
        //Fruit trees outside of ORCHARD plots still flower, but never carry fruit.
        public bool FRUIT_ONLY_ON_ORCHARD_PLOTS = false;
        //Whether the "this tree will only blossom" warning is also sent when planting on unclaimed
        //land. Set to false to only warn inside cities, where an orchard plot is actually an option.
        public bool ORCHARD_WARN_OUTSIDE_CITY = true;


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
        public int CHECK_FOR_PACKETS_TO_SEND_EVERY_N_SECONDS = 1;

        //INNER CLAIM
        public int MAX_NUMBER_INNER_CLAIM_PER_TAVERN = 3;

        //CLAIMSEXT
        public bool NEW_CITY_ONLY_BY_ITEM = false;
        public int MAX_NUMBER_TAVERN_PER_CITY = 3;
        public bool SEND_CITY_BANKS_COORDS = true;
        public int ZONE_PLOTS_LENGTH = 32;
        // Computed — always equals PLOT_SIZE * ZONE_PLOTS_LENGTH. JSON value ignored.
        [Newtonsoft.Json.JsonIgnore]
        public int ZONE_BLOCKS_LENGTH => PLOT_SIZE * ZONE_PLOTS_LENGTH;

        public int AREA_REGION_SIZE = 512;
        public int AREA_MAP_SIZE = 2000;

        public string SELECTED_ECONOMY_HANDLER = "";
        public string CITY_ACCOUNT_STRING_PREFIX = "#city_";
        public string ALLIANCE_ACCOUNT_STRING_PREFIX = "#alliance_";

        public System.Collections.Generic.OrderedDictionary<decimal, string> COINS_VALUES_TO_CODE = new();
        public System.Collections.Generic.OrderedDictionary<int, decimal> ID_TO_COINS_VALUES = new();
        // Full coin denomination list shown in the prices GUI. Derived at runtime
        // from the economy provider and synced to clients via
        // ConfigUpdateValuesPacket; never persisted to claims.json.
        [Newtonsoft.Json.JsonIgnore]
        public System.Collections.Generic.List<CoinDenominationData> COIN_DENOMINATIONS = new();

        public bool VERBOSE_LOGGING = true;
        public bool SEND_ANNOUNCEMENTS_PLOT_IN_UNDER_ATTACK = true;
        public bool SEND_COORDS_OF_PLOT_IN_UNDER_ATTACK = true;
        public bool SEND_ANNOUNCEMENTS_PLOT_WAS_CAPTURED = true;
        public bool SEND_COORDS_OF_PLOT_WAS_CAPTURED = true;
        public bool GUI_SHOW_DEBT = true;
        public bool SHOW_BALANCE_HUD_DEFAULT = false;
        public bool? BalanceHudOverride = null;
        public CITY_AREA_VISIBILITY CITY_AREA_VISIBILITY_STATE = CITY_AREA_VISIBILITY.ALL;
        public int CITY_LOG_MAX_ENTRIES { get; set; } = 100;
        public HashSet<EnumPlayerPermissions> AVAILABLE_CITY_PERMISSIONS = new() {
        };
        public HashSet<string> ROLE_CODES_WITH_ADMIN_RIGHTS = new HashSet<string>();
        public bool CLAIM_LIMITERS_ENABLED = false;
        public Dictionary<string, Dictionary<string, object>> CLAIM_LIMITERS = new Dictionary<string, Dictionary<string, object>>();
        public static void AddDefaultValues()
        {
            claims.config.DAYTIME_MAKE_BACKUP = new HashSet<string> { "6:00", "12:00", "18:00", "0:00" };
            claims.config.PROTECTED_MOB_TYPES = new HashSet<string> { "pig-*", "sheep-*", "chicken-*" };
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
                    ValidatePlotSize(api);
                    api.StoreModConfig<Config>(claims.config, "claims.json");
                    return;
                }
                else
                {
                    claims.config = new Config();
                    AddDefaultValues();
                    ValidatePlotSize(api);
                    api.StoreModConfig<Config>(claims.config, "claims.json");
                }
            }
            catch
            {
                if (claims.config == null)
                {
                    claims.config = new Config();
                    AddDefaultValues();
                    ValidatePlotSize(api);
                    api.StoreModConfig<Config>(claims.config, "claims.json");
                    return;
                }
            }
        }
        private static void ValidatePlotSize(ICoreAPI api)
        {
            if (claims.config.PLOT_SIZE != 16 && claims.config.PLOT_SIZE != 32)
            {
                
                if (claims.config.PLOT_SIZE < 16)
                {
                    claims.config.PLOT_SIZE = 16;
                    api.Logger.Error("[claims] PLOT_SIZE={0} is not supported (allowed: 16, 32). Falling back to 16.", claims.config.PLOT_SIZE);
                }
                else
                {
                    claims.config.PLOT_SIZE = 32;
                    api.Logger.Error("[claims] PLOT_SIZE={0} is not supported (allowed: 16, 32). Falling back to 32.", claims.config.PLOT_SIZE);
                }
            }
        }
        public enum CITY_AREA_VISIBILITY
        {
            ALL, WITHOUT_BORDER, WITHOUT_INNER
        }
        /// <summary>Which enemy plots a fighter may build on / dig up during an active battle.</summary>
        public enum WAR_DESTRUCTION
        {
            /// <summary>Every plot of the enemy city.</summary>
            ALL_PLOTS,
            /// <summary>Plots with a neighbour that is not the same city - unclaimed land counts.
            /// Note that this is nearly every plot of a normally shaped city.</summary>
            BORDER_PLOTS,
            /// <summary>Only plots that actually touch a plot of ANOTHER city.</summary>
            BORDER_WITH_OTHER_CITY,
            /// <summary>Only plots that currently have a capture flag planted on them.</summary>
            FLAG_PLOTS,
            /// <summary>Flagged plots plus the four plots around them - room to storm in.</summary>
            FLAG_PLOTS_AND_NEIGHBOURS
        }
    }

    
}
