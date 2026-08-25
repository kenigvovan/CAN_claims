using claims.src.part.structure.conflict;
using claims.src.part.structure.union;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Declaring a party neutral: it can neither make war nor be warred, and pays for the privilege
    /// every day (NEUTRAL_CITY_PAYMENT / NEUTRAL_ALLANCE_PAYMENT).
    ///
    /// The rules are the same for a city and an alliance, so they live here rather than twice in the
    /// two command files. Turning neutrality ON is gated; turning it OFF never is - a party must
    /// always be able to give up a status it is paying for.
    /// </summary>
    public static class NeutralityHelper
    {
        /// <summary>
        /// Whether this party may declare itself neutral now. Error lang key on refusal.
        ///
        /// Neutrality is a standing position, not an escape hatch: a party in a war, holding a union,
        /// or with a declaration still in the post would otherwise buy its way out of commitments it
        /// has already made. Ending those is an explicit act with its own consequences (cooldowns, a
        /// casus belli for the jilted ally), so they are refused here rather than undone silently.
        /// </summary>
        public static bool CanTurnOn(IConflictParty party, out string errorKey)
        {
            errorKey = null;
            if (!claims.config.NEUTRALITY_ENABLED) { errorKey = "claims:neutrality_disabled"; return false; }
            if (party == null) { errorKey = "claims:no_city"; return false; }

            if (party.RunningConflicts.Count > 0) { errorKey = "claims:neutral_blocked_by_war"; return false; }

            if (CooldownLeft(party) > 0) { errorKey = "claims:neutral_on_cooldown"; return false; }

            foreach (ConflictLetter letter in ConflictHandler.GetLettersFor(party))
            {
                if (letter.Purpose == LetterPurpose.START_CONFLICT || letter.Purpose == LetterPurpose.ULTIMATUM)
                {
                    errorKey = "claims:neutral_blocked_by_letter";
                    return false;
                }
            }

            if (party is Alliance alliance && alliance.ComradAlliancies.Count > 0)
            {
                errorKey = "claims:neutral_blocked_by_union";
                return false;
            }
            // A member city fights and allies through its alliance, so its own flag would decide
            // nothing - the alliance is the one that has to declare itself neutral.
            if (party is City city && city.HasAlliance())
            {
                errorKey = "claims:neutral_city_in_alliance";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Seconds left before neutrality may be declared again, 0 when it may be declared now.
        ///
        /// Giving it up starts the wait: without one, neutrality is a door to duck behind - drop it,
        /// strike while the other side cannot answer, and buy it back the moment the fight turns.
        /// </summary>
        public static long CooldownLeft(IConflictParty party)
        {
            long cooldown = (long)claims.config.NEUTRALITY_REDECLARE_COOLDOWN_HOURS * 3600;
            if (cooldown <= 0 || party == null || party.NeutralDroppedAt <= 0) return 0;

            long passed = auxialiry.TimeFunctions.getEpochSeconds() - party.NeutralDroppedAt;
            return passed >= cooldown ? 0 : cooldown - passed;
        }

        /// <summary>Applies the new state, persisting it. Caller has already run <see cref="CanTurnOn"/>.</summary>
        public static void Set(IConflictParty party, bool neutral)
        {
            // Only dropping it starts the clock; declaring it does not reset one already running.
            if (!neutral && party.Neutral) party.NeutralDroppedAt = auxialiry.TimeFunctions.getEpochSeconds();
            party.Neutral = neutral;
            party.saveToDatabase();
        }

        /// <summary>The daily surcharge this party pays while neutral - for the prices page and messages.</summary>
        public static double DailyCost(IConflictParty party) =>
            party is Alliance ? claims.config.NEUTRAL_ALLANCE_PAYMENT : claims.config.NEUTRAL_CITY_PAYMENT;
    }
}
