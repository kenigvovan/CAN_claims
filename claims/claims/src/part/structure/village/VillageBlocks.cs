using Vintagestory.API.Common;

namespace claims.src.part.structure
{
    /// <summary>
    /// The blocks a village owns, in one place. Placement, removal and the "is this still our
    /// block" checks all read from here, so they cannot drift apart when a code changes.
    /// </summary>
    public static class VillageBlocks
    {
        public const string AnchorCode = "villageanchor";
        public const string GranaryCode = "villagegranary";

        public static AssetLocation Anchor => new AssetLocation("claims", AnchorCode);
        public static AssetLocation Granary => new AssetLocation("claims", GranaryCode);
    }
}
