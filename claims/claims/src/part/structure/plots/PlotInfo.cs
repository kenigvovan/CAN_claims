using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace claims.src.part.structure.plots
{
    public class PlotInfo
    {
        public static Dictionary<PlotType, PlotInfo> dictPlotTypes;

        public static Dictionary<string, PlotType> nameToPlotType;
        public static Dictionary<string, string> plotAccessableForPlayersWithCode;
        
        public static void initDicts()
        {
            dictPlotTypes = new Dictionary<PlotType, PlotInfo>
            {
                {PlotType.DEFAULT, new PlotInfo("default", claims.config.DEFAULT_PLOT_COST) },
                {PlotType.TOURNAMENT, new PlotInfo("tournament", claims.config.TOURNAMENT_PLOT_COST)},
                {PlotType.CAMP, new PlotInfo("camp", claims.config.CAMP_PLOT_COST) },
                {PlotType.TEMPLE, new PlotInfo("temple", claims.config.TEMPLE_PLOT_COST) },
                {PlotType.FARM, new PlotInfo("farm", claims.config.FARM_PLOT_COST) },
                {PlotType.SUMMON, new PlotInfo("summon", claims.config.SUMMON_PLOT_COST) },
                {PlotType.EMBASSY, new PlotInfo("embassy", claims.config.EMBASSY_PLOT_COST) },
                {PlotType.TAVERN, new PlotInfo("tavern", claims.config.TAVERN_PLOT_COST) },
                {PlotType.MAIN_CITY_PLOT, new PlotInfo("mainplot", claims.config.MAIN_CITYPLOT_COST) },
                {PlotType.PRISON, new PlotInfo("prison", claims.config.PRISON_PLOT_COST) },
                {PlotType.ORCHARD, new PlotInfo("orchard", claims.config.ORCHARD_PLOT_COST) },
                // Villages pay no daily upkeep at all, so their main plot costs nothing.
                {PlotType.VILLAGE_MAIN, new PlotInfo("villagemain", 0) }
            };
            nameToPlotType = new Dictionary<string, PlotType>
            {
                {"default", PlotType.DEFAULT },
                {"tournament", PlotType.TOURNAMENT },
                {"camp", PlotType.CAMP },
                {"temple", PlotType.TEMPLE },
                {"farm", PlotType.FARM },
                {"summon", PlotType.SUMMON },
                {"embassy", PlotType.EMBASSY },
                {"tavern", PlotType.TAVERN },
                {"MAIN_CITY_PLOT", PlotType.MAIN_CITY_PLOT },
                {"prison", PlotType.PRISON },
                {"orchard", PlotType.ORCHARD },
                // Upper case like MAIN_CITY_PLOT: set by the code when founding, never typed by a player.
                {"VILLAGE_MAIN", PlotType.VILLAGE_MAIN }
            };
            plotAccessableForPlayersWithCode = new Dictionary<string, string> {
                { "default", "claims:default_plot_type" },
                { "farm", "claims:farm_plot_type" },
                { "orchard", "claims:orchard_plot_type" },
                { "summon", "claims:summon_plot_type" },
                { "embassy", "claims:embassy_plot_type" },
                { "tavern", "claims:tavern_plot_type" },
                { "prison", "claims:prison_plot_type" },
                { "temple", "claims:temple_plot_type" }
            };
        }
        public static void ClearDicts()
        {
            dictPlotTypes = null;
            nameToPlotType = null;
        }
        double cost;
        string fullName;
        public PlotInfo(string fullName, double cost)
        {
            this.cost = cost;
            this.fullName = fullName;
        }
        public double getCost()
        {
            return cost;
        }
        public string getFullName()
        {
            return fullName;
        }
    }
}
