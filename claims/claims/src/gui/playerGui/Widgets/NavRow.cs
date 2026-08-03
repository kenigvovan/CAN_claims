using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>One icon button in the bottom navigation row.</summary>
    public sealed class NavButton
    {
        public string Icon;
        public Action OnClick;
        /// <summary>Optional tooltip. Already translated, not a lang key.</summary>
        public string Tooltip;

        public NavButton(string icon, Action onClick, string tooltip = null)
        {
            Icon = icon;
            OnClick = onClick;
            Tooltip = tooltip;
        }
    }

    /// <summary>
    /// The separator plus row of 48x48 icon buttons pinned near the bottom of a page. Five pages
    /// hand-rolled this with the same 0.85 / 0.90 height fractions and the same RightCopy(20) step.
    /// </summary>
    public static class NavRow
    {
        /// <summary>
        /// Fraction of the dialog height the separator sits at. Public so a page that fills the
        /// window - the log, the map - can size itself to stop right above the row.
        /// </summary>
        public const double LineHeightFraction = 0.85;

        private const double ButtonHeightFraction = 0.90;

        // 40px with a 12px gap fits eight buttons in the 500px window; the old 48/20 sizing ran
        // three of them past the edge once the log and map buttons joined the row.
        private const double ButtonSize = 40;
        private const double ButtonGap = 12;

        public static void Build(CANClaimsGui gui, ElementBounds anchor, ElementBounds lineBounds, double firstButtonX, params NavButton[] buttons)
        {
            var compo = gui.SingleComposer;

            // Anchored to the dialog body, not to whatever bounds the caller happened to pass: the
            // row sits at a fixed height near the bottom, and deriving it from the page's own
            // cursor put it wherever that cursor had wandered to - sometimes off screen.
            var line2Bounds = ElementBounds
                .Fixed(0, gui.mainBounds.fixedHeight * LineHeightFraction, lineBounds.fixedWidth, 5)
                .WithParent(gui.mainBounds);
            compo.AddInset(line2Bounds);

            // Buttons wrap rather than run off the edge: pages have grown past the six that used to
            // fit on one row.
            double usableWidth = lineBounds.fixedWidth - firstButtonX;
            int perRow = Math.Max(1, (int)((usableWidth + ButtonGap) / (ButtonSize + ButtonGap)));

            double baseY = gui.mainBounds.fixedHeight * ButtonHeightFraction;

            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];

                ElementBounds target = ElementBounds
                    .Fixed(0, 0, ButtonSize, ButtonSize)
                    .WithParent(gui.mainBounds)
                    .WithAlignment(EnumDialogArea.LeftTop);
                target.fixedX = firstButtonX + (i % perRow) * (ButtonSize + ButtonGap);
                target.fixedY = baseY + (i / perRow) * (ButtonSize + 4);

                compo.AddIconButton(button.Icon, (bool t) => button.OnClick(), target);
                Tooltip.Add(compo, button.Tooltip, target, "navtip-" + i);
            }
        }
    }
}
