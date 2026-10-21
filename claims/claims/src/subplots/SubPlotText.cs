using System.Globalization;
using Vintagestory.API.Config;

namespace claims.src.subplots
{
    /// <summary>How a sub-plot's offer reads on the banner, the markers and the GUI alike.</summary>
    public static class SubPlotText
    {
        /// <summary>
        /// "for sale 100", "rent 5 a day" for a free one with a tax - that is what it is to the
        /// tenant - or "free"; null when it is not on offer.
        /// </summary>
        public static string Offer(double price, double tax)
        {
            if (price < 0) return null;
            if (price > 0) return Lang.Get("claims:movementgui-price", Number(price));
            return tax > 0 ? Lang.Get("claims:subplot-offer-rent", Number(tax)) : Lang.Get("claims:subplot-offer-free");
        }

        public static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
