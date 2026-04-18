using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace claims.src.cropbehaviors
{
    public class BehaviorOnlyOnFarmPlot : CropBehavior
    {
        public BehaviorOnlyOnFarmPlot(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
        }

        public override bool TryGrowCrop(ICoreAPI api, IFarmlandBlockEntity farmland,
            double currentTotalHours, int newGrowthStage, ref EnumHandling handling)
        {
            if (!claims.config.CROPS_ONLY_ON_FARM_PLOTS) return false;
            if (IsOnFarmPlot(farmland.Pos.X, farmland.Pos.Z)) return false;

            handling = EnumHandling.PreventDefault;
            return false;
        }

        public override void OnPlanted(ICoreAPI api, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (!claims.config.CROPS_ONLY_ON_FARM_PLOTS) return;
            if (api.Side != EnumAppSide.Server) return;
            if (blockSel == null) return;
            if (IsOnFarmPlot(blockSel.Position.X, blockSel.Position.Z)) return;

            if (byEntity is EntityPlayer ep && ep.Player is IServerPlayer sp)
            {
                MessageHandler.sendMsgToPlayer(sp, Lang.Get("claims:crop_wont_grow_outside_farm"));
            }
        }

        private static bool IsOnFarmPlot(int blockX, int blockZ)
        {
            if (claims.dataStorage == null) return false;
            var plotPos = PlotPosition.fromXZ(blockX, blockZ);
            return claims.dataStorage.GetPlot(plotPos, out Plot plot) && plot.Type == PlotType.FARM;
        }
    }
}
