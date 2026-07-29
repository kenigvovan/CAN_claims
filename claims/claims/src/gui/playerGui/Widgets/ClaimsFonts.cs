using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>
    /// Fonts the pages share. Each page used to build these inline, often under a name left over
    /// from wherever it was copied from - "criminalsTabFont" on the cities list and the conflict page.
    /// </summary>
    public static class ClaimsFonts
    {
        /// <summary>Left-aligned label used for the stat lines at the top of most pages.</summary>
        public static CairoFont PageLabel =>
            CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);

        /// <summary>Centered heading above a list.</summary>
        public static CairoFont ListHeader =>
            CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center);
    }
}
