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
        /*=====================================SUMMON===================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CitySummonSet(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            if (!helperFunctionSummon(player, tcr, out City city, out PlayerInfo playerInfo))
            {
                return tcr;
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:plot_not_claimed";
                return tcr;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }
            if (!plotHere.getCity().Equals(city))
            {
                tcr.StatusMessage = "claims:not_same_city";
                return tcr;
            }
            if (plotHere.Type != PlotType.SUMMON)
            {
                tcr.StatusMessage = "claims:need_summon_plot";
                return tcr;
            }
            if (plotHere.PlotDesc is not PlotDescSummon summonDesc)
            {
                tcr.StatusMessage = "claims:internal_error";
                return tcr;
            }
            var oldPoint = summonDesc.SummonPoint.Clone();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement(oldPoint.AsVec3i.Clone(),
                    summonDesc.Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_REMOVE);
            summonDesc.SummonPoint = player.Entity.Pos.XYZ.Clone();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement(summonDesc.SummonPoint.AsVec3i.Clone(),
                    summonDesc.Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ADD);
            tcr.StatusMessage = "claims:summon_point_set_to";
            tcr.MessageParams = new object[] { player.Entity.Pos.XYZ.ToString() };
            plotHere.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult CitySummonSetName(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            if (!helperFunctionSummon(player, tcr, out City city, out PlayerInfo playerInfo))
            {
                return tcr;
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:plot_not_claimed";
                return tcr;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }
            if (!plotHere.getCity().Equals(city))
            {
                tcr.StatusMessage = "claims:not_same_city";
                return tcr;
            }
            if (plotHere.Type != PlotType.SUMMON)
            {
                tcr.StatusMessage = "claims:need_summon_plot";
                return tcr;
            }
            if (plotHere.PlotDesc is not PlotDescSummon summonDesc)
            {
                tcr.StatusMessage = "claims:internal_error";
                return tcr;
            }
            string filteredName = Filter.filterName((string)args.LastArg);
            if(filteredName == "")
            {
                tcr.StatusMessage = "claims:name_cannot_be_empty";
                return tcr;
            }
            foreach(var it in city.summonPlots)
            {
                if (it.PlotDesc is PlotDescSummon itDesc && itDesc.Name.Equals(filteredName))
                {
                    tcr.StatusMessage = "claims:need_unique_name";
                    return tcr;
                }
            }
            summonDesc.Name = filteredName;
            tcr.StatusMessage = "claims:summon_point_name_set_to";
            tcr.MessageParams = new object[] { filteredName };
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement(summonDesc.SummonPoint.AsVec3i.Clone(),
                    summonDesc.Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_UPDATE);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult CitySummonSetNameByCoords(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            if (!helperFunctionSummon(player, tcr, out City city, out PlayerInfo playerInfo))
            {
                return tcr;
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);


            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName == "")
            {
                tcr.StatusMessage = "claims:name_cannot_be_empty";
                return tcr;
            }
            var summonPoint = args.Parsers[0].GetValue() as Vec3i;
            /*summonPoint.Add(player.Entity.Api.World.DefaultSpawnPosition.AsBlockPos.X, 0,
                player.Entity.Api.World.DefaultSpawnPosition.AsBlockPos.Z);*/
            bool found = false;
            foreach (var it in city.summonPlots)
            {
                if (it.PlotDesc is not PlotDescSummon itDesc) continue;
                if (itDesc.SummonPoint.AsVec3i.Equals(summonPoint))
                {
                    plotHere = it;
                    found = true;
                }
            }
            if(!found)
            {
                tcr.StatusMessage = "claims:summon_point_not_found";
                return tcr;
            }

            if (!plotHere.getCity().Equals(city))
            {
                tcr.StatusMessage = "claims:not_same_city";
                return tcr;
            }
            if (plotHere.PlotDesc is not PlotDescSummon foundSummonDesc)
            {
                tcr.StatusMessage = "claims:internal_error";
                return tcr;
            }
            foundSummonDesc.Name = filteredName;
            tcr.StatusMessage = "claims:summon_point_name_set_to";
            tcr.MessageParams = new object[] { filteredName };
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement(foundSummonDesc.SummonPoint.AsVec3i.Clone(),
                    foundSummonDesc.Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_UPDATE);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static bool helperFunctionSummon(IServerPlayer player, TextCommandResult tcr, out City city, out PlayerInfo playerInfo)
        {
            city = null;
            playerInfo = null;

            if (!claims.config.SUMMON_ALLOWED)
            {
                tcr.StatusMessage = "claims:summon_is_not_allowed";
                return false;
            }
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return false;
            }
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }
            city = playerInfo.City;
            return true;
        }
        public static TextCommandResult CitySummonList(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            City city = playerInfo.City;
            tcr.StatusMessage = StringFunctions.getSummonPoints(city);
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult CitySummonTeleport(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;

            if (!helperFunctionSummon(player, tcr, out City city, out PlayerInfo playerInfo))
            {
                return tcr;
            }

            long stamp = CooldownHandler.hasCooldown(playerInfo, CooldownType.SUMMON);
            if (stamp != 0)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:wait_before") + TimeFunctions.getHourFromEpochSeconds(stamp));
                return tcr;
            }
            Plot chosenPlot = null;
            string searchStr = (string)args.LastArg;
            foreach (var it in city.summonPlots)
            {
                if (it.PlotDesc is PlotDescSummon itDesc && itDesc.Name.Equals(searchStr))
                {
                    chosenPlot = it;
                }
            }
            if (chosenPlot == null)
            {
                return tcr;
            }
            if (chosenPlot.PlotDesc is not PlotDescSummon chosenSummonDesc)
            {
                tcr.StatusMessage = "claims:internal_error";
                return tcr;
            }
            if (claims.config.SUMMON_MIN_PLAYERS != 0 &&
                claims.sapi.World.GetPlayersAround(chosenSummonDesc.SummonPoint,
                claims.config.SUMMON_HOR_RANGE,
                claims.config.SUMMON_VER_RANGE).Length < claims.config.SUMMON_MIN_PLAYERS)
            {
                tcr.StatusMessage = "claims:need_more_players_for_summon";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }

            if(claims.economyProvider.GetBalance(playerInfo.Guid) < (decimal)claims.config.SUMMON_PAYMENT)
            {
                tcr.StatusMessage = "claims:not_enough_money";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }

            if (claims.economyProvider.Withdraw(playerInfo.Guid, (decimal)claims.config.SUMMON_PAYMENT) != MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }

            if (TeleportationHandler.addTeleportation(new TeleportationInfo(playerInfo,
                chosenSummonDesc.SummonPoint, true, TimeFunctions.getEpochSeconds() + claims.config.SECONDS_SUMMON_TIME)))
            {
                tcr.StatusMessage = "claims:you_will_be_summoned";
                tcr.MessageParams = new object[] { claims.config.SECONDS_SUMMON_TIME };
                tcr.Status = EnumCommandStatus.Success;
            }
            return tcr;
        }
    }
}
