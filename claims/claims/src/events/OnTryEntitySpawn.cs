using claims.src.auxialiry;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace claims.src.events
{
    public class OnTryEntitySpawn
    {
        public static bool Event_OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            var hostile = properties.Server?.SpawnConditions?.Runtime?.Group.Equals("hostile");
            if(hostile.HasValue && hostile.Value)
            {
                if(claims.dataStorage == null)
                {
                    if(claims.DebugValSet)
                    {
                        return true;
                    }

                    claims.sapi?.Logger.Debug("Event_OnTrySpawnEntity datastorage was null");

                    claims.DebugValSet = true;
                    return true;
                }
                if (spawnPosition == null)
                {
                    if (claims.DebugValSet)
                    {
                        return true;
                    }

                    claims.sapi?.Logger.Debug("Event_OnTrySpawnEntity spawnPosition was null");

                    claims.DebugValSet = true;
                    return true;
                }
                // World flags win over plot flags, same order as blast: forced-on, then forbidden,
                // then what the land itself says.
                WorldInfo worldInfo = claims.dataStorage.getWorldInfo();
                if (worldInfo != null)
                {
                    if (worldInfo.mobSpawnEverywhere)
                    {
                        return true;
                    }
                    if (worldInfo.mobSpawnForbidden)
                    {
                        return false;
                    }
                }
                if (claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)spawnPosition.X, (int)spawnPosition.Z), out Plot plot))
                {
                    // Safety inside the walls is paid for: without the flag the plot spawns mobs
                    // like unclaimed land does.
                    return !plot.getPermsHandler().noMobSpawnFlag;
                }
            }
            return true;
        }
    }
}
