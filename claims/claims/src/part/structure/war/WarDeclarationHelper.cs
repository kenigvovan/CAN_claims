using System;
using claims.src.economy;
using claims.src.messages;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

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
            => TryPassDeclarationGates(ourParty, target, ourAccount, out errorKey, out _);

        /// <summary>
        /// Same gates, reporting what passing them cost. A declaration that still needs the other
        /// side's answer hands <paramref name="charge"/> to the letter, which gives it back through
        /// <see cref="RefundDeclaration"/> if the war never happens.
        /// </summary>
        public static bool TryPassDeclarationGates(IConflictParty ourParty, IConflictParty target, string ourAccount,
            out string errorKey, out DeclarationCharge charge)
        {
            errorKey = null;
            charge = new DeclarationCharge();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Neutrality, checked here as well as in the commands: this is the one place every path
            // to a war goes through, and a free-war justification must not be spent on a war the
            // rules will refuse anyway.
            if (ourParty != null && ourParty.IsNeutral) { errorKey = "claims:our_city_is_neutral"; return false; }
            if (target != null && target.IsNeutral) { errorKey = "claims:target_party_is_neutral"; return false; }

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
                // Noted before it is spent, so a declaration that comes to nothing can put it back.
                charge.JustificationExpire = JustificationExpiryOf(ourParty, target);
                charge.TargetGuid = target.Guid;
                ConsumeWarJustification(ourParty, target);
                // Bypassing the pact gate is not the same as leaving the pact standing: it used to
                // survive the war it was overridden by, so /nap break still charged a penalty for it
                // and the next declaration was refused as nap_active long after the two had fought.
                if (claims.config.WAR_NAP_ENABLED && NonAggressionHelper.HasActivePact(ourParty, target))
                {
                    NonAggressionHelper.RemovePact(ourParty, target);
                }
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
                    var f = claims.economyProvider.GetBalance(ourAccount);
                    errorKey = "claims:not_enough_money";
                    return false;
                }
                if (claims.economyProvider.Withdraw(ourAccount, (decimal)cost) != MoneyOperationResult.Success)
                {
                    errorKey = "claims:economy_money_transaction_error";
                    return false;
                }
                charge.Cost = cost;
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

        /// <summary>
        /// Gives back what a declaration took when it never became a war - refused, expired, or
        /// withdrawn. The fee goes back to the account that paid it; the justification is restored
        /// with its ORIGINAL expiry, so nothing is stretched by declaring and withdrawing, and one
        /// that has since run out is simply not put back.
        /// </summary>
        public static void RefundDeclaration(IConflictParty ourParty, string ourAccount, DeclarationCharge charge)
        {
            if (ourParty == null || charge == null || charge.IsEmpty) return;

            if (charge.Cost > 0 && !string.IsNullOrEmpty(ourAccount))
            {
                if (claims.economyProvider.Deposit(ourAccount, (decimal)charge.Cost) != MoneyOperationResult.Success)
                {
                    MessageHandler.sendErrorMsg("WarDeclarationHelper: could not refund the declaration fee of "
                        + charge.Cost + " to " + ourAccount);
                }
                else
                {
                    MessageHandler.SendMsgInAlliance(ourParty, Lang.Get("claims:war_declaration_refunded", charge.Cost));
                }
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (charge.JustificationExpire > now && charge.TargetGuid != null)
            {
                foreach (City city in ourParty.GetCities())
                {
                    city.WarJustifications[charge.TargetGuid] = charge.JustificationExpire;
                    city.saveToDatabase();
                }
                CasusBelliHelper.Broadcast(ourParty);
            }
        }

        /// <summary>The furthest expiry any of the party's cities holds against this target, 0 if none.</summary>
        private static long JustificationExpiryOf(IConflictParty ourParty, IConflictParty target)
        {
            long best = 0;
            foreach (City city in ourParty.GetCities())
                if (city.WarJustifications.TryGetValue(target.Guid, out long exp) && exp > best) best = exp;
            return best;
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
