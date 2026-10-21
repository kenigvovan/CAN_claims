namespace claims.src.gui.playerGui.structures.cellElements
{
    /// <summary>
    /// One temple of the player's city they may respawn at. The plot coordinates identify it (that
    /// is what the choice is stored by), the block ones are what the player is shown.
    /// </summary>
    public class TempleRespawnPointInfo
    {
        public int PlotX { get; set; }
        public int PlotZ { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        /// <summary>Plot name, empty when the plot was never named.</summary>
        public string Name { get; set; } = "";

        public TempleRespawnPointInfo() { }
        public TempleRespawnPointInfo(int plotX, int plotZ, int x, int y, int z, string name)
        {
            PlotX = plotX;
            PlotZ = plotZ;
            X = x;
            Y = y;
            Z = z;
            Name = name ?? "";
        }
    }
}
