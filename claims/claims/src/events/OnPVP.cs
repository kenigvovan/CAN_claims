using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Server;

namespace claims.src.events
{
    public static class OnPVP
    {
        public static bool canPVPAttackHere(IServerPlayer attacker, IServerPlayer defend)
        {
            // Урон самому себе (например, ожог при снятии горячей вещи с наковальни) — это не PVP, он всегда проходит.
            if (attacker == null || defend == null || attacker.PlayerUID == defend.PlayerUID)
            {
                return true;
            }

            claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(defend.Entity.Pos), out Plot defendPlot);

            claims.dataStorage.GetPlayerByUid(attacker.PlayerUID, out PlayerInfo attackerPlayerInfo);
            claims.dataStorage.GetPlayerByUid(defend.PlayerUID, out PlayerInfo defendPlayerInfo);

            if (claims.dataStorage.getWorldInfo().pvpEverywhere)
            {
                return true;
            }
            if (claims.dataStorage.getWorldInfo().pvpForbidden)
            {
                return false;
            }
            if (/*attackerPlot != null && */defendPlot != null && defendPlot.hasCity())
            {
                City plotCity = defendPlot.getCity();
                if (defendPlot.Type == PlotType.TOURNAMENT || defendPlot.getPermsHandler().pvpFlag
                    || AreEnemies(attackerPlayerInfo?.City, defendPlayerInfo?.City)
                    || (attackerPlayerInfo != null && defendPlayerInfo != null
                        && ((plotCity.criminals.Contains(defendPlayerInfo) && plotCity.isCitizen(attackerPlayerInfo))
                         || (plotCity.criminals.Contains(attackerPlayerInfo) && plotCity.isCitizen(defendPlayerInfo)))))
                {
                    return true;
                }
                if (plotCity.getPermsHandler().pvpFlag)
                {
                    return true;
                }
                if (defendPlot.getPermsHandler().pvpFlag)
                {
                    return true;
                }
                return false;
            }
            return true;
        }
        public static bool AreEnemies(City city1, City city2)
        {
            if (city1 == null || city2 == null)
            {
                return false;
            }
            if (city1.HostileCities.Contains(city2))
            {
                return true;
            }
            return false;
        }
    }
}
