using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace claims.src.events
{
    public class EntityDamageHandler
    {
        public static bool canAttackEntity(IServerPlayer attacker, Entity defend)
        {
            if (OnBlockAction.canAttackAnimals(attacker, defend.Pos.XYZ))
            {
                return true;
            }
            return false;
        }
    }
}
