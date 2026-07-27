using claims.src.auxialiry;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace claims.src.cropbehaviors
{
    /// <summary>
    /// Restricts fruit tree fruiting to plots of type ORCHARD.
    /// Trees outside an orchard still go through the whole vanilla cycle (young -> flowering ->
    /// dormancy -> flowering ...), they just never carry fruit: the Fruiting/Ripe states are
    /// replaced by Empty right after the vanilla root tick computed them.
    /// Called from the Harmony postfix of FruitTreeRootBH.onRootTick, so it only ever runs
    /// server side (the root tick listener is registered for the server only).
    /// </summary>
    public static class OrchardRules
    {
        public static bool IsOnOrchardPlot(int blockX, int blockZ)
        {
            if (claims.dataStorage == null) return false;
            var plotPos = PlotPosition.fromXZ(blockX, blockZ);
            return claims.dataStorage.GetPlot(plotPos, out Plot plot) && plot.Type == PlotType.ORCHARD;
        }

        /// <summary>
        /// Whether planting at this position deserves the "will only blossom" warning.
        /// On unclaimed land the warning is optional (ORCHARD_WARN_OUTSIDE_CITY), since there is no
        /// plot to convert into an orchard anyway and the message would be pure noise.
        /// </summary>
        public static bool ShouldWarnOnPlanting(int blockX, int blockZ)
        {
            if (claims.dataStorage == null) return false;

            var plotPos = PlotPosition.fromXZ(blockX, blockZ);
            if (!claims.dataStorage.GetPlot(plotPos, out Plot plot))
            {
                return claims.config.ORCHARD_WARN_OUTSIDE_CITY;
            }
            return plot.Type != PlotType.ORCHARD;
        }

        /// <summary>
        /// Downgrades Fruiting/Ripe back to Empty when the tree does not stand on an orchard plot.
        /// lastStateChangeTotalDays is deliberately left untouched: the vanilla tick already set it
        /// when it entered Fruiting, so the tree keeps following its normal seasonal schedule from
        /// the Empty state instead of freezing in place.
        /// </summary>
        public static void StripFruitOutsideOrchard(FruitTreeRootBH rootBh)
        {
            if (!claims.config.FRUIT_ONLY_ON_ORCHARD_PLOTS) return;
            if (rootBh?.propsByType == null) return;

            BlockEntity be = rootBh.Blockentity;
            if (be?.Pos == null) return;

            if (!HasFruit(rootBh)) return;
            if (IsOnOrchardPlot(be.Pos.X, be.Pos.Z)) return;

            foreach (var props in rootBh.propsByType.Values)
            {
                if (props.State != EnumFruitTreeState.Fruiting && props.State != EnumFruitTreeState.Ripe) continue;

                props.State = EnumFruitTreeState.Empty;
                props.workingState = EnumFruitTreeState.Empty;
            }

            be.MarkDirty(true);
        }

        private static bool HasFruit(FruitTreeRootBH rootBh)
        {
            foreach (var props in rootBh.propsByType.Values)
            {
                if (props.State == EnumFruitTreeState.Fruiting || props.State == EnumFruitTreeState.Ripe) return true;
            }
            return false;
        }
    }
}
