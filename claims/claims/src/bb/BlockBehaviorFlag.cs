using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using claims.src.auxialiry;
using claims.src.beb;
using claims.src.part.structure.conflict;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace claims.src.bb
{
    public class BlockBehaviorFlag : BlockBehavior
    {
        protected ItemStack[] bannerStacks;
        public float ExpectancyBonus { get; protected set; }
        public float PoleTop { get; protected set; }
        public float PoleBottom { get; protected set; }
        public BlockBehaviorFlag(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.ExpectancyBonus = properties["expectancyBonus"].AsFloat(0f);
            this.PoleTop = properties["poleTop"].AsFloat(3f);
            this.PoleBottom = properties["poleBottom"].AsFloat(2f);
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Client)
            {
                this.bannerStacks = ObjectCacheUtil.GetOrCreate(api, "bannerStacks", delegate
                {

                    List<ItemStack> bannerStacks = new();
                    Item[] banners = api.World.SearchItems(new AssetLocation("cloth-*"));

                    foreach (Item banner in banners)
                        bannerStacks.AddRange(banner.GetHandBookStacks(api as ICoreClientAPI));

                    return bannerStacks.ToArray();
                });
            }
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
        {
            return new WorldInteraction[1] {
                    new () {
                        ActionLangCode = "claims:blockhelp-flag-set",
                        MouseButton    = EnumMouseButton.Right,
                        Itemstacks     = this.bannerStacks
                    }
                };
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling
        )
        {

            handling = EnumHandling.PreventDefault;
            world.BlockAccessor
                .GetBlockEntity(blockSel.Position)?
                .GetBehavior<BlockEntityBehaviorFlag>()?
                .TryStartCapture(byPlayer);

            return true;
        }


        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventSubsequent;
            return true;
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorFlag>() is BlockEntityBehaviorFlag beh)
            {                
                if (--beh.TimesToBreak > 0)
                {
                    handling = EnumHandling.PreventDefault;
                    //byPlayer?.Entity.World.BlockAccessor.MarkBlockDirty(pos);
                    byPlayer?.Entity.World.BlockAccessor.MarkBlockEntityDirty(pos);
                    return;
                }
                else
                {
                    if (beh.ConflictGuid != null)
                    {
                        if (!claims.dataStorage.WarsTimes.TryGetValue(beh.ConflictGuid, out var warTime))
                        {
                            return;
                        }
                        var plotPos = PlotPosition.fromBlockPos(pos);
                        if (warTime.PlotAttacks.TryGetValue(plotPos, out var plotAttack))
                        {
                            warTime.PlotAttacks.Remove(plotPos);
                        }
                    }
                }
            }
            
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
        }
    }
}
