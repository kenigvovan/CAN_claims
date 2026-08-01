using claims.src.gui;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace claims.src.beb
{
    /// <summary>
    /// The granary of a village: the container its citizens fill with food and firewood. The hour
    /// timer takes one of each out when the supplies run low, so the settlement lives off what is
    /// stored here instead of a treasury.
    ///
    /// SupplyHours/DecayHours are owned by PlotDescVillage on the server; the copies here exist so
    /// the dialog can show how long the village still holds out.
    /// </summary>
    public class BlockEntityVillageGranary : BlockEntityOpenableContainer
    {
        public const int SlotCount = 6;

        private InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "villagegranary";

        /// <summary>Hours of supplies left, mirrored from PlotDescVillage for the dialog.</summary>
        public int SupplyHours { get; set; } = 0;
        /// <summary>Hours the village has already spent starving; at the limit it falls apart.</summary>
        public int DecayHours { get; set; } = 0;

        public BlockEntityVillageGranary()
        {
            // Own slot type: the granary only accepts food and fuel, which is how a player finds
            // out what counts without reading the server config.
            inventory = new InventoryGeneric(SlotCount, null, null, (id, self) => new ItemSlotVillageSupply(self));
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () =>
                {
                    GuiDialogVillageGranary dialog = new GuiDialogVillageGranary(
                        Lang.Get("claims:village-granary-title"), Inventory, Pos, Api as ICoreClientAPI);
                    dialog.UpdateSupply(SupplyHours, DecayHours);
                    return dialog;
                });
            }
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            SupplyHours = tree.GetInt("supplyHours");
            DecayHours = tree.GetInt("decayHours");
            // Refresh the open dialog: the numbers change once an hour, without the player acting.
            if (invDialog is GuiDialogVillageGranary dialog)
            {
                dialog.UpdateSupply(SupplyHours, DecayHours);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("supplyHours", SupplyHours);
            tree.SetInt("decayHours", DecayHours);
        }
    }
}
