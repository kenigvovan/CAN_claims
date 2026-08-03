using claims.src.auxialiry;
using claims.src.delayed.cooldowns;
using claims.src.part;
using Vintagestory.API.Server;

namespace claims.src.delayed.teleportation
{
    public class TeleportationHandler
    {
        static ExpiringList<TeleportationInfo> teleportations = new();

        public static bool addTeleportation(TeleportationInfo info)
        {
            foreach (var it in teleportations.Snapshot())
            {
                if (it.getTargetPlayer().Guid.Equals(info.getTargetPlayer().Guid))
                {
                    teleportations.Remove(it);
                    break;
                }
            }
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
                if (player != null)
                    player.Entity.TeleportToDouble(it.getTargetPoint().X + 0.5f, it.getTargetPoint().Y, it.getTargetPoint().Z + 0.5f);
                CooldownHandler.addCooldown(it.getTargetPlayer(),
                    new CooldownInfo(TimeFunctions.getEpochSeconds() + it.getCooldownSeconds(), it.getCooldownType()));
                it.getTargetPlayer().AwaitForTeleporation = false;
            });
        }
    }
}
