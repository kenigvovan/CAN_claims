using System;
using claims.src.auxialiry;
using claims.src.bb;
using claims.src.beb;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.events
{
    public class OnBlockAction
    {
        public static bool Event_OnBlockUse(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            var block = claims.sapi.World.BlockAccessor.GetBlock(blockSel.Position);
            if (claims.config.blockTypesAccess.Contains(block.GetType()))
            {
                return true;
            }
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return true;
            }
            if (!canBlockUse(byPlayer, blockSel))
            {
                return false;
            }
            return true;
        }
        public static bool Event_OnBlockDestroy(IServerPlayer byPlayer, BlockSelection blockSel, out string claimant)
        {
            claimant = "";
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                claimant = "claims";
                return true;
            }
            if (!canBlockDestroy(byPlayer, blockSel, out claimant))
            {               
                return false;
            }
            return true;
        }
        public static bool checkInnerClaimPerm(PermType permType, string uid, Plot plot, BlockSelection blockSel)
        {
            if (plot.PlotDesc is not PlotDescTavern tavernDesc)
                return false;
            foreach(var it in tavernDesc.innerClaims)
            {
                if(it.Contains(blockSel.Position))
                {
                    return it.permissionsFlags[(int)permType];
                }
            }
            return false;
        }
        public static bool checkInnerClaimPerm(PermType permType, string uid, Plot plot, Vec3d blockSel)
        {
            if (plot.PlotDesc is not PlotDescTavern tavernDesc)
                return false;
            foreach (var it in tavernDesc.innerClaims)
            {
                if (it.Contains(blockSel))
                {
                    return it.permissionsFlags[(int)permType];
                }
            }
            return false;
        }
        /*private static bool CheckLandClaimPresence(Vec3d pos)
        {
            long key = claims.sapi.WorldManager.MapRegionIndex2D((int)pos.X / claims.sapi.WorldManager.RegionSize, (int)pos.Z / claims.sapi.WorldManager.RegionSize);
            if (!((ServerMain)claims.sapi.World).WorldMap.LandClaimByRegion.ContainsKey(key))
            {
                return false;
            }

            foreach (LandClaim item in ((ServerMain)claims.sapi.World).WorldMap.LandClaimByRegion[key])
            {
                if (item.PositionInside(pos))
                {
                    return true;
                }
            }
            return false;
        }*/
        public static bool IsDefenderBreakingEnemyFlag(IServerPlayer byPlayer, PlayerInfo playerInfo, Plot plot, BlockPos pos)
        {
            if (plot == null || !plot.hasCity() || !playerInfo.hasCity())
            {
                return false;
            }
            var block = claims.sapi.World.BlockAccessor.GetBlock(pos);
            if (block?.GetBehavior<BlockBehaviorFlag>() == null)
            {
                return false;
            }
            City defenderCity = plot.getCity();
            if (defenderCity.Equals(playerInfo.City))
            {
                return true;
            }
            if (defenderCity.HasAlliance() && playerInfo.HasAlliance())
            {
                Alliance defenderAlliance = defenderCity.Alliance;
                if (defenderAlliance.Equals(playerInfo.Alliance))
                {
                    return true;
                }
                if (defenderAlliance.ComradAlliancies.Contains(playerInfo.Alliance))
                {
                    return true;
                }
            }
            return false;
        }
        // True when the broken block is a war camp's anchor. `allowed` says whether this player may
        // break it (destroying the whole camp): only the owning city (dismantling) and enemies at
        // war with it (the objective). Allies/comrades and neutrals may not — no camp griefing.
        public static bool IsBreakingCampAnchor(PlayerInfo playerInfo, Plot plot, BlockPos pos, out bool allowed)
        {
            allowed = false;
            if (plot == null || plot.Type != PlotType.CAMP || !plot.hasCity() || playerInfo == null)
                return false;
            if (plot.PlotDesc is not PlotDescCamp campDesc || campDesc.AnchorPos == null)
                return false;
            if (pos.X != campDesc.AnchorPos.X || pos.Y != campDesc.AnchorPos.Y || pos.Z != campDesc.AnchorPos.Z)
                return false;

            City owner = plot.getCity();
            if (playerInfo.hasCity())
            {
                if (owner.Equals(playerInfo.City)) allowed = true;                       // owner dismantling
                else if (owner.HostileCities.Contains(playerInfo.City)) allowed = true;  // enemy war objective
            }
            return true;
        }
        public static bool canBlockDestroy(IServerPlayer byPlayer, BlockSelection blockSel, out string claimant)
        {
            claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            claimant = "";
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromBlockPos(blockSel.Position), out Plot plot);
            if (plot == null)
            {
                return true;
            }
            claimant = "claims";
            if (IsDefenderBreakingEnemyFlag(byPlayer, playerInfo, plot, blockSel.Position))
            {
                return true;
            }
            if (IsBreakingCampAnchor(playerInfo, plot, blockSel.Position, out bool campBreakAllowed))
            {
                if (!campBreakAllowed) return false;
                // The anchor is "reinforced": each break attempt is absorbed until the counter
                // runs out; only the last break destroys the camp (mirrors the capture flag).
                if (plot.PlotDesc is PlotDescCamp campDesc && campDesc.BreaksLeft > 1)
                {
                    // Notify the owner side on the first hit.
                    if (campDesc.BreaksLeft == claims.config.WAR_CAMP_ANCHOR_BREAKS && plot.hasCity())
                        MessageHandler.sendMsgInCity(plot.getCity(), Lang.Get("claims:camp_under_attack"));
                    campDesc.BreaksLeft--;
                    plot.saveToDatabase();
                    if (claims.sapi.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCampAnchor anchorBe)
                    {
                        anchorBe.BreaksLeft = campDesc.BreaksLeft;
                        anchorBe.MarkDirty(true);
                    }
                    MessageHandler.sendMsgToPlayer(byPlayer, Lang.Get("claims:camp_anchor_reinforced", campDesc.BreaksLeft));
                    return false;
                }
                PartDemolition.DemolishCamp(plot);
                return true;
            }
            if (VillageBlockRules.TryHandleAnchorHit(byPlayer, playerInfo, plot, blockSel.Position, out bool anchorBreaks))
            {
                return anchorBreaks;
            }
            if (IsPlacingCaptureFlagAllowed(byPlayer, playerInfo, plot, blockSel))
            {
                // Deliberately before the cache and without writing to it: the answer depends on
                // the held item, which the per-plot permission cache knows nothing about.
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromBlockPos(blockSel.Position);
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                //todo
                //MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + currentPosPlayer.getPos().ToString() + " equals " + playerInfo.PlayerCache.getLastLocation().getPos().ToString());
                if (playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].HasValue)
                {
                    if (playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].Value)
                    {
                        //todo
                        //MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + playerInfo.PlayerCache.getLastLocation().getPos().ToString() + " value is true");
                        return true;
                    }
                    else
                    {
                        if (plot.Type == PlotType.TAVERN)
                            return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                        else
                            return false;
                    }
                }
            }
            //todo
           // MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + currentPosPlayer.getPos().ToString() + " set as currentposplayer ");
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            return EvalPermission(playerInfo, plot, PermType.BUILD_AND_DESTROY_PERM, updateCache: true,
                tavernFallback: () => plot.Type == PlotType.TAVERN && checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel));
        }
        public static bool canBlockUse(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromBlockPos(blockSel.Position), out Plot plot);
            if(plot == null)
            {
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromXZ(blockSel.Position.X, blockSel.Position.Z);
            if(currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if(playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].HasValue)
                {
                    if(playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].Value)
                    {
                        return true;
                    }
                    else
                    {
                        if (plot.Type == PlotType.TAVERN)
                            return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                        else
                            return false;
                    }
                }
            }
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            return EvalPermission(playerInfo, plot, PermType.USE_PERM, updateCache: true,
                tavernFallback: () => plot.Type == PlotType.TAVERN && checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel));
        }
        public static bool canBlockUse(IServerPlayer byPlayer, Vec3d vec3)
        {
            claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromXZ((int)vec3.X, (int)vec3.Z), out Plot plot);
            if (plot == null)
            {
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromXZ((int)vec3.X, (int)vec3.Z);
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].HasValue)
                {
                    if (playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].Value)
                        return true;
                    if (plot.Type == PlotType.TAVERN)
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, vec3);
                    return false;
                }
            }
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            return EvalPermission(playerInfo, plot, PermType.USE_PERM, updateCache: true,
                tavernFallback: () => plot.Type == PlotType.TAVERN && checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, vec3));
        }
        public static bool canAttackAnimals(IServerPlayer byPlayer, Vec3d pos)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return true;
            }
            claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromXZ((int)pos.X, (int)pos.Z), out Plot plot);
            if (plot == null)
            {
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromXZ((int)pos.X, (int)pos.Z);
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].HasValue)
                {
                    if(playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].Value)
                    {
                        return true;
                    }
                    else
                    {
                        if (plot.Type == PlotType.TAVERN)
                            return checkInnerClaimPerm(PermType.ATTACK_ANIMALS_PERM, playerInfo.Guid, plot, pos);
                        else
                            return false;
                    }
                }
            }
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            return EvalPermission(playerInfo, plot, PermType.ATTACK_ANIMALS_PERM, updateCache: true,
                tavernFallback: () => plot.Type == PlotType.TAVERN && checkInnerClaimPerm(PermType.ATTACK_ANIMALS_PERM, playerInfo.Guid, plot, pos));
        }
        /// <summary>
        /// How this player stands to this plot. Order matters, and it reads top-down as "the closest
        /// tie wins, except that war overrides every tie short of owning the ground yourself".
        ///
        /// Owning comes before the plot group: a member of a group that holds their own plot used to
        /// be judged by the group's settings rather than as its owner, which could lock them out of
        /// their own land. War comes before the group, the owner's friends list and citizenship: those
        /// are peacetime ties, and letting them outrank FOE meant a besieger who happened to be a
        /// friend of the owner - or still listed in one of the defender's plot groups - fought under
        /// peacetime rules and ignored WAR_DESTRUCTION_SCOPE entirely.
        /// </summary>
        public static PlotRelation getPlotRelationForPlayerInfo(PlayerInfo playerInfo, PlotPosition pos, Plot plot)
        {
            if((plot.hasCity() && plot.getCity().isMayor(playerInfo)))
            {
                return PlotRelation.MANAGABLE_OWNER;
            }

            if (plot.hasPlotOwner() && plot.getPlotOwner().Equals(playerInfo))
            {
                return PlotRelation.PLOT_OWNER;
            }

            // Never against one's own city, whatever the hostile list says: war outranks citizenship
            // here, so a city that ended up listing itself would turn its own people into besiegers
            // of their own land.
            if (plot.hasCity() && playerInfo.hasCity() && !plot.getCity().Equals(playerInfo.City))
            {
                // Covers all war types: city vs city, city vs alliance, alliance vs city, alliance vs alliance
                if (plot.getCity().HostileCities.Contains(playerInfo.City))
                    return PlotRelation.FOE;
            }

            if (plot.hasPlotGroup() && plot.getPlotGroup().PlayersList.Contains(playerInfo))
            {
                return PlotRelation.GROUP_MEMBER;
            }

            if (plot.hasPlotOwner() && plot.getPlotOwner().Friends.Contains(playerInfo))
            {
                return PlotRelation.COMRADE;
            }
            if (plot.hasCity() && plot.getCity().getCityCitizens().Contains(playerInfo))
            {
                return PlotRelation.CITIZEN;
            }
            if (plot.hasCity() && plot.getCity().HasAlliance() && playerInfo.HasAlliance())
            {
                if (plot.getCity().Alliance.ComradAlliancies.Contains(playerInfo.Alliance))
                    return PlotRelation.ALLY;
            }
            
            return PlotRelation.STRANGER;
        }
        private static bool EvalPermission(PlayerInfo playerInfo, Plot plot, PermType permType, bool updateCache,
            Func<bool> tavernFallback = null)
        {
            bool b;
            if ((permType == PermType.BUILD_AND_DESTROY_PERM || permType == PermType.USE_PERM)
                && VillageBlockRules.IsUnderRaidFor(playerInfo, plot))
            {
                if (updateCache) playerInfo.PlayerCache.getCache()[(int)permType] = true;
                return true;
            }
            switch (getPlotRelationForPlayerInfo(playerInfo, plot.plotPosition, plot))
            {
                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    if (updateCache) playerInfo.PlayerCache.getCache()[(int)permType] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    // Every fighter of the owning side may build/dig freely on their war camp.
                    b = (plot.Type == PlotType.CAMP && permType == PermType.BUILD_AND_DESTROY_PERM)
                        || plot.getPermsHandler().getPerm(PermGroup.CITIZEN, permType);
                    break;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, permType);
                    break;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, permType);
                    break;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, permType);
                    break;
                case PlotRelation.FOE:
                    // Enemy war camps are always attackable; everything else follows WAR_DESTRUCTION_SCOPE.
                    b = plot.Type == PlotType.CAMP
                        ? IsActiveWarOnPlot(playerInfo, plot)
                        : IsPlotInWarDestructionScope(playerInfo, plot);
                    if (updateCache) playerInfo.PlayerCache.getCache()[(int)permType] = b;
                    return b;
                case PlotRelation.ALLY:
                    // Allied fighters may also build/dig on the war camp.
                    b = (plot.Type == PlotType.CAMP && permType == PermType.BUILD_AND_DESTROY_PERM)
                        || plot.getPermsHandler().getPerm(PermGroup.ALLY, permType);
                    break;
                default:
                    return false;
            }
            if (updateCache) playerInfo.PlayerCache.getCache()[(int)permType] = b;
            if (!b && tavernFallback != null) return tavernFallback();
            return b;
        }

        public static bool canBlockDestroyWithOutCacheUpdate(PlayerInfo playerInfo, Plot plot)
        {
            if (plot.plotPosition.Equals(playerInfo.PlayerCache.getLastLocation()))
                if (playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].HasValue)
                    return playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].Value;
            return EvalPermission(playerInfo, plot, PermType.BUILD_AND_DESTROY_PERM, updateCache: false);
        }
        public static bool canBlockUseWithOutCacheUpdate(PlayerInfo playerInfo, Plot plot)
        {
            if (plot.plotPosition.Equals(playerInfo.PlayerCache.getLastLocation()))
                if (playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].HasValue)
                    return playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].Value;
            return EvalPermission(playerInfo, plot, PermType.USE_PERM, updateCache: false);
        }
        public static bool canAttackAnimalsWithOutCacheUpdate(PlayerInfo playerInfo, Plot plot)
        {
            if (plot.plotPosition.Equals(playerInfo.PlayerCache.getLastLocation()))
                if (playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].HasValue)
                    return playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].Value;
            return EvalPermission(playerInfo, plot, PermType.ATTACK_ANIMALS_PERM, updateCache: false);
        }
        // Returns true if there is an active war window between the player's party and the plot's party.
        // Handles all combinations: alliance vs alliance, city vs city, city vs alliance.
        private static bool IsActiveWarOnPlot(PlayerInfo playerInfo, Plot plot)
        {
            return TryGetActiveWarOnPlot(playerInfo, plot, out _);
        }
        private static bool TryGetActiveWarOnPlot(PlayerInfo playerInfo, Plot plot, out Conflict conflict)
        {
            conflict = null;
            if (!playerInfo.hasCity() || !plot.hasCity()) return false;
            IConflictParty playerParty = playerInfo.HasAlliance() ? (IConflictParty)playerInfo.Alliance : playerInfo.City;
            IConflictParty plotParty   = plot.getCity().HasAlliance() ? (IConflictParty)plot.getCity().Alliance : plot.getCity();
            return ConflictHandler.TryGetConflictWithSides(playerParty, plotParty, out conflict) && conflict.ActiveWarTime;
        }

        /// <summary>
        /// Whether an enemy fighter may build/destroy on this plot, according to
        /// WAR_DESTRUCTION_SCOPE. Enemy war camps are always fair game (handled by the caller).
        /// </summary>
        private static bool IsPlotInWarDestructionScope(PlayerInfo playerInfo, Plot plot)
        {
            if (!TryGetActiveWarOnPlot(playerInfo, plot, out Conflict conflict)) return false;

            switch (claims.config.WAR_DESTRUCTION_SCOPE)
            {
                case Config.WAR_DESTRUCTION.ALL_PLOTS:
                    return true;

                case Config.WAR_DESTRUCTION.BORDER_PLOTS:
                    return plot.BorderPlot;

                case Config.WAR_DESTRUCTION.BORDER_WITH_OTHER_CITY:
                    return TouchesOtherCity(plot);

                case Config.WAR_DESTRUCTION.FLAG_PLOTS:
                    return HasCaptureFlag(conflict, plot.plotPosition, includeNeighbours: false);

                case Config.WAR_DESTRUCTION.FLAG_PLOTS_AND_NEIGHBOURS:
                    return HasCaptureFlag(conflict, plot.plotPosition, includeNeighbours: true);

                default:
                    return plot.BorderPlot;
            }
        }

        /// <summary>
        /// Placing the capture flag is the entry point of the FLAG_PLOTS destruction scopes: those
        /// scopes only open up where an attack already runs, so without this exception the first
        /// flag could never be placed and both flag modes were unusable. The empty-position check
        /// keeps the flag in hand from doubling as a licence to break blocks.
        /// </summary>
        private static bool IsPlacingCaptureFlagAllowed(IServerPlayer byPlayer, PlayerInfo playerInfo, Plot plot, BlockSelection blockSel)
        {
            if (claims.config.WAR_DESTRUCTION_SCOPE != Config.WAR_DESTRUCTION.FLAG_PLOTS
                && claims.config.WAR_DESTRUCTION_SCOPE != Config.WAR_DESTRUCTION.FLAG_PLOTS_AND_NEIGHBOURS)
            {
                return false;
            }
            Block held = byPlayer.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Block;
            if (held?.GetBehavior<BlockBehaviorFlag>() == null)
            {
                return false;
            }
            // Placement only: the target position must still be empty (or plant-replaceable).
            Block target = claims.sapi.World.BlockAccessor.GetBlock(blockSel.Position);
            if (target.Id != 0 && target.Replaceable < 6000)
            {
                return false;
            }
            return TryGetActiveWarOnPlot(playerInfo, plot, out _);
        }

        private static bool TouchesOtherCity(Plot plot)
        {
            City ownCity = plot.getCity();
            if (ownCity == null) return false;

            PlotPosition posTmp = new PlotPosition(0, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (Math.Abs(i) + Math.Abs(j) != 1) continue;
                    posTmp.X = plot.plotPosition.X + i;
                    posTmp.Z = plot.plotPosition.Z + j;
                    if (!claims.dataStorage.GetPlot(posTmp, out var nearPlot)) continue;

                    City nearCity = nearPlot.getCity();
                    if (nearCity != null && !nearCity.Equals(ownCity)) return true;
                }
            }
            return false;
        }

        private static bool HasCaptureFlag(Conflict conflict, PlotPosition plotPos, bool includeNeighbours)
        {
            if (!claims.dataStorage.WarsTimes.TryGetValue(conflict.Guid, out var warTime)) return false;
            if (warTime.PlotAttacks.ContainsKey(plotPos)) return true;
            if (!includeNeighbours) return false;

            PlotPosition posTmp = new PlotPosition(0, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (Math.Abs(i) + Math.Abs(j) != 1) continue;
                    posTmp.X = plotPos.X + i;
                    posTmp.Z = plotPos.Z + j;
                    if (warTime.PlotAttacks.ContainsKey(posTmp)) return true;
                }
            }
            return false;
        }

        public static void InitPlayerCache(IServerPlayer byPlayer)
        {
            if (byPlayer.Entity == null) return;
            canBlockDestroy(byPlayer, new BlockSelection(byPlayer.Entity.Pos.AsBlockPos, BlockFacing.NORTH, null), out var _);
            canBlockUse(byPlayer, new BlockSelection(byPlayer.Entity.Pos.AsBlockPos, BlockFacing.NORTH, null));
            if (!claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out var player) || player == null) return;
            player.PlayerCache.setPlotPosition(PlotPosition.fromEntityyPos(byPlayer.Entity.Pos));
        }
    }
}
