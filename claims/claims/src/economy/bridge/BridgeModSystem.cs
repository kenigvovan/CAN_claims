using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.economy.bridge
{
    public class BridgeModSystem : ModSystem
    {
        public override double ExecuteOrder() => 1.0;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            if (!api.ModLoader.IsModEnabled("caneconomy")) return;
            api.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, InstallAdapter);
        }

        private static void InstallAdapter()
        {
            var adapter = new CanEconomyAdapter();
            adapter.RefreshCoinDenominations();
            claims.economyProvider = adapter;

            var caneCfg = caneconomy.caneconomy.config;
            if (caneCfg != null)
            {
                claims.config.SELECTED_ECONOMY_HANDLER = caneCfg.SELECTED_ECONOMY_HANDLER;
                claims.config.COINS_VALUES_TO_CODE = caneCfg.COINS_VALUES_TO_CODE;
            }

            caneconomy.src.implementations.VirtualMoney.VirtualMoneyEconomyHandler.AccountBalanceChanged += claims.RaiseAccountBalanceChanged;

            caneconomy.caneconomy.OnBlockRemovedBlockEntityOpenableContainer += RealBankActions.OnBlockRemoved;
            caneconomy.caneconomy.OnReceivedClientPacketBlockEntitySign += RealBankActions.OnButtonSave;
        }
    }
}
