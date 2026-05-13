using caneconomy.src.implementations.VirtualMoney;
using claims.src.part;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class MoneyCommands: BaseCommand
    {
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

            return SuccessWithParams("claims:economy_virtual_city_balance", new object[] { claims.economyHandler.getBalance(playerInfo.City.MoneyAccountName)});
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

            if(claims.economyHandler.getBalance(playerInfo.City.MoneyAccountName) < toWithdraw)
            {
                return TextCommandResult.Success("claims:economy_virtual_city_not_enough_money");
            }

            if(claims.economyHandler.withdraw(playerInfo.City.MoneyAccountName, (decimal)toWithdraw).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                VirtualMoneyEconomyHandler.GiveCurrencyItemsToPlayer(player, toWithdraw);
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

            decimal collectedValue = VirtualMoneyEconomyHandler.TakeCurrencyItemsFromPlayerActiveSlot(player);
            if(collectedValue > 0)
            {
                claims.economyHandler.deposit(playerInfo.City.MoneyAccountName, (decimal)collectedValue);
                return SuccessWithParams("claims:economy_virtual_city_deposited", new object[] { collectedValue });
            }
            return TextCommandResult.Success("claims:economy_virtual_city_deposit_error");
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
            return SuccessWithParams("claims:economy_virtual_alliance_balance", new object[] { claims.economyHandler.getBalance(playerInfo.Alliance.MoneyAccountName) });
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

            if (claims.economyHandler.getBalance(playerInfo.Alliance.MoneyAccountName) < toWithdraw)
            {
                return TextCommandResult.Success("claims:economy_virtual_city_not_enough_money");
            }

            if (claims.economyHandler.withdraw(playerInfo.Alliance.MoneyAccountName, (decimal)toWithdraw).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                VirtualMoneyEconomyHandler.GiveCurrencyItemsToPlayer(player, toWithdraw);
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

            decimal collectedValue = VirtualMoneyEconomyHandler.TakeCurrencyItemsFromPlayerActiveSlot(player);
            if (collectedValue > 0)
            {
                claims.economyHandler.deposit(playerInfo.Alliance.MoneyAccountName, (decimal)collectedValue);
                return SuccessWithParams("claims:economy_virtual_alliance_deposited", new object[] { collectedValue });
            }
            return TextCommandResult.Success("claims:economy_virtual_city_deposit_error");
        }
    }
}
