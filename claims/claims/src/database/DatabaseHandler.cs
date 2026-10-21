using System;
using System.Collections.Generic;
using System.Data;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.part.structure.plots.auction;
using claims.src.part.structure.union;

namespace claims.src.database
{
    public abstract class DatabaseHandler
    {
        public DatabaseHandler()
        {

        }

        //Maintenance
        abstract public bool makeBackup(string fileName);
        

        //PlayerInfo
        abstract public bool savePlayerInfo(PlayerInfo player, bool update = true);
        abstract public bool deleteFromDatabasePlayerInfo(PlayerInfo player);
        abstract public bool loadPlayerInfo(DataRow it);
        abstract public bool loadAllPlayersInfo();
        abstract public bool loadDummyPlayers();
        //City
        abstract public bool saveCity(City city, bool update = true);
        abstract public bool deleteFromDatabaseCity(City city);
        abstract public bool loadCity(DataRow it);
        abstract public bool loadAllCitis();
        abstract public bool loadDummyCitis();
        //Plot
        abstract public bool savePlot(Plot plot, bool update = true);
        abstract public bool deleteFromDatabasePlot(Plot plot);
        abstract public bool loadPlot(DataRow it);
        abstract public bool loadAllPlots();
        abstract public bool loadDummyPlots();
        //WorldInfo
        abstract public bool saveWorldInfo(WorldInfo worldInfo, bool update = true);
        abstract public bool deleteFromDatabaseWorldInfo(WorldInfo worldInfo);
        abstract public bool loadWorldInfo(DataRow it);
        abstract public bool loadDummyWolrdInfo();

        //PRISON
        abstract public bool savePrison(Prison prison, bool update = true);
        abstract public bool deleteFromDatabasePrison(Prison prison);
        abstract public bool loadPrison(DataRow it);
        abstract public bool loadAllPrisons();
        abstract public bool loadDummyPrisons();

        //CITYPLOTGROUP
        abstract public bool saveCityPlotGroup(CityPlotsGroup plotgroup, bool update = true);
        abstract public bool deleteFromDatabaseCityPlotGroup(CityPlotsGroup plotgroup);
        abstract public bool loadCityPlotGroup(DataRow it);
        abstract public bool loadAllCityPlotGroups();
        abstract public bool loadDummyCityPlotGroups();

        //ALLIANCE
        abstract public bool saveAlliance(Alliance alliance, bool update = true);
        abstract public bool deleteFromDatabaseAlliance(Alliance alliance);
        abstract public bool loadAlliance(DataRow it);
        abstract public bool loadAllAlliancies();
        abstract public bool loadDummyAlliancies();

        //CONFLICT
        abstract public bool loadConflicts();
        abstract public bool loadConflict(DataRow it);
        abstract public bool deleteFromDatabaseConflict(Conflict conflict);
        abstract public bool saveConflict(Conflict conflict, bool update = true);

        //PENDING CONFLICT LETTERS
        abstract public bool loadConflictLetters();
        abstract public bool saveConflictLetter(ConflictLetter letter, bool update = true);
        abstract public bool deleteFromDatabaseConflictLetter(ConflictLetter letter);
        abstract public bool deleteConflictLetterByGuid(string guid);

        //VILLAGE RUINS
        abstract public bool loadVillageRuins();
        abstract public bool saveVillageRuin(int x, int z, long since, bool abandoned, bool update = true);
        abstract public bool deleteVillageRuin(int x, int z);

        //INTER-CITY PLOT SALES
        abstract public bool loadPlotSales();
        abstract public bool savePlotSale(PlotSaleRecord record, bool update = false);

        //LAND AUCTION
        abstract public bool loadAuctions();
        abstract public bool saveAuction(PlotAuction auction, bool update = true);
        abstract public bool deleteAuction(PlotAuction auction);
        abstract public bool saveAuctionBid(AuctionBidRecord bid, bool update = false);

        //SUBPLOTS
        abstract public bool loadSubPlots();
        abstract public bool saveSubPlot(subplots.SubPlot subPlot, bool update = true);
        abstract public bool deleteSubPlot(subplots.SubPlot subPlot);

        //PENDING INVITATIONS (city invitations; see CityInvitation)
        abstract public bool loadInvitations();
        abstract public bool saveInvitation(string kind, string senderGuid, string receiverGuid, long expiresAt);
        abstract public bool deleteInvitation(string kind, string senderGuid, string receiverGuid);

        //PENDING UNION LETTERS
        abstract public bool loadUnionLetters();
        abstract public bool saveUnionLetter(UnionLetter letter, bool update = true);
        abstract public bool deleteUnionLetterByGuid(string guid);

        //General
        /// <summary>
        /// Loads every table in order. A table that fails - false or an exception - is logged and
        /// the rest still load: one bad table used to skip all the following ones in silence.
        /// Returns false if anything failed.
        /// </summary>
        public bool loadEveryThing()
        {
            var steps = new (string Name, Func<bool> Load)[]
            {
                ("world", loadDummyWolrdInfo),
                ("cities (ids)", loadDummyCitis),
                ("players (ids)", loadDummyPlayers),
                ("plots (ids)", loadDummyPlots),
                ("prisons (ids)", loadDummyPrisons),
                ("plot groups (ids)", loadDummyCityPlotGroups),
                ("alliances (ids)", loadDummyAlliancies),
                ("players", loadAllPlayersInfo),
                ("plots", loadAllPlots),
                ("plot groups", loadAllCityPlotGroups),
                ("cities", loadAllCitis),
                ("prisons", loadAllPrisons),
                ("alliances", loadAllAlliancies),
                ("conflicts", loadConflicts),
                // after conflicts: a letter references parties that must already exist
                ("conflict letters", loadConflictLetters),
                ("union letters", loadUnionLetters),
                ("village ruins", loadVillageRuins),
                // after cities: a sale record is matched back to the cities that made it
                ("plot sales", loadPlotSales),
                // after plots and cities: a lot points at both
                ("auctions", loadAuctions),
                // after players and cities: an invitation points at both
                ("invitations", loadInvitations),
                // after plots, players and cities: a sub-plot points at all three
                ("sub-plots", loadSubPlots),
                // after sub-plots: the old tavern rooms become sub-plots, once
                ("tavern rooms", subplots.TavernRoomMigration.Run),
            };

            var failed = new List<string>();
            foreach (var step in steps)
            {
                try
                {
                    if (!step.Load()) failed.Add(step.Name);
                }
                catch (Exception e)
                {
                    failed.Add(step.Name);
                    MessageHandler.sendErrorMsg("loadEveryThing: loading " + step.Name + " threw: " + e);
                }
            }
            if (failed.Count > 0)
            {
                claims.sapi.Logger.Error("[claims] Database loaded with errors, not loaded completely: "
                    + string.Join(", ", failed));
            }
            return failed.Count == 0;
        }
        abstract public bool saveEveryThing();

    }
}
