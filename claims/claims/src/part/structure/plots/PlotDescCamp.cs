using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// A war camp (PlotType.CAMP): a forward outpost of an attacking side during an active war.
    /// Bound to a specific conflict; players of the owning city respawn at <see cref="AnchorPos"/>
    /// while the war window is open. Destroyed when the defender breaks the anchor block or the
    /// conflict ends.
    /// </summary>
    public class PlotDescCamp : PlotDesc
    {
        public string ConflictGuid { get; set; } = "";
        public Vec3i AnchorPos { get; set; }
        // Remaining anchor breaks before the camp is destroyed (0/1 = the next break destroys it).
        public int BreaksLeft { get; set; } = 0;

        public PlotDescCamp() { }

        public PlotDescCamp(string conflictGuid, Vec3i anchor)
        {
            ConflictGuid = conflictGuid;
            AnchorPos = anchor;
        }

        public override string Serialize(Plot plot) => JsonConvert.SerializeObject(this);

        public override void Deserialize(string data, Plot plot)
        {
            if (!string.IsNullOrEmpty(data)) JsonConvert.PopulateObject(data, this);
            if (plot.hasCity()) plot.getCity().campPlots.Add(plot);
        }

        public override void OnDeactivated(Plot plot)
        {
            if (plot.hasCity()) plot.getCity().campPlots.Remove(plot);
        }
    }
}
