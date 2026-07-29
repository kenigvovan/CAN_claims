using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.hud
{
    /// <summary>Live score of every conflict currently inside its battle window.</summary>
    public class WarHud : ClaimsHud
    {
        public WarHud(ICoreClientAPI capi) : base(capi)
        {
        }

        public override string ToggleKeyCombinationCode => null;
        protected override string ComposerKey => "claims-war-hud";

        protected override EnumDialogArea Anchor => EnumDialogArea.CenterTop;
        protected override double OffsetY => 40;
        protected override double PanelWidth => 320;

        protected override bool IsVisible =>
            ClaimsHudState.ShowWarHud
            && claims.config?.WAR_HUD_ENABLED == true
            && ActiveConflicts().Count > 0;

        protected override List<string> BuildLines()
        {
            var lines = new List<string> { Lang.Get("claims:gui-war-hud-title", claims.config.WAR_SCORE_TO_WIN) };

            foreach (var conflict in ActiveConflicts())
            {
                lines.Add(conflict.FirstPartyName + "   " + conflict.FirstScore
                    + " : " + conflict.SecondScore + "   " + conflict.SecondPartyName);
            }
            return lines;
        }

        private static List<playerGui.structures.cellElements.ClientConflictCellElement> ActiveConflicts()
        {
            var conflicts = claims.clientDataStorage?.clientPlayerInfo?.CityInfo?.ClientConflictCellElements;
            if (conflicts == null) return new List<playerGui.structures.cellElements.ClientConflictCellElement>();

            return conflicts.Where(c => c.ActiveWarTime).ToList();
        }
    }
}
