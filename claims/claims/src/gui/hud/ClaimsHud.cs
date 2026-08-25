using System.Collections.Generic;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;

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
    public abstract class ClaimsHud : HudElement
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

        private void Compose(List<HudLine> lines)
        {
            const double lineHeight = 22;

            ElementBounds panelBounds = ElementBounds
                .Fixed(EnumDialogArea.None, OffsetX, OffsetY, PanelWidth, lines.Count * lineHeight + 16)
                .WithAlignment(Anchor);

            // Fill inside the panel, nothing else: AddDialogBG already makes this a child of
            // panelBounds, so naming panelBounds a child of it in turn made the two point at each
            // other and CalcWorldBounds recursed until the stack ran out.
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(8);

            var composer = capi.Gui.CreateCompo(ComposerKey, panelBounds)
                                   .AddDialogBG(bgBounds, false);

            ElementBounds row = ElementBounds.Fixed(0, 0, PanelWidth - 16, lineHeight);
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

        /// <summary>Composer name, unique per panel.</summary>
        protected abstract string ComposerKey { get; }

        public override bool TryClose() => false;
        public override void OnMouseDown(MouseEvent args) { }
    }
}
