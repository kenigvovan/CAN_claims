using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace claims.src.gui
{
    /// <summary>
    /// The granary window: the usual slot grid plus the one number that matters - how long the
    /// village still lives off what is stored here. Built like the vanilla firepit dialog, which
    /// pairs slots with a remaining-burn-time readout.
    /// </summary>
    public class GuiDialogVillageGranary : GuiDialogBlockEntity
    {
        private const int SlotsPerRow = 3;
        private const int Rows = 2;

        public GuiDialogVillageGranary(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate) return;
            capi.World.Player.InventoryManager.OpenInventory(inventory);
            SetupDialog();
        }

        private void SetupDialog()
        {
            ElementBounds supplyBounds = ElementBounds.Fixed(0, 30, 250, 45);
            ElementBounds slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 85, SlotsPerRow, Rows);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(supplyBounds, slotBounds);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            SingleComposer = capi.Gui
                .CreateCompo("villagegranary" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddDynamicText("", CairoFont.WhiteSmallText(), supplyBounds, "supply")
                    .AddHoverText(AcceptedItemsText(), CairoFont.WhiteDetailText(), 320, slotBounds.FlatCopy())
                    .AddItemSlotGrid(Inventory, DoSendPacket, SlotsPerRow, slotBounds, "slots")
                .EndChildElements()
                .Compose();
        }

        /// <summary>
        /// What the granary takes, as a hover text over the slots: the config lists codes like
        /// "game:bread-*", so the domain and the wildcard are stripped to leave readable words.
        /// </summary>
        private static string AcceptedItemsText()
        {
            string food = Readable(claims.config.VILLAGE_FOOD_ITEMS);
            string fuel = Readable(claims.config.VILLAGE_FUEL_ITEMS);
            return Lang.Get("claims:village-granary-accepts", food, fuel);
        }

        private static string Readable(System.Collections.Generic.HashSet<string> patterns)
        {
            if (patterns == null || patterns.Count == 0) return "-";

            var names = new System.Collections.Generic.List<string>();
            foreach (string pattern in patterns)
            {
                string name = pattern;
                int colon = name.IndexOf(':');
                if (colon >= 0) name = name.Substring(colon + 1);
                name = name.TrimEnd('*').TrimEnd('-');
                if (name.Length > 0) names.Add(name);
            }
            return string.Join(", ", names);
        }

        /// <summary>
        /// Called by the block entity whenever the server sends new numbers, not only on open -
        /// the supplies tick down once an hour while the window may be sitting there open.
        /// </summary>
        // Last numbers the server sent, kept so the readout can be redrawn when the slots change.
        private int lastSupplyHours;
        private int lastDecayHours;

        public void UpdateSupply(int supplyHours, int decayHours)
        {
            lastSupplyHours = supplyHours;
            lastDecayHours = decayHours;
            if (SingleComposer == null) return;

            // Counting what is in the slots too, so putting bread in changes the number at once
            // instead of at the top of the next hour.
            int total = supplyHours + StoredHours();
            string text = total > 0
                ? Lang.Get("claims:village_supplies_left", total / 24, total % 24)
                : Lang.Get("claims:village_starving", decayHours);

            SingleComposer.GetDynamicText("supply")?.SetNewText(text);
        }

        /// <summary>
        /// How long the stock inside would last. A day costs one food and one fuel, so the shorter
        /// of the two piles decides - a granary full of bread and no firewood feeds nobody.
        /// </summary>
        private int StoredHours()
        {
            int food = 0;
            int fuel = 0;
            foreach (ItemSlot slot in Inventory)
            {
                if (slot.Empty) continue;
                if (part.structure.VillageSupplies.IsFood(slot.Itemstack)) food += slot.StackSize;
                else if (part.structure.VillageSupplies.IsFuel(slot.Itemstack)) fuel += slot.StackSize;
            }
            return System.Math.Min(food, fuel) * claims.config.VILLAGE_SUPPLY_HOURS_PER_ITEM;
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            Inventory.SlotModified += OnSlotModified;
        }

        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnSlotModified;
            SingleComposer.GetSlotGrid("slots")?.OnGuiClosed(capi);
            base.OnGuiClosed();
        }

        // Only the one line is redrawn - the stock just changed, so the time it buys changed too.
        private void OnSlotModified(int slotId)
        {
            UpdateSupply(lastSupplyHours, lastDecayHours);
        }
    }
}
