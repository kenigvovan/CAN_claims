using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>
    /// Hover labels sized to their own text.
    ///
    /// Passing a fixed width to AddHoverText made every tooltip that wide regardless of content, so
    /// a one-word label came out as a wide empty box. The key matters too: several hover texts added
    /// without one end up sharing a generated name, and only the last survives.
    /// </summary>
    public static class Tooltip
    {
        private const double Padding = 16;
        private const double MaxWidth = 320;
        private const double MinHoverHeight = 25;

        public static void Add(GuiComposer compo, string text, ElementBounds bounds, string key)
        {
            if (string.IsNullOrEmpty(text)) return;

            CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
            double width = font.GetTextExtents(text).Width + Padding;
            if (width > MaxWidth) width = MaxWidth;

            // A row that was shrunk to fit its text can end up with no height at all - list headings
            // are one such - and then there is nothing the cursor can be inside of.
            ElementBounds area = bounds;
            if (area.fixedHeight < MinHoverHeight)
            {
                area = bounds.FlatCopy();
                area.fixedHeight = MinHoverHeight;
            }

            compo.AddHoverText(text, font, (int)width, area, key);
        }
    }
}
