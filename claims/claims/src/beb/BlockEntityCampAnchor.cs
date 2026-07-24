using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace claims.src.beb
{
    /// <summary>
    /// Display mirror of a war camp's remaining anchor breaks. The authoritative counter lives on
    /// PlotDescCamp.BreaksLeft (server, persisted in the mod DB); this block entity is synced to
    /// clients only so the block-info HUD can show "breaks left" when looking at the anchor.
    /// </summary>
    public class BlockEntityCampAnchor : BlockEntity
    {
        public int BreaksLeft { get; set; } = 0;

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine(Lang.Get("claims:camp_anchor_breaks_left", BreaksLeft));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            BreaksLeft = tree.GetInt("breaksLeft");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("breaksLeft", BreaksLeft);
        }
    }
}
