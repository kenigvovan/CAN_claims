using System.Collections.Generic;
using Vintagestory.API.Server;

namespace claims.src.economy
{
    public interface IMoneyProvider
    {
        bool NewAccount(string account, IDictionary<string, object> meta = null);
        bool UpdateAccount(string account, IDictionary<string, object> meta = null);
        bool DeleteAccount(string account);
        bool AccountExists(string account);

        decimal GetBalance(string account);
        MoneyOperationResult Withdraw(string account, decimal amount);
        MoneyOperationResult Deposit(string account, decimal amount);
        MoneyOperationResult Transfer(string fromAccount, string toAccount, decimal amount);

        bool SupportsPlayerWallet { get; }

        decimal TakeCoinItemsFromActiveSlot(IServerPlayer player);
        void GiveCoinItemsToPlayer(IServerPlayer player, decimal amount);

        IReadOnlyList<CoinDisplayInfo> CoinDenominations { get; }
    }
}
