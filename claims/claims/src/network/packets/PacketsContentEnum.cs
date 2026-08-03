namespace claims.src.network.packets
{
    public enum PacketsContentEnum
    {
        ADD_SINGLE_PLOT, REMOVE_SINGLE_PLOT, ALL_CITY_COLORS, CLIENT_INFORM_ZONES_TIMESTAMPS,
        SERVER_UPDATED_ZONES_ANSWER, SERVER_REMOVE_COLLECTED_PLOTS, SERVER_UPDATE_COLLECTED_PLOTS, CITY_CITIZENS_RANKS_REQUEST,

        OWN_CITY_DELETED, AGREE_NEEDED_ON_NEW_CITY_CREATION, OWN_CITY_INFO_ON_JOIN, OWN_NEW_CITY_CREATED,
        ON_CITY_JOINED, ON_KICKED_FROM_CITY,
        ON_SOME_CITY_PARAMS_UPDATED,

        CURRENT_PLOT_INFO, CURRENT_PLOT_CLIENT_REQUEST,

        ADMIN_REQUEST_CITY_FLAGS, ADMIN_CITY_FLAGS_ALL,

        // data = (int)EnumRespawnPreference
        CLIENT_SET_RESPAWN_PREFERENCE,
        // no data - same effect as /city war camptp
        CLIENT_CAMP_TELEPORT,

        // no data - asks the server to resend CITY_CASUS_BELLI_ALL for the caller's party
        CLIENT_REQUEST_CASUS_BELLI,

        // city guid -> emblem layer string, for every city in the world. Needed by the city banner
        // block, which knows only the guid of the city it belongs to.
        ALL_CITY_EMBLEMS,

        // data = "<boat entity id>;<BoatShareMode>" - the owner changing how one boat is shared.
        // The entity id travels with it because the dialog is open while the player may look away.
        CLIENT_SET_BOAT_SHARE
    }
}
