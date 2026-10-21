using claims.src.economy;
using claims.src.part;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class MoneyCommands: BaseCommand
    {
        private static TextCommandResult WalletUnavailable()
        {
            return TextCommandResult.Success(Lang.Get("claims:economy_not_configured"));
        }

        private static TextCommandResult UseBankChest()
        {
            return TextCommandResult.Success(Lang.Get("claims:economy_use_bank_chest"));
        }

        public static TextCommandResult OnCityBalance(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:has_city_or_village");
            }

            return SuccessWithParams("claims:economy_virtual_city_balance", new object[] { claims.economyProvider.GetBalance(playerInfo.City.MoneyAccountName)});
        }

        public static TextCommandResult OnCityWithdraw(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            int toWithdraw = (int)args.Parsers[0].GetValue();

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:has_city_or_village");
            }

            if (!claims.economyProvider.SupportsPlayerWallet)
            {
                return WalletUnavailable();
            }
            if (!claims.economyProvider.SupportsRemoteCoinTransfer)
            {
                return UseBankChest();
            }

            if(claims.economyProvider.GetBalance(playerInfo.City.MoneyAccountName) < toWithdraw)
            {
                return TextCommandResult.Success("claims:economy_virtual_city_not_enough_money");
            }

            if(claims.economyProvider.Withdraw(playerInfo.City.MoneyAccountName, (decimal)toWithdraw) == MoneyOperationResult.Success)
            {
                claims.economyProvider.GiveCoinItemsToPlayer(player, toWithdraw);
                return SuccessWithParams("claims:economy_virtual_city_withdrawn", new object[] { toWithdraw });
            }
            return TextCommandResult.Success("claims:economy_virtual_city_withdraw_error");
        }

        public static TextCommandResult OnCityDeposit(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:economy_no_city");
            }

            if (!claims.economyProvider.SupportsPlayerWallet)
            {
                return WalletUnavailable();
            }
            if (!claims.economyProvider.SupportsRemoteCoinTransfer)
            {
                return UseBankChest();
            }

            decimal collectedValue = claims.economyProvider.TakeCoinItemsFromActiveSlot(player);
            if (collectedValue <= 0)
            {
                return TextCommandResult.Success("claims:economy_deposit_no_coin_in_hand");
            }
            var result = claims.economyProvider.Deposit(playerInfo.City.MoneyAccountName, collectedValue);
            if (result != MoneyOperationResult.Success)
            {
                claims.economyProvider.GiveCoinItemsToPlayer(player, collectedValue);
                claims.sapi.Logger.Warning("[claims] City deposit to {0} failed: {1}", playerInfo.City.MoneyAccountName, result);
                return SuccessWithParams("claims:economy_deposit_account_failed", new object[] { result });
            }
            decimal repaid = CityDebt.Repay(playerInfo.City, collectedValue);
            if (repaid > 0)
            {
                return SuccessWithParams("claims:economy_city_deposit_repaid_debt",
                    new object[] { collectedValue, repaid, playerInfo.City.DebtBalance });
            }
            return SuccessWithParams("claims:economy_virtual_city_deposited", new object[] { collectedValue });
        }
        public static TextCommandResult OnAllianceBalance(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }

            if(!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }
            return SuccessWithParams("claims:economy_virtual_alliance_balance", new object[] { claims.economyProvider.GetBalance(playerInfo.Alliance.MoneyAccountName) });
        }

        public static TextCommandResult OnAllianceWithdraw(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            int toWithdraw = (int)args.Parsers[0].GetValue();

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }

            if (!claims.economyProvider.SupportsPlayerWallet)
            {
                return WalletUnavailable();
            }
            if (!claims.economyProvider.SupportsRemoteCoinTransfer)
            {
                return UseBankChest();
            }

            if (claims.economyProvider.GetBalance(playerInfo.Alliance.MoneyAccountName) < toWithdraw)
            {
                return TextCommandResult.Success("claims:economy_virtual_city_not_enough_money");
            }

            if (claims.economyProvider.Withdraw(playerInfo.Alliance.MoneyAccountName, (decimal)toWithdraw) == MoneyOperationResult.Success)
            {
                claims.economyProvider.GiveCoinItemsToPlayer(player, toWithdraw);
                return SuccessWithParams("claims:economy_virtual_alliance_withdrawn", new object[] { toWithdraw });
            }
            return TextCommandResult.Success("claims:economy_virtual_city_withdraw_error");
        }

        public static TextCommandResult OnAllianceDeposit(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }

            if (!claims.economyProvider.SupportsPlayerWallet)
            {
                return WalletUnavailable();
            }
            if (!claims.economyProvider.SupportsRemoteCoinTransfer)
            {
                return UseBankChest();
            }

            decimal collectedValue = claims.economyProvider.TakeCoinItemsFromActiveSlot(player);
            if (collectedValue <= 0)
            {
                return TextCommandResult.Success("claims:economy_deposit_no_coin_in_hand");
            }
            var result = claims.economyProvider.Deposit(playerInfo.Alliance.MoneyAccountName, collectedValue);
            if (result != MoneyOperationResult.Success)
            {
                claims.economyProvider.GiveCoinItemsToPlayer(player, collectedValue);
                claims.sapi.Logger.Warning("[claims] Alliance deposit to {0} failed: {1}", playerInfo.Alliance.MoneyAccountName, result);
                return SuccessWithParams("claims:economy_deposit_account_failed", new object[] { result });
            }
            return SuccessWithParams("claims:economy_virtual_alliance_deposited", new object[] { collectedValue });
        }
    }
}
