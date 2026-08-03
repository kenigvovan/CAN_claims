using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace claims.src.beb
{
    /// <summary>
    /// Display mirror of a village's anchor: how long the supplies last and, during a raid, how
    /// many breaks the anchor still absorbs. The authoritative values live on PlotDescVillage
    /// (server, persisted in the mod DB); this block entity only feeds the block-info HUD.
    /// </summary>
    public class BlockEntityVillageAnchor : BlockEntity
    {
        public int SupplyHours { get; set; } = 0;
        public int BreaksLeft { get; set; } = 0;
        public bool RaidRunning { get; set; } = false;

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine(Lang.Get("claims:village_supplies_left", SupplyHours / 24, SupplyHours % 24));
            if (RaidRunning)
            {
                dsc.AppendLine(Lang.Get("claims:village_anchor_breaks_left", BreaksLeft));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            SupplyHours = tree.GetInt("supplyHours");
            BreaksLeft = tree.GetInt("breaksLeft");
            RaidRunning = tree.GetBool("raidRunning");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("supplyHours", SupplyHours);
            tree.SetInt("breaksLeft", BreaksLeft);
            tree.SetBool("raidRunning", RaidRunning);
        }
    }
}
