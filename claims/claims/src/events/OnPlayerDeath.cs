using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace claims.src.events
{
    public class OnPlayerDeath
    {
        public static void Event_OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null) return;

            // The killer is resolved the same way war score resolves it, projectiles included: the
            // old check accepted only a direct EntityPlayer source, so an arrow kill never reached
            // tryToPrison at all - and its own projectile branch was dead code because of it.
            if (!playerInfo.isPrisoned() && damageSource != null
                && TryResolveKiller(damageSource, out PlayerInfo attackerPlayerInfo))
            {
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
                    else
                    {
                        playerInfo.PrisonedIn = null;
                        playerInfo.PrisonHoursLeft = 0;
                    }
                }
            }

            // War score for kills between warring parties during an active battle window.
            GrantKillScore(damageSource, playerInfo);
            // Casus belli: an enemy killing our citizen on our land is grounds for war.
            RecordGrievanceIfHostileRaid(byPlayer, damageSource, playerInfo);
            // Plunder a share of the victim's wallet on a war kill; pay out any bounty on them.
            GrantPlunderOnKill(damageSource, playerInfo);
            GrantBountyPayout(damageSource, playerInfo);
        }

        private static void GrantBountyPayout(DamageSource damageSource, PlayerInfo victim)
        {
            if (!claims.config.WAR_BOUNTY_ENABLED || victim == null) return;
            long total = victim.GetBountyTotal();
            if (total <= 0) return;
            if (!TryResolveKiller(damageSource, out PlayerInfo killer) || killer == null || killer.Equals(victim)) return;
            if (!claims.economyProvider.SupportsPlayerWallet) return;
            if (claims.economyProvider.Deposit(killer.MoneyAccountName, (decimal)total) != MoneyOperationResult.Success) return;
            victim.BountyPosters.Clear();
            victim.saveToDatabase();
            BountyHelper.BroadcastBoard();
            MessageHandler.sendGlobalMsg(Lang.Get("claims:bounty_paid", killer.GetPartName(), victim.GetPartName(), total));
        }

        private static void GrantPlunderOnKill(DamageSource damageSource, PlayerInfo victim)
        {
            if (!claims.config.WAR_PLUNDER_ON_KILL_ENABLED || claims.config.WAR_PLUNDER_ON_KILL_PERCENT <= 0) return;
            if (!claims.economyProvider.SupportsPlayerWallet) return;
            if (victim == null || !victim.hasCity()) return;
            if (!TryResolveKiller(damageSource, out PlayerInfo killer) || killer == null || !killer.hasCity() || killer.Equals(victim)) return;

            IConflictParty killerParty = killer.HasAlliance() ? (IConflictParty)killer.Alliance : killer.City;
            IConflictParty victimParty = victim.HasAlliance() ? (IConflictParty)victim.Alliance : victim.City;
            if (killerParty.Equals(victimParty)) return;
            if (!ConflictHandler.TryGetConflictWithSides(killerParty, victimParty, out Conflict conflict) || !conflict.ActiveWarTime) return;

            decimal bal = claims.economyProvider.GetBalance(victim.MoneyAccountName);
            if (bal <= 0) return;
            long amount = (long)Math.Floor(bal * (decimal)claims.config.WAR_PLUNDER_ON_KILL_PERCENT / 100m);
            amount = Math.Min(amount, (long)bal);
            if (amount <= 0) return;
            if (claims.economyProvider.Transfer(victim.MoneyAccountName, killer.MoneyAccountName, (decimal)amount) != MoneyOperationResult.Success) return;

            if (claims.sapi.World.PlayerByUid(killer.Guid) is IServerPlayer kp)
                MessageHandler.sendMsgToPlayer(kp, Lang.Get("claims:plunder_looted", amount, victim.GetPartName()));
            if (claims.sapi.World.PlayerByUid(victim.Guid) is IServerPlayer vp)
                MessageHandler.sendMsgToPlayer(vp, Lang.Get("claims:plunder_lost", amount, killer.GetPartName()));
        }

        private static void RecordGrievanceIfHostileRaid(IServerPlayer victimPlayer, DamageSource damageSource, PlayerInfo victim)
        {
            // Recorded regardless of WAR_REQUIRE_CASUS_BELLI so history exists if the admin enables it later.
            if (victim == null || !victim.hasCity()) return;
            if (!TryResolveKiller(damageSource, out PlayerInfo killer) || killer == null || !killer.hasCity()) return;
            if (killer.City.Equals(victim.City)) return;
            if (!claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(victimPlayer.Entity.Pos), out Plot plot)) return;
            if (!plot.hasCity() || !plot.getCity().Equals(victim.City)) return;
            string offenderPartyGuid = killer.HasAlliance() ? killer.Alliance.Guid : killer.City.Guid;
            WarDeclarationHelper.RecordGrievance(victim.City, offenderPartyGuid);
        }

        private static void GrantKillScore(DamageSource damageSource, PlayerInfo victim)
        {
            if (!claims.config.WAR_SCORE_ENABLED || claims.config.WAR_SCORE_PER_KILL <= 0) return;
            if (victim == null || !victim.hasCity()) return;
            if (!TryResolveKiller(damageSource, out PlayerInfo killer)) return;
            if (killer == null || !killer.hasCity() || killer.Equals(victim)) return;

            IConflictParty killerParty = killer.HasAlliance() ? (IConflictParty)killer.Alliance : killer.City;
            IConflictParty victimParty = victim.HasAlliance() ? (IConflictParty)victim.Alliance : victim.City;
            if (killerParty.Equals(victimParty)) return;

            if (!ConflictHandler.TryGetConflictWithSides(killerParty, victimParty, out Conflict conflict)) return;
            if (!conflict.ActiveWarTime) return;

            // Record the kill stat BEFORE AddScore, since AddScore may end the war (and build the report).
            WarScoreHelper.RecordKill(conflict, killerParty);
            WarScoreHelper.AddScore(conflict, killerParty, claims.config.WAR_SCORE_PER_KILL, "kill");
        }

        private static bool TryResolveKiller(DamageSource damageSource, out PlayerInfo killer)
        {
            killer = null;
            Entity src = damageSource?.SourceEntity;
            IServerPlayer killerPlayer = null;
            if (src is EntityPlayer ep)
                killerPlayer = ep.Player as IServerPlayer;
            else if (src is EntityProjectile proj && proj.FiredBy is EntityPlayer firedBy)
                killerPlayer = firedBy.Player as IServerPlayer;
            if (killerPlayer == null) return false;
            return claims.dataStorage.GetPlayerByUid(killerPlayer.PlayerUID, out killer) && killer != null;
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
                if((attacker as EntityProjectile).FiredBy is EntityPlayer firedBy)
                {
                    // .Player, not the entity itself: EntityPlayer is not sealed, so casting it to
                    // IServerPlayer compiles and then quietly yields null - killing anyone with a
                    // bow or spear never landed them in prison.
                    attackPlayer = firedBy.Player as IServerPlayer;
                }
            }
            if (attackPlayer == null)
                return;
            claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(killed.Entity.Pos), out Plot plotKilled); 
            if(plotKilled == null)
            {
                return;
            }
            if(playerInfoAttacker != null && plotKilled.hasCity() && plotKilled.getCity().isCitizen(playerInfoAttacker))
            {
                if (playerInfoAttacker.hasCity() && playerInfoAttacker.City.hasPrison())
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
