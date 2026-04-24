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

            claims.sapi.Event.Timer((() =>
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
            ), 0.5);
        }
        public SqliteConnection getConnection()
        {
            return SqliteConnection;
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

                //templerespawnpoints
                try
                {
                    command.CommandText = "SELECT templerespawnpoints FROM CITIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CITIES ADD COLUMN templerespawnpoints TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }
                //alliance
                try
                {
                    command.CommandText = "SELECT alliance FROM CITIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CITIES ADD COLUMN alliance TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }

                //alliancetitles
                try
                {
                    command.CommandText = "SELECT alliancetitles FROM PLAYERS LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE PLAYERS ADD COLUMN alliancetitles TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }
                //alliance prefix
                try
                {
                    command.CommandText = "SELECT prefix FROM ALLIANCIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE ALLIANCIES ADD COLUMN prefix TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }

                //alliance timestampcreated
                try
                {
                    command.CommandText = "SELECT timestampcreated FROM ALLIANCIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE ALLIANCIES ADD COLUMN timestampcreated TEXT DEFAULT 0";
                    command.ExecuteNonQuery();
                }

                //conflict firstwarranges
                try
                {
                    command.CommandText = "SELECT firstwarranges FROM CONFLICTS LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN firstwarranges TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }

                //conflict secondwarranges
                try
                {
                    command.CommandText = "SELECT secondwarranges FROM CONFLICTS LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN secondwarranges TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }
                //conflict nextbattledatestart
                try
                {
                    command.CommandText = "SELECT nextbattledatestart FROM CONFLICTS LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN nextbattledatestart TEXT DEFAULT \"0001-01-01T00:00:00\"";
                    command.ExecuteNonQuery();
                }
                //conflict nextbattledateend
                try
                {
                    command.CommandText = "SELECT nextbattledateend FROM CONFLICTS LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN nextbattledateend TEXT DEFAULT \"0001-01-01T00:00:00\"";
                    command.ExecuteNonQuery();
                }

                //city hotiles
                try
                {
                    command.CommandText = "SELECT hostiles FROM CITIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CITIES ADD COLUMN hostiles TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }

                //city comrades
                try
                {
                    command.CommandText = "SELECT comrades FROM CITIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CITIES ADD COLUMN comrades TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }

                //wascaptured
                try
                {
                    command.CommandText = "SELECT wascaptured FROM PLOTS LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE PLOTS ADD COLUMN wascaptured INTEGER DEFAULT 0";
                    command.ExecuteNonQuery();
                }

                //city ranks
                try
                {
                    command.CommandText = "SELECT ranks FROM CITIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch
                {
                    command.CommandText = "ALTER TABLE CITIES ADD COLUMN ranks TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }

                //conflict party types
                try { command.CommandText = "SELECT firstside_type FROM CONFLICTS LIMIT 1"; command.ExecuteScalar(); }
                catch { command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN firstside_type TEXT DEFAULT 'alliance'"; command.ExecuteNonQuery(); }
                try { command.CommandText = "SELECT secondside_type FROM CONFLICTS LIMIT 1"; command.ExecuteScalar(); }
                catch { command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN secondside_type TEXT DEFAULT 'alliance'"; command.ExecuteNonQuery(); }
                try { command.CommandText = "SELECT startedby_type FROM CONFLICTS LIMIT 1"; command.ExecuteScalar(); }
                catch { command.CommandText = "ALTER TABLE CONFLICTS ADD COLUMN startedby_type TEXT DEFAULT 'alliance'"; command.ExecuteNonQuery(); }

                //plot claim timestamp
                try { command.CommandText = "SELECT timestampclaimed FROM PLOTS LIMIT 1"; command.ExecuteScalar(); }
                catch { command.CommandText = "ALTER TABLE PLOTS ADD COLUMN timestampclaimed INTEGER DEFAULT 0"; command.ExecuteNonQuery(); }

                //plot last paid price
                try { command.CommandText = "SELECT lastpaidprice FROM PLOTS LIMIT 1"; command.ExecuteScalar(); }
                catch { command.CommandText = "ALTER TABLE PLOTS ADD COLUMN lastpaidprice INTEGER DEFAULT 0"; command.ExecuteNonQuery(); }

            }
            catch (Exception ex)
            {
                claims.sapi.Logger.Error("initializeTables error." + ex.Message);
                return false;
            }

            return true;
        }

        public bool updateDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "CITIES":
                    querryString = QuerryTemplates.UPDATE_CITY;
                    break;
                case "PLAYERS":
                    querryString = QuerryTemplates.UPDATE_PLAYER;
                    break;
                case "CITYPLOTSGROUP":
                    querryString = QuerryTemplates.UPDATE_CITYPLOTGROUP;
                    break;
                case "PRISONS":
                    querryString = QuerryTemplates.UPDATE_PRISON;
                    break;
                case "WORLDS":
                    querryString = QuerryTemplates.UPDATE_WORLD;
                    break;
                case "PLOTS":
                    querryString = QuerryTemplates.UPDATE_PLOT;
                    break;
                case "ALLIANCIES":
                    querryString = QuerryTemplates.UPDATE_ALLIANCE;
                    break;
                case "CONFLICTS":
                    querryString = QuerryTemplates.UPDATE_CONFLICT;
                    break;
            }


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
            string querryString = "";
            switch (querry.targetTable)
            {
                case "CITIES":
                    querryString = QuerryTemplates.DELETE_CITY;
                    break;
                case "PLAYERS":
                    querryString = QuerryTemplates.DELETE_PLAYER;
                    break;
                case "CITYPLOTSGROUP":
                    querryString = QuerryTemplates.DELETE_CITYPLOTGROUP;
                    break;
                case "PRISONS":
                    querryString = QuerryTemplates.DELETE_PRISON;
                    break;
                case "WORLDS":
                    querryString = QuerryTemplates.DELETE_WORLD;
                    break;
                case "PLOTS":
                    querryString = QuerryTemplates.DELETE_PLOT;
                    break;
                case "ALLIANCIES":
                    querryString = QuerryTemplates.DELETE_ALLIANCE;
                    break;
                case "CONFLICTS":
                    querryString = QuerryTemplates.DELETE_CONFLICT;
                    break;
            }


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
            string querryString = "";
            switch (querry.targetTable)
            {
                case "CITIES":
                    querryString = QuerryTemplates.INSERT_CITY;
                    break;
                case "PLAYERS":
                    querryString = QuerryTemplates.INSERT_PLAYER;
                    break;
                case "CITYPLOTSGROUP":
                    querryString = QuerryTemplates.INSERT_CITYPLOTGROUP;
                    break;
                case "PRISONS":
                    querryString = QuerryTemplates.INSERT_PRISON;
                    break;
                case "WORLDS":
                    querryString = QuerryTemplates.INSERT_WORLD;
                    break;
                case "PLOTS":
                    querryString = QuerryTemplates.INSERT_PLOT;
                    break;
                case "ALLIANCIES":
                    querryString = QuerryTemplates.INSERT_ALLIANCE;
                    break;
                case "CONFLICTS":
                    querryString = QuerryTemplates.INSERT_CONFLICT;
                    break;
            }


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
                { "@prisonhoursleft", player.PrisonHoursLeft }

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
            claims.dataStorage.GetPlayerByUid(it["uid"].ToString(), out PlayerInfo tmp);
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
                claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
                tmp.setCity(city);
                city.getCityCitizens().Add(tmp);
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
                { "@ranks", JsonConvert.SerializeObject(city.CustomCityRanks) }
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
                claims.dataStorage.GetPlayerByUid(it["mayor"].ToString(), out PlayerInfo playerInfo);
                city.setMayor(playerInfo);
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
                claims.dataStorage.getPlotsGroup(str, out CityPlotsGroup cityPlotsGroup);
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
                { "@plotdesc",  PlotInfo.getPlotDescByType(plot) },
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
                claims.dataStorage.GetPlayerByUid(it["ownerofplot"].ToString(), out PlayerInfo playerInfo);
                playerInfo.PlayerPlots.Add(plot);
                plot.setPlotOwner(playerInfo);
            }

            plot.Type = (PlotType)(int.Parse(it["type"].ToString()));
            plot.Price = int.Parse(it["price"].ToString());
            plot.setCustomTax(int.Parse(it["customtax"].ToString(), CultureInfo.InvariantCulture));
            plot.getPermsHandler().setPerms(it["perms"].ToString());
            if (claims.dataStorage.getCityPlotsGroupsDict().TryGetValue(it["plotgroupguid"].ToString(), out CityPlotsGroup cityPlotsGroup))
                plot.setPlotGroup(cityPlotsGroup);
            plot.MarkedNoPvp = it["markednopvp"].ToString().Equals("0") ? false : true;
            switch (plot.Type)
            {
                case PlotType.SUMMON:
                    try
                    {
                        PlotDescSummon tmp = JsonConvert.DeserializeObject<PlotDescSummon>(it["plotdesc"].ToString());
                        plot.PlotDesc = tmp;
                        plot.getCity().summonPlots.Add(plot);
                    }
                    catch
                    {
                        return false;
                    }                  
                    break;
                case PlotType.PRISON:
                    PlotDescPrison tmpPri = new PlotDescPrison(it["plotdesc"].ToString());
                    plot.PlotDesc = tmpPri;
                    claims.dataStorage.getPrison(tmpPri.getPrisonGuid(), out Prison prison);
                    plot.Prison = prison;
                    break;
                case PlotType.TAVERN:
                    PlotDescTavern tmpTav = new PlotDescTavern();
                    plot.PlotDesc = tmpTav;
                    tmpTav.fromLoadStringInnerClaims(it["plotdesc"].ToString());
                    break;
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
            throw new NotImplementedException();
        }
        //OTHER
        public override bool saveEveryThing()
        {
            throw new NotImplementedException();
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
            claims.dataStorage.getPrison(it["guid"].ToString(), out Prison prison);
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
                claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
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

            claims.dataStorage.getCityPlotsGroupsDict().TryGetValue(it["guid"].ToString(), out CityPlotsGroup cityPlotsGroup);
            cityPlotsGroup.City = city;
            claims.dataStorage.addPlotsGroup(cityPlotsGroup);
            cityPlotsGroup.PermsHandler.setPerms(it["perms"].ToString());

            foreach(string pl in JsonConvert.DeserializeObject<List<string>>(it["players"].ToString()))
            {
                if (pl.Length == 0)
                    continue;

                claims.dataStorage.GetPlayerByUid(pl, out PlayerInfo plTmp);
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
                { "@timestampcreated", alliance.TimeStampCreated }
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
            claims.dataStorage.GetAllianceByGUID(it["guid"].ToString(), out Alliance alliance);
            claims.dataStorage.getCityByGUID(it["maincity"].ToString(), out City city);
            alliance.MainCity = city;
            foreach (string str in it["cities"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.getCityByGUID(str, out City cityToAdd);
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

                claims.dataStorage.GetAllianceByGUID(str, out Alliance alliance1);
                alliance.ComradAlliancies.Add(alliance1);
            }

            alliance.AllianceFee = int.Parse(it["allianceFee"].ToString());
            alliance.Neutral = it["neutral"].ToString().Equals("0") ? false : true;
            alliance.Prefix = it["prefix"].ToString();
            alliance.Leader = alliance.MainCity.getMayor();
            alliance.TimeStampCreated = long.Parse(it["timestampcreated"].ToString());
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
    }
}
