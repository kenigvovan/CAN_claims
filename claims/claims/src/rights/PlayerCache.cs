using claims.src.auxialiry;

namespace claims.src.rights
{
    public class PlayerCache
    {
        PlotPosition lastChunk;
        public PlotPosition LastChunk { get { return lastChunk; } }
        bool?[] playerCache = new bool?[3];
        public PlayerCache()
        {
            playerCache = new bool?[3]; // use, build, attack in the last plot
        }
        public PlotPosition getLastLocation()
        {
            return lastChunk;
        }
        public void Reset()
        {
            for(int i = 0; i < playerCache.Length; i++)
            {
                playerCache[i] = null;
            }
        }
        public bool?[] getCache()
        {
            return playerCache;
        }
        /// <summary>
        /// Points the cache at a plot. Answers cached for the previous one are dropped: they were
        /// computed against its permissions and mean nothing here.
        ///
        /// Moving the marker without clearing them let one plot answer for another. A block broken
        /// across the border re-points the cache at the neighbouring plot and recomputes only the
        /// build permission, so USE and ATTACK still held what the plot the player stands on had
        /// granted - and the next click on a chest over there was let through unchecked.
        /// </summary>
        public void setPlotPosition(PlotPosition loc)
        {
            if (lastChunk == null || !lastChunk.Equals(loc)) Reset();
            this.lastChunk = loc;
        }
    }
}
