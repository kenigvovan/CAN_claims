using System;
using claims.src.auxialiry;
using claims.src.harmony;
using claims.src.part.structure;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.rustyshellfork
{
    [HarmonyPatch]
    public class harmPatch
    {
        public static bool CommonLogic(IServerWorldAccessor __instance,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int injureRadius,
        int strength)
        {
            // Prefix on a foreign mod's method: on anything unexpected let the original blast run.
            if (pos == null || claims.dataStorage == null)
            {
                return true;
            }
            WorldInfo worldInfo = claims.dataStorage.getWorldInfo();
            if (worldInfo == null)
            {
                return true;
            }
            if (worldInfo.blastEverywhere)
            {
                return true;
            }
            if (worldInfo.blastForbidden)
            {
                return false;
            }
            // Every plot in reach, not nine samples exactly blastRadius away: those missed the plots
            // that only the outer part of the blast reached.
            foreach (Plot tb in harmonyPatches.PlotsInBlastReach((int)pos.X, (int)pos.Z, blastRadius))
            {
                // getCity() is null for a plot outside any city - the city flag simply does not apply then.
                bool plotAllows = tb.getPermsHandler()?.blastFlag == true;
                bool cityAllows = !tb.hasCity() || tb.getCity()?.getPermsHandler()?.blastFlag == true;
                if (!plotAllows || !cityAllows)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool Prefix_IServerWorldAccessor_CommonBlast(IServerWorldAccessor __instance,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int injureRadius,
        int strength)
        {
            return CommonLogic(__instance, byEntity, pos, blastRadius, injureRadius, strength);
        }
        public static bool Prefix_IServerWorldAccessor_GasBlast(IServerWorldAccessor __instance,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int millisecondDuration)
        {
            return CommonLogic(__instance, byEntity, pos, blastRadius, 0, 0);
        }
        public static bool Prefix_IServerWorldAccessor_IncendiaryBlast(IServerWorldAccessor __instance,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int injureRadius)
        {
            return CommonLogic(__instance, byEntity, pos, blastRadius, injureRadius, 0);
        }
    }
}
