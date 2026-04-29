using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src.auxialiry
{
    public static class PosFunctions
    {
        public static Vec3d TranslateCoordsToLocalVec3d(ICoreAPI api, BlockPos pos)
        {
            return new Vec3d(pos.X - api.World.DefaultSpawnPosition.X, pos.Y - api.World.DefaultSpawnPosition.Y, pos.Z - api.World.DefaultSpawnPosition.Z);
        }
    }
}
