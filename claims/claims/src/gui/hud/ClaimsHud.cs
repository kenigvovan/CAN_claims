using System.Collections.Generic;
using Vintagestory.API.Client;

namespace claims.src.gui.hud
{
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
        private List<string> shownLines = new List<string>();
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
        protected abstract List<string> BuildLines();

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
                    shownLines = new List<string>();
                    SingleComposer = null;
                }
                return;
            }

            List<string> lines = BuildLines();
            if (wasVisible && SameLines(lines, shownLines)) return;

            wasVisible = true;
            shownLines = lines;
            Compose(lines);
        }

        private static bool SameLines(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private void Compose(List<string> lines)
        {
            const double lineHeight = 22;

            ElementBounds panelBounds = ElementBounds
                .Fixed(EnumDialogArea.None, OffsetX, OffsetY, PanelWidth, lines.Count * lineHeight + 16)
                .WithAlignment(Anchor);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(8);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(panelBounds);

            var composer = capi.Gui.CreateCompo(ComposerKey, panelBounds)
                                   .AddDialogBG(bgBounds, false);

            ElementBounds row = ElementBounds.Fixed(0, 0, PanelWidth - 16, lineHeight);
            foreach (var line in lines)
            {
                composer.AddStaticText(line, CairoFont.WhiteSmallText(), row);
                row = row.BelowCopy();
            }

            SingleComposer = composer.Compose();
        }

        /// <summary>Composer name, unique per panel.</summary>
        protected abstract string ComposerKey { get; }

        public override bool TryClose() => false;
        public override void OnMouseDown(MouseEvent args) { }
    }
}
