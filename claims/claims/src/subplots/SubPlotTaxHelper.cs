using System.Linq;
using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.subplots
{
    /// <summary>
    /// The daily tax on a sub-plot, and how the city may change it - the same protection as a plots
    /// group fee. Lowering applies at once. A raise on an owned sub-plot is announced: the owner keeps
    /// paying the old tax for <see cref="Config.SUBPLOT_TAX_RAISE_DELAY_HOURS"/>, and when the wait is
    /// over either pays the new one, having accepted it, or gives the sub-plot back with the usual
    /// refund. Nobody is charged a tax they did not agree to.
    /// </summary>
    public static class SubPlotTaxHelper
    {
        public static TextCommandResult SetTax(PlayerInfo actor, SubPlot subPlot, double tax, long now)
        {
            if (!SubPlotEditor.CanManage(actor, subPlot?.City)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (tax < 0) return TextCommandResult.Error("claims:not_negative");
            if (tax > claims.config.MAX_SUBPLOT_TAX) tax = claims.config.MAX_SUBPLOT_TAX;
            long delay = claims.config.SUBPLOT_TAX_RAISE_DELAY_HOURS * 3600L;
            string name = subPlot.GetPartName();

            // Nobody to protect: no owner, paying less, or a host who wants raises to land at once.
            if (subPlot.Owner == null || tax <= subPlot.Tax || delay <= 0)
            {
                subPlot.Tax = tax;
                subPlot.ClearPendingTax();
                subPlot.saveToDatabase();
                // Clients show the tax over the sub-plot.
                SubPlotSync.MarkChanged(subPlot);
                SubPlotSync.NotifyPayment(subPlot.Owner);
                SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotTaxChanged, Number(tax));
                TellOwner(subPlot, Lang.Get("claims:subplot_tax_changed", name, tax));
                return Result("claims:subplot_tax_changed", name, tax);
            }

            // No more than the standing announcement asks keeps its deadline and the owner's answer,
            // so a raise cannot be pushed back forever by re-announcing a coin less.
            if (subPlot.HasPendingTax && tax <= subPlot.PendingTax)
            {
                subPlot.PendingTax = tax;
                subPlot.saveToDatabase();
                SubPlotSync.NotifyCity(subPlot.City);
                int hoursLeft = HoursUntil(subPlot.PendingTaxAt, now);
                TellOwner(subPlot, Lang.Get("claims:subplot_tax_raise_announced", name, tax, hoursLeft, subPlot.Tax));
                return Result("claims:subplot_tax_raise_announced", name, tax, hoursLeft, subPlot.Tax);
            }

            subPlot.PendingTax = tax;
            subPlot.PendingTaxAt = now + delay;
            subPlot.PendingTaxAccepted = false;
            subPlot.saveToDatabase();
            SubPlotSync.NotifyCity(subPlot.City);
            int hours = claims.config.SUBPLOT_TAX_RAISE_DELAY_HOURS;
            TellOwner(subPlot, Lang.Get("claims:subplot_tax_raise_announced", name, tax, hours, subPlot.Tax));
            return Result("claims:subplot_tax_raise_announced", name, tax, hours, subPlot.Tax);
        }

        /// <summary>The owner agreeing to the announced raise.</summary>
        public static TextCommandResult Accept(PlayerInfo actor, SubPlot subPlot)
        {
            if (subPlot?.Owner == null || !subPlot.Owner.Equals(actor)) return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            if (!subPlot.HasPendingTax) return TextCommandResult.Error("claims:subplot_tax_no_pending");
            subPlot.PendingTaxAccepted = true;
            subPlot.saveToDatabase();
            SubPlotSync.NotifyCity(subPlot.City);
            return Result("claims:subplot_tax_accepted", subPlot.GetPartName(), subPlot.PendingTax);
        }

        /// <summary>Lands every announced raise whose wait is over. Called hourly.</summary>
        public static void SettleDueRaises(long now)
        {
            if (claims.dataStorage == null) return;
            foreach (SubPlot subPlot in claims.dataStorage.AllSubPlots().ToArray())
            {
                if (!subPlot.HasPendingTax || now < subPlot.PendingTaxAt) continue;
                Settle(subPlot, now);
            }
        }

        private static void Settle(SubPlot subPlot, long now)
        {
            double newTax = subPlot.PendingTax;
            PlayerInfo owner = subPlot.Owner;
            // The mayor pays no taxes, so there is nothing for them to have agreed to.
            bool keeps = owner == null || subPlot.PendingTaxAccepted || (subPlot.City?.isMayor(owner) ?? false);
            if (keeps)
            {
                subPlot.Tax = newTax;
                subPlot.ClearPendingTax();
                subPlot.saveToDatabase();
                SubPlotSync.MarkChanged(subPlot);
                SubPlotSync.NotifyPayment(subPlot.Owner);
                SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotTaxChanged, Number(newTax));
                TellOwner(subPlot, Lang.Get("claims:subplot_tax_raise_applied", subPlot.GetPartName(), newTax));
                return;
            }

            // Given back as if by the owner: the same refund, if the treasury can pay it.
            long refund = SubPlotEditor.GiveBackRefund(subPlot, now, claims.economyProvider.GetBalance(subPlot.City.MoneyAccountName));
            if (refund > 0 && claims.economyProvider.Transfer(subPlot.City.MoneyAccountName, owner.MoneyAccountName, refund)
                != economy.MoneyOperationResult.Success)
            {
                refund = 0;
            }
            SubPlotLifecycle.ReturnToCity(subPlot, log: false);
            SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotTaxRefused, owner.GetPartName(), Number(newTax));
            MessageHandler.sendMsgToPlayerInfo(owner,
                Lang.Get("claims:subplot_tax_not_accepted_lost", subPlot.GetPartName(), newTax, refund));
        }

        private static string Number(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static int HoursUntil(long at, long now)
        {
            long left = at - now;
            return left <= 3600 ? 1 : (int)((left + 3599) / 3600);
        }

        private static void TellOwner(SubPlot subPlot, string msg)
        {
            if (subPlot.Owner != null) MessageHandler.sendMsgToPlayerInfo(subPlot.Owner, msg);
        }

        private static TextCommandResult Result(string key, params object[] args) => new TextCommandResult
        {
            Status = EnumCommandStatus.Success, StatusMessage = key, MessageParams = args
        };
    }
}
