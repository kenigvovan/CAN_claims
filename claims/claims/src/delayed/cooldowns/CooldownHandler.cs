using claims.src.delayed;
using claims.src.part.interfaces;

namespace claims.src.delayed.cooldowns
{
    public class CooldownHandler
    {
        private class CooldownEntry
        {
            public ICooldown Target { get; }
            public CooldownType Type { get; }
            public long Stamp { get; }

            public CooldownEntry(ICooldown target, CooldownType type, long stamp)
            {
                Target = target;
                Type = type;
                Stamp = stamp;
            }
        }

        static readonly ExpiringList<CooldownEntry> cooldowns = new();

        public static void processCooldowns()
        {
            cooldowns.ExpireOverdue(c => c.Stamp);
        }

        public static long hasCooldown(ICooldown target, CooldownType type)
        {
            foreach (var c in cooldowns.Snapshot())
            {
                if (c.Target.Equals(target) && c.Type.Equals(type))
                    return c.Stamp;
            }
            return 0;
        }

        public static void addCooldown(ICooldown target, CooldownInfo info)
        {
            foreach (var c in cooldowns.Snapshot())
            {
                if (c.Target.Equals(target) && c.Type.Equals(info.getType()))
                {
                    cooldowns.Remove(c);
                    break;
                }
            }
            cooldowns.Add(new CooldownEntry(target, info.getType(), info.getStamp()));
        }
    }
}
