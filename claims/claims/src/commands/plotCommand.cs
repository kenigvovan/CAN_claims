using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.rights;
using System;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class PlotCommand: BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult HereInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (!isOwnerOfPlotMayorAdmin(plot, playerInfo, player))
            {
                return TextCommandResult.Error("");
            }
            return TextCommandResult.Success(string.Join("", plot.getStatus())); 
        }
        public static TextCommandResult plotBorders(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (((string)args.LastArg).Equals("on", StringComparison.OrdinalIgnoreCase) && playerInfo != null)
            {
                playerInfo.showBorders = true;
                PlotPosition.makeChunkHighlight(claims.sapi.World, player);
                return TextCommandResult.Success();
            }
            else if (((string)args.LastArg).Equals("off", StringComparison.OrdinalIgnoreCase) && playerInfo != null)
            {
                playerInfo.showBorders = false;
                PlotPosition.clearChunkHighlight(claims.sapi.World, player);
                return TextCommandResult.Success();
            }
            return TextCommandResult.Success();
        }
        /*==============================================================================================*/
        /*=====================================CLAIM====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult PlotClaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
            TextCommandResult refusal = CheckCanClaim(playerInfo, plot);
            if (refusal != null) return refusal;

            // The sale waits for /agree at the price the player was shown: the mayor may change it in
            // the meantime, and the buyer must never pay a price they did not see.
            int offeredPrice = plot.Price;
            AgreementHandler.addNewAgreementOrReplace(new Agreement(() =>
            {
                TextCommandResult result = claims.dataStorage.GetPlot(plot.plotPosition, out Plot current) && current == plot
                    ? CheckCanClaim(playerInfo, plot)
                    : TextCommandResult.Error("claims:plot_claim_offer_changed");
                if (result == null && plot.Price != offeredPrice)
                {
                    result = TextCommandResult.Error("claims:plot_claim_offer_changed");
                }
                result ??= SellPlotToPlayer(player, playerInfo, plot);
                MessageHandler.sendMsgToPlayer(player, Lang.Get(result.StatusMessage, result.MessageParams ?? new object[0]));
            }, player.PlayerUID));

            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.AGREE_NEEDED_ON_PLOT_CLAIM,
                data = offeredPrice.ToString(CultureInfo.InvariantCulture)
            }, player);
            return SuccessWithParams("claims:plot_claim_ask", new object[] { offeredPrice, claims.config.AGREEMENT_COMMAND });
        }

        /// <summary>Null when the player may buy this plot now, otherwise the reason why not.</summary>
        public static TextCommandResult CheckCanClaim(PlayerInfo playerInfo, Plot plot)
        {
            if (plot.hasPlotOwner())
            {
                return TextCommandResult.Error("claims:plot_has_owner_already");
            }
            if(!plot.IsForSale)
            {
                return TextCommandResult.Error("claims:not_for_sale");
            }

            if (!plot.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }
            // Villages are outside the economy: the coins would land on a treasury nobody can
            // ever spend, so their plots are not for sale.
            if (plot.getCity().IsVillage())
            {
                return TextCommandResult.Error("claims:village_feature_locked");
            }
            if (plot.hasCityPlotsGroup())
            {
                return TextCommandResult.Error("claims:has_plot_group");
            }
            // Cities that already bid on this ground are owed the auction they were promised. Without
            // this the mayor could break any losing auction by selling the plot to a citizen of their
            // own - which is exactly what withdrawing a lot with bids on it is forbidden to do.
            if (part.structure.plots.auction.AuctionRegistry.TryGetRunningFor(plot, out var bidLot)
                && bidLot.HasBid)
            {
                return TextCommandResult.Error("claims:plot_auction_has_bids");
            }
            // Put up for sale before a sub-plot was cut out of it.
            string subPlotProblem = subplots.SubPlotLifecycle.BlockingProblem(plot);
            if (subPlotProblem != null)
            {
                return TextCommandResult.Error(subPlotProblem);
            }
            if (plot.Price > (double)claims.economyProvider.GetBalance(playerInfo.Guid))
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            if (plot.Type != PlotType.EMBASSY && !(playerInfo.hasCity() && plot.getCity().Equals(playerInfo.City)))
            {
                return TextCommandResult.Error("claims:cannot_buy_plot_in_another_city");
            }
            return null;
        }

        /// <summary>Takes the payment and hands the plot over; nothing changes if the payment fails.</summary>
        private static TextCommandResult SellPlotToPlayer(IServerPlayer player, PlayerInfo playerInfo, Plot plot)
        {
            //Save price localy or do not move after we change plot price
            decimal savedPrice = (decimal)plot.Price;

            bool paymentSuccessfull = claims.economyProvider.Transfer(playerInfo.MoneyAccountName, plot.getCity().MoneyAccountName, (decimal)plot.Price)
                == MoneyOperationResult.Success;
            if (!paymentSuccessfull)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            plot.Price = -1;
            // The plot now belongs to a citizen, so any standing offer to other cities is
            // void - it would otherwise sit in the market tab refusing every buyer.
            if (part.structure.plots.auction.AuctionRegistry.TryGetRunningFor(plot,
                    out part.structure.plots.auction.PlotAuction cityLot))
            {
                part.structure.plots.auction.AuctionHandler.CancelLot(cityLot,
                    "claims:plot_auction_cancelled_lot_gone");
            }
            plot.lastPaidPrice = (long)savedPrice;
            plot.TimeStampClaimed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Rights only: the flags belong to the plot. Copying them reset a paid nomobspawn
            // and turned blasts back on with every sale.
            plot.getPermsHandler().setRights(playerInfo.PermsHandler);
            plot.setPlotOwner(playerInfo);
            playerInfo.PlayerPlots.Add(plot);
            plot.saveToDatabase();
            playerInfo.saveToDatabase();
            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            return SuccessWithParams("claims:plot_has_been_claimed_by_player_paid", new object[] { savedPrice });
        }
        public static TextCommandResult PlotUnclaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
            if (!plot.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }

            if (!((plot.hasPlotOwner() && plot.getPlotOwner().Equals(playerInfo)) || plot.getCity().isMayor(playerInfo)))
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
            if (plot.hasCityPlotsGroup())
            {
                return TextCommandResult.Error("claims:has_plots_group");
            }
            // Nobody to give it back from. Let through, the mayor stood in as the owner and was paid
            // the refund the real owner had already had (R09) - and a plot saved before the refund
            // basis was cleared on unclaim still carries it.
            if (!plot.hasPlotOwner())
            {
                return TextCommandResult.Error("claims:plot_has_no_owner");
            }

            // The mayor may unclaim a citizen's plot too: the refund and the owner's plot list are the
            // owner's, not the caller's. Taken from the caller, the owner kept the plot in their list
            // and leaving the city later stripped it from whoever had bought it since (R27).
            PlayerInfo owner = plot.getPlotOwner();

            long desired = PlotRefundHelper.CalculatePersonalUnclaimRefund(plot);
            if (desired > 0)
            {
                long affordable = PlotRefundHelper.GetMaxAffordableRefund(plot, desired);
                if (affordable < desired)
                {
                    long captured = affordable;
                    Plot capturedPlot = plot;
                    PlayerInfo capturedPlayerInfo = playerInfo;
                    IServerPlayer capturedPlayer = player;
                    PlayerInfo ownerAtRequest = owner;
                    PlayerInfo plotOwnerAtRequest = plot.getPlotOwner();
                    AgreementHandler.addNewAgreementOrReplace(new Agreement(
                        () =>
                        {
                            // Checked again: while the request waited, the plot may have been unclaimed
                            // and sold to someone else - the late /agree used to take it from them.
                            if (!claims.dataStorage.GetPlot(capturedPlot.plotPosition, out Plot current) || current != capturedPlot
                                || !capturedPlot.hasCity() || capturedPlot.getPlotOwner() != plotOwnerAtRequest
                                || !(capturedPlayerInfo.Equals(ownerAtRequest) || capturedPlot.getCity().isMayor(capturedPlayerInfo)))
                            {
                                MessageHandler.sendMsgToPlayer(capturedPlayer, Lang.Get("claims:plot_was_changed"));
                                return;
                            }
                            if (captured > 0 && !PlotRefundHelper.TryTransferPersonalRefund(capturedPlot, ownerAtRequest, captured))
                            {
                                MessageHandler.sendMsgToPlayer(capturedPlayer, Lang.Get("claims:economy_money_transaction_error"));
                                return;
                            }
                            FinishPersonalUnclaim(capturedPlot, ownerAtRequest, capturedPlayer);
                            MessageHandler.sendMsgToPlayer(capturedPlayer,
                                Lang.Get("claims:refund_unclaim_done_reduced", captured));
                        },
                        player.PlayerUID));
                    string askKey = affordable <= 0
                        ? "claims:refund_unclaim_no_money_ask"
                        : "claims:refund_unclaim_reduced_ask";
                    return SuccessWithParams(askKey, new object[] { affordable, desired, claims.config.AGREEMENT_COMMAND });
                }
                // A refund that did not go through keeps the plot where it was: the owner gave it
                // back for that money (A4).
                if (!PlotRefundHelper.TryTransferPersonalRefund(plot, owner, desired))
                {
                    return TextCommandResult.Error("claims:economy_money_transaction_error");
                }
            }

            FinishPersonalUnclaim(plot, owner, player);
            return TextCommandResult.Success("claims:plot_has_been_unclaimed_by_player");
        }

        /// <param name="owner">Who loses the plot; <paramref name="player"/> is who asked for it.</param>
        private static void FinishPersonalUnclaim(Plot plot, PlayerInfo owner, IServerPlayer player)
        {
            plot.resetOwner();
            // The refund is paid for this purchase, once - as PlotTransferHelper does on a transfer.
            plot.lastPaidPrice = 0;
            owner.PlayerPlots.Remove(plot);
            owner.saveToDatabase();
            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(owner.Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            plot.saveToDatabase();
        }
        /*==============================================================================================*/
        /*=====================================SET======================================================*/
        /*==============================================================================================*/
        public static TextCommandResult SetPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;
            if (!HelperFunctionSetFlag(player, out Plot plotHere, tcr, EnumPlayerPermissions.PLOT_SET_PVP))
            {
                if (plotHere != null) UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }
            //flag is the same
            if (!plotHere.getPermsHandler().setPvp((string)args.LastArg))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }
            plotHere.MarkNoPvp();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(plotHere.getCity().Guid, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult SetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if (!HelperFunctionSetFlag(player, out Plot plotHere, tcr, EnumPlayerPermissions.PLOT_SET_FIRE))
            {
                if (plotHere != null) UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }

            if (!plotHere.getPermsHandler().setFire((string)args.LastArg))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult SetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if (!HelperFunctionSetFlag(player, out Plot plotHere, tcr, EnumPlayerPermissions.PLOT_SET_BLAST))
            {
                if (plotHere != null) UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }

            if (!plotHere.getPermsHandler().setBlast((string)args.LastArg))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        /// <summary>
        /// Keeps hostile mobs from spawning on the plot. Costs the city daily, so it also refreshes
        /// the payment shown to the client, the same way the pvp flag does.
        /// </summary>
        public static TextCommandResult SetNoMobSpawn(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if (!HelperFunctionSetFlag(player, out Plot plotHere, tcr, EnumPlayerPermissions.PLOT_SET_MOBSPAWN))
            {
                if (plotHere != null) UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }

            if (!plotHere.getPermsHandler().setNoMobSpawn((string)args.LastArg))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }
            plotHere.MarkNoMobSpawn();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(plotHere.getCity().Guid, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult SetName(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Success("claims:no_plots_here");
            }
            if (!plot.hasCity())
            {
                return TextCommandResult.Success("claims:no_city_here");
            }
            if(isOwnerOfPlotMayorAdmin(plot, playerInfo, player))
            {
                //return TextCommandResult.Success("claims:not_owner_or_mayor");
            }
            else
            {
                if(plot.getCity() != playerInfo.City || !BaseCommand.CheckForPlayerPermissions(player, new EnumPlayerPermissions[] { EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS, EnumPlayerPermissions.PLOT_SET_NAME }))
                {
                    return TextCommandResult.Success("claims:you_dont_have_right_for_that_command");
                }
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                return TextCommandResult.Success("claims:invalid_plot_name");
            }
            if (!plot.SetPartName(name))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                return TextCommandResult.Success();
            }

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            plot.saveToDatabase();
            return TextCommandResult.Success();
        }
        public static TextCommandResult SetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            int tax = (int)args.LastArg;

            if (tax < 0)
            {
                return TextCommandResult.Error("claims:not_negative");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            if(!claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }

            if(!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            if(!playerInfo.hasCity() || !playerInfo.City.Equals(plotHere.getCity()))
            {
                return TextCommandResult.Error("claims:not_your_city");
            }
            
            if (!plotHere.setCustomTax(tax))
            {
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return TextCommandResult.Success();
            }

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            if(plotHere.hasPlotOwner())
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(plotHere.getPlotOwner().Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            }
            plotHere.saveToDatabase();
            
            return TextCommandResult.Success();
        }
        public static TextCommandResult SetType(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            string typeName = (string)args.Parsers[0].GetValue();

            if (!PlotInfo.nameToPlotType.ContainsKey(typeName))
            {
                return TextCommandResult.Error("");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            if (!claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere))
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }
            TextCommandResult err = CheckCanSetType(player, plotHere);
            if (err != null) return err;

            // Types a player can't set by hand go straight to setNewType, which explains the refusal.
            double setCost = PlotInfo.plotAccessableForPlayersWithCode.ContainsKey(typeName) && !PlotInfo.IsTypeDisabled(typeName)
                ? PlotInfo.GetSetCost(typeName)
                : 0;
            if (setCost <= 0)
            {
                return ApplyPlotType(player, plotHere, typeName, 0);
            }
            if (plotHere.Type == PlotInfo.nameToPlotType[typeName])
            {
                return TextCommandResult.Success("claims:plot_the_same_type_set");
            }
            if (claims.economyProvider.GetBalance(plotHere.getCity().MoneyAccountName) < (decimal)setCost)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(() =>
            {
                // Anything may have changed while the offer waited.
                TextCommandResult result = CheckCanSetType(player, plotHere);
                if (result == null && PlotInfo.GetSetCost(typeName) != setCost)
                {
                    result = TextCommandResult.Error("claims:plot_set_type_cost_changed");
                }
                result ??= ApplyPlotType(player, plotHere, typeName, setCost);
                if (!string.IsNullOrEmpty(result.StatusMessage))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get(result.StatusMessage, result.MessageParams ?? new object[0]));
                }
            }, player.PlayerUID));

            string typeLangKey = PlotInfo.plotAccessableForPlayersWithCode.TryGetValue(typeName, out string key) ? key : typeName;
            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.AGREE_NEEDED_ON_PLOT_TYPE_SET,
                data = typeLangKey + "|" + setCost.ToString(CultureInfo.InvariantCulture)
            }, player);
            return SuccessWithParams("claims:plot_set_type_cost_ask", new object[] { Lang.Get(typeLangKey), setCost, claims.config.AGREEMENT_COMMAND });
        }

        /// <summary>Null when the player may retype this plot, otherwise the reason why not.</summary>
        private static TextCommandResult CheckCanSetType(IServerPlayer player, Plot plot)
        {
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player");
            }
            if (!plot.hasCity() || !playerInfo.hasCity() || !playerInfo.City.Equals(plot.getCity()))
            {
                return TextCommandResult.Error("claims:not_your_city");
            }
            // Retyping the plot - into a war camp, say - would make it unsellable and void the
            // auction when it closes. Cities that bid are owed the lot they bid on.
            if (part.structure.plots.auction.AuctionRegistry.TryGetRunningFor(plot, out var typeLot)
                && typeLot.HasBid)
            {
                return TextCommandResult.Error("claims:plot_auction_has_bids");
            }
            return null;
        }

        private static TextCommandResult ApplyPlotType(IServerPlayer player, Plot plot, string typeName, double setCost)
        {
            City city = plot.getCity();
            if (setCost > 0 && claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)setCost)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            // Paid first, typed second: a failed withdrawal used to be only logged after the type was
            // already set, handing the paid type out for free.
            if (setCost > 0 && claims.economyProvider.Withdraw(city.MoneyAccountName, (decimal)setCost) != MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;
            if (!plot.setNewType(tcr, typeName, player))
            {
                if (setCost > 0 && claims.economyProvider.Deposit(city.MoneyAccountName, (decimal)setCost) != MoneyOperationResult.Success)
                {
                    claims.sapi.Logger.Error("[claims] Plot type set refused, refund failed: {0}, amount {1}", city.MoneyAccountName, setCost);
                }
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                return tcr;
            }
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            city.FirePlotsMapChanged(EnumPlotsMapChangeReason.TypeChanged);
            return tcr;
        }
        public static TextCommandResult SetPermissions(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Success();
            }

            if (args.RawArgs.Length < 3)
            {
                return TextCommandResult.Success();
            }
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (isOwnerOfPlotMayorAdmin(plot, playerInfo, player))
            {
                //return TextCommandResult.Success();
            }
            else
            {
                if (plot.getCity() != playerInfo.City || !BaseCommand.CheckForPlayerPermissions(player, new EnumPlayerPermissions[] { EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS, EnumPlayerPermissions.PLOT_SET_PLOT_ACCESS_PERMISSIONS }))
                {
                    UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                    return TextCommandResult.Success("claims:you_dont_have_right_for_that_command");
                }
            }

            if (!plot.getPermsHandler().setAccessPerm(args.RawArgs))
            {
                return TextCommandResult.Error("claims:wrong_value");
            }

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            plot.saveToDatabase();
            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return TextCommandResult.Success();
        }
        public static TextCommandResult SetForSale(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if(!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            if(!claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot))
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }

            if (!playerInfo.hasCity() || !playerInfo.City.Equals(plot.getCity()))
            {
                return TextCommandResult.Error("claims:not_your_city");
            }

            int price = (int)args.LastArg;

            if (price < 0)
            {
                return TextCommandResult.Error("claims:try_pos");
            }
            string subPlotProblem = subplots.SubPlotLifecycle.BlockingProblem(plot);
            if (subPlotProblem != null)
            {
                return TextCommandResult.Error(subPlotProblem);
            }
            plot.Price = price;
            plot.saveToDatabase();
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return SuccessWithParams("claims:plot_is_for_sale", new object[] { price });
        }
        public static TextCommandResult SetNotForSale(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;

            if(!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            if(!claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot))
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
            if (!playerInfo.hasCity() || !playerInfo.City.Equals(plot.getCity()))
            {
                return TextCommandResult.Error("claims:not_your_city");
            }

            plot.Price = -1;
            plot.saveToDatabase();
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return TextCommandResult.Success("claims:plot_is_not_for_sale");
        }

        /*==============================================================================================*/
        /*=====================================HELPERS==================================================*/
        /*==============================================================================================*/
        /// <summary>
        /// Resolves the plot the player stands on and checks they may change its flags: the plot owner,
        /// the mayor and admins always may, another citizen of the same city needs the flag's own right
        /// or PLOT_SET_ALL_CITY_PLOTS. Same rule as SetName and SetPermissions.
        /// </summary>
        public static bool HelperFunctionSetFlag(IServerPlayer player, out Plot plotHere, TextCommandResult tcr, EnumPlayerPermissions flagPermission)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:plot_not_claimed";
                return false;
            }

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player";
                return false;
            }

            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return false;
            }

            if (isOwnerOfPlotMayorAdmin(plotHere, playerInfo, player))
            {
                return true;
            }

            if (playerInfo.City != plotHere.getCity()
                || !CheckForPlayerPermissions(player, new EnumPlayerPermissions[] { EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS, flagPermission }))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return false;
            }
            return true;
        }
    }
}
