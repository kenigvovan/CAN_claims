using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Config;

namespace claims.src.cityplotsgroups
{
    /// <summary>
    /// What a plots group charges its members, and how a mayor is allowed to change it.
    ///
    /// A group holds land other people build on, so the fee is the one setting a mayor could use
    /// against the very members it is meant to serve: let them put up houses at one coin a day, then
    /// ask for five hundred. Lowering costs nobody anything and happens at once. A raise is only ever
    /// announced: everyone keeps paying the old rate for
    /// <see cref="Config.PLOTSGROUP_FEE_RAISE_DELAY_HOURS"/>, and when the wait is over the members
    /// who accepted move to the new rate while the rest leave the group. Nobody is ever charged a
    /// rate they did not agree to - a member who was offline the whole time included.
    /// </summary>
    public static class PlotsGroupFeeHelper
    {
        /// <summary>
        /// Sets what the group charges. Lowering it applies immediately; raising it starts the wait
        /// described on this class. Returns false with a lang key when the amount is not usable.
        /// </summary>
        public static bool SetFee(CityPlotsGroup group, double fee, out string errorKey)
        {
            errorKey = null;
            if (group == null) { errorKey = "claims:no_such_group_found"; return false; }
            if (fee < 0) { errorKey = "claims:not_negative"; return false; }
            if (fee > claims.config.MAX_PLOTSGROUP_FEE)
            {
                fee = claims.config.MAX_PLOTSGROUP_FEE;
            }

            // Re-announcing the same raise would reset the wait and wipe the acceptances collected
            // so far, which is a way to keep members permanently unable to accept in time.
            if (group.HasPendingFee && group.PendingFee == fee) return true;

            long delay = claims.config.PLOTSGROUP_FEE_RAISE_DELAY_HOURS * 3600L;

            if (fee <= group.PlotsGroupFee || delay <= 0)
            {
                // Nobody needs protecting from paying less, and a host who set the delay to zero
                // asked for raises to land at once.
                group.PlotsGroupFee = fee;
                group.ClearPendingFee();
                group.saveToDatabase();
                Announce(group, "claims:plotsgroup_fee_changed", fee);
                SendGroupUpdate(group);
                NotifyPayments(group);
                return true;
            }

            group.PendingFee = fee;
            group.PendingFeeAt = TimeFunctions.getEpochSeconds() + delay;
            group.PendingFeeAccepted.Clear();
            group.saveToDatabase();

            Announce(group, "claims:plotsgroup_fee_raise_announced",
                fee, claims.config.PLOTSGROUP_FEE_RAISE_DELAY_HOURS, group.PlotsGroupFee);
            SendGroupUpdate(group);
            return true;
        }

        /// <summary>A member agreeing to the announced raise, so it applies to them when it lands.</summary>
        public static bool Accept(CityPlotsGroup group, PlayerInfo player, out string errorKey)
        {
            errorKey = null;
            if (group == null) { errorKey = "claims:no_such_group_found"; return false; }
            if (!group.HasPendingFee) { errorKey = "claims:plotsgroup_fee_no_pending"; return false; }
            if (player == null || !group.PlayersList.Contains(player))
            {
                errorKey = "claims:plotsgroup_not_a_member";
                return false;
            }
            // Telling them is the caller's job - the command that got them here already answers.
            if (!group.PendingFeeAccepted.Add(player.Guid)) return true;

            group.saveToDatabase();
            SendGroupUpdate(group);
            return true;
        }

        /// <summary>
        /// A member who just joined is taken as agreeing to a raise already announced: they are
        /// signing up while it is on the board, and dropping them the moment it lands would be a
        /// strange welcome.
        /// </summary>
        public static void OnMemberJoined(CityPlotsGroup group, PlayerInfo player)
        {
            if (group == null || player == null || !group.HasPendingFee) return;
            if (group.PendingFeeAccepted.Add(player.Guid)) group.saveToDatabase();
        }

        /// <summary>Forgets a member who left, so a later raise does not count them as having agreed.</summary>
        public static void OnMemberLeft(CityPlotsGroup group, PlayerInfo player)
        {
            if (group == null || player == null) return;
            if (group.PendingFeeAccepted.Remove(player.Guid)) group.saveToDatabase();
        }

        /// <summary>
        /// Applies every announced raise whose wait is over. Called hourly - a raise landing up to an
        /// hour late only ever charges the old, lower rate for longer.
        /// </summary>
        public static void SettleDueRaises()
        {
            long now = TimeFunctions.getEpochSeconds();
            foreach (CityPlotsGroup group in claims.dataStorage.getCityPlotsGroupsDict().Values.ToArray())
            {
                if (!group.HasPendingFee || now < group.PendingFeeAt) continue;
                Settle(group);
            }
        }

        private static void Settle(CityPlotsGroup group)
        {
            double newFee = group.PendingFee;
            var accepted = new HashSet<string>(group.PendingFeeAccepted);

            // Whoever did not agree leaves rather than being charged. The mayor stays regardless -
            // they pay no fee at all, so there is nothing for them to have agreed to.
            List<PlayerInfo> leaving = group.PlayersList
                .Where(p => p != null && !accepted.Contains(p.Guid)
                            && !(group.City?.isMayor(p) ?? false))
                .ToList();

            foreach (PlayerInfo player in leaving)
            {
                group.PlayersList.Remove(player);
                MessageHandler.sendMsgToPlayerInfo(player,
                    Lang.Get("claims:plotsgroup_fee_left_not_accepted", group.GetPartName(), newFee));
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.Guid,
                    EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
                // The rights they had through the group are cached per player and per plot; without
                // this they would keep building on the group's land until something else evicted the
                // cache. Same pair of calls the kick command makes.
                player.PlayerCache?.Reset();
            }

            if (leaving.Count > 0)
            {
                foreach (Plot plot in group.City?.getCityPlots() ?? new List<Plot>())
                {
                    if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(group))
                    {
                        claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                    }
                }
            }

            group.PlotsGroupFee = newFee;
            group.ClearPendingFee();
            group.saveToDatabase();

            Announce(group, "claims:plotsgroup_fee_raise_applied", newFee);
            SendGroupUpdate(group);
            NotifyPayments(group);
        }

        /// <summary>Tells every member of the group the same thing.</summary>
        private static void Announce(CityPlotsGroup group, string langKey, params object[] args)
        {
            var withName = new object[args.Length + 1];
            withName[0] = group.GetPartName();
            args.CopyTo(withName, 1);
            foreach (PlayerInfo player in group.PlayersList.ToArray())
            {
                if (player == null) continue;
                MessageHandler.sendMsgToPlayerInfo(player, Lang.Get(langKey, withName));
            }
        }

        /// <summary>Refreshes what each member sees as their next payment.</summary>
        private static void NotifyPayments(CityPlotsGroup group)
        {
            foreach (PlayerInfo player in group.PlayersList.ToArray())
            {
                if (player == null) continue;
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.Guid,
                    EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            }
        }

        /// <summary>
        /// Pushes the group to the city's clients. The one place a group cell is built: the packet
        /// replaces the client's copy wholesale, so a caller that left the pending raise out of it
        /// would quietly wipe the announcement off everyone's screen.
        /// </summary>
        public static void SendGroupUpdate(CityPlotsGroup group)
        {
            if (group?.City == null) return;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(group.City.Guid,
                new Dictionary<string, object> { { "value", ToCell(group) } },
                EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_UPDATE);
        }

        /// <summary>
        /// The group as its clients see it. Member names come along by default: an update packet
        /// replaces the client's copy of the group wholesale, and a copy without them made the GUI
        /// read the player as a non-member - hiding the accept button on an announced raise.
        /// </summary>
        public static PlotsGroupCellElement ToCell(CityPlotsGroup group, List<string> playerNames = null)
        {
            return new PlotsGroupCellElement(group.Guid, group.GetPartName(),
                group.City?.GetPartName() ?? "",
                playerNames ?? group.PlayersList.Where(p => p != null).Select(p => p.GetPartName()).ToList(),
                group.PermsHandler, group.PlotsGroupFee)
            {
                PlotsCount = group.City?.getCityPlots()
                    .Count(p => p.hasPlotGroup() && group.Equals(p.getPlotGroup())) ?? 0,
                PendingFee = group.PendingFee,
                PendingFeeAt = group.PendingFeeAt,
                PendingFeeAccepted = new List<string>(group.PendingFeeAccepted)
            };
        }
    }
}
