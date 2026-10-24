using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.meteoricexpansion
{
    public class MeteoricExpansionCompat : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "claims.MeteoricExpansion.Patches";
        private const string explosionTypeName = "MeteoricExpansion.EntityBehaviorMeteorExplosion";

        // /meteordown test command, disabled for normal play. To bring it back, uncomment every block marked [meteordown].
        // [meteordown]
        //private const string motionBaseTypeName = "MeteoricExpansion.Entities.Behaviors.EntityBehaviorMeteorMotionBase";
        //private const string meteorDomain = "meteoricexpansion";
        //// Synced to clients with the spawn packet, so both sides drop the horizontal motion of the same meteor.
        //private const string straightDownAttr = "claims:meteorStraightDown";
        //private ICoreAPI api;
        //private PropertyInfo translationProp;

        public override double ExecuteOrder()
        {
            return 3;
        }
        public override bool ShouldLoad(ICoreAPI api)
        {
            if (base.ShouldLoad(api))
            {
                // The fork keeps the original's classes and asset domain but ships under its own modid.
                // [meteordown] needs the client side too: drop the Client check when bringing it back.
                if (api.Side == EnumAppSide.Client
                    || !(api.ModLoader.IsModEnabled("meteoricexpansionfork") || api.ModLoader.IsModEnabled("meteoricexpansion")))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        // [meteordown]
        //public override void Start(ICoreAPI api)
        //{
        //    this.api = api;
        //    Type motionBaseType = AccessTools.TypeByName(motionBaseTypeName);
        //    translationProp = motionBaseType == null ? null : AccessTools.Property(motionBaseType, "RandomTranslation");
        //    if (translationProp == null)
        //    {
        //        api.Logger.Warning("[claims] MeteoricExpansion /meteordown disabled: {0}.RandomTranslation not found (mod update?)", motionBaseTypeName);
        //        return;
        //    }
        //    api.Event.OnEntitySpawn += OnEntitySpawn;
        //}
        //public override void StartClientSide(ICoreClientAPI api)
        //{
        //    if (translationProp != null)
        //    {
        //        // A meteor already falling when the player comes into range arrives as loaded, not spawned.
        //        api.Event.OnEntityLoaded += OnEntitySpawn;
        //    }
        //}

        public override void StartServerSide(ICoreServerAPI api)
        {
            harmonyInstance = new Harmony(harmonyID);
            Type explosionType = AccessTools.TypeByName(explosionTypeName);
            if (explosionType == null)
            {
                api.Logger.Warning("[claims] MeteoricExpansion patches skipped: {0} not found (mod update?)", explosionTypeName);
            }
            else
            {
                TryPatch(api, explosionType, "IsPositionClaimed", postfix: "Postfix_IsPositionClaimed");
                TryPatch(api, explosionType, "InjureEntities", prefix: "Prefix_InjureEntities");
            }

            // [meteordown]
            //if (translationProp != null)
            //{
            //    api.ChatCommands.Create("meteordown")
            //        .WithDescription("Drop a meteor straight down onto you (claims protection test)")
            //        .WithArgs(api.ChatCommands.Parsers.OptionalWord("code"))
            //        .RequiresPrivilege(Privilege.controlserver)
            //        .RequiresPlayer()
            //        .HandleWith(args => SpawnStraightDownMeteor(api, args.Caller.Player as IServerPlayer, args[0] as string));
            //}
        }

        // [meteordown]
        //private TextCommandResult SpawnStraightDownMeteor(ICoreServerAPI sapi, IServerPlayer player, string code)
        //{
        //    EntityProperties type;
        //    if (code != null)
        //    {
        //        type = sapi.World.GetEntityType(new AssetLocation(meteorDomain, code));
        //        if (type == null || type.Class == null || !type.Class.Contains("FallingMeteor"))
        //        {
        //            return TextCommandResult.Error("Unknown falling meteor: " + meteorDomain + ":" + code);
        //        }
        //    }
        //    else
        //    {
        //        var types = sapi.World.EntityTypes.FindAll(t => t.Code?.Domain == meteorDomain && t.Class != null && t.Class.Contains("FallingMeteor"));
        //        if (types.Count == 0)
        //        {
        //            return TextCommandResult.Error("No falling meteor entity types found");
        //        }
        //        type = types[sapi.World.Rand.Next(types.Count)];
        //    }
        //
        //    Entity meteor = sapi.World.ClassRegistry.CreateEntity(type);
        //    if (meteor == null)
        //    {
        //        return TextCommandResult.Error("Could not create " + type.Code);
        //    }
        //    meteor.Pos.SetPos(new EntityPos(player.Entity.Pos.X, sapi.WorldManager.MapSizeY - 6, player.Entity.Pos.Z, 0f, 0f, 0f));
        //    meteor.WatchedAttributes.SetBool(straightDownAttr, true);
        //    sapi.World.SpawnEntity(meteor);
        //    return TextCommandResult.Success("Meteor " + type.Code + " is falling at X=" + (int)player.Entity.Pos.X + " Z=" + (int)player.Entity.Pos.Z);
        //}
        //
        //private void OnEntitySpawn(Entity entity)
        //{
        //    if (!entity.WatchedAttributes.GetBool(straightDownAttr))
        //    {
        //        return;
        //    }
        //    EntityBehavior motion = entity.GetBehavior("fallingmeteormotion");
        //    if (motion == null || !(translationProp.GetValue(motion) is Vec3f translation))
        //    {
        //        return;
        //    }
        //    translation.X = 0;
        //    translation.Z = 0;
        //    if (entity.World.Side == EnumAppSide.Server)
        //    {
        //        // The explosion places the meteor remains and throws shrapnel along this direction,
        //        // which was taken from the random translation before it was zeroed.
        //        Vec3i direction = TreeAttributeUtil.GetVec3i(entity.Attributes, "direction", Vec3i.Zero);
        //        TreeAttributeUtil.SetVec3i(entity.Attributes, "direction", new Vec3i(0, direction.Y, 0));
        //    }
        //}

        private static void TryPatch(ICoreServerAPI api, Type targetType, string targetName, string prefix = null, string postfix = null)
        {
            var target = AccessTools.Method(targetType, targetName);
            if (target == null)
            {
                api.Logger.Warning("[claims] MeteoricExpansion patch skipped: {0}.{1} not found (mod update?)", targetType.Name, targetName);
                return;
            }
            try
            {
                harmonyInstance.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(MeteoricExpansionPatches).GetMethod(prefix)),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(MeteoricExpansionPatches).GetMethod(postfix)));
                api.Logger.Notification("[claims] MeteoricExpansion patch of {0} applied", targetName);
            }
            catch (Exception e)
            {
                api.Logger.Error("[claims] MeteoricExpansion patch of {0} failed: {1}", targetName, e);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            // [meteordown]
            //if (api == null)
            //{
            //    return;
            //}
            //api.Event.OnEntitySpawn -= OnEntitySpawn;
            //if (api is ICoreClientAPI capi)
            //{
            //    capi.Event.OnEntityLoaded -= OnEntitySpawn;
            //    // harmonyInstance is static and owned by the server instance living in the same singleplayer process.
            //    return;
            //}

            // StartServerSide runs again on every world join in a singleplayer process:
            // without unpatching, the patches stack up.
            harmonyInstance?.UnpatchAll(harmonyID);
            harmonyInstance = null;
        }
    }
}
