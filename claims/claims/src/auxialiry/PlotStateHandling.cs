using System;
using System.Collections.Generic;
using claims.src.auxialiry.innerclaims;
using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.subplots;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.auxialiry
{
    public class PlotStateHandling
    {
        /// <summary>Nesting depth of <see cref="BeginBatch"/>, so that a mass operation calling
        /// another one still leaves the batch open until the outermost scope ends.</summary>
        private static int batchDepth;

        /// <summary>Whether a mass claim/unclaim is running. While it is, plot changes go into the
        /// collected-updates queue (see PlayerMovementsListnerServer.checkAndSendUpdates) instead of
        /// each one being broadcast on its own - a radius claim used to send one packet per plot per
        /// subscriber, each with its own serialization.</summary>
        public static bool BatchActive => batchDepth > 0;

        /// <summary>Opens a batch for the lifetime of the returned scope. Meant for `using`.</summary>
        public static IDisposable BeginBatch()
        {
            batchDepth++;
            return new BatchScope();
        }

        private sealed class BatchScope : IDisposable
        {
            private bool closed;

            public void Dispose()
            {
                // Disposing twice must not take the depth below what other scopes still hold.
                if (closed) return;
                closed = true;
                if (batchDepth > 0) batchDepth--;
            }
        }

        /// <summary>
        /// Builds the client-side view of a plot as <paramref name="player"/> sees it: the permission
        /// flags are resolved per viewer (creative always passes), so every send path must use this
        /// instead of assembling SavedPlotInfo by hand - a divergence here shows up as a plot that
        /// looks unclaimed or unprotected on someone's map.
        /// </summary>
        public static SavedPlotInfo BuildSavedPlotInfo(Plot plot, IServerPlayer player, PlayerInfo playerInfo)
        {
            bool creative = player.WorldData.CurrentGameMode == EnumGameMode.Creative;
            var perms = plot.getPermsHandler();
            bool canBuild = creative || OnBlockAction.canBlockDestroyWithOutCacheUpdate(playerInfo, plot);
            bool canUse = creative || OnBlockAction.canBlockUseWithOutCacheUpdate(playerInfo, plot);
            bool canAttack = creative || OnBlockAction.canAttackAnimalsWithOutCacheUpdate(playerInfo, plot);

            // The sub-plots reaching into this chunk: the client checks them before the chunk's flags.
            var pieces = SubPlotSync.PiecesFor(plot, playerInfo, creative, canUse, canBuild, canAttack);

            var info = new SavedPlotInfo((int)plot.Price, perms.pvpFlag, canBuild, canUse, canAttack,
                plot.hasCity() ? plot.getCity().GetPartName() : "", plot.GetPartName(),
                plot.hasCityPlotsGroup() ? plot.getPlotGroup().GetPartName() : "",
                pieces.Count > 0 ? pieces : null,
                plot.hasCity() ? (plot.getCity().Alliance?.Guid ?? "") : "");
            info.FireIsOn = perms.fireFlag;
            info.BlastIsOn = perms.blastFlag;
            // The flag alone decides, as in OnTryEntitySpawn; MarkedNoMobSpawn only bills it.
            info.NoMobSpawn = perms.noMobSpawnFlag;
            info.GroupRights = SavedPlotInfo.EncodeGroupRights(perms);
            return info;
        }

        //Send subscribers of plot's zone info about the newly claimed plot
        public static void broadcastPlotClaimedInZone(Plot plot)
        {
            Vec2i zone = new Vec2i(plot.plotPosition.getPos().X / claims.config.ZONE_PLOTS_LENGTH,
                                   plot.plotPosition.getPos().Y / claims.config.ZONE_PLOTS_LENGTH);

            HashSet<string> subs = claims.serverPlayerMovementListener.GetSubscribers(zone);
            if (subs == null) return;

            foreach (var uid in subs)
            {
                IServerPlayer player = claims.sapi.World.PlayerByUid(uid) as IServerPlayer;
                if (player == null) continue;
                if (!claims.dataStorage.GetPlayerByUid(uid, out PlayerInfo playerInfo)) continue;

                var tmpPlot = BuildSavedPlotInfo(plot, player, playerInfo);
                string serializedPlots = JsonConvert.SerializeObject(new Tuple<Vec2i, SavedPlotInfo>(plot.getPos(), tmpPlot));

                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.ADD_SINGLE_PLOT,
                    data = serializedPlots
                }, player);
            }
        }
        //Send subscribers of plot's zone info about the unclaimed plot
        public static void broadcastPlotUnclaimedInZone(int x, int z)
        {
            Vec2i zone = new Vec2i(x / claims.config.ZONE_PLOTS_LENGTH, z / claims.config.ZONE_PLOTS_LENGTH);

            HashSet<string> subs = claims.serverPlayerMovementListener.GetSubscribers(zone);
            if (subs == null) return;

            var tmpPlot = new SavedPlotInfo(0, false, false, false, false, null, null, null, null, "");
            string serializedPlots = JsonConvert.SerializeObject(new Tuple<Vec2i, SavedPlotInfo>(new Vec2i(x, z), tmpPlot));

            foreach (var uid in subs)
            {
                IServerPlayer player = claims.sapi.World.PlayerByUid(uid) as IServerPlayer;
                if (player == null) continue;

                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.REMOVE_SINGLE_PLOT,
                    data = serializedPlots
                }, player);
            }
        }
    }
}
