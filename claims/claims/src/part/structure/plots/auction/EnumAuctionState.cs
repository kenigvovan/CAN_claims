namespace claims.src.part.structure.plots.auction
{
    /// <summary>
    /// Life stage of a lot. Finished lots are kept rather than deleted, so the market history and a
    /// dispute over a deal still have something to read. Stored as a plain int - append only.
    /// </summary>
    public enum EnumAuctionState
    {
        /// <summary>Open for bids until EndsAt.</summary>
        RUNNING,
        /// <summary>Closed with a winner; the lot has changed hands.</summary>
        SOLD,
        /// <summary>Time ran out with no bid, or no bid was still valid at closing time.</summary>
        EXPIRED,
        /// <summary>Called off - by the seller, or because the lot itself is gone. Escrow refunded.</summary>
        CANCELLED
    }
}
