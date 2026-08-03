using System;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Treasury pillage on plot capture: transfers a configurable percentage of the defending
    /// city's balance to the attacking city. Capped at the defender's actual balance and gated on
    /// a real wallet provider (the Noop provider "succeeds" without moving coins, so we skip it).
    /// </summary>
    public static class WarPillageHelper
    {
        /// <summary>Pillages the defender's treasury into the attacker's. Returns the amount actually stolen (0 if none).</summary>
        public static long Pillage(City attacker, City defender)
        {
            if (attacker == null || defender == null) return 0;
            if (!claims.config.WAR_PILLAGE_ENABLED) return 0;
            if (!claims.economyProvider.SupportsPlayerWallet) return 0; // no real coins -> nothing to steal
            if (claims.config.WAR_PILLAGE_PERCENT <= 0) return 0;

            decimal balance = claims.economyProvider.GetBalance(defender.MoneyAccountName);
            if (balance <= 0) return 0;

            long desired = (long)Math.Floor(balance * (decimal)claims.config.WAR_PILLAGE_PERCENT / 100m);
            long amount = Math.Min(desired, (long)balance);
            if (amount <= 0) return 0;

            var result = claims.economyProvider.Transfer(defender.MoneyAccountName, attacker.MoneyAccountName, (decimal)amount);
            if (result != MoneyOperationResult.Success)
            {
                // Partial failure is detectable via the result; money is NOT counted as stolen.
                MessageHandler.sendErrorMsg("WarPillageHelper: Transfer " + defender.MoneyAccountName + "->" + attacker.MoneyAccountName
                    + " failed (" + result + ") for " + amount);
                return 0;
            }

            string amountStr = amount.ToString();
            defender.AddLogEntry(EnumCityLogEvent.TreasuryPillaged, attacker.GetPartName(), amountStr);
            attacker.AddLogEntry(EnumCityLogEvent.TreasuryPillaged, defender.GetPartName(), amountStr);

            UsefullPacketsSend.AddToQueueCityInfoUpdate(defender.Guid, EnumPlayerRelatedInfo.CITY_BALANCE, EnumPlayerRelatedInfo.CITY_LOG);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(attacker.Guid, EnumPlayerRelatedInfo.CITY_BALANCE, EnumPlayerRelatedInfo.CITY_LOG);

            IConflictParty defenderParty = defender.HasAlliance() ? (IConflictParty)defender.Alliance : defender;
            IConflictParty attackerParty = attacker.HasAlliance() ? (IConflictParty)attacker.Alliance : attacker;
            MessageHandler.SendMsgInAlliance(defenderParty, Lang.Get("claims:war_treasury_pillaged_victim", defender.GetPartName(), attacker.GetPartName(), amountStr));
            MessageHandler.SendMsgInAlliance(attackerParty, Lang.Get("claims:war_treasury_pillaged_attacker", defender.GetPartName(), amountStr));
            return amount;
        }
    }
}
