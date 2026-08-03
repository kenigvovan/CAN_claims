using System.Collections.Generic;
using Vintagestory.API.Common;

namespace claims.src.part.structure
{
    /// <summary>
    /// What a village can live on. Pure rules over item codes, with no world or server access, so
    /// the granary slots and its window use them client-side just as the hourly upkeep does
    /// server-side. The lists themselves are synced in ConfigUpdateValuesPacket.
    /// </summary>
    public static class VillageSupplies
    {
        /// <summary>Whether the granary takes this at all - food or fuel.</summary>
        public static bool IsSupply(ItemStack stack) => IsFood(stack) || IsFuel(stack);

        public static bool IsFood(ItemStack stack) => Matches(stack, claims.config.VILLAGE_FOOD_ITEMS);

        public static bool IsFuel(ItemStack stack) => Matches(stack, claims.config.VILLAGE_FUEL_ITEMS);

        private static bool Matches(ItemStack stack, HashSet<string> patterns)
        {
            AssetLocation code = stack?.Collectible?.Code;
            return code != null && MatchesAny(code, patterns);
        }

        /// <summary>Same wildcard style as PROTECTED_MOB_TYPES: "game:bread-*" or a full code.</summary>
        public static bool MatchesAny(AssetLocation code, HashSet<string> patterns)
        {
            if (patterns == null) return false;
            string full = code.ToString();
            foreach (string pattern in patterns)
            {
                if (pattern.Length == 0) continue;
                if (pattern.EndsWith("*"))
                {
                    if (full.StartsWith(pattern.Substring(0, pattern.Length - 1))) return true;
                }
                else if (full == pattern)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
