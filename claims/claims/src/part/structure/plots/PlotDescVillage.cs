using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// The main plot of a village (PlotType.VILLAGE_MAIN). Holds where its anchor and granary
    /// stand, how long the supplies last and the state of an ongoing raid. Authoritative copy of
    /// everything the anchor block entity mirrors to clients.
    /// </summary>
    public class PlotDescVillage : PlotDesc
    {
        public Vec3i AnchorPos { get; set; }
        public Vec3i GranaryPos { get; set; }
        /// <summary>Hours of supplies left before the village starts to decay.</summary>
        public int SupplyHours { get; set; } = 0;
        /// <summary>Hours spent with an empty granary; the village falls apart once it runs out.</summary>
        public int DecayHours { get; set; } = 0;
        /// <summary>Breaks the anchor still absorbs inside the raid window. Refilled when it opens.</summary>
        public int BreaksLeft { get; set; } = 0;
        /// <summary>
        /// Whether the daily raid window was open the last time it was checked. Only used to spot
        /// the moment it opens or closes - whether it IS open is computed from the founding time.
        /// </summary>
        public bool RaidActive { get; set; } = false;

        public PlotDescVillage() { }

        public PlotDescVillage(Vec3i anchor, Vec3i granary)
        {
            AnchorPos = anchor;
            GranaryPos = granary;
        }

        public override string Serialize(Plot plot) => JsonConvert.SerializeObject(this);

        public override void Deserialize(string data, Plot plot)
        {
            if (!string.IsNullOrEmpty(data)) JsonConvert.PopulateObject(data, this);
            // Plots load before cities are fully built, but the city reference itself is already
            // wired up by loadDummyPlots - same assumption PlotDescCamp makes.
            if (plot.hasCity() && AnchorPos != null)
            {
                plot.getCity().AddTempleRespawnPoint(plot.getPos(), AnchorPos);
            }
        }

        public override void OnDeactivated(Plot plot)
        {
            if (plot.hasCity()) plot.getCity().RemoveTempleRespawnPoint(plot);
        }
    }
}
