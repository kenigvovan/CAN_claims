using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using claims.src.auxialiry;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure.war
{
    public class PlotAttack
    {
        public BlockPos BlockPosition { get; set; }
        public long TimestampStart { get; set; }
        // Last time (unix seconds) a "holding" war-score tick was granted for this attack.
        public long LastHoldScoreTimestamp { get; set; }
        //can player leave from city during wartime?
        public string PlayerStartedGuid { get; set; }
        public PlotAttack(BlockPos blockPos, string playerStartedGuid, long timestampStart)
        {
            BlockPosition = blockPos;
            TimestampStart = timestampStart;
            LastHoldScoreTimestamp = timestampStart;
            PlayerStartedGuid = playerStartedGuid;
        }

    }
}
