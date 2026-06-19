using System.Collections.Generic;
using claims.src.economy;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.economy.bridge
{
    public class BridgeModSystem : ModSystem
    {
        public override double ExecuteOrder() => 1.0;

        private bool _adapterInstalled;
        private CanEconomyAdapter _adapter;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            if (!api.ModLoader.IsModEnabled("caneconomy")) return;
            api.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, InstallAdapter);
            // caneconomy fills its coin index only in the RunGame phase (after
            // ModsAndConfigReady), so the denominations must be read there.
            api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, RefreshCoinData);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!_adapterInstalled) return;
            caneconomy.src.implementations.VirtualMoney.VirtualMoneyEconomyHandler.AccountBalanceChanged -= claims.RaiseAccountBalanceChanged;
            caneconomy.caneconomy.OnBlockRemovedBlockEntityOpenableContainer -= RealBankActions.OnBlockRemoved;
            caneconomy.caneconomy.OnReceivedClientPacketBlockEntitySign -= RealBankActions.OnButtonSave;
            claims.economyProvider = new NoopMoneyProvider();
            _adapterInstalled = false;
        }

        private void InstallAdapter()
        {
            _adapter = new CanEconomyAdapter();
            claims.economyProvider = _adapter;

            var caneCfg = caneconomy.caneconomy.config;
            if (caneCfg != null)
            {
                claims.config.SELECTED_ECONOMY_HANDLER = caneCfg.SELECTED_ECONOMY_HANDLER;
                var ordered = new System.Collections.Generic.OrderedDictionary<decimal, string>();
                foreach (var kv in caneCfg.COINS_VALUES_TO_CODE) ordered.Add(kv.Key, kv.Value);
                claims.config.COINS_VALUES_TO_CODE = ordered;
            }

            caneconomy.src.implementations.VirtualMoney.VirtualMoneyEconomyHandler.AccountBalanceChanged += claims.RaiseAccountBalanceChanged;
            caneconomy.caneconomy.OnBlockRemovedBlockEntityOpenableContainer += RealBankActions.OnBlockRemoved;
            caneconomy.caneconomy.OnReceivedClientPacketBlockEntitySign += RealBankActions.OnButtonSave;
            _adapterInstalled = true;
        }

        // Reads the full coin denomination list from the economy provider (whose
        // backing index caneconomy only populates in the RunGame phase) and
        // mirrors it into claims.config so it gets synced to clients.
        private void RefreshCoinData()
        {
            if (_adapter == null) return;
            _adapter.RefreshCoinDenominations();

            var list = new List<CoinDenominationData>();
            foreach (var coin in _adapter.CoinDenominations)
            {
                list.Add(CoinDenominationData.FromDisplayInfo(coin));
            }
            claims.config.COIN_DENOMINATIONS = list;
        }
    }
}
