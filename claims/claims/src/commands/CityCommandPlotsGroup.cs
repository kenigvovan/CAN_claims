using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.cityplotsgroups;
using claims.src.delayed.cooldowns;
using claims.src.delayed.invitations;
using claims.src.delayed.teleportation;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.rights;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public partial class CityCommand
    {
        /*==============================================================================================*/
        /*=====================================PLOTSGROUP===============================================*/
        /*==============================================================================================*/
        public static TextCommandResult PlotsGroupCreate(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (city.getCityPlotsGroups().Count > claims.config.MAX_PLOTS_GROUP_PER_CITY)
            {
                tcr.StatusMessage = "claims:too_much_plot_groups";
                return tcr;
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup != null)
            {
                tcr.StatusMessage = "claims:group_already_exists";
                return tcr;
            }
            string newGuid = "";
            while (true)
            {
                Guid guid = Guid.NewGuid();
                if (claims.dataStorage.PlotsGroupExistsByGUID(guid.ToString()))
                {
                    continue;
                }
                else
                {
                    newGuid = guid.ToString();
                    break;
                }
            }

            searchedGroup = new CityPlotsGroup(name, newGuid);
            tcr.StatusMessage = "claims:plotsgroup_was_created";
            tcr.MessageParams = new object[] { searchedGroup.GetPartName() };
            city.getCityPlotsGroups().Add(searchedGroup);
            claims.dataStorage.addPlotsGroup(searchedGroup);
            searchedGroup.City = city;
            city.saveToDatabase();
            searchedGroup.saveToDatabase(false);
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", PlotsGroupFeeHelper.ToCell(searchedGroup) } },
                EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ADD);
            return tcr;
        }
        public static TextCommandResult PlotsGroupDelete(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            tcr.StatusMessage = "claims:plotsgroup_was_deleted";
            tcr.MessageParams = new object[] { searchedGroup.GetPartName() };
            city.getCityPlotsGroups().Remove(searchedGroup);
            claims.dataStorage.removePlotsGroup(searchedGroup.Guid);
            city.saveToDatabase();
            foreach(var plot in city.getCityPlots())
            {
                if(plot.hasCityPlotsGroup() && plot.getPlotGroup().Equals(searchedGroup))
                {
                    plot.setPlotGroup(null);
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                }
            }
            claims.getModInstance().getDatabaseHandler().deleteFromDatabaseCityPlotGroup(searchedGroup);
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new PlotsGroupCellElement(searchedGroup.Guid,
                                                                                      null,
                                                                                      null,
                                                                                      null,
                                                                                      null,
                                                                                      0)} },
                EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_REMOVE);
            return tcr;
        }
        public static TextCommandResult PlotsGroupList(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            MessageHandler.sendMsgToPlayer(player,
                StringFunctions.makeFeasibleStringFromNames(
                    StringFunctions.getNamesOfPartsForChat(Lang.Get("claims:city_groups"),
                   new List<Part>(city.getCityPlotsGroups())), ','));
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult PlotsGroupListPlayers(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            MessageHandler.sendMsgToPlayer(player, StringFunctions.makeFeasibleStringFromNames(
                StringFunctions.getNamesOfPartsForChat(
                    Lang.Get("claims:plots_group_members", searchedGroup.GetPartName()),
                    new List<Part>(searchedGroup.PlayersList)), ','));
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult PlotsGroupAddPlayerToGroup(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            string groupName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            string playerName = Filter.filterName((string)args.Parsers[1].GetValue());
            if (playerName.Length == 0 || !Filter.checkForBlockedNames(playerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(playerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                return tcr;
            }
            if (searchedGroup.PlayersList.Contains(targetPlayer))
            {
                tcr.StatusMessage = "claims:already_in_the_group";
                return tcr;
            }
            var timeoutStamp = TimeFunctions.getEpochSeconds() + claims.config.PLOT_GROUP_INVITATION_TIMEOUT * TimeFunctions.secondsInAnHour;
            if (CityPlotsGroupInvitationsHandler.addNewCityPlotGroupInvitation(new CityPlotsGroupInvitation(
                playerInfo.City, targetPlayer, timeoutStamp,
               () =>
               {
                   if (searchedGroup == null)
                   {
                       return;
                   }
                   searchedGroup.PlayersList.Add(targetPlayer);
                   // Joining while a raise is on the board counts as agreeing to it.
                   PlotsGroupFeeHelper.OnMemberJoined(searchedGroup, targetPlayer);
                   searchedGroup.saveToDatabase();
                   SendGroupUpdateWithMembers(searchedGroup);
                   UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid,
                       EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
                    foreach (var plot in city.getCityPlots())
                    {
                        if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(searchedGroup))
                        {
                           targetPlayer.PlayerCache.Reset();
                           claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                        }
                    }
                   UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid,
                    new Dictionary<string, object> { { "value", new ClientToPlotsGroupInvitation(city.GetPartName(), searchedGroup.GetPartName(), 0) } },
                    EnumPlayerRelatedInfo.TO_PLOTS_GROUP_INVITE_REMOVE);
                   if (!(targetPlayer.hasCity() && targetPlayer.City.Guid.Equals(city.Guid)))
                   {
                       UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid,
                        new Dictionary<string, object> { { "value", PlotsGroupFeeHelper.ToCell(searchedGroup,
                            searchedGroup.PlayersList.Select(ele => ele.GetPartName()).ToList()) } },
                        EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ADD);
                   }
               },
               () =>
               {
                   // Whoever sent the invitation is owed an answer: without this a refusal looked
                   // exactly like an invitation nobody had got around to reading yet.
                   MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_declined_plotsgroup_invite",
                       targetPlayer.getPartNameReplaceUnder(), searchedGroup.GetPartName()));
               },
               searchedGroup.GetPartName())))
            {
                // The fee is part of the offer: accepting starts the daily charge, and the invitation
                // used to say nothing about it - the player learned the price after joining.
                MessageHandler.sendMsgToPlayerInfo(targetPlayer, searchedGroup.HasFee()
                    ? Lang.Get("claims:you_were_invited_to_group_paid", city.getPartNameReplaceUnder(),
                        playerInfo.getPartNameReplaceUnder(), searchedGroup.PlotsGroupFee)
                    : Lang.Get("claims:you_were_invited_to_group", city.getPartNameReplaceUnder(),
                        playerInfo.getPartNameReplaceUnder()));
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid,
                    new Dictionary<string, object> { { "value", new ClientToPlotsGroupInvitation(city.GetPartName(),
                        searchedGroup.GetPartName(), timeoutStamp, searchedGroup.PlotsGroupFee) } },
                    EnumPlayerRelatedInfo.TO_PLOTS_GROUP_INVITE_ADD);
                tcr.StatusMessage = "claims:you_invited_player_to_group";
                tcr.MessageParams = new object[] { targetPlayer.GetPartName(), searchedGroup.GetPartName() };
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:you_already_invited_player_in_one_of_city_group";
                tcr.MessageParams = new object[] { targetPlayer.GetPartName() };
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
        }
        /// <summary>
        /// Takes back an invitation into a plots group before it is answered. The counterpart of
        /// /city uninvite, which does the same for the city itself; until now the command existed,
        /// was registered without arguments and answered "todo".
        /// </summary>
        public static TextCommandResult PlotsGroupUnaddTo(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            string groupName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            string playerName = Filter.filterName((string)args.Parsers[1].GetValue());
            if (playerName.Length == 0 || !Filter.checkForBlockedNames(playerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(playerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:no_such_player";
                return tcr;
            }

            CityPlotsGroupInvitation found = null;
            foreach (CityPlotsGroupInvitation invitation in targetPlayer.groupInvitations.ToArray())
            {
                if (invitation.Sender != null && invitation.Sender.Guid == city.Guid
                    && invitation.GroupName.Equals(groupName))
                {
                    found = invitation;
                    break;
                }
            }
            if (found == null)
            {
                tcr.StatusMessage = "claims:no_such_plotsgroup_invite";
            return tcr;
        }

            // Withdrawn, not refused: the reject callback tells the city somebody said no, and
            // taking an offer back is not that.
            CityPlotsGroupInvitationsHandler.RemoveInvitation(found);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid,
                new Dictionary<string, object> { { "value", new ClientToPlotsGroupInvitation(city.GetPartName(), groupName, 0) } },
                EnumPlayerRelatedInfo.TO_PLOTS_GROUP_INVITE_REMOVE);
            MessageHandler.sendMsgToPlayerInfo(targetPlayer,
                Lang.Get("claims:plotsgroup_invite_withdrawn", city.getPartNameReplaceUnder(), groupName));

            return SuccessWithParams("claims:you_withdrew_plotsgroup_invite",
                new object[] { targetPlayer.GetPartName(), groupName });
        }
        public static TextCommandResult PlotsGroupKickPlayerFromGroup(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_city"));
                return tcr;
            }

            string groupName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_group_name"));
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_group_found"));
                return tcr;
            }
            string playerName = Filter.filterName((string)args.Parsers[1].GetValue());
            if (playerName.Length == 0 || !Filter.checkForBlockedNames(playerName))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_player_name"));
                return tcr;
            }
            claims.dataStorage.getPlayerByName(playerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                return tcr;
            }
            if (!searchedGroup.PlayersList.Contains(targetPlayer))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_in_the_group"));
                return tcr;
            }
            searchedGroup.PlayersList.Remove(targetPlayer);
            PlotsGroupFeeHelper.OnMemberLeft(searchedGroup, targetPlayer);
            searchedGroup.saveToDatabase();
            SendGroupUpdateWithMembers(searchedGroup);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid,
                EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            foreach (var plot in city.getCityPlots())
            {
                if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(searchedGroup))
                {
                    targetPlayer.PlayerCache.Reset();
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                }
            }
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult PlotsGroupPlotAdd(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            //ADD PLOTGROUPNAME
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(player.Entity.Pos), out Plot plot);
            //NO CLAIMED PLOT HERE || VILLAGE HERE || PLOT NOT OURS
            // hasCity first: an unclaimed plot object has no city, and asking it for one threw -
            // the command died on the spot and the player was told nothing at all.
            if (plot == null || !plot.hasCity() || !plot.getCity().Equals(playerInfo.City))
            {
                tcr.StatusMessage = "claims:cannot_add_to_group";
                return tcr;
            }
            string groupName = Filter.filterName((string)args.LastArg);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }

            // Silently reassigning a plot from one group to another would move the rights of a set
            // of players onto ground they were never given, so say what is in the way instead.
            if (plot.hasCityPlotsGroup())
            {
                tcr.StatusMessage = searchedGroup.Equals(plot.getPlotGroup())
                    ? "claims:plot_already_in_this_group"
                    : "claims:plot_already_in_other_group";
                tcr.MessageParams = new object[] { plot.getPlotGroup().GetPartName() };
                return tcr;
            }

            // Folding the plot into a group would void an auction that already has bids - the same
            // escape hatch as selling it to a citizen, and closed for the same reason.
            if (part.structure.plots.auction.AuctionRegistry.TryGetRunningFor(plot, out var bidLot)
                && bidLot.HasBid)
            {
                tcr.StatusMessage = "claims:plot_auction_has_bids";
                return tcr;
            }

            //DELETE OWNER, RECALCULATE HIS RIGHTS AND HIS COMRADES
            if (plot.hasPlotOwner())
            {
                PlayerInfo tmp = plot.getPlotOwner();
                plot.setPlotOwner(null);
                RightsHandler.reapplyRights(tmp);
                foreach (var it in tmp.Friends)
                {
                    RightsHandler.reapplyRights(it);
                }
            }
            plot.getPermsHandler().ApplyFromHandler(searchedGroup.PermsHandler, PermGroup.CITIZEN);

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);

            plot.setPlotGroup(searchedGroup);
            // A plot inside a group is not sellable, so a standing offer to other cities goes with it.
            if (part.structure.plots.auction.AuctionRegistry.TryGetRunningFor(plot,
                    out part.structure.plots.auction.PlotAuction lot))
            {
                part.structure.plots.auction.AuctionHandler.CancelLot(lot, "claims:plot_auction_cancelled_lot_gone");
            }
            plot.saveToDatabase();
            // The group's plot count is on its card, and the command said nothing at all before -
            // from the player's side adding a plot looked like it had done nothing.
            PlotsGroupFeeHelper.SendGroupUpdate(searchedGroup);
            return SuccessWithParams("claims:plot_added_to_group",
                new object[] { plot.getPos().X + " " + plot.getPos().Y, searchedGroup.GetPartName() });
        }
        public static TextCommandResult PlotsGroupPlotRemove(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            //ADD PLOTGROUPNAME
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(player.Entity.Pos), out Plot plot);
            //NO CLAIMED PLOT HERE || VILLAGE HERE || PLOT NOT OURS
            if (plot == null || !plot.hasCity() || !plot.getCity().Equals(playerInfo.City) || !plot.hasCityPlotsGroup())
            {
                tcr.StatusMessage = "claims:cannot_remove_from_group";
                return tcr;
            }
            string groupName = Filter.filterName((string)args.LastArg);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            if (!plot.getPlotGroup().Equals(searchedGroup))
            {
                tcr.StatusMessage = "claims:different_groups";
                return tcr;
            }
            plot.setPlotGroup(null);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            plot.saveToDatabase();

            PlotsGroupFeeHelper.SendGroupUpdate(searchedGroup);
            return SuccessWithParams("claims:plot_removed_from_group",
                new object[] { plot.getPos().X + " " + plot.getPos().Y, searchedGroup.GetPartName() });
        }
        public static TextCommandResult PlotsGroupSet(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;
            City city = playerInfo.City;
            if (city == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_city"));
                return tcr;
            }
            string name = Filter.filterName((args.Parsers[0].GetValue() as string));
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_group_name"));
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_group_found"));
                return tcr;
            }

            CmdArgs tmpArgs = new CmdArgs();
            tmpArgs.AppendSingle((string)args.Parsers[1].GetValue());
            tmpArgs.AppendSingle((string)args.Parsers[2].GetValue());
            tmpArgs.AppendSingle((string)args.Parsers[3].GetValue());

            args.RawArgs.PopWord();
            if(!searchedGroup.PermsHandler.setAccessPerm(tmpArgs))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:error_occured"));
            }

            foreach(var plot in city.getCityPlots())
            {
                if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(searchedGroup))
                {
                    plot.getPermsHandler().setAccessPerm(tmpArgs);
                    plot.saveToDatabase();
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                    UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                }
            }
            PlotsGroupFeeHelper.SendGroupUpdate(searchedGroup);
            MessageHandler.sendMsgToPlayer(player,
                Lang.Get("claims:for_plotsgroup_group_perm_set_what", name, args.Parsers[1].GetValue(), (args.Parsers[2].GetValue() as string), (args.Parsers[3].GetValue() as string)));
            return TextCommandResult.Success();
        }
        public static TextCommandResult PlotsGroupSetPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if (!HelperFunctionSetFlag(player, out var searchedGroup, out var city, (args.Parsers[0].GetValue() as string), tcr))
            {
                return tcr;
            }

            if (!searchedGroup.PermsHandler.setPvp((string)args.LastArg))
            {
                return tcr;
            }

            foreach (var plot in city.getCityPlots())
            {
                if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(searchedGroup))
                {
                    plot.getPermsHandler().setPvp((string)args.LastArg);
                    plot.saveToDatabase();
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                    UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                }
            }
            searchedGroup.saveToDatabase();
            PlotsGroupFeeHelper.SendGroupUpdate(searchedGroup);
            return tcr;
        }
        public static TextCommandResult PlotsGroupSetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if (!HelperFunctionSetFlag(player, out var searchedGroup, out var city, (args.Parsers[0].GetValue() as string), tcr))
            {
                return tcr;
            }

            if (!searchedGroup.PermsHandler.setFire((string)args.LastArg))
            {
                return tcr;
            }

            foreach (var plot in city.getCityPlots())
            {
                if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(searchedGroup))
                {
                    plot.getPermsHandler().setFire((string)args.LastArg);
                    plot.saveToDatabase();
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                    UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                }
            }
            searchedGroup.saveToDatabase();
            PlotsGroupFeeHelper.SendGroupUpdate(searchedGroup);
            return tcr;
        }
        public static TextCommandResult PlotsGroupSetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if(!HelperFunctionSetFlag(player, out var searchedGroup, out var city, (args.Parsers[0].GetValue() as string), tcr))
            {
                return tcr;
            }

            if (!searchedGroup.PermsHandler.setBlast((string)args.LastArg))
            {
                return tcr;
            }

            foreach (var plot in city.getCityPlots())
            {
                if (plot.hasPlotGroup() && plot.getPlotGroup().Equals(searchedGroup))
                {
                    plot.getPermsHandler().setBlast((string)args.LastArg);
                    plot.saveToDatabase();
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                    UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                }
            }
            searchedGroup.saveToDatabase();
            PlotsGroupFeeHelper.SendGroupUpdate(searchedGroup);
            return tcr;
        }
        /// <summary>
        /// What members of the group pay daily. A raise does not take effect here - see
        /// <see cref="PlotsGroupFeeHelper"/> for why it has to be accepted first.
        /// </summary>
        public static TextCommandResult PlotsGroupSetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if (!HelperFunctionSetFlag(player, out var searchedGroup, out _, (args.Parsers[0].GetValue() as string), tcr))
            {
                return tcr;
            }

            int fee = Convert.ToInt32(args.Parsers[1].GetValue());
            if (!PlotsGroupFeeHelper.SetFee(searchedGroup, fee, out string errorKey))
            {
                tcr.Status = EnumCommandStatus.Error;
                tcr.StatusMessage = errorKey;
                return tcr;
            }

            return SuccessWithParams(searchedGroup.HasPendingFee
                    ? "claims:plotsgroup_fee_raise_set"
                    : "claims:plotsgroup_fee_set",
                new object[] { searchedGroup.GetPartName(), fee });
        }

        /// <summary>
        /// A member agreeing to an announced raise. Not gated by a permission: it is their own money,
        /// and the group they are in may well belong to a city they are not a citizen of.
        /// </summary>
        public static TextCommandResult PlotsGroupAcceptFee(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            string name = Filter.filterName(args.Parsers[0].GetValue() as string);
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in claims.dataStorage.getCityPlotsGroupsDict().Values)
            {
                if (group.GetPartName().Equals(name) && group.PlayersList.Contains(playerInfo))
                {
                    searchedGroup = group;
                    break;
                }
            }

            if (!PlotsGroupFeeHelper.Accept(searchedGroup, playerInfo, out string errorKey))
            {
                tcr.StatusMessage = errorKey;
                return tcr;
            }
            return SuccessWithParams("claims:plotsgroup_fee_accepted",
                new object[] { searchedGroup.GetPartName(), searchedGroup.PendingFee });
        }

        /// <summary>Group update carrying its member names, for the pages that list them.</summary>
        private static void SendGroupUpdateWithMembers(CityPlotsGroup group)
        {
            if (group?.City == null) return;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(group.City.Guid,
                new Dictionary<string, object> { { "value", PlotsGroupFeeHelper.ToCell(group,
                    group.PlayersList.Select(ele => ele.GetPartName()).ToList()) } },
                EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_UPDATE);
            return tcr;
        }

        public static bool HelperFunctionSetFlag(IServerPlayer player, out CityPlotsGroup searchedGroup, out City city, string groupName, TextCommandResult tcr)
        {
            searchedGroup = null;
            city = null;
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player_info"));
                return false;
            }
            city = playerInfo.City;
            if (city == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_city"));
                return false;
            }
            string name = Filter.filterName(groupName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_group_name"));
                return false;
            }
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_group_found"));
                return false;
            }

            if (!city.isCitizen(playerInfo))
            {
                tcr.StatusMessage = "claims:need_to_be_citizen";
                return false;
            }
            return true;
        }
    }
}
