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
        /*=====================================CLAIM====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult ClaimCityPlot(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }

            if (city.getCityPlots().Count >= Settings.getMaxNumberOfPlotForCity(city))
            {
                return TextCommandResult.Error("claims:max_amount_claimed");
            }

            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            if (claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere))
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.PLOT_CLAIM_PRICE)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city");
            }

            if(!claims.dataStorage.CheckClaimLimiters(playerInfo, currentPlotPosition))
            {
                return TextCommandResult.Error("claims:too_close_to_forbidden_area");
            }

            if (!CheckForAtleastOneClaimedPlotOnBorderSameCity(plotHere))
            {
                return TextCommandResult.Error("claims:should_be_on_the_border_with_another_claimed_plot");
            }

            if(claims.economyProvider.Withdraw(city.MoneyAccountName, (decimal)claims.config.PLOT_CLAIM_PRICE) != MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            plotHere.setCity(playerInfo.City);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.Price = -1;
            plotHere.TimeStampClaimed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());

            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            city.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);

            plotHere.CheckBorderPlotValue();

            return SuccessWithParams("claims:plot_has_been_claimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, claims.config.PLOT_CLAIM_PRICE });
        }
        public static TextCommandResult UnclaimCityPlot(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }

            if (!plotHere.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }

            if (!plotHere.getCity().Equals(city))
            {
                return TextCommandResult.Error("claims:player_should_be_in_same_city");
            }
            if (plotHere.getCity().getCityPlots().Count == 1)
            {
                return TextCommandResult.Error("claims:last_city_plot");
            }
            if (plotHere.extraBought)
            {
                city.Extrachunksbought--;
                city.saveToDatabase();
            }
            PlotRefundHelper.RefundOnCityUnclaim(plotHere);
            PartDemolition.demolishCityPlot(plotHere);

            claims.serverPlayerMovementListener.markPlotToWasRemoved(plotHere.getPos());
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            city.FirePlotsMapChanged(EnumPlotsMapChangeReason.Unclaimed);
            plotHere.CheckBorderPlotValue();
            return SuccessWithParams("claims:plot_has_been_unclaimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y });
        }
        public static TextCommandResult ClaimOutpost(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (city.getCityPlots().Count >= Settings.getMaxNumberOfPlotForCity(city))
            {
                return TextCommandResult.Error("claims:max_amount_claimed");
            }
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.OUTPOST_PLOT_COST)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city");
            }

            if(!claims.dataStorage.OutpostPlotHasDistantEnoughFromOtherCityPlots(plotHere))
            {
                return ErrorWithParams("claims:doesnt_satisfy_outpost_requirements", new object[] { claims.config.MAX_OUTPOST_DISTANCE_FROM_CITY, claims.config.MIN_OUTPOST_DISTANCE_FROM_CITY });
            }

            if (!claims.dataStorage.CheckClaimLimiters(playerInfo, currentPlotPosition))
            {
                return TextCommandResult.Error("claims:too_close_to_forbidden_area");
            }
            if (claims.economyProvider.Withdraw(city.MoneyAccountName, (decimal)claims.config.OUTPOST_PLOT_COST) != MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            plotHere.setCity(playerInfo.City);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.Price = -1;
            plotHere.TimeStampClaimed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());

            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            city.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);
            plotHere.CheckBorderPlotValue();
            return SuccessWithParams("claims:plot_has_been_claimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, claims.config.OUTPOST_PLOT_COST });
        }
        public static TextCommandResult ProcessExtraPlot(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);

            if (city.Extrachunksbought >= Settings.getMaxNumberOfExtraChunksBought(city))
            {
                return TextCommandResult.Error("claims:max_amount_claimed");
            }
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.EXTRA_PLOT_COST)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city");
            }

            if(claims.economyProvider.Withdraw(city.MoneyAccountName, (decimal)claims.config.EXTRA_PLOT_COST) != MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.Price = -1;
            plotHere.extraBought = true;
            plotHere.TimeStampClaimed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            city.Extrachunksbought++;
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();

            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());

            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            city.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);
            plotHere.CheckBorderPlotValue();
            //return SuccessWithParams("claims:plot_has_been_claimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, claims.config.PLOT_CLAIM_PRICE });
            return SuccessWithParams("claims:plot_has_been_claimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, claims.config.PLOT_CLAIM_PRICE });
        }
    }
}
