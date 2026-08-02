namespace claims.src.gui.playerGui.structures
{
    public enum EnumPlayerRelatedInfo
    {
        CITY_NAME, MAYOR_NAME, CITY_CREATED_TIMESTAMP, CITY_MEMBERS, MAX_COUNT_PLOTS, CLAIMED_PLOTS, PLAYER_PREFIX, PLAYER_AFTER_NAME, PLAYER_CITY_TITLES,
        FRIENDS, TO_CITY_INVITES, PLAYER_PERMISSIONS, CITY_POSSIBLE_RANKS, CITY_CITIZENS_RANKS, CITY_CITIZEN_RANK_ADDED, CITY_CITIZEN_RANK_REMOVED,

        SHOW_PLOT_MOVEMENT, 
        CITY_INVITE_ADD,
        CITY_INVITE_REMOVE,
        CITY_PLOTS_COLOR,
        CITY_BALANCE,
        CITY_DEBT,
        CITY_FEE,

        CITY_CRIMINALS_LIST,
        CITY_CRIMINAL_ADDED, CITY_CRIMINAL_REMOVED,
        CITY_ADD_PRISON_CELL,
        CITY_REMOVE_PRISON_CELL,
        CITY_PRISON_CELL_ALL,
        CITY_CELL_PRISON_UPDATE,

        CITY_SUMMON_POINT_ALL,
        CITY_SUMMON_POINT_REMOVE,
        CITY_SUMMON_POINT_ADD,
        CITY_SUMMON_POINT_UPDATE,

        CITY_PLOTS_GROUPS_ALL,
        CITY_PLOTS_GROUPS_REMOVE,
        CITY_PLOTS_GROUPS_ADD,
        CITY_PLOTS_GROUPS_UPDATE,

        TO_PLOTS_GROUP_INVITES,
        TO_PLOTS_GROUP_INVITE_ADD,
        TO_PLOTS_GROUP_INVITE_REMOVE,

        NEW_ALLIANCE_ALL,
        OWN_ALLIANCE_REMOVE,

        CITY_LIST_ALL,
        CITY_LIST_UPDATE,
        CITY_LIST_REMOVE,

        ALLIANCE_LIST_ALL,


        ALLIANCE_BALANCE,
        ALLIANCE_NAME,

        TO_ALLIANCE_INVITES,
        TO_ALLIANCE_INVITE_ADD,
        TO_ALLIANCE_INVITE_REMOVE,

        ALLIANCE_LETTER_ALL,
        ALLIANCE_LETTER_ADD, ALLIANCE_LETTER_REMOVE,

        ALLIANCE_CONFLICT_ADD, ALLIANCE_CONFLICT_REMOVE, ALLIANCE_CONFLICT_ALL,
        ALLIANCE_CONFLICT_WARRANGES_UPDATED,
        ALLIANCE_CONFLICT_SCORE_UPDATED,
        CLIENT_CONFLICT_SUGGESTED_WARRANGE,

        ALLIANCE_CONFLICT_WAR_TIME_MARK_START, ALLIANCE_CONFLICT_WAR_TIME_MARK_END,
        CITY_PLOT_RECOLOR,
        CITY_DAY_PAYMENT,
        CITY_PERMISSIONS_UPDATED,

        ALLIANCE_UNION_LETTER_ALL, ALLIANCE_UNION_LETTER_ADD, ALLIANCE_UNION_LETTER_REMOVE,

        ALLIANCE_ALLY_REMOVED, ALLIANCE_HOSTILE_REMOVED, ALLIANCE_ALLY_ADDED, ALLIANCE_HOSTILE_ADDED,
        ALLIANCE_ALLIES_ALL, ALLIANCE_HOSTILES_ALL, 

        PLAYER_NEXT_PAYMENT,

        CITY_GUID,

        CITY_LOG,

        CITY_PLOTS_MAP,

        // Full snapshot of the casus belli our party holds; the server builds it, the client replaces its list.
        CITY_CASUS_BELLI_ALL,

        // Full snapshot of the announced (not yet effective) union breaks of our alliance.
        ALLIANCE_UNION_BREAKS_ALL,

        PLAYER_BALANCE,

        // Coat of arms of our own city / alliance, as an EmblemHandler layer string.
        CITY_EMBLEM, ALLIANCE_EMBLEM,

        // Settlement tier (CityTier) of our own city. Append-only enum: the values travel as
        // numbers in PlayerGuiRelatedInfoPacket, so new members must stay at the end.
        CITY_TIER,

        // When our village is open to attack, as "unixStart;minutes". Only ever sent to its own
        // citizens - an outsider has to come and find out on the spot.
        CITY_RAID_WINDOW,

        // Inter-city plot market: the listings our city may act on, and the deals it took part in.
        // Kept for its numeric slot: the market listing is now one shape of CITY_PLOT_AUCTIONS.
        CITY_PLOT_MARKET_UNUSED, CITY_PLOT_MARKET_HISTORY,

        // Land offered to other cities: price tags and running auctions alike, as the viewing city
        // is allowed to see them.
        CITY_PLOT_AUCTIONS
    }
}
