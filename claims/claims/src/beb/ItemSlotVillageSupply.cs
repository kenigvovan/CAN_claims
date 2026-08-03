using claims.src.part.structure;
using Vintagestory.API.Common;

namespace claims.src.beb
{
    /// <summary>
    /// A granary slot. Takes only what the village actually lives on, so a player learns the list
    /// by trying: anything else simply will not go in.
    /// </summary>
    public class ItemSlotVillageSupply : ItemSlot
    {
        public ItemSlotVillageSupply(InventoryBase inventory) : base(inventory)
        {
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            return VillageSupplies.IsSupply(sourceSlot?.Itemstack) && base.CanHold(sourceSlot);
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return VillageSupplies.IsSupply(sourceSlot?.Itemstack) && base.CanTakeFrom(sourceSlot, priority);
        }
    }
}
