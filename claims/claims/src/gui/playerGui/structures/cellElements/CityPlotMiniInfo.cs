using claims.src.part.structure;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class CityPlotMiniInfo
    {
        public int X { get; set; }
        public int Z { get; set; }
        public PlotType Type { get; set; }
        public CityPlotMiniInfo() { }
        public CityPlotMiniInfo(int x, int z, PlotType type)
        {
            X = x;
            Z = z;
            Type = type;
        }
    }
}
