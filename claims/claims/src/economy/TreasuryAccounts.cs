using System.Collections.Generic;
using claims.src.part;
using claims.src.part.structure;

namespace claims.src.economy
{
    /// <summary>
    /// City and alliance accounts are created only when the city or alliance is founded. A world
    /// that switched caneconomy from REAL_MONEY (bank chests) to VIRTUAL_MONEY has none of them in
    /// the virtual bank, so every deposit, fee and refund failed with TargetAccountNotFound.
    /// </summary>
    public static class TreasuryAccounts
    {
        /// <summary>Creates the missing accounts; returns how many were created.</summary>
        public static int EnsureAll()
        {
            var provider = claims.economyProvider;
            // Real-money accounts are bank chests and need a position: those are set up by the player.
            if (provider == null || !provider.SupportsRemoteCoinTransfer || claims.dataStorage == null) return 0;

            int created = 0;
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                if (Ensure(provider, city.MoneyAccountName, city.GetPartName())) created++;
            }
            foreach (Alliance alliance in claims.dataStorage.getAllAlliances())
            {
                if (Ensure(provider, alliance.MoneyAccountName, alliance.GetPartName())) created++;
            }
            if (created > 0)
            {
                claims.sapi.Logger.Notification("[claims] Created {0} missing city/alliance treasury accounts.", created);
            }
            return created;
        }

        private static bool Ensure(IMoneyProvider provider, string account, string name)
        {
            if (provider.AccountExists(account)) return false;
            return provider.NewAccount(account, new Dictionary<string, object> { { "lastknownname", name } });
        }
    }
}
