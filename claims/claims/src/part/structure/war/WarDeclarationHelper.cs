using System;
using claims.src.economy;
using claims.src.part.structure.conflict;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Gatekeeping for declaring war: re-declare cooldown (per opponent, survives restart via
    /// City.WarCooldowns), optional casus belli requirement (grievance or ally-at-war), and a
    /// declaration cost. Also records the two persistent facts these gates rely on: war-end
    /// timestamps and grievances.
    /// </summary>
    public static class WarDeclarationHelper
    {
        public static bool TryPassDeclarationGates(IConflictParty ourParty, IConflictParty target, string ourAccount, out string errorKey)
        {
            errorKey = null;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Cooling-off period after betraying a union. Checked BEFORE the free-war justification:
            // otherwise an ultimatum to the ex-ally (refusing it grants a free war) would launder the
            // betrayal straight past this gate.
            if (union.UnionBreakHelper.WarCooldownLeft(ourParty, target) > 0)
            {
                errorKey = "claims:war_blocked_after_union_break";
                return false;
            }

            // A refused/expired ultimatum grants a free, justified war against that target — bypass every other gate.
            if (HasWarJustification(ourParty, target))
            {
                ConsumeWarJustification(ourParty, target);
                return true;
            }

            // Re-declare cooldown
            long cd = (long)claims.config.WAR_REDECLARE_COOLDOWN_DAYS * 86400;
            if (cd > 0)
            {
                foreach (City city in ourParty.GetCities())
                    if (city.WarCooldowns.TryGetValue(target.Guid, out long ts) && now - ts < cd)
                    {
                        errorKey = "claims:war_on_cooldown";
                        return false;
                    }
            }

            // Non-aggression pact blocks the declaration unless we hold a casus belli (which
            // justifies breaking it) — then the pact is torn up as part of declaring war.
            if (claims.config.WAR_NAP_ENABLED && NonAggressionHelper.HasActivePact(ourParty, target))
            {
                if (!HasValidCasusBelli(ourParty, target))
                {
                    errorKey = "claims:nap_active";
                    return false;
                }
                NonAggressionHelper.RemovePact(ourParty, target);
            }

            // Casus belli (only enforced when the server requires it)
            if (claims.config.WAR_REQUIRE_CASUS_BELLI && !HasValidCasusBelli(ourParty, target))
            {
                errorKey = "claims:no_casus_belli";
                return false;
            }

            // Declaration cost
            double cost = claims.config.WAR_DECLARATION_COST;
            if (cost > 0)
            {
                if (claims.economyProvider.GetBalance(ourAccount) < (decimal)cost)
                {
                    errorKey = "claims:not_enough_money";
                    return false;
                }
                if (claims.economyProvider.Withdraw(ourAccount, (decimal)cost) != MoneyOperationResult.Success)
                {
                    errorKey = "claims:economy_money_transaction_error";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// True if the party currently holds a valid casus belli against the target (a recent
        /// grievance, or an ally already at war with them). Unconditional — independent of
        /// WAR_REQUIRE_CASUS_BELLI, so it can gate NAP-breaking regardless of the server setting.
        /// </summary>
        public static bool HasValidCasusBelli(IConflictParty ourParty, IConflictParty target)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long grace = (long)claims.config.WAR_CASUS_BELLI_GRACE_DAYS * 86400;
            foreach (City city in ourParty.GetCities())
            {
                // A recent grievance (enemy killed our citizen) is a valid casus belli.
                if (city.Grievances.TryGetValue(target.Guid, out long ts) && now - ts <= grace) return true;
                // An ally already at war with the target lets us join.
                foreach (City comrade in city.ComradeCities)
                    foreach (City tCity in target.GetCities())
                        if (comrade.HostileCities.Contains(tCity)) return true;
            }
            return false;
        }

        /// <summary>Records the war-end timestamp for both sides so the re-declare cooldown applies.</summary>
        public static void RecordWarEnd(Conflict conflict)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (City city in conflict.First.GetCities()) { city.WarCooldowns[conflict.Second.Guid] = now; city.saveToDatabase(); }
            foreach (City city in conflict.Second.GetCities()) { city.WarCooldowns[conflict.First.Guid] = now; city.saveToDatabase(); }
            // The war ending removes the "our ally is at war with them" reason from everyone's list.
            CasusBelliHelper.Broadcast(conflict.First);
            CasusBelliHelper.Broadcast(conflict.Second);
        }

        /// <summary>Records a casus-belli grievance (offender killed a citizen of victimCity).</summary>
        public static void RecordGrievance(City victimCity, string offenderPartyGuid)
        {
            if (victimCity == null || string.IsNullOrEmpty(offenderPartyGuid)) return;
            victimCity.Grievances[offenderPartyGuid] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            victimCity.saveToDatabase();
            CasusBelliHelper.BroadcastForCity(victimCity);
        }

        /// <summary>True if any of ourParty's cities holds an unexpired free-war justification against target.</summary>
        public static bool HasWarJustification(IConflictParty ourParty, IConflictParty target)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (City city in ourParty.GetCities())
                if (city.WarJustifications.TryGetValue(target.Guid, out long exp) && exp > now) return true;
            return false;
        }

        /// <summary>
        /// Grants a free-war justification (plus a grievance) after an ultimatum was refused or expired.
        /// The window lasts WAR_CASUS_BELLI_GRACE_DAYS; while it holds, declaring war on the target skips
        /// the cooldown, casus-belli requirement and declaration cost.
        /// </summary>
        public static void RecordWarJustification(IConflictParty ourParty, IConflictParty target)
        {
            long exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)claims.config.WAR_CASUS_BELLI_GRACE_DAYS * 86400;
            foreach (City city in ourParty.GetCities())
            {
                city.WarJustifications[target.Guid] = exp;
                RecordGrievance(city, target.Guid); // also persists the city (both dicts saved together)
            }
            CasusBelliHelper.Broadcast(ourParty);
        }

        /// <summary>Consumes a free-war justification against target (called once the war is actually declared).</summary>
        public static void ConsumeWarJustification(IConflictParty ourParty, IConflictParty target)
        {
            foreach (City city in ourParty.GetCities())
                if (city.WarJustifications.Remove(target.Guid)) city.saveToDatabase();
            CasusBelliHelper.Broadcast(ourParty);
        }
    }
}
