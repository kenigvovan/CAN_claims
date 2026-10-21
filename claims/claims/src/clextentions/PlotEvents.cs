using System.Collections.Generic;
using claims.src.auxialiry;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

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

            // Inside a mass operation the plots are collected and sent as one packet per player;
            // a single unclaim still goes out right away so the map reacts immediately.
            if (PlotStateHandling.BatchActive)
            {
                claims.serverPlayerMovementListener.markPlotToWasRemoved(new Vec2i(chX, chZ));
                return;
            }

            PlotStateHandling.broadcastPlotUnclaimedInZone(chX, chZ);
        }
        public static void updatedPlotHandlerClaimed(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            int chX = tree.GetInt("chX");
            int chZ = tree.GetInt("chZ");

            if (PlotStateHandling.BatchActive)
            {
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(new Vec2i(chX, chZ));
                return;
            }

            if(claims.dataStorage.GetPlot(new PlotPosition(chX, chZ), out var plot))
            {
                PlotStateHandling.broadcastPlotClaimedInZone(plot);
            }
        }
    }
}
