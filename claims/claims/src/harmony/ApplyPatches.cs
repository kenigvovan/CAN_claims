using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace claims.src.harmony
{
    public class ApplyPatches
    {
        /// <summary>
        /// Patches a single target, skipping it with a warning when the target method no longer exists.
        /// Passing a null method to Harmony throws, which would abort the whole patch chain and leave the
        /// mod half-patched after a VS update.
        /// </summary>
        /// <param name="patchClass">Where the named patch methods live. Defaults to the catch-all
        /// harmonyPatches; a feature with its own class passes it here.</param>
        private static void TryPatch(Harmony harmonyInstance, MethodInfo target, string targetName,
            string prefix = null, string postfix = null, string transpiler = null, Type patchClass = null)
        {
            if (target == null)
            {
                Logger()?.Warning("[claims] patch skipped: {0} not found (VS update?)", targetName);
                return;
            }
            patchClass ??= typeof(harmonyPatches);
            try
            {
                harmonyInstance.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(patchClass.GetMethod(prefix)),
                    postfix: postfix == null ? null : new HarmonyMethod(patchClass.GetMethod(postfix)),
                    transpiler: transpiler == null ? null : new HarmonyMethod(patchClass.GetMethod(transpiler)));
            }
            catch (Exception e)
            {
                Logger()?.Error("[claims] patch of {0} failed, feature disabled: {1}", targetName, e);
            }
        }

        private static Vintagestory.API.Common.ILogger Logger()
        {
            return claims.sapi?.Logger ?? claims.capi?.Logger;
        }

        public static void ApplyClientPatches(Harmony harmonyInstance, string harmonyID)
        {
            harmonyInstance = new Harmony(harmonyID);

            // Boat sharing needs the client too: CanMount checks ownership before asking the server,
            // and the tooltip is client-only. The client answers from the synced stamp.
            TryPatch(harmonyInstance, typeof(EntityBehaviorOwnable).GetMethod("IsOwner"),
                "EntityBehaviorOwnable.IsOwner", postfix: "Postfix_Ownable_IsOwner", patchClass: typeof(BoatSharePatches));
            TryPatch(harmonyInstance, typeof(EntityBehaviorOwnable).GetMethod("GetInfoText"),
                "EntityBehaviorOwnable.GetInfoText", postfix: "Postfix_Ownable_GetInfoText", patchClass: typeof(BoatSharePatches));
        }
        public static void ApplyServerPatches(Harmony harmonyInstance, string harmonyID)
        {
            harmonyInstance = new Harmony(harmonyID);
            //Falling block patch
            if (claims.config.FALLING_BLOCKS_TO_CITY_PLOTS_PATCH)
            {
                if (harmonyPatches.EntityBlockFallingUpdateBlock == null || harmonyPatches.EntityBlockFallingDropItems == null)
                    claims.sapi.Logger.Warning("[claims] FALLING_BLOCKS patch skipped: EntityBlockFalling.UpdateBlock or DropItems not found (VS update?)");
                else
                    TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.EntityBlockFalling).GetMethod("OnFallToGround"),
                        "EntityBlockFalling.OnFallToGround", prefix: "Prefix_OnFallToGround");
            }

            if (claims.config.WATER_FLOW_CITY_PLOTS_PATCH)
            {
                if (harmonyPatches.TrySpreadIntoBlock == null)
                    claims.sapi.Logger.Warning("[claims] WATER_FLOW patch skipped: BlockBehaviorFiniteSpreadingLiquid.TrySpreadIntoBlock not found (VS update?)");
                else
                {
                    TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid).GetMethod("TrySpreadHorizontal",
                        BindingFlags.NonPublic | BindingFlags.Instance), "BlockBehaviorFiniteSpreadingLiquid.TrySpreadHorizontal", prefix: "Prefix_TrySpreadHorizontal");
                    TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid).GetMethod("FindDownwardPaths"),
                        "BlockBehaviorFiniteSpreadingLiquid.FindDownwardPaths", postfix: "Postfix_FindDownwardPaths");
                }
            }

            TryPatch(harmonyInstance, typeof(Vintagestory.API.Common.EntityAgent).GetMethod("ShouldReceiveDamage"),
                "EntityAgent.ShouldReceiveDamage", prefix: "Prefix_On_ReceiveDamage");

            TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.BEBehaviorBurning).GetMethod("TrySpreadTo"),
                "BEBehaviorBurning.TrySpreadTo", prefix: "Prefix_On_TrySpreadFireAllDirs");

            TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.BlockEntityBomb).GetMethod("HasPermissionToUse"),
                "BlockEntityBomb.HasPermissionToUse", prefix: "Prefix_HasPermissionToUse");

            TryPatch(harmonyInstance, typeof(Vintagestory.Common.ChatCommandApi).GetMethod("Execute", new[] { typeof(string), typeof(IServerPlayer), typeof(int), typeof(string), typeof(Action<TextCommandResult>) }),
                "ChatCommandApi.Execute", prefix: "Prefix_HandleCommand");

            TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.ItemPlumbAndSquare).GetMethod("OnHeldInteractStart"),
                "ItemPlumbAndSquare.OnHeldInteractStart", prefix: "Prefix_OnHeldInteractStart");

            TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.BlockEntityBarrel).GetMethod("OnReceivedClientPacket"),
                "BlockEntityBarrel.OnReceivedClientPacket", prefix: "Prefix_BlockEntityBarrel_OnReceivedClientPacket");

            if (claims.config.FRUIT_ONLY_ON_ORCHARD_PLOTS)
            {
                TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.FruitTreeRootBH).GetMethod("onRootTick",
                    BindingFlags.NonPublic | BindingFlags.Instance), "FruitTreeRootBH.onRootTick",
                    postfix: "Postfix_FruitTreeRootTick");

                TryPatch(harmonyInstance, typeof(Vintagestory.GameContent.BlockFruitTreeBranch).GetMethod("TryPlaceBlock",
                    new[] { typeof(IWorldAccessor), typeof(IPlayer), typeof(ItemStack), typeof(BlockSelection), typeof(string).MakeByRefType() }),
                    "BlockFruitTreeBranch.TryPlaceBlock", postfix: "Postfix_FruitTreeTryPlaceBlock");
            }

            TryPatch(harmonyInstance, typeof(ServerSystemEntitySimulation).GetMethod("OnPlayerRespawn", BindingFlags.NonPublic | BindingFlags.Instance),
                "ServerSystemEntitySimulation.OnPlayerRespawn", transpiler: "Transpiler_ComposeSlotOverlays_Add_Socket_Overlays_Not_Draw_ItemDamage");

            // Boat sharing: IsOwner is the gate every ownership check goes through, verifyOwnership
            // stamps the owner's city onto the boat for clients. Patched regardless of
            // BOAT_SHARE_WITH_CITY - the postfix reads the flag, so setcfg works without a restart.
            TryPatch(harmonyInstance, typeof(EntityBehaviorOwnable).GetMethod("IsOwner"),
                "EntityBehaviorOwnable.IsOwner", postfix: "Postfix_Ownable_IsOwner", patchClass: typeof(BoatSharePatches));
            TryPatch(harmonyInstance, typeof(EntityBehaviorOwnable).GetMethod("verifyOwnership", BindingFlags.NonPublic | BindingFlags.Instance),
                "EntityBehaviorOwnable.verifyOwnership", postfix: "Postfix_Ownable_VerifyOwnership", patchClass: typeof(BoatSharePatches));
        }
    }
}
