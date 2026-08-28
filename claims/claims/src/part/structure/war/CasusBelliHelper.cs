using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Collects the casus belli a party currently holds, for the GUI and the /cb commands.
    /// Mirrors the reasons <see cref="WarDeclarationHelper.HasValidCasusBelli"/> accepts, plus the
    /// free-war windows granted by refused ultimatums (WarJustifications), which bypass every gate.
    /// </summary>
    public static class CasusBelliHelper
    {
        /// <summary>The party a city fights as: its alliance if it has one, otherwise the city itself.</summary>
        public static IConflictParty PartyOf(City city)
            => city == null ? null : (city.HasAlliance() ? (IConflictParty)city.Alliance : city);

        /// <summary>Every casus belli the party holds right now, strongest reason first per target.</summary>
        public static List<ClientCasusBelliCellElement> BuildForParty(IConflictParty party)
        {
            var result = new List<ClientCasusBelliCellElement>();
            if (party == null) return result;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long grace = (long)claims.config.WAR_CASUS_BELLI_GRACE_DAYS * 86400;
            // target guid -> best reason so far
            var byTarget = new Dictionary<string, ClientCasusBelliCellElement>();

            long cooldown = (long)claims.config.WAR_REDECLARE_COOLDOWN_DAYS * 86400;
            // Foes of our allies, deduplicated across every city of the party.
            var allyFoeGuids = new HashSet<string>();

            foreach (City city in party.GetCities())
            {
                foreach (var kv in city.Grievances)
                {
                    if (now - kv.Value > grace) continue;
                    Merge(byTarget, party, kv.Key, CasusBelliKind.Grievance, kv.Value + grace);
                }
                foreach (var kv in city.WarJustifications)
                {
                    if (kv.Value <= now) continue;
                    Merge(byTarget, party, kv.Key, CasusBelliKind.FreeWar, kv.Value);
                }
                // An ally already at war with someone lets us join that war. Allies overlap heavily
                // (every city of an allied alliance lists the same foes), so collapse to guids first
                // instead of running Merge once per city-pair.
                foreach (City comrade in city.ComradeCities)
                    foreach (City hostile in comrade.HostileCities)
                    {
                        IConflictParty hostileParty = PartyOf(hostile);
                        if (hostileParty == null || !allyFoeGuids.Add(hostileParty.Guid)) continue;
                        Merge(byTarget, party, hostileParty.Guid, CasusBelliKind.AllyAtWar, 0);
                    }
                // Blockers get their own entries too, so the GUI can explain why a war is NOT possible.
                if (cooldown > 0)
                    foreach (var kv in city.WarCooldowns)
                    {
                        long until = kv.Value + cooldown;
                        if (until <= now) continue;
                        var entry = Merge(byTarget, party, kv.Key, CasusBelliKind.None, 0);
                        if (entry != null && until > entry.CooldownUntil) entry.CooldownUntil = until;
                    }
                if (claims.config.WAR_NAP_ENABLED)
                    foreach (var kv in city.NonAggressionPacts)
                    {
                        if (kv.Value <= now) continue;
                        var entry = Merge(byTarget, party, kv.Key, CasusBelliKind.None, 0);
                        if (entry != null && kv.Value > entry.PactUntil) entry.PactUntil = kv.Value;
                    }
            }

            // Cooling-off after betraying a union - blocks war just like the re-declare cooldown does.
            if (party is Alliance ourAlliance)
                foreach (var kv in ourAlliance.UnionBreakCooldowns)
                {
                    long until = kv.Value + (long)claims.config.UNION_BREAK_WAR_COOLDOWN_DAYS * 86400;
                    if (until <= now) continue;
                    var entry = Merge(byTarget, party, kv.Key, CasusBelliKind.None, 0);
                    if (entry != null && until > entry.UnionBreakUntil) entry.UnionBreakUntil = until;
                }

            // A war already running with that party makes the whole entry moot - and so does the
            // target being neutral, which no reason on this list can override.
            foreach (var pair in byTarget)
            {
                if (!ResolveParty(pair.Key, out IConflictParty target)) continue;
                if (ConflictHandler.conflictAlreadyExist(party, target)) continue;
                if (target.IsNeutral) continue;
                result.Add(pair.Value);
            }
            // Free wars first (they expire and cost nothing), then grievances by soonest expiry.
            result.Sort((a, b) =>
            {
                int k = Rank(b.Kind).CompareTo(Rank(a.Kind));
                if (k != 0) return k;
                if (a.ExpiresAt != b.ExpiresAt) return a.ExpiresAt.CompareTo(b.ExpiresAt);
                return string.Compare(a.TargetName, b.TargetName, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        public static List<ClientCasusBelliCellElement> BuildForCity(City city) => BuildForParty(PartyOf(city));

        /// <summary>Pushes a fresh list to every citizen of the party (their GUI replaces its copy).</summary>
        public static void Broadcast(IConflictParty party)
        {
            if (party == null) return;
            foreach (City city in party.GetCities())
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CASUS_BELLI_ALL);
        }

        public static void BroadcastForCity(City city) => Broadcast(PartyOf(city));

        private static int Rank(CasusBelliKind kind) => kind switch
        {
            CasusBelliKind.FreeWar => 3,
            CasusBelliKind.Grievance => 2,
            CasusBelliKind.AllyAtWar => 1,
            _ => 0
        };

        /// <summary>
        /// Adds or upgrades the entry for a target and returns it, so blockers can attach their own
        /// timestamps to the same row. Null when the target is us or no longer exists.
        /// </summary>
        private static ClientCasusBelliCellElement Merge(Dictionary<string, ClientCasusBelliCellElement> byTarget,
            IConflictParty ourParty, string targetGuid, CasusBelliKind kind, long expiresAt)
        {
            if (string.IsNullOrEmpty(targetGuid) || targetGuid == ourParty.Guid) return null;
            // Our own cities are never a target (a city guid also matches when we fight as an alliance).
            foreach (City own in ourParty.GetCities())
                if (own.Guid == targetGuid) return null;

            if (byTarget.TryGetValue(targetGuid, out var existing))
            {
                if (Rank(kind) > Rank(existing.Kind)
                    || (Rank(kind) == Rank(existing.Kind) && expiresAt > existing.ExpiresAt))
                {
                    existing.Kind = kind;
                    existing.ExpiresAt = expiresAt;
                }
                return existing;
            }

            if (!ResolveParty(targetGuid, out IConflictParty target)) return null;
            var created = new ClientCasusBelliCellElement(target.GetPartName(), targetGuid,
                WarTargetTypeHelper.FromConflictParty(target), kind, expiresAt);
            byTarget[targetGuid] = created;
            return created;
        }

        /// <summary>Resolves a stored party guid; false if that city/alliance no longer exists.</summary>
        private static bool ResolveParty(string guid, out IConflictParty party)
        {
            if (claims.dataStorage.getCityByGUID(guid, out City c)) { party = c; return true; }
            if (claims.dataStorage.GetAllianceByGUID(guid, out Alliance a)) { party = a; return true; }
            party = null;
            return false;
        }
    }
}
