using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.messages;
using Vintagestory.API.Config;

namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// What happens to a city that cannot pay its debts. By default it is simply demolished, which
    /// makes a settlement disappear overnight with nothing to show for it. Switched on, the city is
    /// sold off instead: its land goes to whoever bids, the proceeds pay the debt down, and a city
    /// that sheds enough plots may survive at a smaller size.
    /// </summary>
    public static class BankruptcyHelper
    {
        private const string ModeOff = "off";
        private const string ModePlots = "plots";
        private const string ModeWholeCity = "whole_city";

        private static string Mode => (claims.config.CITY_BANKRUPTCY_MODE ?? ModeOff).ToLowerInvariant();

        private static bool subscribed;

        /// <summary>Starts listening for won whole-city lots. Called once on server start.</summary>
        public static void Init()
        {
            if (subscribed) return;
            subscribed = true;
            AuctionHandler.WholeCityLotWon += AwardWholeCity;
        }

        /// <summary>
        /// A fire sale needs somebody able to bid and an economy where money means something, so it
        /// falls back to demolition on the same conditions the auction itself is off.
        /// </summary>
        public static bool IsEnabled => Mode != ModeOff && AuctionRules.IsEnabled;

        /// <summary>
        /// Called instead of demolishing an insolvent city. Returns false when the city should be
        /// demolished after all - the feature is off, the sale is over and the debt still stands, or
        /// there is nothing left to sell.
        /// </summary>
        public static bool Begin(City city)
        {
            if (city == null || !IsEnabled) return false;
            // A village pays no fees and holds no treasury, so it never gets here by debt; guard
            // anyway, since its plots are not market goods.
            if (city.IsVillage() || city.isTechnicalCity()) return false;

            List<PlotAuction> running = GetForcedLots(city);
            if (running.Count > 0)
            {
                // The sale is still going. Demolition waits for it, however long the grace period.
                return true;
            }

            // No lots left. Either the sale never started, or it just ended without covering the debt.
            if (HasRecentSale(city))
            {
                // The land went under the hammer and the debt is still here: nothing more to sell.
                return false;
            }

            int opened = Mode == ModeWholeCity ? OpenWholeCityLot(city) : OpenPlotLots(city);
            if (opened == 0) return false;

            city.AddLogEntry(EnumCityLogEvent.CityBankrupt, city.DebtBalance.ToString("0"));
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG);
            MessageHandler.sendGlobalMsg(Lang.Get("claims:bankruptcy_started",
                city.getPartNameReplaceUnder(), opened));
            return true;
        }

        /// <summary>Forced lots of this city that are still open.</summary>
        private static List<PlotAuction> GetForcedLots(City city)
        {
            var result = new List<PlotAuction>();
            foreach (PlotAuction it in AuctionRegistry.All)
            {
                if (!it.IsRunning || it.Reason != EnumAuctionReason.BANKRUPTCY) continue;
                if (it.SellerCityGuid == city.Guid || it.LotCityGuid == city.Guid) result.Add(it);
            }
            return result;
        }

        /// <summary>
        /// Whether this city has just been through a forced sale. Bounded in time on purpose: a city
        /// that survived a bankruptcy years ago, recovered and went broke again deserves the same
        /// chance as any other, and a lifetime check would send it straight to the wrecking ball.
        /// </summary>
        private static bool HasRecentSale(City city)
        {
            long since = TimeFunctions.getEpochSeconds()
                - (claims.config.CITY_BANKRUPTCY_GRACE_DAYS * 2L + 1L) * 24L * 3600L;
            foreach (PlotAuction it in AuctionRegistry.All)
            {
                if (it.Reason != EnumAuctionReason.BANKRUPTCY) continue;
                if (it.StartedAt < since) continue;
                if (it.SellerCityGuid == city.Guid || it.LotCityGuid == city.Guid) return true;
            }
            return false;
        }

        /// <summary>
        /// Puts every sellable plot of the city up separately. The main plot, plots a citizen owns
        /// and plots inside a group are not sold - the same rules a voluntary sale follows, so a
        /// bankruptcy cannot take a citizen's buildings out from under them.
        /// </summary>
        private static int OpenPlotLots(City city)
        {
            int hours = LotHours();
            long startPrice = StartPrice();
            int opened = 0;

            foreach (Plot plot in city.getCityPlots().ToArray())
            {
                if (!PlotSaleRules.CanSellPlot(plot, city, out _)) continue;
                // A plot the city already put up - at a price or for bids - is on the block anyway;
                // replacing that offer would throw away bids already made on it.
                if (AuctionRegistry.HasRunningFor(plot)) continue;

                // The reason is part of the lot from the first row on: the sale finds its own lots by
                // it, and a lot written as NORMAL would be invisible to the bankruptcy that made it.
                AuctionHandler.CreateForPlot(plot, city, startPrice, hours,
                    claims.config.AUCTION_MIN_INCREMENT, -1, EnumPlotSaleAudience.EVERYONE, null,
                    EnumAuctionReason.BANKRUPTCY);
                opened++;
            }
            return opened;
        }

        /// <summary>Puts the settlement up as one lot. The winner takes the whole of it.</summary>
        private static int OpenWholeCityLot(City city)
        {
            long now = TimeFunctions.getEpochSeconds();
            string guid = Guid.NewGuid().ToString();
            var lot = new PlotAuction(guid, guid)
            {
                Kind = EnumAuctionKind.WHOLE_CITY,
                LotCityGuid = city.Guid,
                SellerCityGuid = city.Guid,
                StartPrice = StartPrice() * Math.Max(1, city.getCityPlots().Count),
                MinIncrement = claims.config.AUCTION_MIN_INCREMENT,
                BuyoutPrice = -1,
                StartedAt = now,
                EndsAt = now + LotHours() * 3600L,
                Audience = EnumPlotSaleAudience.EVERYONE,
                Reason = EnumAuctionReason.BANKRUPTCY,
                State = EnumAuctionState.RUNNING
            };

            AuctionRegistry.Add(lot);
            AuctionEscrow.Open(lot);
            AuctionHandler.Schedule(lot);
            AuctionHandler.NotifyAuctionsChanged();
            return 1;
        }

        /// <summary>
        /// Hands a whole city to the winner: every plot moves over, then the empty shell is taken
        /// down. The buyer's plot limit is deliberately not checked - a limit would make absorbing a
        /// city impossible, which is the one thing this lot is for.
        /// </summary>
        private static void AwardWholeCity(PlotAuction auction, City bankrupt, City winner)
        {
            if (bankrupt == null || winner == null) return;
            if (auction.Reason != EnumAuctionReason.BANKRUPTCY) return;

            foreach (Plot plot in bankrupt.getCityPlots().ToArray())
            {
                PlotTransferHelper.Transfer(plot, bankrupt, winner, markCaptured: false);
            }

            winner.AddLogEntry(EnumCityLogEvent.CityAbsorbed, bankrupt.GetPartName(),
                auction.CurrentBid.ToString());
            UsefullPacketsSend.AddToQueueCityInfoUpdate(winner.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG);
            MessageHandler.sendGlobalMsg(Lang.Get("claims:bankruptcy_absorbed",
                winner.getPartNameReplaceUnder(), bankrupt.getPartNameReplaceUnder(), auction.CurrentBid));

            winner.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);
            // The shell goes last: demolishing it first would take the plots with it.
            PartDemolition.demolishCity(bankrupt, Lang.Get("claims:bankruptcy_absorbed_reason",
                winner.getPartNameReplaceUnder()));
        }

        /// <summary>How long a forced lot runs - the grace period, within the auction's own bounds.</summary>
        private static int LotHours()
        {
            int hours = claims.config.CITY_BANKRUPTCY_GRACE_DAYS * 24;
            if (hours < claims.config.AUCTION_MIN_HOURS) hours = claims.config.AUCTION_MIN_HOURS;
            if (hours > claims.config.AUCTION_MAX_HOURS) hours = claims.config.AUCTION_MAX_HOURS;
            return hours;
        }

        private static long StartPrice()
        {
            double price = claims.config.PLOT_CLAIM_PRICE * claims.config.CITY_BANKRUPTCY_START_PRICE_FACTOR;
            return (long)Math.Max(0, Math.Round(price));
        }
    }
}
