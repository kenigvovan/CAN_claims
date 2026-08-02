using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Opening a lot: a starting price, how long it runs, and who may bid.
    ///
    /// The duration is a dropdown rather than a free field, and its entries are filtered by the
    /// host's own bounds - so a player can only pick a length the server would accept, instead of
    /// typing one and being refused after the fact. Addressing a lot to one named city stays a
    /// command-line option, as it does for a price tag: a dropdown cannot ask for a city name.
    /// </summary>
    public sealed class AuctionStartDialog : CANGuiDialogPanel
    {
        private const string PriceKey = "auction-start-price";

        /// <summary>Offered lengths in hours, before the host's bounds are applied.</summary>
        private static readonly int[] HourSteps = { 1, 3, 6, 12, 24, 48, 72, 168 };

        /// <summary>Command word plus display key, the same set the market offers.</summary>
        private static readonly string[] AudienceWords = { "allies", "nonhostile", "everyone" };
        private static readonly string[] AudienceKeys =
        {
            "claims:plot_trade_audience_allies",
            "claims:plot_trade_audience_nonhostile",
            "claims:plot_trade_audience_everyone"
        };

        protected override void BuildContent(DialogLayout l)
        {
            Text(l, Lang.Get("claims:gui-enter-auction-start-price"));
            TextInput(l, PriceKey, value => Args.Text = value);

            int min = claims.config?.AUCTION_MIN_HOURS ?? 1;
            int max = claims.config?.AUCTION_MAX_HOURS ?? 168;

            var hours = new List<int>();
            foreach (int step in HourSteps)
            {
                if (step >= min && step <= max) hours.Add(step);
            }
            // A host may narrow the window to something no step falls into (say 5 to 7 hours); the
            // bounds themselves are always valid, so offer those rather than an empty dropdown.
            if (hours.Count == 0)
            {
                hours.Add(min);
                if (max != min) hours.Add(max);
            }

            string[] values = new string[hours.Count];
            string[] names = new string[hours.Count];
            for (int i = 0; i < hours.Count; i++)
            {
                values[i] = hours[i].ToString(CultureInfo.InvariantCulture);
                names[i] = Lang.Get("claims:gui-auction-hours", hours[i]);
            }

            // Preselected on purpose, unlike the dialogs that kick people: every option here is
            // harmless, and a lot without a length cannot be opened at all.
            string preselected = values[DefaultIndex(hours)];
            Args.Second = preselected;

            TextRow(l, Lang.Get("claims:gui-auction-duration"));
            DropDown(l, values, names, picked => Args.Second = picked, preselected);

            // Who may bid - and therefore who may take the plot. Allies first and preselected: a lot
            // opened without a thought must not be open to the whole server.
            string[] otherCities = DialogRegistry.OtherCityNames();
            // "One city" is only offered when there is a city to name; an empty dropdown crashes the
            // client, and an addressed lot with nobody addressed would be refused by the server.
            bool canAddress = otherCities.Length > 0;

            var audienceWords = new List<string>(AudienceWords);
            var audienceNames = new List<string>();
            foreach (string key in AudienceKeys) audienceNames.Add(Lang.Get(key));
            if (canAddress)
            {
                audienceWords.Add("city");
                audienceNames.Add(Lang.Get("claims:plot_trade_audience_city"));
            }
            Args.First = audienceWords[0];

            TextRow(l, Lang.Get("claims:gui-plot-label-city-audience"));
            DropDown(l, audienceWords.ToArray(), audienceNames.ToArray(),
                picked => Args.First = picked, audienceWords[0]);

            if (canAddress)
            {
                Args.Selected = "";
                TextRow(l, Lang.Get("claims:gui-select-plot-city-target"));
                DropDown(l, otherCities, otherCities, picked => Args.Selected = picked);
            }

            Button(l, Lang.Get("claims:gui-auction-start-button"), () =>
            {
                if (!int.TryParse(Args.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int price)
                    || price < 0)
                {
                    claims.capi.ShowChatMessage(Lang.Get("claims:gui-input-not-a-number"));
                    return true;
                }
                // The buyer list is deliberately not preselected, so an addressed lot cannot go to
                // whichever city happens to sort first.
                if (Args.First == "city" && Args.Selected.Length == 0)
                {
                    claims.capi.ShowChatMessage(Lang.Get("claims:gui-select-plot-city-target"));
                    return true;
                }

                // The increment and the buyout keep their defaults here; both are command arguments
                // for a seller who wants them.
                Send("/plot auction " + price + " " + Args.Second
                     + " " + claims.config?.AUCTION_MIN_INCREMENT + " -1 " + Args.First
                     + (Args.First == "city" ? " " + Args.Selected : ""));
                Close();
                return true;
            });
        }

        /// <summary>The host's default length if it is on the list, otherwise the middle option.</summary>
        private static int DefaultIndex(List<int> hours)
        {
            int preferred = claims.config?.AUCTION_DEFAULT_HOURS ?? 24;
            int index = hours.IndexOf(preferred);
            return index >= 0 ? index : hours.Count / 2;
        }
    }
}
