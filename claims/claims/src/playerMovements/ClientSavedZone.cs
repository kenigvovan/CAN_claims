using System.Collections.Generic;
using claims.src.clientMapHandling;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace claims.src.playerMovements
{
    [ProtoContract]
    public class ClientSavedZone
    {
        [ProtoMember(1)]
        public long timestamp;
        // Key: Vec3i(gridX, layerY, gridZ). layerY=-1 for column plots.
        [ProtoMember(2)]
        public Dictionary<Vec3i, SavedPlotInfo> savedPlots;
        public ClientSavedZone()
        {
            timestamp = 0;
            savedPlots = new Dictionary<Vec3i, SavedPlotInfo>();
        }
        public ClientSavedZone(Dictionary<Vec3i, SavedPlotInfo> dict)
        {
            timestamp = 0;
            savedPlots = dict;
        }
        public void addClientSavedPlots(Vec3i vec, SavedPlotInfo savedPlotInfo)
        {
            this.savedPlots[vec] = savedPlotInfo;
        }
        public bool removeClientSavedPlot(Vec3i vec)
        {
            return this.savedPlots.Remove(vec);
        }
    }
}
