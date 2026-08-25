using claims.src.part.structure;

namespace claims.src.part.structure.plots
{
    public class PlotDescTemple : PlotDesc
    {
        // Guarded like the camp and village descriptions: a plot can lose its city before its type
        // is reset, and an unguarded getCity() throws on the way out.
        public override void OnDeactivated(Plot plot)
        {
            if (plot.hasCity()) plot.getCity().RemoveTempleRespawnPoint(plot);
        }
    }
}
