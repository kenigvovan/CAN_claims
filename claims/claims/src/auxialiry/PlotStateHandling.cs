using System;
using System.Collections.Generic;
using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.auxialiry
{
    public class PlotStateHandling
    {
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

                var tmpPlot = new SavedPlotInfo((int)plot.Price, plot.getPermsHandler().pvpFlag,
                    player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockDestroyWithOutCacheUpdate(playerInfo, plot),
                    player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockUseWithOutCacheUpdate(playerInfo, plot),
                    player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canAttackAnimalsWithOutCacheUpdate(playerInfo, plot),
                    plot.getCity().GetPartName(), plot.GetPartName(),
                    plot.hasCityPlotsGroup() ? plot.getPlotGroup().GetPartName() : "",
                    plot.Type == PlotType.TAVERN ? plot.GetClientInnerClaimFromDefault(playerInfo) : null,
                    plot.getCity().Alliance?.Guid ?? "");
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
