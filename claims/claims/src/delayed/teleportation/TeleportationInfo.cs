using System;
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
        // Checked again when the teleport is due: the player may have lost the right to go there
        // while waiting (left the city, the point was removed).
        Func<bool> stillAllowed;
        // Paid up front; given back if the teleport does not happen.
        string refundAccount;
        decimal refundAmount;
        public TeleportationInfo(PlayerInfo playerInfo, Vec3d targetPoint, bool canBeCanceled, long timeStamp,
            CooldownType cooldownType = CooldownType.SUMMON, int cooldownSeconds = -1,
            Func<bool> stillAllowed = null, string refundAccount = null, decimal refundAmount = 0)
        {
            targetPlayer = playerInfo;
            this.targetPoint = targetPoint;
            this.canBeCanceled = canBeCanceled;
            timeStampWhenFinished = timeStamp;
            this.cooldownType = cooldownType;
            this.cooldownSeconds = cooldownSeconds < 0 ? claims.config.SECONDS_SUMMON_COOLDOWN : cooldownSeconds;
            this.stillAllowed = stillAllowed;
            this.refundAccount = refundAccount;
            this.refundAmount = refundAmount;
        }
        public bool IsStillAllowed()
        {
            return stillAllowed == null || stillAllowed();
        }
        public void Refund()
        {
            if (refundAccount == null || refundAmount <= 0) return;
            claims.economyProvider.Deposit(refundAccount, refundAmount);
            refundAmount = 0; // at most once
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
