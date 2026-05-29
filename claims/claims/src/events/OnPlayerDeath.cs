using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace claims.src.events
{
    public class OnPlayerDeath
    {
        public static void Event_OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);

            if (!playerInfo.isPrisoned() && damageSource != null && damageSource.SourceEntity is EntityPlayer attackerEntity)
            {
                claims.dataStorage.GetPlayerByUid(attackerEntity.PlayerUID, out PlayerInfo attackerPlayerInfo);
                tryToPrison(damageSource.SourceEntity, byPlayer, playerInfo, attackerPlayerInfo);
                if (playerInfo.isPrisoned())
                {
                    if (playerInfo.PrisonedIn.TryGetRandomCell(out PrisonCellInfo cell))
                    {
                        cell.AddPlayer(playerInfo);
                        byPlayer.SetSpawnPosition(new PlayerSpawnPos(cell.spawnPostion.X, cell.spawnPostion.Y, cell.spawnPostion.Z));
                        UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.PrisonedIn.City.Guid, new Dictionary<string, object> { { "value", new PrisonCellElement(cell.spawnPostion, cell.playerNames) } },
                            EnumPlayerRelatedInfo.CITY_CELL_PRISON_UPDATE);
                    }
                }
            }
        }
        public static void tryToPrison(Entity attacker, IServerPlayer killed, PlayerInfo playerInfoKilled, PlayerInfo playerInfoAttacker)
        {
            IServerPlayer attackPlayer = null;
            if(attacker is EntityPlayer)
            {
                attackPlayer = (attacker as EntityPlayer).Player as IServerPlayer;
            }
            else if(attacker is EntityProjectile)
            {
                if((attacker as EntityProjectile).FiredBy is EntityPlayer)
                {
                    attackPlayer = ((attacker as EntityProjectile).FiredBy as EntityPlayer) as IServerPlayer;
                }
            }
            if (attackPlayer == null)
                return;
            claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(killed.Entity.Pos), out Plot plotKilled); 
            if(plotKilled == null)
            {
                return;
            }
            if(plotKilled.hasCity() && plotKilled.getCity().isCitizen(playerInfoAttacker))
            {
                if (playerInfoAttacker.City.hasPrison())
                {
                    if(playerInfoAttacker.City.TryGetRandomPrisonWithCell(out Prison prison))
                    {
                        playerInfoKilled.PrisonedIn = prison;
                    }
                }
            }
        }
    }
}
