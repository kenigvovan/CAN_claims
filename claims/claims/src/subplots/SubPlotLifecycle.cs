using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.subplots.geometry;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.subplots
{
    /// <summary>
    /// What happens to sub-plots when a chunk under them changes. A change the city chooses - an
    /// unclaim, a sale, a plots group, a tavern - is refused while a sub-plot reaches into the chunk
    /// (<see cref="BlockingProblem"/>). A change forced on the city - capture, cession, bankruptcy,
    /// demolition, an admin - cuts the chunk out of each sub-plot (<see cref="CutChunk"/>); a sub-plot
    /// left with nothing, or in pieces, is removed and its owner refunded.
    /// </summary>
    public static class SubPlotLifecycle
    {
        /// <summary>
        /// The refusal for a change the city chooses on this chunk, or null when no sub-plot reaches
        /// into it. Names the sub-plots in the way.
        /// </summary>
        public static string BlockingProblem(Vec2i chunk)
        {
            if (claims.dataStorage == null) return null;
            var inChunk = claims.dataStorage.SubPlotsInChunk(chunk);
            if (inChunk.Count == 0) return null;
            return Lang.Get("claims:subplot_blocks_chunk", string.Join(", ", inChunk.Select(s => s.GetPartName())));
        }

        public static string BlockingProblem(Plot plot) => plot == null ? null : BlockingProblem(plot.getPos());

        /// <summary>
        /// Takes this chunk out of every sub-plot reaching into it. Call before the chunk leaves the
        /// city, while the sub-plot's city still holds its treasury.
        /// </summary>
        public static void CutChunk(Vec2i chunk)
        {
            if (claims.dataStorage == null) return;
            var inChunk = claims.dataStorage.SubPlotsInChunk(chunk).ToArray();
            if (inChunk.Length == 0) return;
            BlockBox column = SubPlot.ChunkColumn(chunk, PlotPosition.plotSize, SubPlot.WorldHeight);
            foreach (SubPlot subPlot in inChunk)
            {
                BoxShape rest = subPlot.Shape.Without(column);
                if (rest == null || !rest.IsConnected())
                {
                    Dissolve(subPlot);
                    continue;
                }
                claims.dataStorage.ReshapeSubPlot(subPlot, rest);
                subPlot.saveToDatabase();
                Tell(subPlot, Lang.Get("claims:subplot_cut", subPlot.GetPartName(), chunk.X + ":" + chunk.Y));
            }
        }

        /// <summary>
        /// A citizen leaving the city gives back what they own in it, as with their bought plots: no
        /// refund, the city holds it again. They also stop being a member of anyone's sub-plot there.
        /// </summary>
        public static void OnPlayerLeftCity(PlayerInfo player, City city)
        {
            if (player == null || city == null || claims.dataStorage == null) return;
            foreach (SubPlot subPlot in OfCity(city))
            {
                if (player.Equals(subPlot.Owner))
                {
                    ReturnToCity(subPlot);
                }
                else if (subPlot.Members.Remove(player))
                {
                    subPlot.saveToDatabase();
                    SubPlotSync.MarkChanged(subPlot);
                }
            }
        }

        /// <summary>Takes back what the player owns in the city, as for bought plots when the fee goes unpaid.</summary>
        public static void TakeBackFrom(PlayerInfo player, City city)
        {
            if (player == null || city == null || claims.dataStorage == null) return;
            foreach (SubPlot subPlot in OfCity(city))
            {
                if (player.Equals(subPlot.Owner)) ReturnToCity(subPlot);
            }
        }

        /// <summary>
        /// The sub-plot is the city's again: no owner, no members, not for sale. An announced tax raise
        /// lands at once - it was waiting for the owner's say, and there is no owner now.
        /// </summary>
        /// <param name="log">False when the caller writes a log line of its own about why.</param>
        public static void ReturnToCity(SubPlot subPlot, bool log = true)
        {
            SubPlotSync.NotifyPayment(subPlot.Owner);
            if (log && subPlot.Owner != null)
            {
                SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotReturned, subPlot.Owner.GetPartName());
            }
            subPlot.Owner = null;
            subPlot.Members.Clear();
            subPlot.Price = -1;
            subPlot.LastPaidPrice = 0;
            subPlot.TimeStampClaimed = 0;
            if (subPlot.HasPendingTax) subPlot.Tax = subPlot.PendingTax;
            subPlot.ClearPendingTax();
            subPlot.saveToDatabase();
            SubPlotSync.MarkChanged(subPlot);
        }

        /// <summary>A plots group removed: its sub-plots stay, out of any group.</summary>
        public static void OnGroupDeleted(CityPlotsGroup group)
        {
            if (group == null || claims.dataStorage == null) return;
            foreach (SubPlot subPlot in claims.dataStorage.AllSubPlots().Where(s => s.Group == group).ToArray())
            {
                subPlot.Group = null;
                subPlot.saveToDatabase();
                SubPlotSync.MarkChanged(subPlot);
            }
        }

        /// <summary>A city going away takes all its sub-plots at once, rather than chunk by chunk.</summary>
        public static void DissolveAllOf(City city)
        {
            if (city == null || claims.dataStorage == null) return;
            foreach (SubPlot subPlot in OfCity(city))
            {
                Dissolve(subPlot);
            }
        }

        private static SubPlot[] OfCity(City city) => claims.dataStorage.AllSubPlots().Where(s => city.Equals(s.City)).ToArray();

        /// <summary>
        /// Removes a sub-plot that lost its ground. Its owner gets back what they paid, as far as the
        /// city's treasury goes.
        /// </summary>
        public static void Dissolve(SubPlot subPlot) =>
            Dissolve(subPlot, "claims:subplot_dissolved", "claims:subplot_dissolved_refund");

        /// <param name="messageKey">Told to the owner and the mayor: {0} the sub-plot.</param>
        /// <param name="refundMessageKey">The same with {1} the refund.</param>
        public static void Dissolve(SubPlot subPlot, string messageKey, string refundMessageKey)
        {
            long refund = Refund(subPlot);
            SubPlotSync.NotifyPayment(subPlot.Owner);
            SubPlotSync.Log(subPlot, citylog.EnumCityLogEvent.SubPlotRemoved);
            claims.dataStorage.RemoveSubPlot(subPlot);
            claims.getModInstance().getDatabaseHandler().deleteSubPlot(subPlot);
            Tell(subPlot, refund > 0
                ? Lang.Get(refundMessageKey, subPlot.GetPartName(), refund)
                : Lang.Get(messageKey, subPlot.GetPartName()));
        }

        /// <summary>What the owner is paid back on removal: what they paid, capped at the city's balance.</summary>
        public static long RefundAmount(SubPlot subPlot, decimal cityBalance)
        {
            if (subPlot?.Owner == null || subPlot.LastPaidPrice <= 0 || cityBalance <= 0) return 0;
            return cityBalance >= subPlot.LastPaidPrice ? subPlot.LastPaidPrice : (long)Math.Floor(cityBalance);
        }

        private static long Refund(SubPlot subPlot)
        {
            if (subPlot.City == null) return 0;
            string cityAccount = subPlot.City.MoneyAccountName;
            long amount = RefundAmount(subPlot, claims.economyProvider.GetBalance(cityAccount));
            if (amount <= 0) return 0;
            var result = claims.economyProvider.Transfer(cityAccount, subPlot.Owner.MoneyAccountName, amount);
            if (result != MoneyOperationResult.Success)
            {
                MessageHandler.sendErrorMsg("SubPlotLifecycle: refund " + amount + " for " + subPlot.Guid + " failed (" + result + ")");
                return 0;
            }
            return amount;
        }

        private static void Tell(SubPlot subPlot, string msg)
        {
            var told = new HashSet<PlayerInfo>();
            if (subPlot.Owner != null && told.Add(subPlot.Owner)) MessageHandler.sendMsgToPlayerInfo(subPlot.Owner, msg);
            PlayerInfo mayor = subPlot.City?.getMayor();
            if (mayor != null && told.Add(mayor)) MessageHandler.sendMsgToPlayerInfo(mayor, msg);
        }
    }
}
