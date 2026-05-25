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
                if (defendPlot.Type == PlotType.TOURNAMENT || defendPlot.getPermsHandler().pvpFlag
                    || AreEnemies(attackerPlayerInfo.City, defendPlayerInfo.City)
                    || (defendPlot.getCity().criminals.Contains(defendPlayerInfo) && defendPlot.getCity().isCitizen(attackerPlayerInfo)
                    || (defendPlot.getCity().criminals.Contains(attackerPlayerInfo) && defendPlot.getCity().isCitizen(defendPlayerInfo))))
                {
                    return true;
                }                         
                if (defendPlot.getCity().getPermsHandler().pvpFlag)
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
