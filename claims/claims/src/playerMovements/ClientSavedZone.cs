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
        [ProtoMember(2)]
        public Dictionary<Vec2i, SavedPlotInfo> savedPlots;

        /// <summary>Whether this zone changed since it was last written to the local map database.
        /// Deliberately without a ProtoMember: it describes the copy in memory, not the stored one,
        /// and adding it to the contract would change the blob format of existing databases.</summary>
        public bool Dirty;

        public ClientSavedZone()
        {
            timestamp = 0;
            savedPlots = new Dictionary<Vec2i, SavedPlotInfo>();
        }
        public ClientSavedZone(Dictionary<Vec2i, SavedPlotInfo> dict)
        {
            timestamp = 0;
            savedPlots = dict;
        }
        public void addClientSavedPlots(Vec2i vec, SavedPlotInfo savedPlotInfo)
        {
            this.savedPlots[vec] = savedPlotInfo;
            Dirty = true;
        }
        public bool removeClientSavedPlot(Vec2i vec)
        {
            if (!this.savedPlots.Remove(vec)) return false;
            Dirty = true;
            return true;
        }

        /// <summary>Takes the zone's plots wholesale, as a fresh answer from the server does.</summary>
        public void ReplacePlots(Dictionary<Vec2i, SavedPlotInfo> plots, long ts)
        {
            this.savedPlots = plots;
            this.timestamp = ts;
            Dirty = true;
        }
    }
}
