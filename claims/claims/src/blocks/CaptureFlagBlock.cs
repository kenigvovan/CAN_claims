using Vintagestory.API.Common;

namespace claims.src.blocks
{
    /// <summary>
    /// Capture flag. The tooltip text is built by BlockEntityBehaviorFlag.GetBlockInfo, which the
    /// vanilla Block.GetPlacedBlockInfo already collects from the block entity - this class used to
    /// override that and return a bare, unlabelled number instead.
    /// </summary>
    public class CaptureFlagBlock : Block
    {
    }
}
