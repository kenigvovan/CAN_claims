using HarmonyLib;
using RustyShell.Utilities.Blasts;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.rustyshellfork
{
    public class RustyShellForkCompat: ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "claims.RustyShellFork.Patches";
        public override double ExecuteOrder()
        {
            return 3;
        }
        public override bool ShouldLoad(ICoreAPI api)
        {
            if (base.ShouldLoad(api))
            {
                if (api.Side == EnumAppSide.Client || !api.ModLoader.IsModEnabled("rustyshellfork"))
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            harmonyInstance = new Harmony(harmonyID);
            TryPatch(api, "CommonBlast", "Prefix_IServerWorldAccessor_CommonBlast");
            TryPatch(api, "GasBlast", "Prefix_IServerWorldAccessor_GasBlast");
            TryPatch(api, "IncendiaryBlast", "Prefix_IServerWorldAccessor_IncendiaryBlast");
        }

        /// <summary>
        /// These targets belong to another mod, so they may disappear or be renamed independently of VS.
        /// A missing one must disable just that patch, not throw out of StartServerSide and kill the mod.
        /// </summary>
        private static void TryPatch(ICoreServerAPI api, string targetName, string prefixName)
        {
            var target = typeof(BlastExtensions).GetMethod(targetName);
            if (target == null)
            {
                api.Logger.Warning("[claims] RustyShellFork patch skipped: BlastExtensions.{0} not found (mod update?)", targetName);
                return;
            }
            try
            {
                harmonyInstance.Patch(target, prefix: new HarmonyMethod(typeof(harmPatch).GetMethod(prefixName)));
            }
            catch (System.Exception e)
            {
                api.Logger.Error("[claims] RustyShellFork patch of {0} failed: {1}", targetName, e);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            // StartServerSide runs again on every world join in a singleplayer process:
            // without unpatching, the prefixes stack up and the blast checks run repeatedly.
            harmonyInstance?.UnpatchAll(harmonyID);
            harmonyInstance = null;
        }
    }
}
