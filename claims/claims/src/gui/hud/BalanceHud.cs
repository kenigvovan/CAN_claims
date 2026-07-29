using System.Collections.Generic;
using claims.src.rights;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.hud
{
    /// <summary>The player's purse, and the city treasury for those allowed to see it.</summary>
    public class BalanceHud : ClaimsHud
    {
        public BalanceHud(ICoreClientAPI capi) : base(capi)
        {
        }

        public override string ToggleKeyCombinationCode => null;
        protected override string ComposerKey => "claims-balance-hud";

        protected override EnumDialogArea Anchor => EnumDialogArea.RightBottom;
        protected override double OffsetX => -20;
        protected override double OffsetY => -60;
        protected override double PanelWidth => 220;

        protected override bool IsVisible =>
            ClaimsHudState.ShowBalance
            && claims.config?.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY"
            && claims.clientDataStorage?.clientPlayerInfo != null;

        protected override List<string> BuildLines()
        {
            var lines = new List<string>();
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;

            lines.Add(Lang.Get("claims:gui-hud-player-balance", clientInfo.PlayerBalance));

            if (clientInfo.CityInfo != null
                && clientInfo.PlayerPermissions.HasPermission(EnumPlayerPermissions.CITY_SEE_BALANCE))
            {
                lines.Add(Lang.Get("claims:gui-city-balance-hud", clientInfo.CityInfo.CityBalance));
            }

            return lines;
        }
    }
}
