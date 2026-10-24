using claims.src.auxialiry;
using claims.src.part.structure;
using Vintagestory.API.MathTools;

namespace claims.src.meteoricexpansion
{
    public class MeteoricExpansionPatches
    {
        public static bool IsProtected(int x, int z)
        {
            if (claims.dataStorage == null)
            {
                return false;
            }
            WorldInfo worldInfo = claims.dataStorage.getWorldInfo();
            if (worldInfo == null || worldInfo.blastEverywhere)
            {
                return false;
            }
            if (!claims.dataStorage.GetPlot(PlotPosition.fromXZ(x, z), out Plot plot) || plot == null)
            {
                return false;
            }
            if (worldInfo.blastForbidden)
            {
                return true;
            }
            bool plotAllows = plot.getPermsHandler()?.blastFlag == true;
            bool cityAllows = !plot.hasCity() || plot.getCity()?.getPermsHandler()?.blastFlag == true;
            return !plotAllows || !cityAllows;
        }

        public static void Postfix_IsPositionClaimed(BlockPos blockPos, ref bool __result)
        {
            if (!__result && blockPos != null)
            {
                __result = IsProtected(blockPos.X, blockPos.Z);
            }
        }

        public static bool Prefix_InjureEntities(Vec3d explosionPos)
        {
            return explosionPos == null || !IsProtected((int)explosionPos.X, (int)explosionPos.Z);
        }
    }
}
