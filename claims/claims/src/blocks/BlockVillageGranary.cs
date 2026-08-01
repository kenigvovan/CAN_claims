using claims.src.auxialiry;
using claims.src.beb;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.blocks
{
    /// <summary>
    /// Village granary. The state lives in <see cref="BlockEntityVillageGranary"/>; the block itself
    /// only forwards the right click and keeps the village's <see cref="PlotDescVillage.GranaryPos"/>
    /// pointing at wherever the granary actually stands.
    ///
    /// It drops as an item and can be put back: breaking it must not doom the village with no way
    /// to fix it. Only one may stand per village, and only on that village's own land.
    /// </summary>
    public class BlockVillageGranary : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityVillageGranary granary)
            {
                return granary.OnPlayerRightClick(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack,
            BlockSelection blockSel, ref string failureCode)
        {
            if (world.Side == EnumAppSide.Server && !CanStandHere(world, blockSel.Position, out string errorKey))
            {
                if (byPlayer is Vintagestory.API.Server.IServerPlayer serverPlayer)
                {
                    MessageHandler.sendMsgToPlayer(serverPlayer, Lang.Get(errorKey));
                }
                failureCode = "claims:granary-not-here";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (world.Side != EnumAppSide.Server) return;

            if (TryGetVillageAt(blockPos, out City village, out Plot mainPlot, out PlotDescVillage desc))
            {
                desc.GranaryPos = new Vec3i(blockPos.X, blockPos.Y, blockPos.Z);
                mainPlot.saveToDatabase();
                VillageSupplyHelper.SyncBlockEntities(desc);
                MessageHandler.sendMsgInCity(village, Lang.Get("claims:village_granary_placed"));
            }
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.Side == EnumAppSide.Server
                && TryGetVillageAt(pos, out City village, out Plot mainPlot, out PlotDescVillage desc)
                && desc.GranaryPos != null
                && desc.GranaryPos.X == pos.X && desc.GranaryPos.Y == pos.Y && desc.GranaryPos.Z == pos.Z)
            {
                // Forgetting the position is what lets a new granary be placed afterwards.
                desc.GranaryPos = null;
                mainPlot.saveToDatabase();
                MessageHandler.sendMsgInCity(village, Lang.Get("claims:village_granary_broken"));
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        /// <summary>A granary belongs on the land of a village that has none standing yet.</summary>
        private bool CanStandHere(IWorldAccessor world, BlockPos pos, out string errorKey)
        {
            if (!TryGetVillageAt(pos, out _, out _, out PlotDescVillage desc))
            {
                errorKey = "claims:village_granary_needs_village";
                return false;
            }
            if (desc.GranaryPos != null && IsGranaryStandingAt(world, desc.GranaryPos))
            {
                errorKey = "claims:village_granary_already_exists";
                return false;
            }
            errorKey = null;
            return true;
        }

        private bool IsGranaryStandingAt(IWorldAccessor world, Vec3i pos)
        {
            BlockPos bp = new BlockPos(pos.X, pos.Y, pos.Z);
            return world.BlockAccessor.GetBlockEntity(bp) is BlockEntityVillageGranary;
        }

        private static bool TryGetVillageAt(BlockPos pos, out City village, out Plot mainPlot, out PlotDescVillage desc)
        {
            village = null;
            mainPlot = null;
            desc = null;
            if (!claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos), out Plot plot)) return false;
            if (!plot.hasCity() || !plot.getCity().IsVillage()) return false;

            village = plot.getCity();
            return village.TryGetVillageMain(out mainPlot, out desc);
        }
    }
}
