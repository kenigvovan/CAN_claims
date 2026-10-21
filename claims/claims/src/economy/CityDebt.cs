using System;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;

namespace claims.src.economy
{
    /// <summary>
    /// Paying off a city's debt from its treasury. Without it a city in debt could deposit coins
    /// and spend them on new plots straight away, while the debt only grew towards demolition.
    /// </summary>
    public static class CityDebt
    {
        /// <summary>
        /// Withdraws up to <paramref name="available"/> from the city's treasury against its debt.
        /// Returns what was repaid; 0 when there is no debt or the withdrawal failed.
        /// </summary>
        public static decimal Repay(City city, decimal available)
        {
            if (city == null || city.DebtBalance <= 0 || available <= 0) return 0;

            decimal balance = claims.economyProvider.GetBalance(city.MoneyAccountName);
            decimal amount = Math.Min((decimal)city.DebtBalance, Math.Min(available, balance));
            if (amount <= 0) return 0;

            if (claims.economyProvider.Withdraw(city.MoneyAccountName, amount) != MoneyOperationResult.Success)
            {
                return 0;
            }

            city.DebtBalance = Math.Max(0, city.DebtBalance - (double)amount);
            city.saveToDatabase();
            string guid = city.Guid;
            claims.sapi.Event.RegisterCallback(_ =>
                UsefullPacketsSend.AddToQueueCityInfoUpdate(guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DEBT), 0);
            return amount;
        }
    }
}
