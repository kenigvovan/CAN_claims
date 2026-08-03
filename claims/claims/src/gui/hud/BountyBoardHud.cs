using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.hud
{
    /// <summary>Standing bounties, newest list as the server last sent it.</summary>
    public class BountyBoardHud : ClaimsHud
    {
        public BountyBoardHud(ICoreClientAPI capi) : base(capi)
        {
        }

        public override string ToggleKeyCombinationCode => null;
        protected override string ComposerKey => "claims-bounty-hud";

        protected override EnumDialogArea Anchor => EnumDialogArea.CenterTop;
        protected override double OffsetY => 120;
        protected override double PanelWidth => 280;

        protected override bool IsVisible => ClaimsHudState.ShowBountyBoard;

        protected override List<HudLine> BuildLines()
        {
            var lines = new List<HudLine> { HudLine.Title(Lang.Get("claims:gui-bounty-board-title")) };

            var board = ClaimsHudState.BountyBoard;
            if (board == null || board.Count == 0)
            {
                lines.Add(HudLine.Muted(Lang.Get("claims:gui-bounty-board-empty")));
                return lines;
            }

            foreach (var entry in board)
            {
                lines.Add(HudLine.Value(entry.Name + "  -  " + entry.Amount));
            }
            return lines;
        }
    }
}
