using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.part.structure.plots;
using Vintagestory.API.Config;

namespace claims.src.part.structure
{
    /// <summary>
    /// Turning a village into a full city. Everything a village owns is either shared with cities
    /// already (plots, permissions, the map) or has to be taken down here: its anchor, its granary
    /// and its daily raid window all belong to the village stage only.
    /// </summary>
    public static class VillageUpgradeHelper
    {
        public static void UpgradeToCity(City village)
        {
            if (village == null || !village.IsVillage()) return;

            if (village.TryGetVillageMain(out Plot mainPlot, out _))
            {
                // Drops the stock on the ground instead of deleting it.
                PartDemolition.RemoveVillageBlocks(village);

                // The anchor also served as the respawn point; a city respawns at its temples instead.
                village.RemoveTempleRespawnPoint(mainPlot);
                mainPlot.Type = PlotType.DEFAULT;
                mainPlot.PlotDesc = new PlotDesc();
                mainPlot.saveToDatabase();
            }

            // The village got this land for free; as a city it must not be refunded the claim price
            // for it (VU5). Plots claimed before ClaimCityPlot left the stamp at 0 still carry one.
            // An owned plot keeps its stamp: there it dates the purchase the owner's refund is for.
            foreach (Plot plot in village.getCityPlots())
            {
                if (plot.hasPlotOwner() || plot.TimeStampClaimed == 0) continue;
                plot.TimeStampClaimed = 0;
                plot.saveToDatabase();
            }

            village.Tier = CityTier.CITY;
            village.AddLogEntry(EnumCityLogEvent.VillageUpgraded, village.GetPartName());
            village.saveToDatabase();

            // Village heads run on a smaller permission group, so every citizen has to be re-rated.
            foreach (PlayerInfo citizen in village.getCityCitizens())
            {
                RightsHandler.reapplyRights(citizen);
            }

            UsefullPacketsSend.AddToQueueCityInfoUpdate(village.Guid, EnumPlayerRelatedInfo.CITY_TIER,
                EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
            FireVillageUpgraded(village);
        }

        /// <summary>Raised after a village became a city. Extension point for other mods.</summary>
        public static event System.Action<City> VillageUpgraded;
        private static void FireVillageUpgraded(City village) => VillageUpgraded?.Invoke(village);
    }
}
