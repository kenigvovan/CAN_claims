using System;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.messages;

namespace claims.src.part.structure.plots
{
    public static class PlotRefundHelper
    {
        // Above 100 every claim/unclaim round would pay out more than was paid in. Clamped here
        // too, not only on load: /cadmin setcfg changes the config at runtime.
        private static int RefundPercent => Math.Clamp(claims.config.PLOT_UNCLAIM_REFUND_PERCENT, 0, 100);

        public static void RefundOnCityUnclaim(Plot plot)
        {
            if (plot == null) return;
            if (RefundPercent <= 0) return;
            if (plot.WasCaptured) return;
            // 0: claimed before the stamp existed, or claimed for free by a village.
            if (plot.TimeStampClaimed == 0) return;
            if (!plot.hasCity()) return;
            // Village plots cost nothing to claim, so there is nothing to refund either. Kept for
            // plots a village claimed before the stamp was left at 0 for free claims.
            if (plot.getCity().IsVillage()) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - plot.TimeStampClaimed < claims.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS) return;

            double basePrice = plot.extraBought
                ? claims.config.EXTRA_PLOT_COST
                : claims.config.PLOT_CLAIM_PRICE;
            double refund = basePrice * RefundPercent / 100.0;
            if (refund <= 0) return;

            claims.economyProvider.Deposit(plot.getCity().MoneyAccountName, (decimal)refund);
        }

        /// <summary>
        /// Returns the intended refund amount for a personal plot unclaim, or 0 if the refund
        /// is not applicable (feature disabled, captured, legacy plot, too young, or never paid).
        /// </summary>
        public static long CalculatePersonalUnclaimRefund(Plot plot)
        {
            if (plot == null) return 0;
            if (RefundPercent <= 0) return 0;
            if (plot.WasCaptured) return 0;
            if (plot.TimeStampClaimed == 0) return 0;
            if (plot.lastPaidPrice <= 0) return 0;
            if (!plot.hasCity()) return 0;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - plot.TimeStampClaimed < claims.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS) return 0;

            return plot.lastPaidPrice * RefundPercent / 100;
        }

        /// <summary>
        /// Returns how much of the desired refund the city can actually afford.
        /// </summary>
        public static long GetMaxAffordableRefund(Plot plot, long desiredAmount)
        {
            if (desiredAmount <= 0 || plot == null || !plot.hasCity()) return 0;
            // Compared as decimal: without an economy mod the balance is decimal.MaxValue, and
            // casting that to long threw OverflowException out of /plot unclaim.
            decimal cityBalance = claims.economyProvider.GetBalance(plot.getCity().MoneyAccountName);
            if (cityBalance <= 0) return 0;
            return cityBalance >= desiredAmount ? desiredAmount : (long)Math.Floor(cityBalance);
        }

        /// <summary>
        /// Transfers <paramref name="amount"/> from city treasury to the player's account.
        /// Rollback is the adapter's responsibility.
        /// Returns true on success, false if the transfer failed (money is NOT lost).
        /// </summary>
        public static bool TryTransferPersonalRefund(Plot plot, PlayerInfo player, long amount)
        {
            if (amount <= 0) return true;
            if (plot == null || !plot.hasCity() || player == null) return false;

            string cityAccount = plot.getCity().MoneyAccountName;
            string playerAccount = player.MoneyAccountName;
            decimal amt = (decimal)amount;

            var result = claims.economyProvider.Transfer(cityAccount, playerAccount, amt);
            if (result != MoneyOperationResult.Success)
            {
                MessageHandler.sendErrorMsg("PlotRefundHelper: Transfer " + cityAccount + "->" + playerAccount + " failed (" + result + ") for refund " + amount);
                return false;
            }
            return true;
        }
    }
}
