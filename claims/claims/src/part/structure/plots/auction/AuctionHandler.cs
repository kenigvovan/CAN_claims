using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.messages;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// The life of a lot: opening it, taking bids into escrow, closing it when its time is up.
    ///
    /// Who may do what is <see cref="AuctionRules"/>; which lots are open is
    /// <see cref="AuctionRegistry"/>. What is left here is the sequence of steps and the money.
    /// </summary>
    public static class AuctionHandler
    {
        /// <summary>
        /// A whole-settlement lot found a winner. Handing a city over is not the auction's business,
        /// so whoever put the lot up listens for this.
        /// </summary>
        public static event Action<PlotAuction, City, City> WholeCityLotWon;

        private static bool subscribed;

        /// <summary>Hooks the events lots react to. Called once on server start.</summary>
        public static void Init()
        {
            if (subscribed) return;
            subscribed = true;
            PlotTransferHelper.PlotLeavingCity += OnPlotLeftCity;
        }

        /*==========================================================================================*/
        /*=====================================OPENING A LOT========================================*/
        /*==========================================================================================*/

        /// <summary>
        /// Puts a plot up for bids for a while. Caller has already run
        /// <see cref="AuctionRules.CanCreateForPlot"/>; hours and the increment are clamped here so a
        /// command and the GUI cannot disagree about the bounds.
        /// </summary>
        public static PlotAuction CreateForPlot(Plot plot, City seller, long startPrice, int hours,
            long minIncrement, long buyoutPrice, EnumPlotSaleAudience audience, City targetCity,
            EnumAuctionReason reason = EnumAuctionReason.NORMAL)
        {
            hours = GameMath.Clamp(hours, claims.config.AUCTION_MIN_HOURS, claims.config.AUCTION_MAX_HOURS);
            return Create(plot, seller, startPrice, hours * 3600L,
                Math.Max(claims.config.AUCTION_MIN_INCREMENT, minIncrement),
                buyoutPrice > 0 ? buyoutPrice : -1, audience, targetCity, reason);
        }

        /// <summary>
        /// Puts a plot up at a plain price: no closing time, buyout equal to the asking price, so the
        /// first city that pays it takes the plot. The fixed-price market is this shape of lot.
        /// </summary>
        public static PlotAuction CreateFixedPrice(Plot plot, City seller, long price,
            EnumPlotSaleAudience audience, City targetCity)
        {
            return Create(plot, seller, price, 0, claims.config.AUCTION_MIN_INCREMENT,
                Math.Max(0, price), audience, targetCity);
        }

        /// <summary>
        /// Changes the price or audience of a standing price tag in place. Price tags only: a lot with
        /// bids cannot be re-priced under the cities that bid.
        /// </summary>
        public static void RepriceFixedLot(PlotAuction auction, long price,
            EnumPlotSaleAudience audience, City targetCity)
        {
            if (auction == null || !auction.IsRunning || !auction.IsFixedPrice) return;
            // A price tag normally closes on the bid that meets it, so it has no bids to speak of -
            // except one whose payout failed and is waiting to be retried. Re-pricing that would
            // change what the waiting buyer pays after their money is already held.
            if (auction.HasBid) return;

            auction.StartPrice = Math.Max(0, price);
            auction.BuyoutPrice = auction.StartPrice;
            auction.Audience = audience;
            auction.TargetCityGuid = audience == EnumPlotSaleAudience.SPECIFIC_CITY ? targetCity?.Guid ?? "" : "";
            auction.saveToDatabase();

            if (auction.TryGetPlot(out Plot plot))
            {
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            }
            NotifyAuctionsChanged(auction);
        }

        private static PlotAuction Create(Plot plot, City seller, long startPrice, long durationSeconds,
            long minIncrement, long buyoutPrice, EnumPlotSaleAudience audience, City targetCity,
            EnumAuctionReason reason = EnumAuctionReason.NORMAL)
        {
            long now = TimeFunctions.getEpochSeconds();
            string guid = Guid.NewGuid().ToString();

            var auction = new PlotAuction(guid, guid)
            {
                Kind = EnumAuctionKind.PLOT,
                PlotX = plot.plotPosition.X,
                PlotZ = plot.plotPosition.Z,
                SellerCityGuid = seller.Guid,
                StartPrice = Math.Max(0, startPrice),
                MinIncrement = Math.Max(1, minIncrement),
                BuyoutPrice = buyoutPrice,
                StartedAt = now,
                EndsAt = durationSeconds > 0 ? now + durationSeconds : 0,
                Audience = audience,
                TargetCityGuid = audience == EnumPlotSaleAudience.SPECIFIC_CITY ? targetCity?.Guid ?? "" : "",
                Reason = reason,
                State = EnumAuctionState.RUNNING
            };

            AuctionRegistry.Add(auction);
            AuctionEscrow.Open(auction);
            Schedule(auction);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            NotifyAuctionsChanged(auction);
            WarnSeller(auction, plot, seller, audience);
            return auction;
        }

        /// <summary>Two things a seller learns too late otherwise.</summary>
        private static void WarnSeller(PlotAuction auction, Plot plot, City seller, EnumPlotSaleAudience audience)
        {
            // "Allies only" is the default, and a city with no alliance addresses nobody with it.
            if (audience == EnumPlotSaleAudience.ALLIES && !seller.HasAlliance())
            {
                MessageHandler.sendMsgInCity(seller, Lang.Get("claims:plot_trade_audience_no_allies"));
            }
            // Handing the plot over resets its type, tearing down whatever stood on it.
            if (plot.Type == PlotType.PRISON || plot.Type == PlotType.SUMMON || plot.Type == PlotType.TEMPLE)
            {
                MessageHandler.sendMsgInCity(seller, Lang.Get("claims:plot_trade_special_type_warning"));
            }
        }

        /*==========================================================================================*/
        /*=====================================BIDDING==============================================*/
        /*==========================================================================================*/

        /// <summary>
        /// Takes a bid: the money moves into escrow first, and only then does the lot change hands on
        /// paper. The previous leader is refunded from the same escrow.
        /// </summary>
        public static bool PlaceBid(PlotAuction auction, City bidder, long amount, out string errorKey)
        {
            if (!AuctionRules.CanBid(auction, bidder, amount, out errorKey)) return false;

            // A bid at or above the buyout is a buyout, whatever the player typed.
            bool buyout = auction.HasBuyout && amount >= auction.BuyoutPrice;
            if (buyout) amount = auction.BuyoutPrice;

            if (!AuctionEscrow.Hold(auction, bidder, amount))
            {
                errorKey = "claims:economy_money_transaction_error";
                return false;
            }

            // Refund the outbid city only after the new bid is safely in escrow: the other order
            // would leave the lot unbacked for a moment if the new hold then failed.
            if (auction.HasBid && auction.TryGetLeader(out City previous))
            {
                AuctionEscrow.Refund(auction, previous, auction.CurrentBid);
                MessageHandler.sendMsgInCity(previous,
                    Lang.Get("claims:plot_auction_outbid", LotName(auction), amount));
            }

            long now = TimeFunctions.getEpochSeconds();
            auction.CurrentBid = amount;
            auction.CurrentBidderCityGuid = bidder.Guid;

            var record = new AuctionBidRecord(Guid.NewGuid().ToString(), auction.Guid, bidder, amount, now);
            auction.Bids.Add(record);
            claims.getModInstance().getDatabaseHandler().saveAuctionBid(record);

            // Anti-sniping: a late bid pushes the end back by the same window, so the lot cannot be
            // decided by whoever clicks last in the final second.
            long window = claims.config.AUCTION_EXTEND_WINDOW_SECONDS;
            if (!buyout && !auction.IsOpenEnded && window > 0 && auction.EndsAt - now < window)
            {
                auction.EndsAt = now + window;
            }
            auction.saveToDatabase();

            if (auction.TryGetSeller(out City seller))
            {
                MessageHandler.sendMsgInCity(seller,
                    Lang.Get("claims:plot_auction_new_bid", LotName(auction),
                        bidder.getPartNameReplaceUnder(), amount));
            }

            if (buyout)
            {
                CloseLot(auction);
            }
            else
            {
                // Anyone standing on the plot sees the old bid in their "plot underfoot" panel.
                if (auction.TryGetPlot(out Plot bidPlot))
                {
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(bidPlot.getPos());
                }
                NotifyAuctionsChanged(auction);
            }
            return true;
        }

        /*==========================================================================================*/
        /*=====================================SCHEDULING===========================================*/
        /*==========================================================================================*/

        /// <summary>
        /// Which callback each lot is currently waiting on. A lot whose end was pushed back by a late
        /// bid must not end up with two.
        ///
        /// The number is there because a callback cannot be called off once armed: a lot that closes
        /// and reopens - a failed payout does exactly that - leaves its old callback in flight, and
        /// without a way to tell the two apart the stale one would arm a third. Each arming gets its
        /// own number and a callback that no longer holds the current one simply goes away.
        /// </summary>
        private static readonly Dictionary<string, long> scheduled = new Dictionary<string, long>();

        private static long scheduleCounter;

        /// <summary>
        /// Rebuilds the index and arms every open lot. Called once after the world has loaded: a lot
        /// that ran out while the server was down closes on the first pass.
        /// </summary>
        public static void ScheduleAll()
        {
            AuctionRegistry.Rebuild();
            // Static state outlives the world: another world's guids would make Schedule refuse to
            // arm callbacks for lots it thinks are already waiting, and would carry failed-payout
            // counts over to lots that never failed here.
            scheduled.Clear();
            payoutAttempts.Clear();

            // A lot whose plot or seller is gone would sit in the index forever, blocking every future
            // offer on that ground - and an open-ended one has no callback to notice. The hooks
            // normally take such lots down; this catches what slipped past them.
            foreach (PlotAuction auction in AuctionRegistry.GetRunning())
            {
                if (!auction.TryGetSeller(out City seller))
                {
                    CancelLot(auction, "claims:plot_auction_cancelled_seller_gone");
                    continue;
                }
                if (auction.Kind == EnumAuctionKind.PLOT
                    && (!auction.TryGetPlot(out Plot plot) || !plot.hasCity() || !plot.getCity().Equals(seller)))
                {
                    CancelLot(auction, "claims:plot_auction_cancelled_lot_gone");
                    continue;
                }
                if (auction.Kind == EnumAuctionKind.WHOLE_CITY && !auction.TryGetLotCity(out _))
                {
                    CancelLot(auction, "claims:plot_auction_cancelled_lot_gone");
                    continue;
                }
                // A price tag carrying a bid is a purchase whose payout failed mid-retry: the retry
                // callback died with the old process, and nothing else ever wakes an open-ended lot.
                // Left alone it would hold the buyer's money forever - so the settlement resumes here.
                if (auction.IsOpenEnded && auction.HasBid)
                {
                    CloseLot(auction);
                    continue;
                }
                Schedule(auction);
            }
        }

        /// <summary>Arms the callback that closes this lot when its time is up.</summary>
        public static void Schedule(PlotAuction auction)
        {
            if (auction == null || !auction.IsRunning) return;
            // A price tag has no closing time: it ends when somebody buys it or the seller withdraws.
            if (auction.IsOpenEnded) return;
            if (scheduled.ContainsKey(auction.Guid)) return;

            long ticket = ++scheduleCounter;
            scheduled[auction.Guid] = ticket;

            // Capped at an hour: a week-long lot in milliseconds overflows an int. Waking early costs
            // nothing - the lot is simply not over yet and the next callback is armed again.
            long left = auction.SecondsLeft(TimeFunctions.getEpochSeconds());
            int delaySeconds = (int)Math.Min(left, 3600L);
            string guid = auction.Guid;
            claims.sapi.Event.RegisterCallback(_ => OnLotDue(guid, ticket), delaySeconds * 1000);
        }

        private static void OnLotDue(string auctionGuid, long ticket)
        {
            // A callback the lot has stopped waiting on - it was closed, or closed and armed anew.
            // Leaving the current ticket alone matters: removing it would let the live callback pass
            // for a stale one and the lot would never be woken again.
            if (!scheduled.TryGetValue(auctionGuid, out long current) || current != ticket) return;
            scheduled.Remove(auctionGuid);

            // The lot may have been bought out or cancelled while the callback was pending.
            if (!AuctionRegistry.TryGet(auctionGuid, out PlotAuction auction) || !auction.IsRunning) return;

            if (auction.HasExpired(TimeFunctions.getEpochSeconds())) CloseLot(auction);
            else Schedule(auction);
        }

        /*==========================================================================================*/
        /*=====================================CLOSING==============================================*/
        /*==========================================================================================*/

        /// <summary>
        /// Decides a lot. The leading bid wins if it still holds up; if it does not - the winner's
        /// city is gone, a war broke out, the plot is no longer sellable - the lot walks down its own
        /// bid log looking for a bidder who does, and expires if nobody qualifies.
        /// </summary>
        public static void CloseLot(PlotAuction auction)
        {
            if (auction == null || !auction.IsRunning) return;

            if (!auction.TryGetSeller(out City seller))
            {
                // The seller is gone: nothing to buy and nobody to pay. Give the money back.
                CancelLot(auction, "claims:plot_auction_cancelled_seller_gone");
                return;
            }

            if (auction.Kind == EnumAuctionKind.WHOLE_CITY)
            {
                CloseWholeCityLot(auction, seller);
                return;
            }

            if (!auction.TryGetPlot(out Plot plot) || !PlotSaleRules.CanSellPlot(plot, seller, out _))
            {
                CancelLot(auction, "claims:plot_auction_cancelled_lot_gone");
                return;
            }

            if (!auction.HasBid && auction.Bids.Count == 0)
            {
                ExpireLot(auction, seller, plot);
                return;
            }

            if (auction.HasBid)
            {
                // The leading bid is already in escrow: it costs nothing to accept.
                if (auction.TryGetLeader(out City leader)
                    && AuctionRules.IsVisibleTo(auction, leader)
                    && PlotSaleRules.CanTakePlot(plot, leader, out _))
                {
                    Award(auction, plot, seller, leader, auction.CurrentBid);
                    return;
                }
                // The leader no longer qualifies - give the held money back before looking further.
                if (auction.TryGetLeader(out City goneLeader))
                {
                    AuctionEscrow.Refund(auction, goneLeader, auction.CurrentBid);
                }
            }

            // Walk back down the bid log. Everyone below the leader was refunded, so taking their bid
            // means charging them again - which re-checks that they can afford it.
            for (int i = auction.Bids.Count - 1; i >= 0; i--)
            {
                AuctionBidRecord bid = auction.Bids[i];
                if (bid.CityGuid == auction.CurrentBidderCityGuid) continue;
                if (!claims.dataStorage.getCityByGUID(bid.CityGuid, out City candidate)) continue;
                if (!AuctionRules.IsVisibleTo(auction, candidate)) continue;
                if (!PlotSaleRules.CanTakePlot(plot, candidate, out _)) continue;
                if (claims.economyProvider.GetBalance(candidate.MoneyAccountName) < bid.Amount) continue;
                if (!AuctionEscrow.Hold(auction, candidate, bid.Amount)) continue;

                auction.CurrentBid = bid.Amount;
                auction.CurrentBidderCityGuid = bid.CityGuid;
                Award(auction, plot, seller, candidate, bid.Amount);
                return;
            }

            // Nobody left who could take it.
            auction.CurrentBid = -1;
            auction.CurrentBidderCityGuid = "";
            ExpireLot(auction, seller, plot);
        }

        /// <summary>
        /// Settles a lot on a whole bankrupt city. The plot limit is not re-checked - absorbing a city
        /// always exceeds it - and the money goes into the bankrupt treasury, against its debt.
        /// </summary>
        private static void CloseWholeCityLot(PlotAuction auction, City seller)
        {
            if (!auction.TryGetLotCity(out City bankrupt))
            {
                CancelLot(auction, "claims:plot_auction_cancelled_lot_gone");
                return;
            }
            if (!auction.HasBid || !auction.TryGetLeader(out City winner)
                || !AuctionRules.IsVisibleTo(auction, winner)
                || !AuctionRules.CanOwnLand(winner, out _))
            {
                // A leader who no longer qualifies gets their bid back first: ExpireLot sweeps the
                // account to the holding pot, which is for money that could not be delivered, not for
                // money nobody tried to deliver. Same order as the plot-lot path.
                if (auction.HasBid && auction.TryGetLeader(out City disqualified))
                {
                    AuctionEscrow.Refund(auction, disqualified, auction.CurrentBid);
                }
                // Nobody took it: the caller's usual path - demolition - takes over from here.
                ExpireLot(auction, seller, null);
                return;
            }

            SetClosed(auction, EnumAuctionState.SOLD);
            if (!AuctionEscrow.Release(auction, seller, auction.CurrentBid))
            {
                ReopenAfterFailedPayout(auction);
                return;
            }
            payoutAttempts.Remove(auction.Guid);

            WholeCityLotWon?.Invoke(auction, bankrupt, winner);
            NotifyAuctionsChanged();
        }

        private static void Award(PlotAuction auction, Plot plot, City seller, City winner, long price)
        {
            SetClosed(auction, EnumAuctionState.SOLD);

            // Pay the seller out of escrow before the plot moves: a failed payout must not leave the
            // land already handed over. Only the winning bid is theirs - see AuctionEscrow.Release.
            if (!AuctionEscrow.Release(auction, seller, price))
            {
                ReopenAfterFailedPayout(auction);
                return;
            }
            payoutAttempts.Remove(auction.Guid);

            PlotDealCloser.Complete(plot, seller, winner, price);
            MessageHandler.sendMsgInCity(winner,
                Lang.Get("claims:plot_auction_won", LotName(auction), price));
            NotifyAuctionsChanged();
        }

        /// <summary>
        /// The seller could not be paid, so the deal is not done: the lot reopens and is retried
        /// shortly. A price tag keeps its open end - a deadline would turn a standing offer into an
        /// auction - and the waiting buyer's money stays in escrow either way.
        /// </summary>
        private static void ReopenAfterFailedPayout(PlotAuction auction)
        {
            // Retrying forever would spend the log on a deal that is never going to settle. After a
            // few attempts the lot is called off instead and the bid goes back to its city.
            if (!payoutAttempts.TryGetValue(auction.Guid, out int attempts)) attempts = 0;
            payoutAttempts[auction.Guid] = ++attempts;
            if (attempts > MaxPayoutAttempts)
            {
                payoutAttempts.Remove(auction.Guid);
                MessageHandler.sendErrorMsg("AuctionHandler: lot " + auction.Guid + " could not be paid out "
                    + MaxPayoutAttempts + " times, cancelling it");
                SetRunning(auction);
                CancelLot(auction, "claims:plot_auction_cancelled_lot_gone");
                return;
            }

            SetRunning(auction);
            if (!auction.IsOpenEnded)
            {
                auction.EndsAt = TimeFunctions.getEpochSeconds() + 60;
            }
            auction.saveToDatabase();
            Schedule(auction);

            if (auction.IsOpenEnded)
            {
                // Nothing wakes an open-ended lot up on its own, so the retry is armed by hand.
                string guid = auction.Guid;
                claims.sapi.Event.RegisterCallback(_ =>
                {
                    if (AuctionRegistry.TryGet(guid, out PlotAuction retried) && retried.IsRunning) CloseLot(retried);
                }, 60 * 1000);
            }
        }

        /// <summary>How many times a lot may fail to pay its seller before it is called off.</summary>
        private const int MaxPayoutAttempts = 5;

        /// <summary>Failed payouts per lot. In memory only: a restart is itself a fresh attempt.</summary>
        private static readonly Dictionary<string, int> payoutAttempts = new Dictionary<string, int>();

        private static void ExpireLot(PlotAuction auction, City seller, Plot plot)
        {
            SetClosed(auction, EnumAuctionState.EXPIRED);
            // Nothing was sold, so the seller is owed nothing. Anything still on the account is a
            // refund that never arrived, and it goes to the holding account rather than to them.
            AuctionEscrow.Sweep(auction);
            if (plot != null) claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            if (seller != null)
            {
                MessageHandler.sendMsgInCity(seller, Lang.Get("claims:plot_auction_expired", LotName(auction)));
            }
            NotifyAuctionsChanged();
        }

        /// <summary>
        /// Calls the lot off and gives the leading bid back. Used when the lot itself disappears - the
        /// plot was taken by war, ceded, handed to a citizen, or the seller was demolished.
        /// </summary>
        public static void CancelLot(PlotAuction auction, string reasonLangKey)
        {
            if (auction == null || !auction.IsRunning) return;
            SetClosed(auction, EnumAuctionState.CANCELLED);
            payoutAttempts.Remove(auction.Guid);

            if (auction.HasBid && auction.TryGetLeader(out City leader))
            {
                AuctionEscrow.Refund(auction, leader, auction.CurrentBid);
                MessageHandler.sendMsgInCity(leader, Lang.Get(reasonLangKey, LotName(auction)));
            }
            // Sweep rather than close: a refund that did not go through leaves money on the account,
            // and closing it would destroy somebody's bid. Sweep parks it where it can be found.
            AuctionEscrow.Sweep(auction);

            if (auction.TryGetSeller(out City seller))
            {
                MessageHandler.sendMsgInCity(seller, Lang.Get(reasonLangKey, LotName(auction)));
            }
            if (auction.TryGetPlot(out Plot plot))
            {
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            }
            NotifyAuctionsChanged();
        }

        /// <summary>
        /// Seller pulling their own lot. A lot with bids on it is not theirs to withdraw - cities
        /// that bid are owed the contest they paid into.
        ///
        /// Unless the leader could no longer take the plot anyway: a bid, followed by anything that
        /// disqualifies the bidder - declaring a war, filling their plot limit, leaving the alliance
        /// an "allies only" lot was addressed to - would otherwise let anyone freeze a stranger's
        /// plot for the whole run of the lot and get their money back at the end of it. The lot is
        /// not decided in their favour either way, so nothing is taken from them by letting go.
        /// </summary>
        public static bool CancelBySeller(PlotAuction auction, City seller, out string errorKey)
        {
            errorKey = null;
            if (auction == null || !auction.IsRunning) { errorKey = "claims:plot_auction_not_running"; return false; }
            if (seller == null || auction.SellerCityGuid != seller.Guid)
            {
                errorKey = "claims:not_your_city";
                return false;
            }
            if (auction.HasBid && LeaderStillQualifies(auction))
            {
                errorKey = "claims:plot_auction_has_bids";
                return false;
            }
            CancelLot(auction, "claims:plot_auction_cancelled_by_seller");
            return true;
        }

        /// <summary>Whether the leading city could still be handed the lot as things stand.</summary>
        private static bool LeaderStillQualifies(PlotAuction auction)
        {
            if (!auction.TryGetLeader(out City leader)) return false;
            if (!AuctionRules.IsVisibleTo(auction, leader)) return false;
            if (!AuctionRules.CanOwnLand(leader, out _)) return false;
            if (auction.Kind != EnumAuctionKind.PLOT) return true;
            return auction.TryGetPlot(out Plot plot) && PlotSaleRules.CanTakePlot(plot, leader, out _);
        }

        /// <summary>
        /// The one place a lot stops running: state, index and pending callback move together, so they
        /// cannot fall out of step.
        /// </summary>
        private static void SetClosed(PlotAuction auction, EnumAuctionState state)
        {
            auction.State = state;
            AuctionRegistry.Unindex(auction);
            scheduled.Remove(auction.Guid);
            // Not payoutAttempts: a lot that fails to pay out is closed and reopened on every try,
            // so clearing the count here would make the retry limit unreachable. It is cleared where
            // the money actually moves - and where the lot is called off.
            auction.saveToDatabase();
        }

        /// <summary>
        /// Puts a closed lot back on the market - the mirror of <see cref="SetClosed"/>, so state and
        /// index move together here too. The caller writes the row: it is still changing the lot.
        /// </summary>
        private static void SetRunning(PlotAuction auction)
        {
            auction.State = EnumAuctionState.RUNNING;
            AuctionRegistry.Index(auction);
        }

        /// <summary>
        /// Takes the leading bid off the lot and returns the money, leaving it open with no leader.
        /// The bidder's guid stays on the lot so the closing walk knows to skip them.
        /// </summary>
        private static void DropLeader(PlotAuction auction, City leader)
        {
            if (!auction.HasBid) return;
            AuctionEscrow.Refund(auction, leader, auction.CurrentBid);
            // The bid that just left is the new floor. Without this the lot would fall back to its
            // starting price and could be taken for less than cities had already offered for it.
            auction.StartPrice = Math.Max(auction.StartPrice, auction.CurrentBid);
            auction.CurrentBid = -1;
            auction.saveToDatabase();
            NotifyAuctionsChanged(auction);
        }

        /*==========================================================================================*/
        /*=====================================HOOKS================================================*/
        /*==========================================================================================*/

        /// <summary>
        /// The plot left its city some other way - a war capture, a cession, a handover to a citizen,
        /// an unclaim. Any lot on it is void.
        /// </summary>
        public static void OnPlotLeftCity(Plot plot)
        {
            if (plot == null) return;
            if (AuctionRegistry.TryGetRunningFor(plot, out PlotAuction auction))
            {
                CancelLot(auction, "claims:plot_auction_cancelled_lot_gone");
            }
        }

        /// <summary>
        /// A city is being demolished. Its own lots are void, and lots where it was leading lose that
        /// bid - escrow cannot be returned to a city that no longer has an account.
        /// </summary>
        public static void OnCityRemoved(City city)
        {
            if (city == null) return;
            foreach (PlotAuction auction in AuctionRegistry.GetRunning())
            {
                if (auction.SellerCityGuid == city.Guid || auction.LotCityGuid == city.Guid)
                {
                    CancelLot(auction, "claims:plot_auction_cancelled_seller_gone");
                    continue;
                }
                if (auction.CurrentBidderCityGuid == city.Guid)
                {
                    // Refund now, while the account still exists. The lot keeps running without a
                    // leader; its bid log is walked when it closes.
                    DropLeader(auction, city);
                }
            }
        }

        /*==========================================================================================*/
        /*=====================================MISC=================================================*/
        /*==========================================================================================*/

        /// <summary>
        /// Pushes the lot list to the cities it concerns - who may see a lot depends on the viewer.
        /// </summary>
        /// <param name="changed">
        /// The lot that moved, when it is a single one: then only the seller, the leader and the
        /// cities it is addressed to are told. Rebuilding the market for everyone re-judges every lot
        /// for every city. Pass null when the change is not about one lot.
        /// </param>
        public static void NotifyAuctionsChanged(PlotAuction changed = null)
        {
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                if (changed != null
                    && city.Guid != changed.SellerCityGuid
                    && !AuctionRules.IsVisibleTo(changed, city)
                    && city.Guid != changed.CurrentBidderCityGuid)
                {
                    continue;
                }
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                    gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_PLOT_AUCTIONS);
            }
        }

        /// <summary>How a lot is named in a message - its coordinates, or the city being sold whole.</summary>
        public static string LotName(PlotAuction auction)
        {
            if (auction.Kind == EnumAuctionKind.WHOLE_CITY)
            {
                return auction.TryGetLotCity(out City city) ? city.getPartNameReplaceUnder() : auction.LotCityGuid;
            }
            return auction.PlotX + " " + auction.PlotZ;
        }
    }
}
