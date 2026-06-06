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
            if (playerInfo.hasCity() && plot.getCity().Equals(playerInfo.City)) return;
            playerInfo.PlayerPlots.Remove(plot);
            playerInfo.saveToDatabase();
        }
    }
}
