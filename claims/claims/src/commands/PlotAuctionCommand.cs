using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.part.structure.plots.auction;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.commands
{
    /// <summary>
    /// Commands behind the land auction: a city puts a plot up for bids, other cities bid on it.
    /// Every rule is judged by <see cref="AuctionHandler"/>; this only resolves the arguments.
    /// </summary>
    public class PlotAuctionCommand : BaseCommand
    {
        /// <summary>/plot auction &lt;startPrice&gt; &lt;hours&gt; [increment] [buyout] [audience] [cityName]</summary>
        public static TextCommandResult StartAuction(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            if (!claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }
            if (!AuctionRules.CanCreateForPlot(plot, playerInfo.City, timed: true, out string errorKey))
            {
                return TextCommandResult.Error(errorKey);
            }

            int startPrice = (int)args.Parsers[0].GetValue();
            if (startPrice < 0) return TextCommandResult.Error("claims:try_pos");

            int hours = (int)args.Parsers[1].GetValue();
            if (hours < claims.config.AUCTION_MIN_HOURS || hours > claims.config.AUCTION_MAX_HOURS)
            {
                return ErrorWithParams("claims:plot_auction_bad_duration",
                    new object[] { claims.config.AUCTION_MIN_HOURS, claims.config.AUCTION_MAX_HOURS });
            }

            int increment = args.Parsers.Count > 2 && args.Parsers[2].GetValue() != null
                ? System.Convert.ToInt32(args.Parsers[2].GetValue())
                : claims.config.AUCTION_MIN_INCREMENT;
            if (increment < claims.config.AUCTION_MIN_INCREMENT) increment = claims.config.AUCTION_MIN_INCREMENT;

            int buyout = args.Parsers.Count > 3 && args.Parsers[3].GetValue() != null
                ? System.Convert.ToInt32(args.Parsers[3].GetValue())
                : -1;
            // A buyout below the starting price would end the lot on the first bid.
            if (buyout > 0 && buyout < startPrice) return TextCommandResult.Error("claims:plot_auction_bad_buyout");

            string audienceWord = args.Parsers.Count > 4 ? args.Parsers[4].GetValue() as string : null;
            if (!PlotMarketCommand.TryParseAudience(audienceWord, out EnumPlotSaleAudience audience))
            {
                return TextCommandResult.Error("claims:plot_trade_unknown_audience");
            }

            City target = null;
            if (audience == EnumPlotSaleAudience.SPECIFIC_CITY)
            {
                string cityName = args.Parsers.Count > 5 && args.Parsers[5].GetValue() != null
                    ? Filter.filterName((string)args.Parsers[5].GetValue())
                    : "";
                if (!claims.dataStorage.GetCityByName(cityName, out target))
                {
                    return TextCommandResult.Error("claims:no_such_city");
                }
                if (target.Equals(playerInfo.City)) return TextCommandResult.Error("claims:plot_trade_not_to_self");
            }

            AuctionHandler.CreateForPlot(plot, playerInfo.City, startPrice, hours, increment, buyout, audience, target);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return SuccessWithParams("claims:plot_auction_started",
                new object[] { startPrice, hours, Lang.Get(audience.LangKey()) });
        }

        /// <summary>/plot auctioncancel - pulls the lot on the plot the caller stands on.</summary>
        public static TextCommandResult CancelAuction(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            if (!claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }
            if (!AuctionRegistry.TryGetRunningFor(plot, out PlotAuction auction))
            {
                return TextCommandResult.Error("claims:plot_auction_not_running");
            }
            if (!AuctionHandler.CancelBySeller(auction, playerInfo.City, out string errorKey))
            {
                return TextCommandResult.Error(errorKey);
            }
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return TextCommandResult.Success("claims:plot_auction_cancelled");
        }

        /// <summary>
        /// /plot bid &lt;amount&gt; [x] [z] - bids on the lot underfoot, or on a distant one when the
        /// host allows remote deals at all.
        /// </summary>
        public static TextCommandResult PlaceBid(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            long amount = System.Convert.ToInt64(args.Parsers[0].GetValue());
            if (amount <= 0) return TextCommandResult.Error("claims:try_pos");

            if (!TryResolveLot(args, player, 1, out PlotAuction auction, out Plot plot, out TextCommandResult error))
            {
                return error;
            }
            if (!AuctionHandler.PlaceBid(auction, playerInfo.City, amount, out string errorKey))
            {
                return TextCommandResult.Error(errorKey);
            }
            // Only when the bidder really is standing on the lot: after a bid placed by coordinates
            // this panel would start describing a plot on the other side of the world as the one
            // underfoot.
            if (plot.plotPosition.Equals(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z)))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            }
            // PlaceBid already told the seller and the outbid city; repeating it here would print the
            // bid twice for the player who placed it.
            return TextCommandResult.Success();
        }

        /// <summary>
        /// /plot bidcity &lt;cityName&gt; &lt;amount&gt; - bids on a bankrupt settlement put up whole. Such a
        /// lot stands on no plot, so there is nowhere to walk to and it is addressed by name.
        /// </summary>
        public static TextCommandResult BidOnCity(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");

            string cityName = Filter.filterName((string)args.Parsers[0].GetValue() ?? "");
            if (!claims.dataStorage.GetCityByName(cityName, out City lotCity))
            {
                return TextCommandResult.Error("claims:no_such_city");
            }
            if (!AuctionRegistry.TryGetRunningForCity(lotCity, out PlotAuction auction))
            {
                return TextCommandResult.Error("claims:plot_auction_not_running");
            }

            long amount = System.Convert.ToInt64(args.Parsers[1].GetValue());
            if (amount <= 0) return TextCommandResult.Error("claims:try_pos");

            if (!AuctionHandler.PlaceBid(auction, playerInfo.City, amount, out string errorKey))
            {
                return TextCommandResult.Error(errorKey);
            }
            return TextCommandResult.Success();
        }

        /// <summary>/plot auctioninfo [x] [z] - the state of a lot.</summary>
        public static TextCommandResult AuctionInfo(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!TryResolveLot(args, player, 0, out PlotAuction auction, out _, out TextCommandResult error))
            {
                return error;
            }

            // An offer addressed to allies or to one named city is not public knowledge: standing on
            // the plot must not tell an enemy what it is fetching and from whom.
            City viewer = playerInfo.hasCity() ? playerInfo.City : null;
            bool ours = viewer != null && viewer.Guid == auction.SellerCityGuid;
            if (!ours && !AuctionRules.IsVisibleTo(auction, viewer))
            {
                return TextCommandResult.Error("claims:plot_trade_not_for_you");
            }

            string seller = auction.TryGetSeller(out City sellerCity) ? sellerCity.getPartNameReplaceUnder() : "?";
            string leader = auction.TryGetLeader(out City leaderCity)
                ? leaderCity.getPartNameReplaceUnder()
                : Lang.Get("claims:plot_auction_no_bids");
            long left = auction.SecondsLeft(TimeFunctions.getEpochSeconds());

            // A price tag never runs out, so reporting "0 h 0 min left" would read as expired.
            if (auction.IsFixedPrice)
            {
                return SuccessWithParams("claims:plot_auction_info_fixed",
                    new object[] { AuctionHandler.LotName(auction), seller, auction.BuyoutPrice });
            }

            return SuccessWithParams("claims:plot_auction_info", new object[]
            {
                AuctionHandler.LotName(auction),
                seller,
                auction.HasBid ? auction.CurrentBid : auction.StartPrice,
                leader,
                auction.MinNextBid,
                auction.HasBuyout ? auction.BuyoutPrice.ToString() : "-",
                left / 3600, (left % 3600) / 60
            });
        }

        /// <summary>
        /// Finds the lot the command is about: the one underfoot, or the one at the given plot
        /// coordinates. Distant lots follow the same host switch as distant purchases - a mayor who
        /// must travel to buy must travel to bid.
        /// </summary>
        private static bool TryResolveLot(TextCommandCallingArgs args, IPlayer player, int firstCoordParser,
            out PlotAuction auction, out Plot plot, out TextCommandResult error)
        {
            auction = null;
            plot = null;
            error = null;

            PlotPosition here = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            PlotPosition wanted = here;
            if (args.Parsers.Count > firstCoordParser + 1
                && args.Parsers[firstCoordParser].GetValue() != null
                && args.Parsers[firstCoordParser + 1].GetValue() != null)
            {
                wanted = new PlotPosition(System.Convert.ToInt32(args.Parsers[firstCoordParser].GetValue()),
                                          System.Convert.ToInt32(args.Parsers[firstCoordParser + 1].GetValue()));
            }
            if (!wanted.Equals(here) && !claims.config.CITY_PLOT_TRADE_REMOTE_BUY)
            {
                error = TextCommandResult.Error("claims:plot_trade_come_in_person");
                return false;
            }
            if (!claims.dataStorage.GetPlot(wanted, out plot))
            {
                error = TextCommandResult.Error("claims:plot_not_claimed");
                return false;
            }
            if (!AuctionRegistry.TryGetRunningFor(plot, out auction))
            {
                error = TextCommandResult.Error("claims:plot_auction_not_running");
                return false;
            }
            return true;
        }
    }
}
