using System.Collections.Generic;
using claims.src.network.packets;

namespace claims.src.gui.hud
{
    /// <summary>
    /// Which HUDs are showing and what the bounty board holds. Kept out of any one GUI so the flags
    /// survive either front-end being removed, and so the toggle commands have somewhere neutral to
    /// write to.
    /// </summary>
    public static class ClaimsHudState
    {
        public static bool ShowBalance;
        public static bool ShowBountyBoard;
        public static bool ShowWarHud = true;

        public static List<BountyBoardEntry> BountyBoard = new List<BountyBoardEntry>();
    }
}
