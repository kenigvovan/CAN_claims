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
            "templerespawnpoints TEXT," +
            "ranks TEXT," +
            "eventlog TEXT," +
            "warcooldowns TEXT DEFAULT \"\"," +
            "grievances TEXT DEFAULT \"\"," +
            "overlord TEXT DEFAULT \"\"," +
            "vassals TEXT DEFAULT \"\"," +
            "vassalsince INTEGER DEFAULT 0," +
            "naps TEXT DEFAULT \"\"," +
            "warjustifications TEXT DEFAULT \"\"," +
            "emblem TEXT DEFAULT \"\"" +
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
            "prisonhoursleft INTEGER," +
            "bounties TEXT DEFAULT \"\"" +
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
           "neutral INTEGER," +
           "pendingunionbreaks TEXT," +
           "unionbreakcooldowns TEXT," +
           "emblem TEXT DEFAULT \"\"" +
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
             "startedby_type TEXT DEFAULT 'alliance'," +
             "firstscore INTEGER DEFAULT 0," +
             "secondscore INTEGER DEFAULT 0," +
             "firstplotscaptured INTEGER DEFAULT 0," +
             "secondplotscaptured INTEGER DEFAULT 0," +
             "firstkills INTEGER DEFAULT 0," +
             "secondkills INTEGER DEFAULT 0," +
             "firstpillaged INTEGER DEFAULT 0," +
             "secondpillaged INTEGER DEFAULT 0" +
             ");";

        // Pending war/peace/NAP/ultimatum offers. Their accept/deny behaviour is rebuilt from
        // purpose + terms by ConflictLetterFactory, so only the plain data is stored here.
        public static string conflictLettersTable =
             "CREATE TABLE IF NOT EXISTS CONFLICTLETTERS(" +
             "guid TEXT PRIMARY KEY NOT NULL," +
             "fromside TEXT," +
             "fromside_type TEXT DEFAULT 'alliance'," +
             "toside TEXT," +
             "toside_type TEXT DEFAULT 'alliance'," +
             "purpose INTEGER," +
             "timestampexpire INTEGER DEFAULT 0," +
             "termtype INTEGER DEFAULT 0," +
             "termamount INTEGER DEFAULT 0," +
             "termplotx INTEGER DEFAULT 0," +
             "termplotz INTEGER DEFAULT 0," +
             "termhasplot INTEGER DEFAULT 0," +
             "napdays INTEGER DEFAULT 0" +
             ");";

        // Pending union offers / mutual-dissolution offers between alliances. Their accept/deny
        // behaviour is rebuilt from the purpose by UnionLetterFactory.
        public static string unionLettersTable =
             "CREATE TABLE IF NOT EXISTS UNIONLETTERS(" +
             "guid TEXT PRIMARY KEY NOT NULL," +
             "fromside TEXT," +
             "toside TEXT," +
             "purpose INTEGER DEFAULT 0," +
             "timestampexpire INTEGER DEFAULT 0" +
             ");";
    }
}
