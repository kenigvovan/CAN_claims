using claims.src.economy;
using claims.src.messages;

namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// Holds the leading bid of a lot on an account of its own until the lot closes.
    ///
    /// The money leaves the bidder's treasury the moment the bid is placed, so the winner is solvent
    /// by construction - a promise to pay later would let a city outbid everyone and then spend the
    /// money elsewhere before the lot closed. An account (rather than a number kept on the lot) is
    /// used because <see cref="IMoneyProvider"/> already persists accounts, so an escrow survives a
    /// server restart without any bookkeeping of ours.
    /// </summary>
    public static class AuctionEscrow
    {
        /// <summary>Creates the lot's account. Called once, when the lot opens.</summary>
        public static bool Open(PlotAuction auction)
        {
            string account = auction.EscrowAccountName;
            if (claims.economyProvider.AccountExists(account)) return true;
            return claims.economyProvider.NewAccount(account);
        }

        /// <summary>Moves a bid from the city's treasury into the lot's account.</summary>
        public static bool Hold(PlotAuction auction, City city, long amount)
        {
            if (city == null || amount < 0) return false;
            // A plot given away for nothing is a valid offer: there is simply nothing to hold, and
            // refusing it would make a zero-priced listing impossible to take.
            if (amount == 0) return true;
            if (!Open(auction)) return false;
            MoneyOperationResult result = claims.economyProvider.Transfer(
                city.MoneyAccountName, auction.EscrowAccountName, amount);
            if (result != MoneyOperationResult.Success)
            {
                MessageHandler.sendErrorMsg("AuctionEscrow::Hold failed for " + city.GetPartName()
                    + " on lot " + auction.Guid + ": " + result);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gives an outbid city its money back. A refund that cannot be delivered - the city was
        /// demolished meanwhile - is logged and the money stays in escrow until the lot is closed
        /// and swept, rather than silently vanishing.
        /// </summary>
        public static bool Refund(PlotAuction auction, City city, long amount)
        {
            if (amount <= 0) return true;
            if (city == null) return false;
            MoneyOperationResult result = claims.economyProvider.Transfer(
                auction.EscrowAccountName, city.MoneyAccountName, amount);
            if (result != MoneyOperationResult.Success)
            {
                MessageHandler.sendErrorMsg("AuctionEscrow::Refund failed for " + city.GetPartName()
                    + " on lot " + auction.Guid + ": " + result);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Pays the deal out to the seller and drops the account.
        /// </summary>
        /// <param name="amount">
        /// What the seller is owed - the winning bid. Anything else on the account is not theirs: a
        /// refund that could not be delivered ends up here, and handing it over would pay the seller
        /// for land they did not sell. Pass -1 to release the whole balance, which is what a lot with
        /// nothing but its own bid on it needs.
        /// </param>
        public static bool Release(PlotAuction auction, City toCity, long amount = -1)
        {
            decimal left = claims.economyProvider.GetBalance(auction.EscrowAccountName);
            decimal payout = amount < 0 ? left : System.Math.Min(left, amount);

            if (toCity != null && payout > 0)
            {
                MoneyOperationResult result = claims.economyProvider.Transfer(
                    auction.EscrowAccountName, toCity.MoneyAccountName, payout);
                if (result != MoneyOperationResult.Success)
                {
                    // Leave the account alone: deleting it now would destroy money that belongs to
                    // someone. It can be released by hand once the payout target exists again.
                    MessageHandler.sendErrorMsg("AuctionEscrow::Release failed for lot " + auction.Guid
                        + " to " + toCity.GetPartName() + ": " + result);
                    return false;
                }
            }
            Sweep(auction);
            return true;
        }

        /// <summary>
        /// Empties the lot's account and drops it. Whatever is still on it belongs to somebody who
        /// could not be paid - an outbid city demolished before its refund went through - so it is
        /// moved to the holding account rather than deleted along with the lot. Nothing pays out of
        /// that account on its own; it exists so the money can be found and returned by hand.
        /// </summary>
        public static void Sweep(PlotAuction auction)
        {
            decimal left = claims.economyProvider.GetBalance(auction.EscrowAccountName);
            if (left > 0)
            {
                string holding = HoldingAccountName;
                if (!claims.economyProvider.AccountExists(holding))
                {
                    claims.economyProvider.NewAccount(holding);
                }
                MoneyOperationResult result = claims.economyProvider.Transfer(
                    auction.EscrowAccountName, holding, left);
                if (result != MoneyOperationResult.Success)
                {
                    // Could not even park it - keep the account rather than delete the money with it.
                    MessageHandler.sendErrorMsg("AuctionEscrow::Sweep failed for lot " + auction.Guid
                        + " (" + left + " left): " + result);
                    return;
                }
                MessageHandler.sendErrorMsg("AuctionEscrow: " + left + " from lot " + auction.Guid
                    + " could not be paid to anyone and was moved to " + holding);
            }
            Close(auction);
        }

        /// <summary>Where unpayable escrow ends up. One account for the whole server.</summary>
        public static string HoldingAccountName => claims.config.AUCTION_ACCOUNT_STRING_PREFIX + "unclaimed";

        /// <summary>Drops the lot's account. Only safe once the balance has been paid out.</summary>
        public static void Close(PlotAuction auction)
        {
            if (claims.economyProvider.AccountExists(auction.EscrowAccountName))
            {
                claims.economyProvider.DeleteAccount(auction.EscrowAccountName);
            }
        }
    }
}
