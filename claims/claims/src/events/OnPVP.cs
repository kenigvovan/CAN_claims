using System;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
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
                // Just-respawned defenders are immune to war PvP inside their own respawn safe zone
                // (this only suppresses enemy-war PvP, not tournament / criminal / pvp-flag PvP).
                bool inOwnSafeZone = IsInOwnRespawnSafeZone(defendPlayerInfo, defend.Entity.Pos);
                if (defendPlot.Type == PlotType.TOURNAMENT || defendPlot.getPermsHandler().pvpFlag
                    || (!inOwnSafeZone && AreEnemies(attackerPlayerInfo?.City, defendPlayerInfo?.City))
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
        // True while the defender is within WAR_RESPAWN_SAFEZONE_RADIUS of one of their city's
        // respawn points (temple points or active camp anchors) AND respawned within the last
        // WAR_RESPAWN_SAFEZONE_SECONDS. Anti-spawn-kill without permanent camping.
        public static bool IsInOwnRespawnSafeZone(PlayerInfo defender, EntityPos pos)
        {
            if (!claims.config.WAR_RESPAWN_SAFEZONE_ENABLED) return false;
            if (defender == null || !defender.hasCity() || pos == null) return false;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - defender.LastRespawnTimestamp > claims.config.WAR_RESPAWN_SAFEZONE_SECONDS) return false;

            City city = defender.City;
            double radius = claims.config.WAR_RESPAWN_SAFEZONE_RADIUS;
            double radiusSq = radius * radius;

            foreach (var rp in city.TempleRespawnPoints.Values)
                if (rp != null && WithinSq(pos, rp, radiusSq)) return true;
            foreach (var camp in city.campPlots)
                if (camp.PlotDesc is PlotDescCamp pd && pd.AnchorPos != null && WithinSq(pos, pd.AnchorPos, radiusSq)) return true;
            return false;
        }

        private static bool WithinSq(EntityPos pos, Vec3i p, double radiusSq)
        {
            double dx = pos.X - p.X, dy = pos.Y - p.Y, dz = pos.Z - p.Z;
            return dx * dx + dy * dy + dz * dz <= radiusSq;
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
