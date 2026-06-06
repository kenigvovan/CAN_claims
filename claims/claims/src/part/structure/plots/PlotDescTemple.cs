using claims.src.part.structure;

namespace claims.src.part.structure.plots
{
    public class PlotDescTemple : PlotDesc
    {
        public override void OnDeactivated(Plot plot) => plot.getCity().RemoveTempleRespawnPoint(plot);
    }
}
