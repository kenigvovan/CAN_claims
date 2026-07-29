namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>
    /// The palette every page and cell shares. These arrays used to be copied verbatim into each
    /// cell element that wanted them.
    /// </summary>
    public static class ClaimsColors
    {
        /// <summary>Heading of a card or a block inside a page.</summary>
        public static readonly double[] Section = { 0.95, 0.84, 0.52, 1.0 };

        /// <summary>Muted grey for the left-hand caption of a caption/value row.</summary>
        public static readonly double[] Label = { 0.74, 0.74, 0.74, 1.0 };

        /// <summary>Warm accent for the value itself, so it reads apart from its caption.</summary>
        public static readonly double[] Value = { 1.00, 0.88, 0.45, 1.0 };

        public static readonly double[] Warning = { 0.93, 0.75, 0.30, 1.0 };
        public static readonly double[] Danger = { 0.90, 0.35, 0.30, 1.0 };
        public static readonly double[] Success = { 0.45, 0.80, 0.45, 1.0 };
    }
}
