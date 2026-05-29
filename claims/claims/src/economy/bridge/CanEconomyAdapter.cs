using System.Collections.Generic;
using caneconomy.src.implementations;
using caneconomy.src.implementations.VirtualMoney;
using caneconomy.src.interfaces;
using Vintagestory.API.Server;

namespace claims.src.economy.bridge
{
    public sealed class CanEconomyAdapter : IMoneyProvider
    {
        private readonly List<CoinDisplayInfo> coinDenominations = new();

        public bool SupportsPlayerWallet => true;
        public IReadOnlyList<CoinDisplayInfo> CoinDenominations => coinDenominations;

        private static EconomyHandler Handler => caneconomy.caneconomy.getHandler();

        public bool NewAccount(string account, IDictionary<string, object> meta = null)
            => Handler?.newAccount(account, ToDict(meta)) ?? false;

        public bool UpdateAccount(string account, IDictionary<string, object> meta = null)
            => Handler?.updateAccount(account, ToDict(meta)) ?? false;

        public bool DeleteAccount(string account) => Handler?.deleteAccount(account) ?? false;
        public bool AccountExists(string account) => Handler?.accountExist(account) ?? false;

        public decimal GetBalance(string account) => Handler?.getBalance(account) ?? 0m;

        public MoneyOperationResult Withdraw(string account, decimal amount) => Map(Handler?.withdraw(account, amount));
        public MoneyOperationResult Deposit(string account, decimal amount) => Map(Handler?.deposit(account, amount));
        public MoneyOperationResult Transfer(string fromAccount, string toAccount, decimal amount) => Map(Handler?.depositFromAToB(fromAccount, toAccount, amount));

        public decimal TakeCoinItemsFromActiveSlot(IServerPlayer player)
            => VirtualMoneyEconomyHandler.TakeCurrencyItemsFromPlayerActiveSlot(player);

        public void GiveCoinItemsToPlayer(IServerPlayer player, decimal amount)
            => VirtualMoneyEconomyHandler.GiveCurrencyItemsToPlayer(player, amount);

        public void RefreshCoinDenominations()
        {
            coinDenominations.Clear();
            var cfg = caneconomy.caneconomy.config;
            if (cfg == null) return;
            foreach (var it in cfg.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE)
            {
                var ci = it.Value;
                coinDenominations.Add(new CoinDisplayInfo(ci.CoinValue, ci.CollectibleCode, ci.CoinAttributes));
            }
        }

        private static Dictionary<string, object> ToDict(IDictionary<string, object> meta)
        {
            if (meta == null) return null;
            if (meta is Dictionary<string, object> d) return d;
            return new Dictionary<string, object>(meta);
        }

        private static MoneyOperationResult Map(OperationResult result)
        {
            if (result == null) return MoneyOperationResult.NotSupported;
            return result.ResultState switch
            {
                OperationResult.EnumOperationResultState.SUCCCESS => MoneyOperationResult.Success,
                OperationResult.EnumOperationResultState.WRONG_PARAMETER_VALUE => MoneyOperationResult.WrongParameter,
                OperationResult.EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND => MoneyOperationResult.SourceAccountNotFound,
                OperationResult.EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND => MoneyOperationResult.TargetAccountNotFound,
                OperationResult.EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY => MoneyOperationResult.SourceNotEnoughMoney,
                OperationResult.EnumOperationResultState.FAILED_TARGET_DEPOSIT => MoneyOperationResult.FailedTargetDeposit,
                OperationResult.EnumOperationResultState.FAILED_SOURCE_WITHDRAW => MoneyOperationResult.FailedSourceWithdraw,
                OperationResult.EnumOperationResultState.GREATER_THAN_MAX_BALANCE => MoneyOperationResult.GreaterThanMaxBalance,
                _ => MoneyOperationResult.None,
            };
        }
    }
}
