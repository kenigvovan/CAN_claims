using System;
using claims.src.part.structure.conflict;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Non-aggression pacts (NAPs): a timed mutual agreement not to declare war. Stored symmetrically
    /// on every city of both parties (City.NonAggressionPacts: partner party guid -> expiry unix seconds).
    /// A live pact blocks a war declaration (see WarDeclarationHelper) unless the declarer has a
    /// casus belli or breaks the pact first (paying a penalty).
    /// </summary>
    public static class NonAggressionHelper
    {
        public static bool HasActivePact(IConflictParty a, IConflictParty b)
        {
            if (a == null || b == null) return false;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (City c in a.GetCities())
                if (c.NonAggressionPacts.TryGetValue(b.Guid, out long exp) && exp > now)
                    return true;
            return false;
        }

        public static void RecordPact(IConflictParty a, IConflictParty b, long expiry)
        {
            foreach (City c in a.GetCities()) { c.NonAggressionPacts[b.Guid] = expiry; c.saveToDatabase(); }
            foreach (City c in b.GetCities()) { c.NonAggressionPacts[a.Guid] = expiry; c.saveToDatabase(); }
            CasusBelliHelper.Broadcast(a);
            CasusBelliHelper.Broadcast(b);
        }

        public static void RemovePact(IConflictParty a, IConflictParty b)
        {
            foreach (City c in a.GetCities()) { if (c.NonAggressionPacts.Remove(b.Guid)) c.saveToDatabase(); }
            foreach (City c in b.GetCities()) { if (c.NonAggressionPacts.Remove(a.Guid)) c.saveToDatabase(); }
            CasusBelliHelper.Broadcast(a);
            CasusBelliHelper.Broadcast(b);
        }
    }
}
