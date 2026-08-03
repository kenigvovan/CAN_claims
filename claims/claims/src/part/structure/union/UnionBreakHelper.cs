using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure.conflict;
using claims.src.rights;
using Vintagestory.API.Config;

namespace claims.src.part.structure.union
{
    /// <summary>
    /// Leaving a union. A one-sided exit is a denunciation: announced now, effective after
    /// UNION_BREAK_DELAY_DAYS. While it is pending the union still holds, so the denouncer cannot
    /// stab the ally in the back on the same turn. Once it takes effect both sides carry cooldowns
    /// before war may be declared or the union signed again. Both sides can instead agree to dissolve
    /// the union at once - that path is free of delay and cooldowns.
    /// </summary>
    public static class UnionBreakHelper
    {
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Seconds until the announced break takes effect; 0 when none is pending.</summary>
        public static long PendingBreakLeft(Alliance alliance, Alliance other)
        {
            if (alliance == null || other == null) return 0;
            if (!alliance.PendingUnionBreaks.TryGetValue(other.Guid, out long at)) return 0;
            long left = at - Now;
            return left > 0 ? left : 0;
        }

        /// <summary>Seconds left before war may be declared on a former ally; 0 when free.</summary>
        public static long WarCooldownLeft(IConflictParty first, IConflictParty second)
        {
            if (claims.config.UNION_BREAK_WAR_COOLDOWN_DAYS <= 0) return 0;
            if (first is not Alliance a || second is not Alliance b) return 0;
            if (!a.UnionBreakCooldowns.TryGetValue(b.Guid, out long brokenAt)) return 0;
            long left = brokenAt + (long)claims.config.UNION_BREAK_WAR_COOLDOWN_DAYS * 86400 - Now;
            return left > 0 ? left : 0;
        }

        /// <summary>Seconds left before a union with a former ally may be signed again; 0 when free.</summary>
        public static long ReformCooldownLeft(Alliance a, Alliance b)
        {
            if (claims.config.UNION_REFORM_COOLDOWN_DAYS <= 0) return 0;
            if (a == null || b == null) return 0;
            if (!a.UnionBreakCooldowns.TryGetValue(b.Guid, out long brokenAt)) return 0;
            long left = brokenAt + (long)claims.config.UNION_REFORM_COOLDOWN_DAYS * 86400 - Now;
            return left > 0 ? left : 0;
        }

        /// <summary>True while both alliances share at least one running conflict (fighting side by side).</summary>
        public static bool SharesRunningWar(Alliance a, Alliance b)
        {
            foreach (Conflict conflict in a.RunningConflicts)
            {
                IConflictParty foe = conflict.First.Equals(a) ? conflict.Second : conflict.First;
                foreach (Conflict theirs in b.RunningConflicts)
                {
                    IConflictParty theirFoe = theirs.First.Equals(b) ? theirs.Second : theirs.First;
                    if (foe.Equals(theirFoe)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Announces a one-sided break. Returns a lang key on refusal, null on success.
        /// </summary>
        public static string TryAnnounceBreak(Alliance initiator, Alliance target)
        {
            if (!UnionHander.unionAlreadyExist(initiator, target)) return "claims:no_union_found";
            if (PendingBreakLeft(initiator, target) > 0) return "claims:union_break_already_announced";
            if (claims.config.UNION_BREAK_BLOCKED_IN_SHARED_WAR && SharesRunningWar(initiator, target))
                return "claims:union_break_blocked_shared_war";

            long delay = (long)claims.config.UNION_BREAK_DELAY_DAYS * 86400;
            if (delay <= 0)
            {
                // Delay disabled by config: break right away, cooldowns still apply.
                FinishBreak(initiator, target, mutual: false);
                return null;
            }

            long at = Now + delay;
            initiator.PendingUnionBreaks[target.Guid] = at;
            target.PendingUnionBreaks[initiator.Guid] = at;
            initiator.saveToDatabase();
            target.saveToDatabase();

            string left = StringFunctions.FormatDuration(delay);
            BroadcastPendingBreaks(initiator, target);
            LogForBoth(initiator, target, EnumCityLogEvent.UnionBreakAnnounced, left);
            MessageHandler.SendMsgInAlliance(initiator, Lang.Get("claims:union_break_announced_by_us", target.getPartNameReplaceUnder(), left));
            MessageHandler.SendMsgInAlliance(target, Lang.Get("claims:union_break_announced_by_them", initiator.getPartNameReplaceUnder(), left));
            return null;
        }

        /// <summary>Calls off a pending denunciation. Either side may do it - it takes two to stay allied.</summary>
        public static string TryCancelBreak(Alliance canceller, Alliance other)
        {
            if (PendingBreakLeft(canceller, other) <= 0) return "claims:union_break_not_announced";
            canceller.PendingUnionBreaks.Remove(other.Guid);
            other.PendingUnionBreaks.Remove(canceller.Guid);
            canceller.saveToDatabase();
            other.saveToDatabase();
            BroadcastPendingBreaks(canceller, other);
            MessageHandler.SendMsgInAlliance(canceller, Lang.Get("claims:union_break_cancelled", other.getPartNameReplaceUnder()));
            MessageHandler.SendMsgInAlliance(other, Lang.Get("claims:union_break_cancelled", canceller.getPartNameReplaceUnder()));
            return null;
        }

        /// <summary>Dissolves the union at once by mutual consent: no delay, no cooldowns.</summary>
        public static void DissolveByAgreement(Alliance a, Alliance b) => FinishBreak(a, b, mutual: true);

        /// <summary>Executes every denunciation whose delay has run out. Called from the letters timer.</summary>
        public static void ApplyDueBreaks()
        {
            if (claims.dataStorage == null) return;
            long now = Now;
            foreach (Alliance alliance in new List<Alliance>(claims.dataStorage.getAllAlliances()))
            {
                if (alliance.PendingUnionBreaks.Count == 0) continue;
                // Copy: FinishBreak mutates the dictionary we are iterating.
                foreach (var pair in new List<KeyValuePair<string, long>>(alliance.PendingUnionBreaks))
                {
                    if (pair.Value > now) continue;
                    if (!claims.dataStorage.GetAllianceByGUID(pair.Key, out Alliance other))
                    {
                        alliance.PendingUnionBreaks.Remove(pair.Key);
                        alliance.saveToDatabase();
                        continue;
                    }
                    FinishBreak(alliance, other, mutual: false);
                }
            }
        }

        /// <summary>
        /// Tears the union down. <paramref name="mutual"/> marks an agreed dissolution: it skips the
        /// post-break cooldowns, since nobody was betrayed.
        /// </summary>
        private static void FinishBreak(Alliance a, Alliance b, bool mutual)
        {
            a.PendingUnionBreaks.Remove(b.Guid);
            b.PendingUnionBreaks.Remove(a.Guid);

            PartDemolition.DemolishUnion(a, b);

            if (!mutual)
            {
                long now = Now;
                a.UnionBreakCooldowns[b.Guid] = now;
                b.UnionBreakCooldowns[a.Guid] = now;
            }
            a.saveToDatabase();
            b.saveToDatabase();

            // Ally rights come from ComradePerms, so they must be recomputed for everyone involved.
            ReapplyRights(a);
            ReapplyRights(b);

            BroadcastPendingBreaks(a, b);
            LogForBoth(a, b, EnumCityLogEvent.UnionBroken);
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(b.Guid,
                new Dictionary<string, object> { { "value", a.GetPartName() } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_REMOVED);
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(a.Guid,
                new Dictionary<string, object> { { "value", b.GetPartName() } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_REMOVED);

            string key = mutual ? "claims:union_dissolved_mutual" : "claims:union_broken";
            MessageHandler.SendMsgInAlliance(a, Lang.Get(key, b.getPartNameReplaceUnder()));
            MessageHandler.SendMsgInAlliance(b, Lang.Get(key, a.getPartNameReplaceUnder()));
        }

        /// <summary>Pushes the announced-breaks snapshot to both alliances' citizens.</summary>
        public static void BroadcastPendingBreaks(Alliance a, Alliance b = null)
        {
            foreach (Alliance alliance in new[] { a, b })
            {
                if (alliance == null) continue;
                foreach (City city in alliance.Cities)
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.ALLIANCE_UNION_BREAKS_ALL);
            }
        }

        private static void ReapplyRights(Alliance alliance)
        {
            foreach (City city in alliance.Cities)
                foreach (PlayerInfo citizen in city.getCityCitizens())
                    RightsHandler.reapplyRights(citizen);
        }

        private static void LogForBoth(Alliance a, Alliance b, EnumCityLogEvent evt, params string[] extra)
        {
            foreach (City city in a.Cities) city.AddLogEntry(evt, Prepend(b.GetPartName(), extra));
            foreach (City city in b.Cities) city.AddLogEntry(evt, Prepend(a.GetPartName(), extra));
        }

        private static string[] Prepend(string first, string[] rest)
        {
            var args = new string[rest.Length + 1];
            args[0] = first;
            Array.Copy(rest, 0, args, 1, rest.Length);
            return args;
        }
    }
}
