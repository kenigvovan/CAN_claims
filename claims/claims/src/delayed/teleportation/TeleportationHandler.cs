using claims.src.auxialiry;
using claims.src.delayed.cooldowns;
using claims.src.messages;
using claims.src.part;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.delayed.teleportation
{
    public class TeleportationHandler
    {
        static ExpiringList<TeleportationInfo> teleportations = new();

        public static bool addTeleportation(TeleportationInfo info)
        {
            // A new one replaces the waiting one, which is then cancelled like any other.
            removeTeleportation(info.getTargetPlayer());
            info.getTargetPlayer().AwaitForTeleporation = true;
            teleportations.Add(info);
            return true;
        }

        public static bool removeTeleportation(PlayerInfo playerInfo)
        {
            foreach (var it in teleportations.Snapshot())
            {
                if (it.getTargetPlayer().Guid.Equals(playerInfo.Guid))
                {
                    it.getTargetPlayer().AwaitForTeleporation = false;
                    teleportations.Remove(it);
                    it.Refund();
                    return true;
                }
            }
            return false;
        }

        public static bool hasTeleportation(PlayerInfo playerInfo)
        {
            foreach (var it in teleportations.Snapshot())
            {
                if (it.getTargetPlayer().Guid.Equals(playerInfo.Guid))
                    return true;
            }
            return false;
        }

        public static void UpdateTeleportations()
        {
            teleportations.ExpireOverdue(it => it.getTimeStamp(), it =>
            {
                IServerPlayer player = claims.sapi.World.PlayerByUid(it.getTargetPlayer().Guid) as IServerPlayer;
                if (!it.IsStillAllowed())
                {
                    it.Refund();
                    it.getTargetPlayer().AwaitForTeleporation = false;
                    if (player != null)
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:teleport_cancelled_no_longer_allowed"));
                    return;
                }
                if (player != null)
                    player.Entity.TeleportToDouble(it.getTargetPoint().X + 0.5f, it.getTargetPoint().Y, it.getTargetPoint().Z + 0.5f);
                CooldownHandler.addCooldown(it.getTargetPlayer(),
                    new CooldownInfo(TimeFunctions.getEpochSeconds() + it.getCooldownSeconds(), it.getCooldownType()));
                it.getTargetPlayer().AwaitForTeleporation = false;
            });
        }
    }
}
