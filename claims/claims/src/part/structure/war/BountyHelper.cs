using claims.src.economy;
using claims.src.network.packets;
using claims.src.part;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Bounties on players' heads. A poster escrows coins onto a target; the killer collects the
    /// pooled total on death (payout lives in OnPlayerDeath, which resolves the killer). Amounts
    /// are tracked per poster so an unclaimed bounty can be cancelled and refunded.
    /// </summary>
    public static class BountyHelper
    {
        /// <summary>Escrows <paramref name="amount"/> from the poster onto the target. Returns null on success or an error lang key.</summary>
        public static string Place(PlayerInfo target, PlayerInfo poster, long amount, out long newTotal)
        {
            newTotal = 0;
            if (!claims.config.WAR_BOUNTY_ENABLED) return "claims:bounty_disabled";
            if (target == null || poster == null) return "claims:player_not_found";
            if (target.Equals(poster)) return "claims:bounty_not_self";
            if (amount < (long)claims.config.WAR_BOUNTY_MIN) return "claims:bounty_too_small";
            if (!claims.economyProvider.SupportsPlayerWallet) return "claims:economy_not_available";
            if (claims.economyProvider.GetBalance(poster.MoneyAccountName) < (decimal)amount) return "claims:not_enough_money";
            if (claims.economyProvider.Withdraw(poster.MoneyAccountName, (decimal)amount) != MoneyOperationResult.Success) return "claims:economy_money_transaction_error";

            target.BountyPosters.TryGetValue(poster.Guid, out long cur);
            target.BountyPosters[poster.Guid] = cur + amount;
            target.saveToDatabase();
            newTotal = target.GetBountyTotal();
            BroadcastBoard();
            return null;
        }

        /// <summary>Refunds and removes the poster's own bounty on the target. Returns null on success or an error lang key.</summary>
        public static string Cancel(PlayerInfo target, PlayerInfo poster, out long refunded)
        {
            refunded = 0;
            if (target == null || poster == null) return "claims:player_not_found";
            if (!target.BountyPosters.TryGetValue(poster.Guid, out long amount) || amount <= 0) return "claims:bounty_none_from_you";
            target.BountyPosters.Remove(poster.Guid);
            target.saveToDatabase();
            if (claims.economyProvider.SupportsPlayerWallet)
                claims.economyProvider.Deposit(poster.MoneyAccountName, (decimal)amount);
            refunded = amount;
            BroadcastBoard();
            return null;
        }

        /// <summary>Broadcasts the public bounty board (all players with a bounty) to every client.</summary>
        public static void BroadcastBoard()
        {
            var packet = new BountyBoardPacket();
            foreach (var p in claims.dataStorage.getPlayersDict().Values)
            {
                long total = p.GetBountyTotal();
                if (total > 0)
                    packet.Entries.Add(new BountyBoardEntry { Name = p.GetPartName(), Amount = total });
            }
            claims.serverChannel?.BroadcastPacket(packet);
        }
    }
}
