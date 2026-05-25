namespace claims.src.database
{
    public class SQLiteTables
    {
        public static string cityTable =
            "CREATE TABLE IF NOT EXISTS CITIES(" +
            "name TEXT," +
            "mayor TEXT," +
            "guid TEXT PRIMARY KEY," +
            "timestampcreated INTEGER DEFAULT 0," +
            "debtbalance INTEGER," +
            "perm TEXT,"+
            "plotgroups TEXT," +
            "prisons TEXT," +
            "alliance TEXT," +
            "hostiles TEXT," +
            "comrades TEXT," +
            "defaultplotcost INTEGER," +
            "invMsg TEXT," +
            "opencity INTEGER," +
            "fee INTEGER," +
            "criminals TEXT," +
            "bonusplots INTEGER DEFAULT 0," +
            "istechnical INTEGER," +
            "extrachunksbought INTEGER," +
            "citycolor INTEGER," +
            "templerespawnpoints TEXT" +
            "ranks TEXT," +
            "eventlog TEXT" +
             ");";

        public static string playerTable =
            "CREATE TABLE IF NOT EXISTS PLAYERS(" +
            "name TEXT," +
            "uid TEXT PRIMARY KEY NOT NULL," +
            "timestampfirstjoined INTEGER DEFAULT 0," +
            "timestamplastonline INTEGER DEFAULT 0," +
            "comrades TEXT," +
            "city TEXT," +
            "citytitles TEXT,"+
            "alliancetitles TEXT," +
            "title TEXT," +
            "aftername TEXT," +
            "perms TEXT," +
            "prisonguid TEXT," +
            "prisonhoursleft INTEGER" +
            ");";
        public static string plotTable =
            "CREATE TABLE IF NOT EXISTS PLOTS(" +
            "name TEXT," +
            "x INTEGER DEFAULT 0," + //-
            "z INTEGER DEFAULT 0," + //-
            "city TEXT," + //-
            "ownerofplot TEXT," +
            "type INTEGER," +
            "price INTEGER," +
            "customtax REAL," +
            "perms TEXT," +
            "plotgroupguid TEXT," +
            "markednopvp INTEGER," +
            "plotdesc TEXT," +
            "extraBought INTEGER," +
            "wascaptured INTEGER," +
            "timestampclaimed INTEGER DEFAULT 0," +
            "lastpaidprice INTEGER DEFAULT 0," +
            "PRIMARY KEY(x, z)" +
            ");";

        public static string plotGroupTable =
            "CREATE TABLE IF NOT EXISTS CITYPLOTSGROUP(" +
            "name TEXT," +
            "guid TEXT PRIMARY KEY NOT NULL," +
            "perms TEXT," +
            "players TEXT," +
            "plotsgroupfee INTEGER," +
            "city TEXT" +
            ");";

        public static string worldTable =
           "CREATE TABLE IF NOT EXISTS WORLDS(" +
           "name TEXT PRIMARY KEY NOT NULL," +
           "guid TEXT," +
           "pvpeverywhere INTEGER," +
           "fireeverywhere INTEGER," +
           "blasteverywhere INTEGER," +
           "fireforbidden INTEGER," +
           "pvpforbidden INTEGER," +
           "blastforbidden INTEGER" +
           ");";

        public static string prisonsTable =
           "CREATE TABLE IF NOT EXISTS PRISONS(" +
           "name TEXT," +
           "guid TEXT PRIMARY KEY NOT NULL," +
           "prisonCells TEXT," +
           "city TEXT," +
           "x INTEGER," +
           "z INTEGER" +
           ");";
        public static string allianceTable =
           "CREATE TABLE IF NOT EXISTS ALLIANCIES(" +
           "name TEXT," +
           "guid TEXT PRIMARY KEY NOT NULL," +
           "timestampcreated INTEGER DEFAULT 0," +
           "maincity TEXT," +
           "cities TEXT," +
           "hostiles TEXT," +
           "comrades TEXT," +
           "alliancefee INTEGER," +
           "neutral INTEGER" +
           ");";

        public static string conflictsTable =
             "CREATE TABLE IF NOT EXISTS CONFLICTS(" +
             "name TEXT," +
             "guid TEXT PRIMARY KEY NOT NULL," +
             "firstside TEXT," +
             "secondside TEXT," +
             "conflictstate INTEGER," +
             "startedby TEXT," +
             "warranges TEXT," +
             "firstwarranges TEXT," +
             "secondwarranges TEXT," +
             "minimumdaysbetweenbattles INTEGER," +
             "lastbattledatestart TEXT," +
             "lastbattledateend TEXT," +
             "nextbattledatestart TEXT," +
             "nextbattledateend TEXT," +
             "timestampstarted INTEGER DEFAULT 0," +
             "firstside_type TEXT DEFAULT 'alliance'," +
             "secondside_type TEXT DEFAULT 'alliance'," +
             "startedby_type TEXT DEFAULT 'alliance'" +
             ");";
    }
}
