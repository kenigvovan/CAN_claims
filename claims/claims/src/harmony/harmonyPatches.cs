using Cairo;
using caneconomy.src.harmony;
using claims.src.auxialiry;
using claims.src.claimsext.map;
using claims.src.events;
using claims.src.part;
using claims.src.part.structure;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods.NoObf;

namespace claims.src.harmony
{
    [HarmonyPatch]
    public class harmonyPatches
    {
        public static void Prefix_testBlockAccessInternal(Vintagestory.Common.WorldMap __instance, IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant)
        {
            claimant = "";
            ServerMain serverMain = __instance.World as ServerMain;
        }

        public static bool Prefix_nearToClaimedLand(Vintagestory.GameContent.BlockEntityBomb __instance, ref bool __result)
        {

            int tmpX = __instance.Pos.X;
            int tmpZ = __instance.Pos.Z;
            bool blastEV = claims.dataStorage.getWorldInfo().blastEverywhere;
            if (blastEV)
            {
                __result = true;
                return false;
            }
            if (claims.dataStorage.getWorldInfo().blastForbidden)
            {
                __result = false;
                return false;
            }

            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {

                    claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)(tmpX + (i * __instance.BlastRadius)),
                                                                          (int)(tmpZ + (j * __instance.BlastRadius))), out Plot tb);
                    if (tb == null)
                    {
                        continue;
                    }

                    if ((tb.getPermsHandler().blastFlag || tb.getCity().getPermsHandler().blastFlag))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }

        public static bool Prefix_On_ReceiveDamage(Vintagestory.API.Common.Entities.Entity __instance, DamageSource damageSource, float damage
            , ref bool __result)
        {
            if(__instance.Api.Side == EnumAppSide.Client)
            {
                return true;
            }
            //No source entity, TODO - probably need to add check on fire damage or smth.           
            if (damageSource.SourceEntity == null)
            {
                return true;
            }

            if (__instance is EntityPlayer) { 
                
                if(damageSource.SourceEntity is EntityPlayer)
                {                
                    if(claims.config.PVP_DURING_PART_OF_THE_DAY && Settings.isPvpTime())
                    {
                        __result = true;
                        return false;
                    }
                    __result = OnPVP.canPVPAttackHere((damageSource.SourceEntity as EntityPlayer).Player as IServerPlayer, (__instance as EntityPlayer).Player as IServerPlayer);
                    return false;
                }
                else if((damageSource.SourceEntity is EntityProjectile) && (damageSource.SourceEntity as EntityProjectile).FiredBy is EntityPlayer)
                {
                    if (claims.config.PVP_DURING_PART_OF_THE_DAY && Settings.isPvpTime())
                    {
                        __result = true;
                        return false;
                    }
                    __result = OnPVP.canPVPAttackHere(((damageSource.SourceEntity as EntityProjectile).FiredBy as EntityPlayer).Player as IServerPlayer, (__instance as EntityPlayer).Player as IServerPlayer);
                    return false;
                }                              
            }
            if(damageSource.SourceEntity is EntityPlayer)
            {
                __result = EntityDamageHandler.canAttackEntity((damageSource.SourceEntity as EntityPlayer).Player as IServerPlayer, __instance) ||
                   !Settings.IsProtectedMob(__instance.Code);
                return false;
            }

            //The origin method will run
            return true;
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(Vintagestory.GameContent.BEBehaviorBurning), "TrySpreadTo")]*/
        public static bool Prefix_On_TrySpreadFireAllDirs(BlockPos pos, Vintagestory.GameContent.BEBehaviorBurning __instance)
        {
            WorldInfo worldInfo = claims.dataStorage.getWorldInfo();
            claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos), out Plot tmp);

            if (tmp == null)
            {
                return true;               
            }
            if (worldInfo.fireEverywhere)
            {
                return true;
            }
            //claims.dataStorage.GetPlot(new ChunkLocation(pos), out )
            if (!worldInfo.fireForbidden && tmp.getPermsHandler().fireFlag)
            {
                return true;
            }
            BlockEntity be = __instance.Api.World.BlockAccessor.GetBlockEntity(__instance.FirePos.DownCopy(1));
            if (be is not BlockEntityPitKiln && (__instance.Blockentity != null && __instance.Blockentity is not BlockEntityGroundStorage))
            {
                __instance.KillFire(false);
                return false;
            }

            return false;
        }

       public static bool Prefix_HasPermissionToUse(Vintagestory.GameContent.BlockEntityBomb __instance, ref bool __result)
        {

            int tmpX = __instance.Pos.X;
            int tmpZ = __instance.Pos.Z;
            if (claims.dataStorage.getWorldInfo().blastEverywhere)
            {
                __result = true;
                return false;
            }
            if(claims.dataStorage.getWorldInfo().blastForbidden)
            {
                __result = false;
                return false;
            }


            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {

                     claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)(tmpX + (i * __instance.BlastRadius)),
                                                                           (int)(tmpZ + (j * __instance.BlastRadius))), out Plot tb);
                    if (tb == null)
                    { 
                        continue;
                    }

                    if ((tb.getPermsHandler().blastFlag || tb.getCity().getPermsHandler().blastFlag))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
        public static bool Prefix_HandleCommand(Vintagestory.Server.ServerMain __instance, string commandName, IServerPlayer player, string args, Action<TextCommandResult> onCommandComplete)
        {
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(player.Entity.ServerPos), out Plot tmpPlot);
            if (playerInfo.isPrisoned() && playerInfo.PrisonedIn.Plot.Equals(tmpPlot))
            {
                if (Settings.blockedCommandsForPrison.Contains(commandName) || (args.Length > 0 && Settings.blockedCommandsForPrison.Contains(args.Split(' ')[0])))
                {
                    return false;
                }
            }
            if (commandName == "land")
            {
                if (!claims.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Contains(player.Role.Code))
                {
                    player.SendMessage(0, "This command is blocked by a mod.", EnumChatType.Notification);
                    return false;
                }
                else
                {
                    return true;
                }
                
            }
            else { return true; }
        }

        public static bool Prefix_TrySpreadHorizontal(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid __instance, Block ourblock, Block ourSolid, IWorldAccessor world, BlockPos pos)
        {
            claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos), out Plot source);
            foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
            {
                claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos.AddCopy(facing)), out Plot dest);
                if (dest == null || (dest == null && source == null) || (source != null && dest.hasCity() && source.hasCity() && dest.getCity().Equals(source.getCity())))
                {
                    MethodInfo dynMethod = __instance.GetType().GetMethod("TrySpreadIntoBlock",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                    dynMethod.Invoke(__instance, new object[] { ourblock, ourSolid, pos, pos.AddCopy(facing), facing, world });
                }
            }
            return false;
        }


        public static void Postfix_FindDownwardPaths(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid __instance, IWorldAccessor world, BlockPos pos, Block ourBlock,
            List<PosAndDist> __result)
        {
            claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos), out Plot source);
            foreach (var it in new List<PosAndDist>(__result))
            {
                claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(it.pos), out Plot dest);
                if (dest == null)
                {
                    continue;
                }
                else if (source != null)
                {
                    if ((source.hasCity() && source.getCity().Equals(dest.getCity())))
                    {
                        continue;
                    }
                }
                else
                {
                    __result.Remove(it);
                }
            }
        }

        public static bool Prefix_OnFallToGround(Vintagestory.GameContent.EntityBlockFalling __instance, double motionY, ref bool ___nowImpacted, ref int ___lingerTicks, ref bool ___fallHandled, ref bool ___canFallSideways, ref Vec3f ___fallMotion, ref float ___impactDamageMul)
        {

            if (___fallHandled) return false;

            BlockPos pos = __instance.Pos.AsBlockPos;
            BlockPos finalPos = __instance.Pos.AsBlockPos;
            Block block = null;
            
            if (__instance.Api.Side == EnumAppSide.Server)
            {
                block = __instance.World.BlockAccessor.GetBlock(finalPos);

                if (block.OnFallOnto(__instance.World, finalPos, __instance.Block, __instance.blockEntityAttributes))
                {
                    ___lingerTicks = 3;
                    ___fallHandled = true;
                    return false;
                }
            }

            if (___canFallSideways)
            {
                claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(__instance.ServerPos), out Plot source);
                for (int i = 0; i < 4; i++)
                {
                    BlockFacing facing = BlockFacing.HORIZONTALS[i];
                    if (
                        __instance.World.BlockAccessor.GetBlock(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z).Replaceable >= 6000 &&
                        __instance.World.BlockAccessor.GetBlock(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y - 1, pos.Z + facing.Normali.Z).Replaceable >= 6000)
                    {

                        //Only def from wild chunk to city's chunk. Two cities back to back is problem of config.
                        claims.dataStorage.GetPlot(PlotPosition.fromXZ(pos.X + facing.Normali.X, pos.Z + facing.Normali.Z), out Plot dest);
                        if (source == null && dest != null)
                        {
                            continue;
                        }
                        claims.dataStorage.GetPlot(PlotPosition.fromXZ(pos.X + facing.Normali.X, pos.Z + facing.Normali.Z), out dest);
                        if (source == null && dest != null)
                        {
                            continue;
                        }

                        if (__instance.Api.Side == EnumAppSide.Server)
                        {
                            __instance.SidedPos.X += facing.Normali.X;
                            __instance.SidedPos.Y += facing.Normali.Y;
                            __instance.SidedPos.Z += facing.Normali.Z;
                        }
                        ___fallMotion.X = facing.Normalf.X;
                        ___fallMotion.X = 0;
                        ___fallMotion.X = facing.Normalf.Z;
                        return false;
                    }
                }
            }

            ___nowImpacted = true;

            Block blockAtFinalPos = __instance.World.BlockAccessor.GetBlock(finalPos);

            if (__instance.Api.Side == EnumAppSide.Server)
            {
                if (!block.IsReplacableBy(__instance.Block))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        BlockFacing facing = BlockFacing.HORIZONTALS[i];
                        block = __instance.World.BlockAccessor.GetBlock(finalPos.X + facing.Normali.X, finalPos.Y + facing.Normali.Y, finalPos.Z + facing.Normali.Z);

                        if (block.Replaceable >= 6000)
                        {
                            finalPos.X += facing.Normali.X;
                            finalPos.Y += facing.Normali.Y;
                            finalPos.Z += facing.Normali.Z;
                            break;
                        }
                    }
                }

                if (block.IsReplacableBy(__instance.Block))
                {
                    if (!block.IsLiquid() || __instance.Block.BlockMaterial != EnumBlockMaterial.Snow)
                    {
                        MethodInfo dynMethod = __instance.GetType().GetMethod("UpdateBlock",
                         BindingFlags.NonPublic | BindingFlags.Instance);
                        dynMethod.Invoke(__instance, new object[] { false, finalPos });
                    }

                    (__instance.Api as ICoreServerAPI).Network.BroadcastEntityPacket(__instance.EntityId, 1234);
                }
                else
                {
                    // Space is occupied by maybe a torch or some other block we shouldn't replace
                    MethodInfo dynMethod = __instance.GetType().GetMethod("DropItems",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                    dynMethod.Invoke(__instance, new object[] { finalPos });
                }

                if (___impactDamageMul > 0)
                {
                    Entity[] entities = __instance.World.GetEntitiesInsideCuboid(finalPos, finalPos.AddCopy(1, 1, 1), (e) => !(e is EntityBlockFalling));
                    bool didhit = false;
                    foreach (var entity in entities)
                    {
                        bool nowhit = entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, Type = EnumDamageType.Crushing, SourceBlock = __instance.Block, SourcePos = finalPos.ToVec3d() }, 18 * (float)Math.Abs(motionY) * ___impactDamageMul);
                        if (nowhit && !didhit)
                        {
                            didhit = nowhit;
                            __instance.Api.World.PlaySoundAt(__instance.Block.Sounds.Break, entity);
                        }
                    }
                }
            }
            ___lingerTicks = 50;
            ___fallHandled = true;
            return false;
        }

        public static void BaseMethodDummy(Vintagestory.GameContent.BlockTorch instance, IWorldAccessor world,
                                                                                               Entity byEntity,
                                                                                               Entity attackedEntity,
                                                                                               ItemSlot itemslot)
        {  return; }

        
        public static bool Prefix_OnHeldInteractStart(Vintagestory.GameContent.ItemPlumbAndSquare __instance,
            ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return false;
            }
            if (blockSel == null)
            {
                return false;
            }            
            return OnBlockAction.canBlockDestroy((byEntity as EntityPlayer).Player as IServerPlayer, blockSel, out string claimant);
        }

        public static bool Prefix_BlockEntityBarrel_OnReceivedClientPacket(Vintagestory.GameContent.BlockEntityBarrel __instance,
            IPlayer player, int packetid, byte[] data)
        {

            Block b1 = player.Entity.World.BlockAccessor.GetBlock(__instance.Pos);
            if (OnBlockAction.canBlockUse(player as IServerPlayer, __instance.Pos.ToVec3d()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool ReviveReplace(ServerSystemEntitySimulation __instance, IServerPlayer plr)
        {
            if(claims.dataStorage.GetPlayerByUid(plr.PlayerUID, out PlayerInfo playerInfo))
            {
                if(playerInfo.isPrisoned())
                {
                    return false;
                }
                if(playerInfo.hasCity())
                {
                    City city = playerInfo.City;
                    Vec3i ePos = plr.Entity.Pos.XYZ.AsVec3i;
                    if (city.HasTempleRespawnPoints())
                    {
                        var nearestP = 0;
                        Vec3i bestP = city.TempleRespawnPoints.First().Value;
                        foreach (var rPoint in city.TempleRespawnPoints)
                        {
                            var tmp = rPoint.Value.DistanceTo(ePos);
                            if(tmp <= nearestP)
                            {
                                bestP = rPoint.Value;
                                nearestP = (int)tmp;
                            }
                        }
                        var cpos = plr.Entity.Pos.Copy();
                        cpos.X = bestP.X + 0.5;
                        cpos.Y = bestP.Y + 2.5;
                        cpos.Z = bestP.Z + 1.5;
   
                        var inst = __instance;
                        plr.Entity.TeleportTo(cpos, (Action)(() =>
                        {
                            plr.Entity.Revive();
                            var f = inst;
                            var serv = typeof(ServerSystem).GetField("server", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inst) as ServerMain;
                            ConnectedClient client = serv.Clients[plr.ClientId];
                            serv.ServerUdpNetwork.physicsManager.UpdateTrackedEntitiesStates(
                                    client
                              );
                            
                            claims.sapi.World.RegisterCallback((dt) => { Particles.PlayerRespawnParticles(plr.Entity.Pos.XYZ); }, 1000);
                        }));
                        return true;
                    }
                }
            }
            return false;
        }
        public static IEnumerable<CodeInstruction> Transpiler_ComposeSlotOverlays_Add_Socket_Overlays_Not_Draw_ItemDamage(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);
            var proxyMethod = AccessTools.Method(typeof(harmonyPatches), "ReviveReplace");
            Label returnLabelNotResurectedByCity = il.DefineLabel();
            for (int i = 0; i < codes.Count; i++)
            {

                if (!found &&
                        codes[i].opcode == OpCodes.Ldloc_0 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Stfld)
                {

                    //push this on stack
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    //push this on stack again to get plr
                    yield return new CodeInstruction(OpCodes.Ldloc_0);

                    Type[] nestedTypes = typeof(ServerSystemEntitySimulation).GetNestedTypes(BindingFlags.Static |
                                      BindingFlags.Instance |
                                      BindingFlags.Public |
                                      BindingFlags.NonPublic);
                    var c2 = AccessTools.Field(nestedTypes[4], "player");
                    yield return new CodeInstruction(OpCodes.Ldfld, c2);
                    yield return new CodeInstruction(OpCodes.Call, proxyMethod);                   
                    yield return new CodeInstruction(OpCodes.Brfalse_S, returnLabelNotResurectedByCity);
                    yield return new CodeInstruction(OpCodes.Ret);
                    codes[i].labels.Add(returnLabelNotResurectedByCity);
                    found = true;
                }
                yield return codes[i];
            }
        }
        private static EnumWorldAccessResponse testBlockAccess(IPlayer player, EnumCANBlockAccessFlags accessType, out string claimant)
        {
            if (player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
            {
                claimant = "custommessage-inspectatormode";
                return EnumWorldAccessResponse.InSpectatorMode;
            }
            if (!player.Entity.Alive)
            {
                claimant = "custommessage-dead";
                return EnumWorldAccessResponse.PlayerDead;
            }
            if (accessType == EnumCANBlockAccessFlags.Build || accessType == EnumCANBlockAccessFlags.Break)
            {
                if (player.WorldData.CurrentGameMode == EnumGameMode.Guest)
                {
                    claimant = "custommessage-inguestmode";
                    return EnumWorldAccessResponse.InGuestMode;
                }
                if (!player.HasPrivilege(Privilege.buildblocks))
                {
                    claimant = "custommessage-nobuildprivilege";
                    return EnumWorldAccessResponse.NoPrivilege;
                }
                claimant = null;
                return EnumWorldAccessResponse.Granted;
            }
            else
            {
                if (!player.HasPrivilege(Privilege.useblock))
                {
                    claimant = "custommessage-nouseprivilege";
                    return EnumWorldAccessResponse.NoPrivilege;
                }
                claimant = null;
                return EnumWorldAccessResponse.Granted;
            }
        }

        /// <summary>
        /// Prevents the game from auto-dropping mouse cursor items
        /// while an ImGui inventory grid is active.
        /// </summary>
        public static bool Prefix_DropMouseSlotItems()
        {
            if (ImGuiInventoryGrid.SuppressMouseDrop)
            {
                return false; // skip original method
            }
            return true;
        }
    }
}
