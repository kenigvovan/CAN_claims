using System;
using claims.src.auxialiry;
using claims.src.messages;

namespace claims.src.part.structure.plots
{
    public static class PlotRefundHelper
    {
        public static void RefundOnCityUnclaim(Plot plot)
        {
            if (plot == null) return;
            if (claims.config.PLOT_UNCLAIM_REFUND_PERCENT <= 0) return;
            if (plot.WasCaptured) return;
            if (plot.TimeStampClaimed == 0) return;
            if (!plot.hasCity()) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - plot.TimeStampClaimed < claims.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS) return;

            double basePrice = plot.extraBought
                ? claims.config.EXTRA_PLOT_COST
                : claims.config.PLOT_CLAIM_PRICE;
            double refund = basePrice * claims.config.PLOT_UNCLAIM_REFUND_PERCENT / 100.0;
            if (refund <= 0) return;

            claims.economyHandler.deposit(plot.getCity().MoneyAccountName, (decimal)refund);
        }

        /// <summary>
        /// Returns the intended refund amount for a personal plot unclaim, or 0 if the refund
        /// is not applicable (feature disabled, captured, legacy plot, too young, or never paid).
        /// </summary>
        public static long CalculatePersonalUnclaimRefund(Plot plot)
        {
            if (plot == null) return 0;
            if (claims.config.PLOT_UNCLAIM_REFUND_PERCENT <= 0) return 0;
            if (plot.WasCaptured) return 0;
            if (plot.TimeStampClaimed == 0) return 0;
            if (plot.lastPaidPrice <= 0) return 0;
            if (!plot.hasCity()) return 0;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - plot.TimeStampClaimed < claims.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS) return 0;

            return plot.lastPaidPrice * claims.config.PLOT_UNCLAIM_REFUND_PERCENT / 100;
        }

        /// <summary>
        /// Returns how much of the desired refund the city can actually afford.
        /// </summary>
        public static long GetMaxAffordableRefund(Plot plot, long desiredAmount)
        {
            if (desiredAmount <= 0 || plot == null || !plot.hasCity()) return 0;
            long cityBalance = (long)claims.economyHandler.getBalance(plot.getCity().MoneyAccountName);
            if (cityBalance <= 0) return 0;
            return Math.Min(desiredAmount, cityBalance);
        }

        /// <summary>
        /// Transfers <paramref name="amount"/> from city treasury to the player's account.
        /// Mirrors the two-step pattern used in PlotClaim: manual withdraw+deposit with
        /// rollback for REAL_MONEY handler, atomic depositFromAToB otherwise.
        /// Returns true on success, false if the transfer failed (money is NOT lost).
        /// </summary>
        public static bool TryTransferPersonalRefund(Plot plot, PlayerInfo player, long amount)
        {
            if (amount <= 0) return true;
            if (plot == null || !plot.hasCity() || player == null) return false;

            string cityAccount = plot.getCity().MoneyAccountName;
            string playerAccount = player.MoneyAccountName;
            decimal amt = (decimal)amount;

            if (caneconomy.caneconomy.config.SELECTED_ECONOMY_HANDLER == "REAL_MONEY")
            {
                var withdrawState = claims.economyHandler.withdraw(cityAccount, amt);
                if (withdrawState.ResultState != caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
                {
                    MessageHandler.sendErrorMsg("PlotRefundHelper: withdraw from " + cityAccount + " failed for refund " + amount);
                    return false;
                }
                var depositState = claims.economyHandler.deposit(playerAccount, amt);
                if (depositState.ResultState != caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
                {
                    claims.economyHandler.deposit(cityAccount, amt);
                    MessageHandler.sendErrorMsg("PlotRefundHelper: deposit to " + playerAccount + " failed, rolled back refund " + amount);
                    return false;
                }
                return true;
            }

            var result = claims.economyHandler.depositFromAToB(cityAccount, playerAccount, amt);
            if (result.ResultState != caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                MessageHandler.sendErrorMsg("PlotRefundHelper: depositFromAToB " + cityAccount + "->" + playerAccount + " failed for refund " + amount);
                return false;
            }
            return true;
        }
    }
}
