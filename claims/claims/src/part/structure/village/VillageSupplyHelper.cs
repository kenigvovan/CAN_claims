using System.Collections.Generic;
using claims.src.beb;
using claims.src.messages;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure
{
    /// <summary>
    /// Hourly upkeep of villages: one portion of food and one of fuel out of the granary keeps the
    /// settlement going for a while. An empty granary starts the decay, and a village that stays
    /// empty long enough falls apart - the equivalent of a city failing to pay its daily upkeep.
    /// </summary>
    public static class VillageSupplyHelper
    {
        /// <summary>Runs the upkeep for every village. Called once an hour from HourTimer.</summary>
        public static void ProcessVillages()
        {
            // Collect first, demolish after the loop: demolishing mutates the city list and the
            // plots of the village we are standing on.
            List<City> starved = new List<City>();

            foreach (City village in claims.dataStorage.getCitiesList())
            {
                if (!village.IsVillage() || village.isTechnicalCity()) continue;
                if (!village.TryGetVillageMain(out Plot mainPlot, out PlotDescVillage desc)) continue;

                ProcessVillage(village, mainPlot, desc, starved);
            }

            foreach (City village in starved)
            {
                MessageHandler.sendMsgInCity(village, Lang.Get("claims:village_starved", village.getPartNameReplaceUnder()));
                PartDemolition.demolishCity(village, "Village ran out of supplies");
            }
        }

        private static void ProcessVillage(City village, Plot mainPlot, PlotDescVillage desc, List<City> starved)
        {
            if (desc.SupplyHours > 0)
            {
                desc.SupplyHours--;
                desc.DecayHours = 0;
            }
            else if (TryConsumeSupplies(desc, out bool granaryReachable))
            {
                // Minus one: this very hour is already being paid for by the portion just taken,
                // otherwise every portion would quietly last an hour longer than configured.
                desc.SupplyHours = claims.config.VILLAGE_SUPPLY_HOURS_PER_ITEM - 1;
                desc.DecayHours = 0;
            }
            else if (!granaryReachable)
            {
                // The chunk holding the granary is not loaded, so there is no way to look inside.
                // The village waits instead of starving: it must not fall apart just because
                // nobody happened to be standing there.
                return;
            }
            else
            {
                desc.DecayHours++;
                int decayLimitHours = claims.config.VILLAGE_DECAY_HOURS;
                // Warn on the first empty hour and then once a day, not every single hour.
                if (desc.DecayHours == 1 || desc.DecayHours % 24 == 0)
                {
                    int hoursLeft = decayLimitHours - desc.DecayHours;
                    // A missing granary reads as "empty" otherwise, and the village would starve
                    // without anyone understanding why.
                    string langKey = desc.GranaryPos == null
                        ? "claims:village_granary_missing"
                        : "claims:village_granary_empty";
                    MessageHandler.sendMsgInCity(village, Lang.Get(langKey, hoursLeft / 24, hoursLeft % 24));
                }
                if (desc.DecayHours >= decayLimitHours)
                {
                    starved.Add(village);
                    return;
                }
            }

            SyncBlockEntities(desc);
            mainPlot.saveToDatabase();
        }

        /// <summary>
        /// Takes one portion of food and one of fuel out of the granary, all or nothing.
        /// <paramref name="granaryReachable"/> tells the caller whether we could look at all:
        /// an unloaded chunk is not the same thing as an empty granary.
        /// </summary>
        private static bool TryConsumeSupplies(PlotDescVillage desc, out bool granaryReachable)
        {
            granaryReachable = true;
            // No position stored means the granary was broken - that is a real shortage.
            if (desc.GranaryPos == null) return false;

            BlockPos granaryPos = new BlockPos(desc.GranaryPos.X, desc.GranaryPos.Y, desc.GranaryPos.Z);
            if (claims.sapi.World.BlockAccessor.GetChunkAtBlockPos(granaryPos) == null)
            {
                granaryReachable = false;
                return false;
            }
            if (claims.sapi.World.BlockAccessor.GetBlockEntity(granaryPos) is not BlockEntityVillageGranary granary)
            {
                // Chunk is loaded and there is nothing there: the granary is gone for good
                // (blown up, replaced by another mod) and the village does starve.
                return false;
            }

            ItemSlot foodSlot = FindSlot(granary, VillageSupplies.IsFood);
            ItemSlot fuelSlot = FindSlot(granary, VillageSupplies.IsFuel);
            if (foodSlot == null || fuelSlot == null) return false;

            foodSlot.TakeOut(1);
            foodSlot.MarkDirty();
            fuelSlot.TakeOut(1);
            fuelSlot.MarkDirty();
            return true;
        }

        // What counts as food or fuel lives in VillageSupplies - the slots and the granary window
        // ask the same question client-side, where this class has no business running.
        private static ItemSlot FindSlot(BlockEntityVillageGranary granary, System.Func<ItemStack, bool> accepts)
        {
            foreach (ItemSlot slot in granary.Inventory)
            {
                if (slot.Empty) continue;
                if (accepts(slot.Itemstack)) return slot;
            }
            return null;
        }

        /// <summary>Pushes the numbers to the blocks so the HUD and the granary dialog show them.</summary>
        public static void SyncBlockEntities(PlotDescVillage desc)
        {
            if (desc.AnchorPos != null
                && claims.sapi.World.BlockAccessor.GetBlockEntity(
                    new BlockPos(desc.AnchorPos.X, desc.AnchorPos.Y, desc.AnchorPos.Z)) is BlockEntityVillageAnchor anchor)
            {
                anchor.SupplyHours = desc.SupplyHours;
                anchor.BreaksLeft = desc.BreaksLeft;
                anchor.RaidRunning = desc.RaidActive;
                anchor.MarkDirty(true);
            }
            if (desc.GranaryPos != null
                && claims.sapi.World.BlockAccessor.GetBlockEntity(
                    new BlockPos(desc.GranaryPos.X, desc.GranaryPos.Y, desc.GranaryPos.Z)) is BlockEntityVillageGranary granary)
            {
                granary.SupplyHours = desc.SupplyHours;
                granary.DecayHours = desc.DecayHours;
                granary.MarkDirty(true);
            }
        }

    }
}
