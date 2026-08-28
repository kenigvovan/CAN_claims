using System;
using System.Collections.Generic;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.hud
{
    /// <summary>
    /// One line of a HUD panel. The panels used to hand up bare strings and every one of them was
    /// drawn in the same white, so a heading, a score and a "nothing here" notice all read alike.
    /// </summary>
    public sealed class HudLine
    {
        public string Text;
        public double[] Color;
        public int FontSize;

        /// <summary>Heading of a panel, in the shared section colour.</summary>
        public static HudLine Title(string text)
            => new HudLine { Text = text, Color = ClaimsColors.Section, FontSize = 17 };

        /// <summary>An ordinary line - what the panel exists to show.</summary>
        public static HudLine Value(string text)
            => new HudLine { Text = text, Color = ClaimsColors.Value, FontSize = 15 };

        /// <summary>An aside: an empty state, a caption, something not worth the accent colour.</summary>
        public static HudLine Muted(string text)
            => new HudLine { Text = text, Color = ClaimsColors.Label, FontSize = 15 };
    }

    /// <summary>
    /// A HUD panel made of plain text lines.
    ///
    /// In immediate mode these were redrawn every frame, so they always showed current data for
    /// free. Here the composer has to be rebuilt on purpose, so each panel reports the lines it
    /// wants and the base recomposes only when that list actually changed - not every frame, and
    /// not never.
    /// </summary>
    public abstract class ClaimsHud : HudElement, IMovableHudPanel
    {
        private List<HudLine> shownLines = new List<HudLine>();
        private bool wasVisible;

        protected ClaimsHud(ICoreClientAPI capi) : base(capi)
        {
        }

        public override EnumDialogType DialogType => EnumDialogType.HUD;
        public override bool Focusable => false;
        public override bool ShouldReceiveKeyboardEvents() => false;
        public override float ZSize => 0f;

        /// <summary>Whether the panel should be on screen at all right now.</summary>
        protected abstract bool IsVisible { get; }

        /// <summary>The lines to show, top to bottom. An empty list hides the panel.</summary>
        protected abstract List<HudLine> BuildLines();

        /// <summary>Where the panel sits, as a fraction of the screen.</summary>
        protected abstract EnumDialogArea Anchor { get; }

        protected virtual double OffsetX => 0;
        protected virtual double OffsetY => 0;
        protected virtual double PanelWidth => 260;

        public override void OnRenderGUI(float deltaTime)
        {
            Refresh();
            base.OnRenderGUI(deltaTime);
        }

        /// <summary>Recomposes only when the visible text has changed.</summary>
        private void Refresh()
        {
            bool visible = IsVisible;
            if (!visible)
            {
                if (wasVisible)
                {
                    wasVisible = false;
                    shownLines = new List<HudLine>();
                    // NOT `SingleComposer = null`: the vanilla DlgComposers setter dereferences the
                    // assigned value, so hiding the panel that way crashed the render thread the
                    // moment the first battle involving the player's own city ended.
                    SingleComposer?.Dispose();
                    Composers.Remove("single");
                }
                return;
            }

            List<HudLine> lines = BuildLines();
            if (wasVisible && SameLines(lines, shownLines)) return;

            wasVisible = true;
            shownLines = lines;
            Compose(lines);
        }

        private static bool SameLines(List<HudLine> a, List<HudLine> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                // Colour too: a line can keep its wording and change what it means.
                if (a[i].Text != b[i].Text || a[i].Color != b[i].Color) return false;
            }
            return true;
        }

        /// <summary>Recompose on the next frame even though the lines have not changed.</summary>
        public void OnLayoutChanged() => wasVisible = false;

        protected const double LineHeight = 22;
        protected const double PanelPadding = 16;

        private void Compose(List<HudLine> lines)
        {
            double height = lines.Count * LineHeight + PanelPadding;
            ElementBounds panelBounds = PanelBounds(height);

            // Fill inside the panel, nothing else: AddDialogBG already makes this a child of
            // panelBounds, so naming panelBounds a child of it in turn made the two point at each
            // other and CalcWorldBounds recursed until the stack ran out.
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(8);

            var composer = capi.Gui.CreateCompo(ComposerKey, panelBounds)
                                   .AddDialogBG(bgBounds, false);

            ElementBounds row = ElementBounds.Fixed(0, 0, PanelWidth - PanelPadding, LineHeight);
            for (int i = 0; i < lines.Count; i++)
            {
                HudLine line = lines[i];

                composer.AddStaticText(line.Text,
                    CairoFont.WhiteSmallText().WithFontSize(line.FontSize).WithColor(line.Color),
                    row, ComposerKey + "-line" + i);
                row = row.BelowCopy();
            }

            // The previous composer is not disposed by the setter; without this every score change
            // leaked one.
            SingleComposer?.Dispose();
            SingleComposer = composer.Compose();
        }

        /// <summary>
        /// Bounds for a panel of the given height: the spot the player dragged it to, or the
        /// hardcoded anchor. Stored positions are normalised (0..1 of the unscaled viewport) and
        /// clamped so a panel cannot end up off-screen after a resolution change.
        /// </summary>
        private ElementBounds PanelBounds(double height)
        {
            var pos = ClaimsHudLayout.Current?.Get(ComposerKey);
            if (pos == null)
            {
                return ElementBounds
                    .Fixed(EnumDialogArea.None, OffsetX, OffsetY, PanelWidth, height)
                    .WithAlignment(Anchor);
            }

            double screenW = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
            double screenH = capi.Render.FrameHeight / RuntimeEnv.GUIScale;
            double x = GameMath.Clamp(pos.X * screenW, 0, Math.Max(0, screenW - PanelWidth));
            double y = GameMath.Clamp(pos.Y * screenH, 0, Math.Max(0, screenH - height));
            return ElementBounds.Fixed(x, y, PanelWidth, height);
        }

        // Geometry the layout editor needs to draw a ghost of this panel.

        public string LayoutKey => ComposerKey;
        public double EditorWidth => PanelWidth;

        /// <summary>Ghost height: panels grow with their lines, this is a typical size.</summary>
        public virtual double EditorHeight => 2 * LineHeight + PanelPadding;

        /// <summary>What the ghost is labeled with in the layout editor.</summary>
        public abstract string EditorLabel { get; }

        /// <summary>Unscaled top-left corner the anchor would place a panel of this height at.</summary>
        public (double X, double Y) DefaultTopLeft(double screenW, double screenH, double height)
        {
            switch (Anchor)
            {
                case EnumDialogArea.CenterTop:
                    return (screenW / 2 - PanelWidth / 2 + OffsetX, OffsetY);
                case EnumDialogArea.CenterBottom:
                    return (screenW / 2 - PanelWidth / 2 + OffsetX, screenH - height + OffsetY);
                case EnumDialogArea.RightTop:
                    return (screenW - PanelWidth + OffsetX, OffsetY);
                case EnumDialogArea.RightBottom:
                    return (screenW - PanelWidth + OffsetX, screenH - height + OffsetY);
                case EnumDialogArea.LeftBottom:
                    return (OffsetX, screenH - height + OffsetY);
                default:
                    return (OffsetX, OffsetY);
            }
        }

        /// <summary>Composer name, unique per panel.</summary>
        protected abstract string ComposerKey { get; }

        public override bool TryClose() => false;
        public override void OnMouseDown(MouseEvent args) { }
    }
}
