using claims.src.messages;
using claims.src.part.structure.plots;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part.structure
{
    /// <summary>
    /// How villages answer the two block questions the permission pipeline asks: may this player
    /// build or dig here, and what happens when the anchor itself is hit. Kept out of
    /// OnBlockAction, which is long enough already and is about permissions in general.
    ///
    /// Both entry points sit on the hot path - every swing at every block runs through them - so
    /// the cheapest checks come first and neither touches the database unless something happened.
    /// </summary>
    public static class VillageBlockRules
    {
        /// <summary>
        /// True while a raid window is open on the village owning this plot and the player is not
        /// one of its own. A raid strips the village of its block protection outright - that is the
        /// point of it - so raiders may dig in and loot, not merely hit the anchor.
        /// </summary>
        public static bool IsUnderRaidFor(PlayerInfo playerInfo, Plot plot)
        {
            if (plot == null || !plot.hasCity()) return false;
            City village = plot.getCity();
            if (!village.IsVillage()) return false;
            if (playerInfo != null && playerInfo.hasCity() && playerInfo.City.Equals(village)) return false;

            return VillageRaidHelper.IsRaidWindowOpen(village);
        }

        /// <summary>
        /// Deals with a hit on a village anchor. Returns false when the block is not an anchor at
        /// all, so the caller carries on with its normal checks; otherwise <paramref name="breaks"/>
        /// says whether the block actually gives way this time.
        /// </summary>
        public static bool TryHandleAnchorHit(IServerPlayer byPlayer, PlayerInfo playerInfo, Plot plot,
            BlockPos pos, out bool breaks)
        {
            breaks = false;
            // The anchor only ever stands on the main plot, which rules out every other block in
            // one comparison.
            if (plot == null || plot.Type != PlotType.VILLAGE_MAIN || playerInfo == null) return false;
            if (plot.PlotDesc is not PlotDescVillage desc || desc.AnchorPos == null) return false;
            if (pos.X != desc.AnchorPos.X || pos.Y != desc.AnchorPos.Y || pos.Z != desc.AnchorPos.Z) return false;
            if (!plot.hasCity()) return false;

            City village = plot.getCity();
            bool ownCitizen = playerInfo.hasCity() && playerInfo.City.Equals(village);

            if (ownCitizen || !VillageRaidHelper.IsRaidWindowOpen(village))
            {
                // The only way an outsider learns the schedule: by walking up and trying.
                if (!ownCitizen) VillageRaidHelper.HintScheduleTo(byPlayer, village);
                return true;
            }

            // A window can be open by the clock while the state that goes with it was never applied -
            // the schedule is only armed for a server that started with raiding switched on, so
            // turning it on later leaves the counter at zero. Zero here reads as "one more hit ends
            // it", which would drop a village on the first swing; refill it instead.
            if (desc.BreaksLeft <= 0)
            {
                desc.RaidActive = true;
                desc.BreaksLeft = claims.config.VILLAGE_ANCHOR_BREAKS;
            }

            // Same "reinforced block" trick as the war camp anchor: every hit is absorbed and only
            // the last one actually takes the village down.
            if (desc.BreaksLeft > 1)
            {
                desc.BreaksLeft--;
                plot.saveToDatabase();
                VillageSupplyHelper.SyncBlockEntities(desc);
                MessageHandler.sendMsgToPlayer(byPlayer, Lang.Get("claims:village_anchor_breaks_left", desc.BreaksLeft));
                return true;
            }

            MessageHandler.sendGlobalMsg(Lang.Get("claims:village_raid_succeeded",
                village.getPartNameReplaceUnder(), byPlayer.PlayerName));
            PartDemolition.demolishCity(village, string.Format("Raided by player {0}", byPlayer.PlayerName));
            breaks = true;
            return true;
        }
    }
}
