using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    /// <summary>
    /// Commands behind the inter-city plot market: a city lists one of its plots, another city buys
    /// it. Every rule is judged by <see cref="PlotMarketHelper"/>; this only resolves the arguments.
    /// </summary>
    public class PlotMarketCommand : BaseCommand
    {
        /// <summary>/plot citysell &lt;price&gt; [audience] [cityName]</summary>
        public static TextCommandResult SetForSaleToCities(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            if (!claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }

            if (!PlotMarketHelper.CanList(plot, playerInfo.City, out string errorKey))
            {
                return TextCommandResult.Error(errorKey);
            }

            int price = (int)args.Parsers[0].GetValue();
            if (price < 0) return TextCommandResult.Error("claims:try_pos");

            string audienceWord = args.Parsers.Count > 1 ? args.Parsers[1].GetValue() as string : null;
            if (!TryParseAudience(audienceWord, out EnumPlotSaleAudience audience))
            {
                return TextCommandResult.Error("claims:plot_trade_unknown_audience");
            }

            City target = null;
            if (audience == EnumPlotSaleAudience.SPECIFIC_CITY)
            {
                string cityName = args.Parsers.Count > 2 && args.Parsers[2].GetValue() != null
                    ? Filter.filterName((string)args.Parsers[2].GetValue())
                    : "";
                if (!claims.dataStorage.GetCityByName(cityName, out target))
                {
                    return TextCommandResult.Error("claims:no_such_city");
                }
                if (target.Equals(playerInfo.City)) return TextCommandResult.Error("claims:plot_trade_not_to_self");
            }

            PlotMarketHelper.List(plot, price, audience, target);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return SuccessWithParams("claims:plot_trade_listed", new object[] { price, Lang.Get(audience.LangKey()) });
        }

        /// <summary>/plot citynfs - takes the plot the caller stands on off the market.</summary>
        public static TextCommandResult SetNotForSaleToCities(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            if (!claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }
            if (!plot.hasCity() || !plot.getCity().Equals(playerInfo.City))
            {
                return TextCommandResult.Error("claims:not_your_city");
            }
            if (!plot.IsForSaleForCity) return TextCommandResult.Error("claims:plot_trade_not_listed");

            PlotMarketHelper.Unlist(plot);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return TextCommandResult.Success("claims:plot_trade_unlisted");
        }

        /// <summary>
        /// /plot citybuy [x] [z] - buys the listing. Without coordinates it is the plot the caller
        /// stands on; with them it is a remote purchase, which the host may have switched off.
        /// </summary>
        public static TextCommandResult BuyPlotAsCity(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            PlotPosition here = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            PlotPosition wanted = here;

            // Coordinates are plot coordinates, the same ones the market tab lists.
            if (args.Parsers.Count > 1 && args.Parsers[0].GetValue() != null && args.Parsers[1].GetValue() != null)
            {
                wanted = new PlotPosition(System.Convert.ToInt32(args.Parsers[0].GetValue()),
                                          System.Convert.ToInt32(args.Parsers[1].GetValue()));
            }
            if (!wanted.Equals(here) && !claims.config.CITY_PLOT_TRADE_REMOTE_BUY)
            {
                return TextCommandResult.Error("claims:plot_trade_come_in_person");
            }

            if (!claims.dataStorage.GetPlot(wanted, out Plot plot))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }
            if (!PlotMarketHelper.CanBuy(plot, playerInfo.City, out string errorKey))
            {
                return TextCommandResult.Error(errorKey);
            }

            if (!PlotMarketHelper.Buy(plot, playerInfo.City, out string buyError))
            {
                return TextCommandResult.Error(buyError);
            }
            // Only refresh the "plot underfoot" panel when the buyer really is standing here: after a
            // remote purchase it would show a plot on the other side of the world as the current one.
            if (wanted.Equals(here))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            }
            // Buy() already told both cities what happened; saying it again here would print the
            // purchase twice for the player who pressed the button.
            return TextCommandResult.Success();
        }

        /// <summary>
        /// Rejects an unknown word rather than quietly listing to somebody. Omitting the word means
        /// allies only - the narrowest audience, so a forgotten argument cannot open the offer up.
        /// </summary>
        private static bool TryParseAudience(string word, out EnumPlotSaleAudience audience)
        {
            switch ((word ?? "allies").ToLowerInvariant())
            {
                case "everyone": audience = EnumPlotSaleAudience.EVERYONE; return true;
                case "nonhostile": audience = EnumPlotSaleAudience.NON_HOSTILE; return true;
                case "allies": audience = EnumPlotSaleAudience.ALLIES; return true;
                case "city": audience = EnumPlotSaleAudience.SPECIFIC_CITY; return true;
                default: audience = EnumPlotSaleAudience.ALLIES; return false;
            }
        }

    }
}
