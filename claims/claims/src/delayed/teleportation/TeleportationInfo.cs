using claims.src.delayed.cooldowns;
using claims.src.part;
using Vintagestory.API.MathTools;

namespace claims.src.delayed.teleportation
{
    public class TeleportationInfo
    {
        PlayerInfo targetPlayer;
        Vec3d targetPoint;
        bool canBeCanceled;
        long timeStampWhenFinished;
        // Which cooldown the finished teleport puts the player on. Summon is the default so all
        // existing call sites keep their previous behaviour.
        CooldownType cooldownType;
        int cooldownSeconds;
        public TeleportationInfo(PlayerInfo playerInfo, Vec3d targetPoint, bool canBeCanceled, long timeStamp,
            CooldownType cooldownType = CooldownType.SUMMON, int cooldownSeconds = -1)
        {
            targetPlayer = playerInfo;
            this.targetPoint = targetPoint;
            this.canBeCanceled = canBeCanceled;
            timeStampWhenFinished = timeStamp;
            this.cooldownType = cooldownType;
            this.cooldownSeconds = cooldownSeconds < 0 ? claims.config.SECONDS_SUMMON_COOLDOWN : cooldownSeconds;
        }
        public CooldownType getCooldownType()
        {
            return cooldownType;
        }
        public int getCooldownSeconds()
        {
            return cooldownSeconds;
        }
        public PlayerInfo getTargetPlayer()
        {
            return targetPlayer;
        }
        public Vec3d getTargetPoint()
        {
            return targetPoint;
        }
        public bool getCanBeCanceled()
        {
            return canBeCanceled;
        }
        public long getTimeStamp()
        {
            return timeStampWhenFinished;
        }
    }
}
