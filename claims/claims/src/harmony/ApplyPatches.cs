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
        public static void ApplyClientPatches(Harmony harmonyInstance, string harmonyID)
        {
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(
                typeof(Vintagestory.Common.PlayerInventoryManager).GetMethod("DropMouseSlotItems"),
                prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_DropMouseSlotItems")));
        }
        public static void ApplyServerPatches(Harmony harmonyInstance, string harmonyID)
        {
            harmonyInstance = new Harmony(harmonyID);           
            //Falling block patch
            if (claims.config.FALLING_BLOCKS_TO_CITY_PLOTS_PATCH)
            {
                harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityBlockFalling).GetMethod("OnFallToGround"), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_OnFallToGround")));
            }

            if (claims.config.WATER_FLOW_CITY_PLOTS_PATCH)
            {
                harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid).GetMethod("TrySpreadHorizontal",
                    BindingFlags.NonPublic | BindingFlags.Instance), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_TrySpreadHorizontal")));
                harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid).GetMethod("FindDownwardPaths"), postfix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Postfix_FindDownwardPaths")));
            }

            harmonyInstance.Patch(typeof(Vintagestory.API.Common.EntityAgent).GetMethod("ShouldReceiveDamage"), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_On_ReceiveDamage")));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BEBehaviorBurning).GetMethod("TrySpreadTo"), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_On_TrySpreadFireAllDirs")));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityBomb).GetMethod("HasPermissionToUse"), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_HasPermissionToUse")));

            harmonyInstance.Patch(typeof(Vintagestory.Common.ChatCommandApi).GetMethod("Execute", new[] { typeof(string), typeof(IServerPlayer), typeof(int), typeof(string), typeof(Action<TextCommandResult>) }), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_HandleCommand")));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.ItemPlumbAndSquare).GetMethod("OnHeldInteractStart"), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_OnHeldInteractStart")));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityBarrel).GetMethod("OnReceivedClientPacket"), prefix: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Prefix_BlockEntityBarrel_OnReceivedClientPacket")));

            harmonyInstance.Patch(typeof(ServerSystemEntitySimulation).GetMethod("OnPlayerRespawn", BindingFlags.NonPublic | BindingFlags.Instance), transpiler: new HarmonyMethod(typeof(harmonyPatches).GetMethod("Transpiler_ComposeSlotOverlays_Add_Socket_Overlays_Not_Draw_ItemDamage")));

        }
    }
}
