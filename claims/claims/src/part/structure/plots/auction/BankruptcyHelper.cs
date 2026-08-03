using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.economy;
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

            // Land cannot change hands while its city is at war - PlotSaleRules says so, and every
            // lot would be refused when it closed. Tearing the city down instead would decide a
            // siege by bookkeeping, so the debt waits for the peace.
            if (PlotSaleRules.IsAtWar(city)) return true;

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

            bool wholeCity = Mode == ModeWholeCity;
            int opened = wholeCity ? OpenWholeCityLot(city) : OpenPlotLots(city);
            if (opened == 0) return false;

            city.AddLogEntry(EnumCityLogEvent.CityBankrupt, city.DebtBalance.ToString("0"));
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG);
            // Announced differently: "1 plots go under the hammer" is not what happens when the whole
            // settlement is the lot.
            MessageHandler.sendGlobalMsg(wholeCity
                ? Lang.Get("claims:bankruptcy_started_whole", city.getPartNameReplaceUnder())
                : Lang.Get("claims:bankruptcy_started", city.getPartNameReplaceUnder(), opened));
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
            // Counted in the mod's own days, the ones the fee is charged in - a host who made a day
            // an hour long would otherwise get a window of a hundred and sixty-eight billing days.
            long since = TimeFunctions.getEpochSeconds()
                - (claims.config.CITY_BANKRUPTCY_GRACE_DAYS * 2L + 1L) * claims.config.MOD_DAY_DURATION_IN_SECONDS;
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

                if (AuctionRegistry.TryGetRunningFor(plot, out PlotAuction standing))
                {
                    // Bids are already riding on a timed lot, so it is left alone - but it now counts
                    // as part of the fire sale, or a city that had put its whole land up voluntarily
                    // would look like it had nothing to sell and go straight to the wrecking ball.
                    if (!standing.IsFixedPrice)
                    {
                        standing.Reason = EnumAuctionReason.BANKRUPTCY;
                        standing.saveToDatabase();
                        opened++;
                        continue;
                    }
                    // A price tag never closes on its own, so counting it would postpone demolition
                    // forever. It is withdrawn and the plot goes under the hammer with the rest.
                    AuctionHandler.CancelLot(standing, "claims:plot_auction_cancelled_lot_gone");
                }

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
            // Before the shell goes down with its treasury: what the winner paid is sitting on the
            // bankrupt account, and demolition deletes that account.
            PayOutToCitizens(bankrupt);
            // The shell goes last: demolishing it first would take the plots with it.
            PartDemolition.demolishCity(bankrupt, Lang.Get("claims:bankruptcy_absorbed_reason",
                winner.getPartNameReplaceUnder()));
        }

        /// <summary>
        /// Hands what is left of the bankrupt treasury to the people who just lost their city, split
        /// evenly, the remainder to the mayor. The debt that started all this is written off the top:
        /// the sale was held to cover it.
        ///
        /// Without this the winner's money would sit on the bankrupt account for the one moment
        /// between the sale and the demolition that deletes it - paid by somebody, received by
        /// nobody. Needs real wallets; with a stubbed economy there is nowhere to pay it.
        /// </summary>
        private static void PayOutToCitizens(City bankrupt)
        {
            if (!claims.economyProvider.SupportsPlayerWallet) return;

            // The debt is settled first, the same way the daily fee settles it: withdrawn from the
            // treasury into nothing, since it is owed to no one in particular. Only what the sale
            // fetched on top of it belongs to the citizens.
            decimal balance = claims.economyProvider.GetBalance(bankrupt.MoneyAccountName);
            decimal debt = (decimal)Math.Max(0, bankrupt.DebtBalance);
            if (debt > 0)
            {
                decimal repaid = Math.Min(balance, debt);
                if (claims.economyProvider.Withdraw(bankrupt.MoneyAccountName, repaid) == MoneyOperationResult.Success)
                {
                    bankrupt.DebtBalance -= (double)repaid;
                }
            }

            decimal left = claims.economyProvider.GetBalance(bankrupt.MoneyAccountName);
            if (left <= 0) return;

            PlayerInfo[] citizens = bankrupt.getCityCitizens().ToArray();
            if (citizens.Length == 0) return;

            long share = (long)(left / citizens.Length);
            long paid = 0;
            if (share > 0)
            {
                foreach (PlayerInfo citizen in citizens)
                {
                    if (claims.economyProvider.Transfer(bankrupt.MoneyAccountName,
                            citizen.MoneyAccountName, share) != MoneyOperationResult.Success)
                    {
                        continue;
                    }
                    paid += share;
                    MessageHandler.sendMsgToPlayerInfo(citizen, Lang.Get("claims:bankruptcy_payout", share));
                }
            }

            // Whatever the split could not divide - or could not deliver - goes to the mayor rather
            // than down with the city.
            decimal rest = left - paid;
            PlayerInfo mayor = bankrupt.getMayor();
            if (rest > 0 && mayor != null)
            {
                if (claims.economyProvider.Transfer(bankrupt.MoneyAccountName,
                        mayor.MoneyAccountName, rest) == MoneyOperationResult.Success)
                {
                    MessageHandler.sendMsgToPlayerInfo(mayor, Lang.Get("claims:bankruptcy_payout", rest));
                }
            }
        }

        /// <summary>
        /// How long a forced lot runs - the grace period, within the auction's own bounds. Measured
        /// in the mod's days rather than real ones: the debt that started this grows once per those,
        /// so a sale lasting "three days" must not outlive three billing rounds.
        /// </summary>
        private static int LotHours()
        {
            long graceSeconds = claims.config.CITY_BANKRUPTCY_GRACE_DAYS * claims.config.MOD_DAY_DURATION_IN_SECONDS;
            int hours = (int)Math.Max(1, graceSeconds / 3600);
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
