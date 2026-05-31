using System.Collections.Generic;
using claims.src.auxialiry;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace claims.src.clextentions
{
    public class PlotEvents
    {
        public static Dictionary<string, long> lastTimePlayerAskedForPlotsAround;
             
        public static void updatedPlotHandlerUnclaimed(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            int chX = tree.GetInt("chX");
            int chZ = tree.GetInt("chZ");
            int chY = tree.GetInt("chY", PlotPosition.COLUMN_Y);
            PlotStateHandling.broadcastPlotUnclaimedInZone(chX, chZ, chY);
        }
        public static void updatedPlotHandlerClaimed(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            int chX = tree.GetInt("chX");
            int chZ = tree.GetInt("chZ");
            int chY = tree.GetInt("chY", PlotPosition.COLUMN_Y);
            var pp = new PlotPosition(chX, chZ) { LayerY = chY };
            if (claims.dataStorage.GetPlot(pp, out var plot))
            {
                PlotStateHandling.broadcastPlotClaimedInZone(plot);
            }
        }
    }
}
