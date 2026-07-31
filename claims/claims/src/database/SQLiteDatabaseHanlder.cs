using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using claims.src.auxialiry;
using claims.src.auxialiry.converters;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.part.structure.union;
using claims.src.part.structure.war;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.database
{

    public class SQLiteDatabaseHanlder : DatabaseHandler
    {
        ConcurrentQueue<QuerryInfo> queryQueue = new ConcurrentQueue<QuerryInfo>();
        private SqliteConnection SqliteConnection = null;

        public SQLiteDatabaseHanlder() : base()
        {
            string folderPath;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
            {
                folderPath = @"" + Path.Combine(GamePaths.ModConfig, claims.config.DB_NAME);
            }
            else
            {
                folderPath = Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, claims.config.DB_NAME);
            }

            folderPath.Replace(@"\\", @"\");

            claims.sapi.Logger.Debug("[claims] db path is " + folderPath);

            SqliteConnection = new SqliteConnection(@"Data Source=" + folderPath);
            if (SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }

            if (!initializeTables())
            {
                claims.sapi.Logger.Error("[claims] SQLiteDatabaseHanlder::initializeTables error.");
            }

            claims.sapi.Event.Timer(drainQueue, 0.5);
        }

        /// <summary>
        /// Executes every pending query in the queue synchronously.
        /// Called periodically by the 0.5s timer and on demand by <see cref="saveEveryThing"/>.
        /// </summary>
        private void drainQueue()
        {
            while (!this.queryQueue.IsEmpty)
            {
                this.queryQueue.TryDequeue(out QuerryInfo query);

                if (query.action == QuerryType.UPDATE)
                {
                    updateDatabase(query);
                }
                else if (query.action == QuerryType.INSERT)
                {
                    insertToDatabase(query);
                }
                else
                {
                    deleteFromDatabase(query);
                }
            }
        }
        public SqliteConnection getConnection()
        {
            return SqliteConnection;
        }

        private void TryAlterTable(string checkSql, string alterSql)
        {
            try { new SqliteCommand(checkSql, SqliteConnection).ExecuteScalar(); }
            catch { new SqliteCommand(alterSql, SqliteConnection).ExecuteNonQuery(); }
        }

        public bool initializeTables()
        {
            try
            {
                //CITY
                SqliteCommand command = new SqliteCommand(SQLiteTables.cityTable, SqliteConnection);
                command.ExecuteNonQuery();
                //PLAYER
                command = new SqliteCommand(SQLiteTables.playerTable, SqliteConnection);
                command.ExecuteNonQuery();

                //PLOT
                command = new SqliteCommand(SQLiteTables.plotTable, SqliteConnection);
                command.ExecuteNonQuery();

                //CITYPLOTGROUP
                command = new SqliteCommand(SQLiteTables.plotGroupTable, SqliteConnection);
                command.ExecuteNonQuery();

                //WORLD
                command = new SqliteCommand(SQLiteTables.worldTable, SqliteConnection);
                command.ExecuteNonQuery();

                //PRISON
                command = new SqliteCommand(SQLiteTables.prisonsTable, SqliteConnection);
                command.ExecuteNonQuery();

                //ALLIANCIES
                command = new SqliteCommand(SQLiteTables.allianceTable, SqliteConnection);
                command.ExecuteNonQuery();

                //CONFLICT
                command = new SqliteCommand(SQLiteTables.conflictsTable, SqliteConnection);
                command.ExecuteNonQuery();

                //PENDING CONFLICT LETTERS
                command = new SqliteCommand(SQLiteTables.conflictLettersTable, SqliteConnection);
                command.ExecuteNonQuery();

                //PENDING UNION LETTERS
                command = new SqliteCommand(SQLiteTables.unionLettersTable, SqliteConnection);
                command.ExecuteNonQuery();

                // Column migrations run unconditionally, not gated by user_version. TryAlterTable is
                // idempotent (it probes the column first), so re-running costs one cheap SELECT per
                // column at startup, and it removes a whole class of bugs: a column appended to an
                // already-released migration block would otherwise never be created on databases
                // that already carry that version number (this is how 'no such column: naps' happened).
                applyColumnMigrations();

                new SqliteCommand("PRAGMA user_version = 3", SqliteConnection).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                claims.sapi.Logger.Error("initializeTables error." + ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds every column the current code expects but older databases may lack.
        /// Safe to call on every startup and in any order - each entry is a no-op when the column
        /// already exists. When adding a new column to the code, add it here as well; there is no
        /// version gate to remember to bump.
        /// </summary>
        private void applyColumnMigrations()
        {
                    TryAlterTable("SELECT templerespawnpoints FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN templerespawnpoints TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT alliance FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN alliance TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT alliancetitles FROM PLAYERS LIMIT 1",
                        "ALTER TABLE PLAYERS ADD COLUMN alliancetitles TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT prefix FROM ALLIANCIES LIMIT 1",
                        "ALTER TABLE ALLIANCIES ADD COLUMN prefix TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT timestampcreated FROM ALLIANCIES LIMIT 1",
                        "ALTER TABLE ALLIANCIES ADD COLUMN timestampcreated TEXT DEFAULT 0");
                    TryAlterTable("SELECT firstwarranges FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN firstwarranges TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT secondwarranges FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN secondwarranges TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT nextbattledatestart FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN nextbattledatestart TEXT DEFAULT \"0001-01-01T00:00:00\"");
                    TryAlterTable("SELECT nextbattledateend FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN nextbattledateend TEXT DEFAULT \"0001-01-01T00:00:00\"");
                    TryAlterTable("SELECT hostiles FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN hostiles TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT comrades FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN comrades TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT wascaptured FROM PLOTS LIMIT 1",
                        "ALTER TABLE PLOTS ADD COLUMN wascaptured INTEGER DEFAULT 0");
                    TryAlterTable("SELECT ranks FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN ranks TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT firstside_type FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN firstside_type TEXT DEFAULT 'alliance'");
                    TryAlterTable("SELECT secondside_type FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN secondside_type TEXT DEFAULT 'alliance'");
                    TryAlterTable("SELECT startedby_type FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN startedby_type TEXT DEFAULT 'alliance'");
                    TryAlterTable("SELECT timestampclaimed FROM PLOTS LIMIT 1",
                        "ALTER TABLE PLOTS ADD COLUMN timestampclaimed INTEGER DEFAULT 0");
                    TryAlterTable("SELECT lastpaidprice FROM PLOTS LIMIT 1",
                        "ALTER TABLE PLOTS ADD COLUMN lastpaidprice INTEGER DEFAULT 0");
                    TryAlterTable("SELECT eventlog FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN eventlog TEXT DEFAULT \"\"");

                    // War score (CONFLICTS)
                    TryAlterTable("SELECT firstscore FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN firstscore INTEGER DEFAULT 0");
                    TryAlterTable("SELECT secondscore FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN secondscore INTEGER DEFAULT 0");

                    // War diplomacy: cooldowns, grievances, vassalage (CITIES)
                    TryAlterTable("SELECT warcooldowns FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN warcooldowns TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT grievances FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN grievances TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT overlord FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN overlord TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT vassals FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN vassals TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT vassalsince FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN vassalsince INTEGER DEFAULT 0");
                    TryAlterTable("SELECT naps FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN naps TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT warjustifications FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN warjustifications TEXT DEFAULT \"\"");
                    // Union denunciations and post-break cooldowns (ALLIANCIES)
                    TryAlterTable("SELECT pendingunionbreaks FROM ALLIANCIES LIMIT 1",
                        "ALTER TABLE ALLIANCIES ADD COLUMN pendingunionbreaks TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT unionbreakcooldowns FROM ALLIANCIES LIMIT 1",
                        "ALTER TABLE ALLIANCIES ADD COLUMN unionbreakcooldowns TEXT DEFAULT \"\"");
                    // Bounties on players (PLAYERS)
                    TryAlterTable("SELECT bounties FROM PLAYERS LIMIT 1",
                        "ALTER TABLE PLAYERS ADD COLUMN bounties TEXT DEFAULT \"\"");
                    // After-action stats (CONFLICTS)
                    TryAlterTable("SELECT firstplotscaptured FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN firstplotscaptured INTEGER DEFAULT 0");
                    TryAlterTable("SELECT secondplotscaptured FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN secondplotscaptured INTEGER DEFAULT 0");
                    TryAlterTable("SELECT firstkills FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN firstkills INTEGER DEFAULT 0");
                    TryAlterTable("SELECT secondkills FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN secondkills INTEGER DEFAULT 0");
                    TryAlterTable("SELECT firstpillaged FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN firstpillaged INTEGER DEFAULT 0");
                    TryAlterTable("SELECT secondpillaged FROM CONFLICTS LIMIT 1",
                        "ALTER TABLE CONFLICTS ADD COLUMN secondpillaged INTEGER DEFAULT 0");
                    // Coats of arms (CITIES, ALLIANCIES)
                    TryAlterTable("SELECT emblem FROM CITIES LIMIT 1",
                        "ALTER TABLE CITIES ADD COLUMN emblem TEXT DEFAULT \"\"");
                    TryAlterTable("SELECT emblem FROM ALLIANCIES LIMIT 1",
                        "ALTER TABLE ALLIANCIES ADD COLUMN emblem TEXT DEFAULT \"\"");
        }

        /// <summary>
        /// Reads a "guid -> unix seconds" column added by a migration. Missing column or empty cell
        /// means "keep what the object already has", which is what every diplomacy dictionary wants.
        /// </summary>
        private static Dictionary<string, long> ReadTimestampDict(DataRow row, string column, Dictionary<string, long> current)
        {
            if (!row.Table.Columns.Contains(column)) return current;
            string raw = row[column].ToString();
            if (raw.Length == 0) return current;
            return JsonConvert.DeserializeObject<Dictionary<string, long>>(raw) ?? current;
        }

        public bool updateDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = QuerryTemplates.ByTable.TryGetValue(querry.targetTable, out var updateStatements)
                ? updateStatements.Update : "";


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }
            if (rowsChanged == 0)
            {
                querry.action = QuerryType.INSERT;
                queryQueue.Enqueue(querry);
            }
            return true;
        }

        public bool deleteFromDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = QuerryTemplates.ByTable.TryGetValue(querry.targetTable, out var deleteStatements)
                ? deleteStatements.Delete : "";


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;

        }

        public bool insertToDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = QuerryTemplates.ByTable.TryGetValue(querry.targetTable, out var insertStatements)
                ? insertStatements.Insert : "";


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;
        }

        public DataTable readFromDatabase(string querry, Dictionary<string, object> dict)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            using (var cmd = new SqliteCommand(querry, SqliteConnection))
            {
                foreach (KeyValuePair<string, object> entry in dict)
                {
                    cmd.Parameters.AddWithValue(entry.Key, entry.Value);
                }
                var dt = new DataTable();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public DataTable readFromDatabaseWithoutID(string querry)
        {
            using (var cmd = new SqliteCommand(querry, SqliteConnection))
            {
                var dt = new DataTable();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                    return dt;
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        //PLAYER
        public override bool savePlayerInfo(PlayerInfo player, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", player.GetPartName() },
                { "@uid", player.Guid},
                { "@timestampfirstjoined", player.TimeStampFirstJoined},
                { "@timestamplastonline", player.TimeStampLasOnline},
                { "@comrades", string.Join(";", StringFunctions.concatStringsWithDelim(player.Friends, ';'))},
                { "@city", player.hasCity() ? player.City.Guid : ""},
                { "@citytitles", string.Join(";", player.getCityTitles().ToArray()) },
                { "@title", player.Prefix},
                { "@aftername", player.AfterName },
                { "@perms", player.PermsHandler.ToString() },
                { "@prisonguid", player.isPrisoned() ? player.PrisonedIn.Guid : "" },
                { "@prisonhoursleft", player.PrisonHoursLeft },
                { "@bounties", JsonConvert.SerializeObject(player.BountyPosters) }

            };

            queryQueue.Enqueue(new QuerryInfo("PLAYERS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabasePlayerInfo(PlayerInfo player)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object>
            {
                {"@uid",player.Guid }
            };
            queryQueue.Enqueue(new QuerryInfo("PLAYERS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadPlayerInfo(DataRow it)
        {
            if (!claims.dataStorage.GetPlayerByUid(it["uid"].ToString(), out PlayerInfo tmp))
            {
                MessageHandler.sendErrorMsg("loadPlayerInfo: player '" + it["uid"] + "' not found in dummy set, skipped");
                return false;
            }
            tmp.TimeStampFirstJoined = long.Parse(it["timestampfirstjoined"].ToString());
            tmp.TimeStampLasOnline = long.Parse(it["timestamplastonline"].ToString());
            foreach (string str in it["comrades"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.GetPlayerByUid(str, out PlayerInfo plTmp);
                tmp.addComrade(plTmp);
            }
            if (it["city"].ToString().Length != 0)
            {
                if (!claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city))
                {
                    MessageHandler.sendErrorMsg("loadPlayerInfo: city '" + it["city"] + "' of player '" + tmp.GetPartName() + "' not found, city skipped");
                }
                else
                {
                    tmp.setCity(city);
                    city.getCityCitizens().Add(tmp);
                }
            }
            foreach (string str in it["citytitles"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                tmp.addCityTitle(str);
            }
            tmp.Prefix = it["title"].ToString();
            tmp.AfterName = it["aftername"].ToString();
            tmp.setPerms(it["perms"].ToString());
            if (claims.dataStorage.getPrison(it["prisonguid"].ToString(), out Prison prison))
            {
                tmp.PrisonedIn = prison;
                tmp.PrisonHoursLeft = int.Parse(it["prisonhoursleft"].ToString());
            }
            if (it.Table.Columns.Contains("bounties"))
            {
                string b = it["bounties"].ToString();
                if (b.Length != 0)
                    tmp.BountyPosters = JsonConvert.DeserializeObject<Dictionary<string, long>>(b) ?? new();
            }
            return true;
        }

        public override bool loadAllPlayersInfo()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM PLAYERS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadPlayerInfo(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllPlayersInfo::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool loadDummyPlayers()
        {
            MessageHandler.sendDebugMsg("Load dummy players.");
            DataTable dt = readFromDatabase("SELECT name, uid FROM PLAYERS", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                PlayerInfo tmp = new PlayerInfo(it["name"].ToString(), it["uid"].ToString());
                claims.dataStorage.addPlayer(tmp);
            }
            return true;
        }
        //CITY
        public override bool loadDummyCitis()
        {
            MessageHandler.sendDebugMsg("Load dummy cities.");
            DataTable dt = readFromDatabase("SELECT name, guid FROM CITIES", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                City tmp = new City(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addCity(tmp);
                MessageHandler.sendDebugMsg("Load dummy city: " + tmp.GetPartName());
            }
            return true;
        }

        public override bool saveCity(City city, bool update = true)
        {
            var c = JsonConvert.SerializeObject(city.CustomCityRanks);
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", city.GetPartName() },
                { "@guid", city.Guid},
                { "@mayor", city.getMayor() == null ? "" : city.getMayor().Guid},
                { "@timestampcreated", city.TimeStampCreated},
                { "@debtbalance", city.DebtBalance},
                { "@perm", city.getPermsHandler().ToString() },
                { "@plotgroups", StringFunctions.concatStringsWithDelim(city.getCityPlotsGroups(), ';') },
                { "@prisons",  StringFunctions.concatStringsWithDelim(city.getPrisons(), ';')},
                { "@alliance", (city.HasAlliance() ? city.Alliance.Guid : "")},
                { "@hostiles", StringFunctions.concatStringsWithDelim(city.HostileCities, ';') },
                { "@comrades", StringFunctions.concatStringsWithDelim(city.ComradeCities, ';')},
                { "@defaultplotcost", city.getDefaultPlotCost() },
                { "@invmsg", city.invMsg },
                { "@opencity", city.openCity },
                { "@fee", city.fee },
                { "@criminals", StringFunctions.concatStringsWithDelim(city.getCriminals(), ';') },
                { "@istechnical", city.isTechnicalCity() ? 1 : 0 },
                { "@bonusplots", city.getBonusPlots() },
                { "@extrachunksbought", city.Extrachunksbought },
                { "@citycolor", city.cityColor },
                { "@templerespawnpoints", JsonConvert.SerializeObject(city.TempleRespawnPoints) },
                { "@ranks", JsonConvert.SerializeObject(city.CustomCityRanks) },
                { "@eventlog", JsonConvert.SerializeObject(city.EventLog) },
                { "@warcooldowns", JsonConvert.SerializeObject(city.WarCooldowns) },
                { "@grievances", JsonConvert.SerializeObject(city.Grievances) },
                { "@overlord", city.OverlordGuid ?? "" },
                { "@vassals", StringFunctions.concatStringsWithDelim(city.VassalCities, ';') },
                { "@vassalsince", city.VassalSince },
                { "@naps", JsonConvert.SerializeObject(city.NonAggressionPacts) },
                { "@warjustifications", JsonConvert.SerializeObject(city.WarJustifications) },
                { "@emblem", city.Emblem ?? "" }
            };

            queryQueue.Enqueue(new QuerryInfo("CITIES", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseCity(City city)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", city.Guid }
            };

            queryQueue.Enqueue(new QuerryInfo("CITIES", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadCity(DataRow it)
        {
            claims.dataStorage.getCityByGUID(it["guid"].ToString(), out City city);
            city.setIsTechnicalCity(it["istechnical"].ToString().Equals("0") ? false : true);
            if (!city.isTechnicalCity())
            {
                if (!claims.dataStorage.GetPlayerByUid(it["mayor"].ToString(), out PlayerInfo playerInfo) || playerInfo == null)
                {
                    // Non-technical city with an unresolvable mayor UID would silently
                    // lose its mayor here. Surface it instead of dropping the mayor.
                    MessageHandler.sendErrorMsg("loadCity: mayor '" + it["mayor"] + "' of city '" + city.GetPartName() + "' (" + city.Guid + ") not found, mayor kept null");
                }
                city.setMayor(playerInfo);
                // Self-heal a desynced save. loadAllPlayersInfo (PLAYERS.city) ran before this,
                // so playerInfo.City is already resolved. The mayor must point back to this city
                // and be in its citizen set; if a historical bug cleared PLAYERS.city while
                // CITIES.mayor still referenced this player, reapplyRights() would see City==null
                // on login and silently downgrade the mayor to citizen permissions even though
                // MAYOR_NAME keeps showing them as mayor.
                if (playerInfo != null)
                {
                    if (playerInfo.City == null)
                    {
                        MessageHandler.sendErrorMsg("loadCity: mayor '" + playerInfo.GetPartName() + "' of city '" + city.GetPartName() + "' (" + city.Guid + ") had no city set, reattaching and re-saving");
                        playerInfo.setCity(city);
                        playerInfo.saveToDatabase();
                    }
                    if (city.Equals(playerInfo.City))
                    {
                        city.getCityCitizens().Add(playerInfo);
                    }
                    else
                    {
                        MessageHandler.sendErrorMsg("loadCity: mayor '" + playerInfo.GetPartName() + "' of city '" + city.GetPartName() + "' (" + city.Guid + ") is attached to a different city '" + (playerInfo.City?.GetPartName() ?? "null") + "', not reconciling");
                    }
                }
            }
            else
            {
                if (it["mayor"].ToString().Length != 0)
                {
                    claims.dataStorage.GetPlayerByUid(it["mayor"].ToString(), out PlayerInfo playerInfo);
                    city.setMayor(playerInfo);
                }
                else
                {
                    city.setMayor(null);
                }
            }
            city.TimeStampCreated = long.Parse(it["timestampcreated"].ToString(), CultureInfo.InvariantCulture);
            city.DebtBalance = double.Parse(it["debtbalance"].ToString(), CultureInfo.InvariantCulture);
            city.getPermsHandler().setPerms(it["perm"].ToString());
            foreach (string str in it["plotgroups"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                if (!claims.dataStorage.getPlotsGroup(str, out CityPlotsGroup cityPlotsGroup))
                {
                    MessageHandler.sendErrorMsg("loadCity: plot group '" + str + "' of city '" + city.GetPartName() + "' not found, skipped");
                    continue;
                }
                city.getCityPlotsGroups().Add(cityPlotsGroup);
            }
            foreach (string str in it["prisons"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                claims.dataStorage.getPrison(str, out Prison prison);
                if (prison != null)
                    city.getPrisons().Add(prison);
            }
            var pp = it["alliance"].ToString();
            if (it["alliance"].ToString().Length != 0)
            {
                claims.dataStorage.GetAllianceByGUID(it["alliance"].ToString(), out Alliance alliance);
                if (alliance != null)
                {
                    city.Alliance = alliance;
                }
            }
            city.setDefaultPlotCost(int.Parse(it["defaultplotcost"].ToString(), CultureInfo.InvariantCulture));          
            foreach (string str in it["criminals"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.GetPlayerByUid(str, out PlayerInfo criminalPlayer);
                if (criminalPlayer == null)
                {
                    continue;
                }
                city.getCriminals().Add(criminalPlayer);
            }
            city.fee = int.Parse(it["fee"].ToString());
            city.invMsg = it["invMsg"].ToString();
            city.openCity = it["opencity"].ToString().Equals("0") ? false : true;
            city.setBonusPlots(int.Parse(it["bonusplots"].ToString(), CultureInfo.InvariantCulture));
            city.Extrachunksbought = int.Parse(it["extrachunksbought"].ToString(), CultureInfo.InvariantCulture);
            try
            {
                city.cityColor = int.Parse(it["citycolor"].ToString(), CultureInfo.InvariantCulture);
            }
            catch 
            {
                MessageHandler.sendDebugMsg("loadCity::exc no color" + city.GetPartName());
            }
            string rPoints = it["templerespawnpoints"].ToString();
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new Veс2iVec3iConverter());
            if (rPoints.Length != 0)
            {
                foreach (var rPoint in JsonConvert.DeserializeObject<Dictionary<Vec2i, Vec3i>>(rPoints, settings))
                {
                    city.AddTempleRespawnPoint(rPoint.Key, rPoint.Value);
                }
            }
            foreach (string str in it["hostiles"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.getCityByGUID(str, out City city1);
                if (city1 == null)
                    continue;
                //HERE WAS NULL BECAUSE HOSTILE CITY WAS DELETED
                city.HostileCities.Add(city1);
            }
            foreach (string str in it["comrades"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.getCityByGUID(str, out City city1);
                if (city1 == null)
                    continue;
                city.ComradeCities.Add(city1);
            }

            string ranksString = it["ranks"].ToString();
            if (ranksString.Length != 0)
            {
                city.CustomCityRanks = JsonConvert.DeserializeObject<Dictionary<string, CustomCityRank>>(ranksString);
            }

            try
            {
                string eventlogString = it["eventlog"].ToString();
                if (eventlogString.Length != 0)
                {
                    city.EventLog = JsonConvert.DeserializeObject<List<citylog.CityLogEntry>>(eventlogString) ?? new List<citylog.CityLogEntry>();
                }
            }
            catch (Exception ex) { claims.sapi.Logger.Warning("[claims] Failed to load city data for '{0}': {1}", city.GetPartName(), ex.Message); }

            try
            {
                city.WarCooldowns = ReadTimestampDict(it, "warcooldowns", city.WarCooldowns);
                city.Grievances = ReadTimestampDict(it, "grievances", city.Grievances);
                if (it.Table.Columns.Contains("overlord"))
                    city.OverlordGuid = it["overlord"].ToString();
                city.NonAggressionPacts = ReadTimestampDict(it, "naps", city.NonAggressionPacts);
                city.WarJustifications = ReadTimestampDict(it, "warjustifications", city.WarJustifications);
                if (it.Table.Columns.Contains("vassalsince") && long.TryParse(it["vassalsince"].ToString(), out long vs))
                    city.VassalSince = vs;
                if (it.Table.Columns.Contains("vassals"))
                {
                    foreach (string str in it["vassals"].ToString().Split(';'))
                    {
                        if (str.Length == 0) continue;
                        claims.dataStorage.getCityByGUID(str, out City vcity);
                        if (vcity != null) city.VassalCities.Add(vcity);
                    }
                }
            }
            catch (Exception ex) { claims.sapi.Logger.Warning("[claims] Failed to load war/vassal data for '{0}': {1}", city.GetPartName(), ex.Message); }

            // Stored verbatim, not normalized: a server that temporarily narrows the emblem
            // whitelist should not have every city's emblem stripped on the next save. Layers are
            // filtered where they are drawn instead.
            if (it.Table.Columns.Contains("emblem"))
                city.Emblem = it["emblem"].ToString();

            foreach(var citizen in city.getCityCitizens())
            {
                foreach(var title in citizen.getCityTitles())
                {
                    if(city.CustomCityRanks.TryGetValue(title, out var rank))
                    {
                        rank.CitizensNames.Add(citizen.GetPartName());
                    }
                }
            }

            MessageHandler.sendDebugMsg("loadCity::load city" + city.GetPartName());
            return true;

        }

        public override bool loadAllCitis()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM CITIES", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadCity(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllCitis::error" + e.Message);
                return false;
            }
            return true;
        }
        //PLOT
        public override bool loadDummyPlots()
        {
            MessageHandler.sendDebugMsg("Load dummy plots.");
            try
            {
                DataTable dt = readFromDatabase("SELECT x, z, city FROM PLOTS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    if (it["city"].ToString() != "")
                    {
                        claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
                        if (city != null)
                        {
                            Plot tmp = new Plot(new Vec2i(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())));
                            tmp.setCity(city);
                            claims.dataStorage.addClaimedPlot(new PlotPosition(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())), tmp);
                            city.getCityPlots().Add(tmp);
                        }
                        continue;
                    }
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadDummyPlots::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool savePlot(Plot plot, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", plot.GetPartName() },
                { "@x", plot.getPos().X },
                { "@z", plot.getPos().Y},
                { "@city", plot.hasCity() ? plot.getCity().Guid :"" },
                { "@ownerofplot", plot.hasPlotOwner() ? plot.getPlotOwner().Guid : ""},
                { "@type", (int)plot.Type },
                { "@price", plot.Price },
                { "@customtax", plot.getCustomTax() },
                { "@perms", plot.getPermsHandler().ToString() },
                { "@plotgroupguid", plot.hasPlotGroup() ? plot.getPlotGroup().Guid : "" },
                { "@markednopvp", plot.MarkedNoPvp },
                { "@plotdesc", plot.PlotDesc?.Serialize(plot) ?? "" },
                { "@extraBought", plot.extraBought },
                { "@wascaptured", plot.WasCaptured },
                { "@timestampclaimed", plot.TimeStampClaimed },
                { "@lastpaidprice", plot.lastPaidPrice }
            };

            queryQueue.Enqueue(new QuerryInfo("PLOTS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabasePlot(Plot plot)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@x", plot.getPos().X},
                { "@z", plot.getPos().Y }
            };

            queryQueue.Enqueue(new QuerryInfo("PLOTS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadPlot(DataRow it)
        {
            claims.dataStorage.GetPlot(new PlotPosition(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())), out Plot plot);
            if (plot == null)
            {
                MessageHandler.sendErrorMsg("loadPlot at " + it["x"].ToString() + " " + it["z"].ToString() + " failed");
                return false;
            }
            plot.SetPartName(it["name"].ToString());
            if (it["ownerofplot"].ToString().Length != 0)
            {
                if (claims.dataStorage.GetPlayerByUid(it["ownerofplot"].ToString(), out PlayerInfo playerInfo))
                {
                    playerInfo.PlayerPlots.Add(plot);
                    plot.setPlotOwner(playerInfo);
                }
                else
                {
                    MessageHandler.sendErrorMsg("loadPlot: owner '" + it["ownerofplot"].ToString() + "' of plot at " + it["x"] + " " + it["z"] + " not found, ownership dropped");
                }
            }

            plot.Type = (PlotType)(int.Parse(it["type"].ToString()));
            plot.Price = int.Parse(it["price"].ToString());
            plot.setCustomTax(int.Parse(it["customtax"].ToString(), CultureInfo.InvariantCulture));
            plot.getPermsHandler().setPerms(it["perms"].ToString());
            if (claims.dataStorage.getCityPlotsGroupsDict().TryGetValue(it["plotgroupguid"].ToString(), out CityPlotsGroup cityPlotsGroup))
                plot.setPlotGroup(cityPlotsGroup);
            plot.MarkedNoPvp = it["markednopvp"].ToString().Equals("0") ? false : true;
            PlotDesc plotDesc = PlotDesc.Load(plot.Type);
            if (plotDesc != null)
            {
                plotDesc.Deserialize(it["plotdesc"].ToString(), plot);
                plot.PlotDesc = plotDesc;
            }
            plot.extraBought = it["extraBought"].ToString().Equals("0") ? false : true;
            plot.WasCaptured = it["wascaptured"].ToString().Equals("0") ? false : true;
            if (it.Table.Columns.Contains("timestampclaimed") && it["timestampclaimed"] != DBNull.Value)
            {
                plot.TimeStampClaimed = long.Parse(it["timestampclaimed"].ToString());
            }
            if (it.Table.Columns.Contains("lastpaidprice") && it["lastpaidprice"] != DBNull.Value)
            {
                plot.lastPaidPrice = long.Parse(it["lastpaidprice"].ToString());
            }
            return true;
        }

        public override bool loadAllPlots()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM PLOTS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadPlot(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllPlots::error" + e.Message);
                return false;
            }
            return true;
        }
        //WORLD
        public override bool loadDummyWolrdInfo()
        {
            MessageHandler.sendDebugMsg("Load dummy world.");
            string a = claims.sapi.World.Seed.ToString();
            DataTable dt = readFromDatabase("SELECT * FROM WORLDS", new Dictionary<string, object> { { "@name", claims.sapi.World.Seed.ToString() } });
            foreach (DataRow dr in dt.Rows)
            {
                claims.dataStorage.setWorldInfo(new WorldInfo(dt.Rows[0]["name"].ToString(), dt.Rows[0]["guid"].ToString()));
                var world = claims.dataStorage.getWorldInfo();
                world.SetPartName(dr[0].ToString());
                world.Guid = dt.Rows[0]["guid"].ToString();
                world.fireEverywhere = dr["fireeverywhere"].ToString().Equals("0") ? false : true;
                world.pvpEverywhere = dr["pvpeverywhere"].ToString().Equals("0") ? false : true;
                world.blastEverywhere = dr["blasteverywhere"].ToString().Equals("0") ? false : true;
                world.fireForbidden = dr["fireforbidden"].ToString().Equals("0") ? false : true;
                world.pvpForbidden = dr["pvpforbidden"].ToString().Equals("0") ? false : true;
                world.blastForbidden = dr["blastforbidden"].ToString().Equals("0") ? false : true;
            }
            return true;
        }

        public override bool saveWorldInfo(WorldInfo worldInfo, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", worldInfo.GetPartName() },
                { "@guid", worldInfo.Guid },
                { "@pvpeverywhere", worldInfo.pvpEverywhere},
                { "@fireeverywhere", worldInfo.fireEverywhere },
                { "@blasteverywhere", worldInfo.blastEverywhere },
                { "@fireforbidden", worldInfo.fireForbidden },
                { "@pvpforbidden", worldInfo.pvpForbidden },
                { "@blastforbidden", worldInfo.blastForbidden }
            };

            queryQueue.Enqueue(new QuerryInfo("WORLDS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseWorldInfo(WorldInfo worldInfo)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", worldInfo.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("WORLDS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadWorldInfo(DataRow it)
        {
            claims.dataStorage.setWorldInfo(new WorldInfo(it["name"].ToString(), it["guid"].ToString()));
            var world = claims.dataStorage.getWorldInfo();
            world.fireEverywhere = !it["fireeverywhere"].ToString().Equals("0");
            world.pvpEverywhere = !it["pvpeverywhere"].ToString().Equals("0");
            world.blastEverywhere = !it["blasteverywhere"].ToString().Equals("0");
            world.fireForbidden = !it["fireforbidden"].ToString().Equals("0");
            world.pvpForbidden = !it["pvpforbidden"].ToString().Equals("0");
            world.blastForbidden = !it["blastforbidden"].ToString().Equals("0");
            return true;
        }
        //OTHER
        public override bool saveEveryThing()
        {
            drainQueue();
            return true;
        }

        public ConcurrentQueue<QuerryInfo> getQueue()
        {
            return queryQueue;
        }

        public override bool makeBackup(string fileName)
        {
            string cs;
            if (claims.config.FULL_BACKUP_FOLDER.Length == 0)
            {
                string folderPath = @"" + Path.Combine(GamePaths.DataPath, claims.config.BACKUP_FOLDER_NAME_IN_DATA_FOLDER);
                if(!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    cs = @"" + Path.Combine(folderPath, fileName);
                }
                else
                {
                    cs = @"" + Path.Combine(folderPath, fileName);
                }

            }
            else
            {
                cs = Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, fileName);
            }
            try
            {
                using (SqliteConnection dest = new SqliteConnection(@"Data Source=" + cs))
                {
                    dest.Open();
                    this.getConnection().BackupDatabase(dest, "main", "main");
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("[claims] makeBackup::" + fileName + " - " + e.Message);
                return false;
            }
            return true;
        }
        //PRISON
        public override bool savePrison(Prison prison, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", prison.GetPartName() },
                { "@guid", prison.Guid },
                { "@x", prison.Plot.getPos().X },
                { "@z", prison.Plot.getPos().Y },
                { "@prisonCells", prison.SerializeCells() },
                { "@city", prison.City.Guid },
            };

            queryQueue.Enqueue(new QuerryInfo("PRISONS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabasePrison(Prison prison)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", prison.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("PRISONS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadPrison(DataRow it)
        {
            if (!claims.dataStorage.getPrison(it["guid"].ToString(), out Prison prison))
            {
                MessageHandler.sendErrorMsg("loadPrison: prison '" + it["guid"] + "' not found in dummy set, skipped");
                return false;
            }
            prison.DeserializeCells(it["prisonCells"].ToString());
            claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
            prison.City = city;
            claims.dataStorage.GetPlot(new PlotPosition(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())), out Plot plot);
            prison.Plot = plot;
            return true;
        }

        public override bool loadAllPrisons()
        {
            MessageHandler.sendDebugMsg("Load all prisons.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM PRISONS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadPrison(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllPrisons::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool loadDummyPrisons()
        {
            MessageHandler.sendDebugMsg("Load dummy prisons.");
            DataTable dt = readFromDatabase("SELECT guid, city, name, x, z FROM PRISONS", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                if (!claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city))
                {
                    MessageHandler.sendErrorMsg("loadDummyPrisons: city '" + it["city"] + "' for prison '" + it["guid"] + "' not found, skipped");
                    continue;
                }
                Prison tmp = new Prison(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addPrison(tmp);
                city.getPrisons().Add(tmp);
            }
            return true;
        }

        //CITYPLOTSGROUP
        public override bool saveCityPlotGroup(CityPlotsGroup plotgroup, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", plotgroup.GetPartName() },
                { "@guid", plotgroup.Guid },
                { "@perms", plotgroup.PermsHandler.ToString() },
                { "@players", JsonConvert.SerializeObject(plotgroup.PlayersList.Select(pl => pl.Guid)) },
                { "@plotsgroupfee", plotgroup.PlotsGroupFee },
                { "@city", plotgroup.City.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("CITYPLOTSGROUP", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseCityPlotGroup(CityPlotsGroup plotgroup)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", plotgroup.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("CITYPLOTSGROUP", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadCityPlotGroup(DataRow it)
        {
            claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
            if (city == null)
            {
                return false;
            }

            if (!claims.dataStorage.getCityPlotsGroupsDict().TryGetValue(it["guid"].ToString(), out CityPlotsGroup cityPlotsGroup))
            {
                MessageHandler.sendErrorMsg("loadCityPlotGroup: group shell '" + it["guid"] + "' not found, skipped");
                return false;
            }
            cityPlotsGroup.City = city;
            claims.dataStorage.addPlotsGroup(cityPlotsGroup);
            cityPlotsGroup.PermsHandler.setPerms(it["perms"].ToString());

            foreach(string pl in JsonConvert.DeserializeObject<List<string>>(it["players"].ToString()))
            {
                if (pl.Length == 0)
                    continue;

                if (!claims.dataStorage.GetPlayerByUid(pl, out PlayerInfo plTmp))
                {
                    MessageHandler.sendErrorMsg("loadCityPlotGroup: member '" + pl + "' not found, skipped");
                    continue;
                }
                cityPlotsGroup.PlayersList.Add(plTmp);
            }

            cityPlotsGroup.PlotsGroupFee = int.Parse(it["plotsgroupfee"].ToString(), CultureInfo.InvariantCulture);
            return true;
        }

        public override bool loadAllCityPlotGroups()
        {
            MessageHandler.sendDebugMsg("Load all cityplotgroups.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM CITYPLOTSGROUP", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadCityPlotGroup(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllCityPlotGroups::error" + e.Message);
                return false;
            }
            return true;
        }
        public override bool loadDummyCityPlotGroups()
        {
            DataTable dt = readFromDatabase("SELECT guid, name FROM CITYPLOTSGROUP", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                CityPlotsGroup cpg = new CityPlotsGroup(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addPlotsGroup(cpg);
            }
            return true;
        }

        //ALLIANCE
        public override bool saveAlliance(Alliance alliance, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", alliance.GetPartName() },
                { "@guid", alliance.Guid },
                { "@maincity", alliance.MainCity.Guid},
                { "@cities", StringFunctions.concatStringsWithDelim(alliance.Cities, ';') },
                { "@hostiles", string.Join(";", alliance.HostileParties.Select(p => (p is City ? "c:" : "a:") + p.Guid)) },
                { "@comrades", StringFunctions.concatStringsWithDelim(alliance.ComradAlliancies, ';') },
                { "@alliancefee", alliance.AllianceFee },
                { "@neutral", alliance.Neutral },
                { "@prefix", alliance.Prefix },
                { "@timestampcreated", alliance.TimeStampCreated },
                { "@pendingunionbreaks", JsonConvert.SerializeObject(alliance.PendingUnionBreaks) },
                { "@unionbreakcooldowns", JsonConvert.SerializeObject(alliance.UnionBreakCooldowns) },
                { "@emblem", alliance.Emblem ?? "" }
            };

            queryQueue.Enqueue(new QuerryInfo("ALLIANCIES", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseAlliance(Alliance alliance)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", alliance.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("ALLIANCIES", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadAlliance(DataRow it)
        {
            if (!claims.dataStorage.GetAllianceByGUID(it["guid"].ToString(), out Alliance alliance))
            {
                MessageHandler.sendErrorMsg("loadAlliance: alliance '" + it["guid"] + "' not found in dummy set, skipped");
                return false;
            }
            claims.dataStorage.getCityByGUID(it["maincity"].ToString(), out City city);
            alliance.MainCity = city;
            foreach (string str in it["cities"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                if (!claims.dataStorage.getCityByGUID(str, out City cityToAdd))
                {
                    MessageHandler.sendErrorMsg("loadAlliance: member city '" + str + "' of alliance '" + alliance.GetPartName() + "' not found, skipped");
                    continue;
                }
                alliance.Cities.Add(cityToAdd);
                cityToAdd.Alliance = alliance;
            }

            foreach (string str in it["hostiles"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                string type = "alliance";
                string guid = str;
                if (str.StartsWith("c:")) { type = "city"; guid = str.Substring(2); }
                else if (str.StartsWith("a:")) { guid = str.Substring(2); }
                claims.dataStorage.GetConflictPartyByGuid(guid, type, out IConflictParty hostile);
                if (hostile != null)
                    alliance.HostileParties.Add(hostile);
            }
            foreach (string str in it["comrades"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                if (!claims.dataStorage.GetAllianceByGUID(str, out Alliance alliance1))
                {
                    MessageHandler.sendErrorMsg("loadAlliance: comrade alliance '" + str + "' of '" + alliance.GetPartName() + "' not found, skipped");
                    continue;
                }
                alliance.ComradAlliancies.Add(alliance1);
            }

            alliance.AllianceFee = int.Parse(it["allianceFee"].ToString());
            alliance.Neutral = it["neutral"].ToString().Equals("0") ? false : true;
            alliance.Prefix = it["prefix"].ToString();
            alliance.Leader = alliance.MainCity?.getMayor();
            alliance.TimeStampCreated = long.Parse(it["timestampcreated"].ToString());

            try
            {
                alliance.PendingUnionBreaks = ReadTimestampDict(it, "pendingunionbreaks", alliance.PendingUnionBreaks);
                alliance.UnionBreakCooldowns = ReadTimestampDict(it, "unionbreakcooldowns", alliance.UnionBreakCooldowns);
            }
            catch (Exception ex) { claims.sapi.Logger.Warning("[claims] Failed to load union data for '{0}': {1}", alliance.GetPartName(), ex.Message); }

            if (it.Table.Columns.Contains("emblem"))
                alliance.Emblem = it["emblem"].ToString();
            return true;
        }

        public override bool loadAllAlliancies()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM ALLIANCIES", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadAlliance(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllAlliancies::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool loadDummyAlliancies()
        {
            MessageHandler.sendDebugMsg("Load dummy alliancies.");
            DataTable dt = readFromDatabase("SELECT name, guid FROM ALLIANCIES", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                Alliance tmp = new Alliance(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addAlliance(tmp);
            }
            return true;
        }

        private static string GetPartyType(IConflictParty party)
            => party is City ? "city" : "alliance";

        //CONFLICT
        public override bool saveConflict(Conflict conflict, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", conflict.GetPartName() },
                { "@guid", conflict.Guid },
                { "@firstside", conflict.First.Guid},
                { "@secondside", conflict.Second.Guid },
                { "@conflictstate", conflict.State },
                { "@startedby", conflict.StartedBy.Guid },
                { "@warranges", JsonConvert.SerializeObject(conflict.WarRanges) },
                { "@firstwarranges", JsonConvert.SerializeObject(conflict.FirstWarRanges) },
                { "@secondwarranges", JsonConvert.SerializeObject(conflict.SecondWarRanges) },
                { "@minimumdaysbetweenbattles", conflict.MinimumDaysBetweenBattles },
                { "@lastbattledatestart", JsonConvert.SerializeObject(conflict.LastBattleDateStart) },
                { "@lastbattledateend", JsonConvert.SerializeObject(conflict.LastBattleDateEnd) },
                { "@nextbattledatestart", JsonConvert.SerializeObject(conflict.NextBattleDateStart) },
                { "@nextbattledateend", JsonConvert.SerializeObject(conflict.NextBattleDateEnd) },
                { "@timestampstarted", conflict.TimeStampStarted },
                { "@firstside_type", GetPartyType(conflict.First) },
                { "@secondside_type", GetPartyType(conflict.Second) },
                { "@startedby_type", GetPartyType(conflict.StartedBy) },
                { "@firstscore", conflict.FirstScore },
                { "@secondscore", conflict.SecondScore },
                { "@firstplotscaptured", conflict.FirstPlotsCaptured },
                { "@secondplotscaptured", conflict.SecondPlotsCaptured },
                { "@firstkills", conflict.FirstKills },
                { "@secondkills", conflict.SecondKills },
                { "@firstpillaged", conflict.FirstPillaged },
                { "@secondpillaged", conflict.SecondPillaged },
            };

            queryQueue.Enqueue(new QuerryInfo("CONFLICTS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }
        public override bool loadConflict(DataRow it)
        {
            string firstType = it.Table.Columns.Contains("firstside_type") ? it["firstside_type"].ToString() : "alliance";
            string secondType = it.Table.Columns.Contains("secondside_type") ? it["secondside_type"].ToString() : "alliance";
            string startedByType = it.Table.Columns.Contains("startedby_type") ? it["startedby_type"].ToString() : "alliance";

            claims.dataStorage.GetConflictPartyByGuid(it["firstside"].ToString(), firstType, out IConflictParty firstSide);
            claims.dataStorage.GetConflictPartyByGuid(it["secondside"].ToString(), secondType, out IConflictParty secondSide);

            if (firstSide == null || secondSide == null)
            {
                return false;
            }
            Conflict tmpConflict = new Conflict(it["name"].ToString(), it["guid"].ToString());

            tmpConflict.State = ((ConflictState)int.Parse(it["conflictstate"].ToString()));
            tmpConflict.First = firstSide;
            tmpConflict.Second = secondSide;
            claims.dataStorage.GetConflictPartyByGuid(it["startedby"].ToString(), startedByType, out IConflictParty startedBy);
            tmpConflict.StartedBy = startedBy;
            tmpConflict.WarRanges = JsonConvert.DeserializeObject<List<SelectedWarRange>>(it["warranges"].ToString());
            tmpConflict.FirstWarRanges = JsonConvert.DeserializeObject<List<SelectedWarRange>>(it["firstwarranges"].ToString());
            tmpConflict.SecondWarRanges = JsonConvert.DeserializeObject<List<SelectedWarRange>>(it["secondwarranges"].ToString());
            tmpConflict.MinimumDaysBetweenBattles = int.Parse(it["minimumdaysbetweenbattles"].ToString());
            tmpConflict.LastBattleDateStart = JsonConvert.DeserializeObject<DateTime>(it["lastbattledatestart"].ToString());
            tmpConflict.LastBattleDateEnd = JsonConvert.DeserializeObject<DateTime>(it["lastbattledateend"].ToString());
            tmpConflict.NextBattleDateStart = JsonConvert.DeserializeObject<DateTime>(it["nextbattledatestart"].ToString());
            tmpConflict.NextBattleDateEnd = JsonConvert.DeserializeObject<DateTime>(it["nextbattledateend"].ToString());

            tmpConflict.TimeStampStarted = long.Parse(it["timestampstarted"].ToString());

            if (it.Table.Columns.Contains("firstscore") && int.TryParse(it["firstscore"].ToString(), out int fScore))
                tmpConflict.FirstScore = fScore;
            if (it.Table.Columns.Contains("secondscore") && int.TryParse(it["secondscore"].ToString(), out int sScore))
                tmpConflict.SecondScore = sScore;
            if (it.Table.Columns.Contains("firstplotscaptured") && int.TryParse(it["firstplotscaptured"].ToString(), out int fPlots))
                tmpConflict.FirstPlotsCaptured = fPlots;
            if (it.Table.Columns.Contains("secondplotscaptured") && int.TryParse(it["secondplotscaptured"].ToString(), out int sPlots))
                tmpConflict.SecondPlotsCaptured = sPlots;
            if (it.Table.Columns.Contains("firstkills") && int.TryParse(it["firstkills"].ToString(), out int fKills))
                tmpConflict.FirstKills = fKills;
            if (it.Table.Columns.Contains("secondkills") && int.TryParse(it["secondkills"].ToString(), out int sKills))
                tmpConflict.SecondKills = sKills;
            if (it.Table.Columns.Contains("firstpillaged") && long.TryParse(it["firstpillaged"].ToString(), out long fPill))
                tmpConflict.FirstPillaged = fPill;
            if (it.Table.Columns.Contains("secondpillaged") && long.TryParse(it["secondpillaged"].ToString(), out long sPill))
                tmpConflict.SecondPillaged = sPill;

            tmpConflict.First.RunningConflicts.Add(tmpConflict);
            tmpConflict.Second.RunningConflicts.Add(tmpConflict);

            claims.dataStorage.TryAddConflict(tmpConflict);
            return true;
        }
        public override bool loadConflicts()
        {
            MessageHandler.sendDebugMsg("Load all CONFLICTS.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM CONFLICTS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadConflict(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllConflicts::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool deleteFromDatabaseConflict(Conflict conflict)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", conflict.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("CONFLICTS", QuerryType.DELETE, tmpDict));
            return true;
        }

        //PENDING CONFLICT LETTERS
        public override bool saveConflictLetter(ConflictLetter letter, bool update = true)
        {
            if (letter == null || !ConflictLetterFactory.IsPersistable(letter.Purpose)) return false;

            PeaceTerms terms = letter.Terms ?? new PeaceTerms();
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", letter.Guid },
                { "@fromside", letter.From.Guid },
                { "@fromside_type", GetPartyType(letter.From) },
                { "@toside", letter.To.Guid },
                { "@toside_type", GetPartyType(letter.To) },
                { "@purpose", (int)letter.Purpose },
                { "@timestampexpire", letter.TimeStampExpire },
                { "@termtype", (int)terms.Type },
                { "@termamount", terms.Amount },
                { "@termplotx", terms.CededPlot?.X ?? 0 },
                { "@termplotz", terms.CededPlot?.Z ?? 0 },
                { "@termhasplot", terms.CededPlot != null ? 1 : 0 },
                { "@napdays", letter.NapDays }
            };

            queryQueue.Enqueue(new QuerryInfo("CONFLICTLETTERS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }
        public override bool deleteFromDatabaseConflictLetter(ConflictLetter letter)
        {
            return letter != null && deleteConflictLetterByGuid(letter.Guid);
        }
        public override bool saveUnionLetter(UnionLetter letter, bool update = true)
        {
            if (letter?.From == null || letter.To == null) return false;

            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", letter.Guid },
                { "@fromside", letter.From.Guid },
                { "@toside", letter.To.Guid },
                { "@purpose", (int)letter.Purpose },
                { "@timestampexpire", letter.TimeStampExpire }
            };

            queryQueue.Enqueue(new QuerryInfo("UNIONLETTERS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }
        public override bool deleteUnionLetterByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            queryQueue.Enqueue(new QuerryInfo("UNIONLETTERS", QuerryType.DELETE,
                new Dictionary<string, object> { { "@guid", guid } }));
            return true;
        }
        public override bool loadUnionLetters()
        {
            MessageHandler.sendDebugMsg("Load all UNIONLETTERS.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            List<string> staleGuids = new List<string>();
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM UNIONLETTERS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    string guid = it["guid"].ToString();
                    long expire = long.Parse(it["timestampexpire"].ToString());
                    UnionLetterPurpose purpose = (UnionLetterPurpose)int.Parse(it["purpose"].ToString());

                    if (!claims.dataStorage.GetAllianceByGUID(it["fromside"].ToString(), out Alliance from)
                        || !claims.dataStorage.GetAllianceByGUID(it["toside"].ToString(), out Alliance to))
                    {
                        staleGuids.Add(guid);
                        continue;
                    }

                    // A letter whose premise is gone (offering a union that already exists, offering to
                    // dissolve one that does not) would sit in the GUI doing nothing when answered.
                    bool allied = UnionHander.unionAlreadyExist(from, to);
                    if ((purpose == UnionLetterPurpose.Form && allied) ||
                        (purpose == UnionLetterPurpose.Dissolve && !allied))
                    {
                        staleGuids.Add(guid);
                        continue;
                    }

                    UnionLetter letter = UnionLetterFactory.Build(from, to, purpose, expire, guid);
                    if (letter == null || !UnionHander.addUnionLetter(letter, persist: false))
                    {
                        staleGuids.Add(guid);
                    }
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadUnionLetters::error" + e.Message);
                return false;
            }

            foreach (string guid in staleGuids)
            {
                queryQueue.Enqueue(new QuerryInfo("UNIONLETTERS", QuerryType.DELETE,
                    new Dictionary<string, object> { { "@guid", guid } }));
            }
            return true;
        }
        public override bool deleteConflictLetterByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", guid }
            };

            queryQueue.Enqueue(new QuerryInfo("CONFLICTLETTERS", QuerryType.DELETE, tmpDict));
            return true;
        }
        public override bool loadConflictLetters()
        {
            MessageHandler.sendDebugMsg("Load all CONFLICTLETTERS.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            long now = TimeFunctions.getEpochSeconds();
            List<string> staleGuids = new List<string>();
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM CONFLICTLETTERS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    string guid = it["guid"].ToString();
                    long expire = long.Parse(it["timestampexpire"].ToString());
                    // Anything that ran out while the server was down is handled by the normal
                    // expiry path (which fires OnDeny) instead of silently vanishing.
                    LetterPurpose purpose = (LetterPurpose)int.Parse(it["purpose"].ToString());

                    claims.dataStorage.GetConflictPartyByGuid(it["fromside"].ToString(), it["fromside_type"].ToString(), out IConflictParty from);
                    claims.dataStorage.GetConflictPartyByGuid(it["toside"].ToString(), it["toside_type"].ToString(), out IConflictParty to);
                    if (from == null || to == null)
                    {
                        staleGuids.Add(guid);
                        continue;
                    }

                    // A letter whose premise is gone (peace offer for a finished war, declaration for
                    // a war that already runs) would sit in the GUI doing nothing when answered.
                    bool warRuns = ConflictHandler.conflictAlreadyExist(from, to);
                    if ((purpose == LetterPurpose.END_CONFLICT && !warRuns) ||
                        (purpose == LetterPurpose.START_CONFLICT && warRuns))
                    {
                        staleGuids.Add(guid);
                        continue;
                    }

                    PeaceTerms terms = new PeaceTerms((PeaceTermType)int.Parse(it["termtype"].ToString()),
                                                      long.Parse(it["termamount"].ToString()));
                    if (int.Parse(it["termhasplot"].ToString()) == 1)
                    {
                        terms.CededPlot = new PlotPosition(int.Parse(it["termplotx"].ToString()),
                                                           int.Parse(it["termplotz"].ToString()));
                    }

                    ConflictLetter letter = ConflictLetterFactory.Build(from, to, purpose, expire, guid,
                        terms, int.Parse(it["napdays"].ToString()));
                    if (letter == null || !ConflictHandler.addConflictLetter(letter, persist: false))
                    {
                        staleGuids.Add(guid);
                    }
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadConflictLetters::error" + e.Message);
                return false;
            }

            foreach (string guid in staleGuids)
            {
                queryQueue.Enqueue(new QuerryInfo("CONFLICTLETTERS", QuerryType.DELETE,
                    new Dictionary<string, object> { { "@guid", guid } }));
            }
            return true;
        }
    }
}
