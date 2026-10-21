using claims.src.part;
using claims.src.part.structure;

namespace claims.src.part.structure.plots
{
    public class PlotDescEmbassy : PlotDesc
    {
        public override void OnDeactivated(Plot plot)
        {
            if (!plot.hasPlotOwner()) return;
            PlayerInfo playerInfo = plot.getPlotOwner();
            // hasCity() first: an embassy whose plot has lost its city still has to be taken off its
            // owner, and getCity() would throw on the way there.
            if (plot.hasCity() && playerInfo.hasCity() && plot.getCity().Equals(playerInfo.City)) return;
            // A foreigner may only own the plot while it is an embassy: both sides go, or the plot
            // kept an owner that neither listed it nor belonged to the city (inv2, inv4).
            plot.resetOwner();
            playerInfo.PlayerPlots.Remove(plot);
            playerInfo.saveToDatabase();
        }
    }
}
