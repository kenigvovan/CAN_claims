using System.Data;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
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

        //PENDING UNION LETTERS
        abstract public bool loadUnionLetters();
        abstract public bool saveUnionLetter(UnionLetter letter, bool update = true);
        abstract public bool deleteUnionLetterByGuid(string guid);

        //General
        public bool loadEveryThing()
        {
            return loadDummyWolrdInfo()
               && loadDummyCitis()
               && loadDummyPlayers()
               && loadDummyPlots()
               && loadDummyPrisons()
               && loadDummyCityPlotGroups()
               && loadDummyAlliancies()
               && loadAllPlayersInfo()
               && loadAllPlots()
               && loadAllCityPlotGroups()
               && loadAllCitis()        
               && loadAllPrisons()
               && loadAllAlliancies()
               && loadConflicts()
               // after conflicts: a letter references parties that must already exist
               && loadConflictLetters()
               && loadUnionLetters();
        }
        abstract public bool saveEveryThing();

    }
}
