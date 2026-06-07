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
        /*=====================================SET======================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CitySetInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Error("");
            }
            return TextCommandResult.Success(city.getPermsHandler().getStringForChat() + "\n");
        }
        public static TextCommandResult SetCityName(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.CITY_NAME_CHANGE_COST)
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_NAME);
                return TextCommandResult.Success("claims:not_enough_money");
            }

            if (city.rename((string)args.LastArg))
            {
                if (claims.economyProvider.Withdraw(playerInfo.City.MoneyAccountName, (decimal)claims.config.CITY_NAME_CHANGE_COST) == MoneyOperationResult.Success)
                {
                    return SuccessWithParams("claims:city_name_changed_to", new object[] { (string)args.LastArg });
                }
            }

            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_NAME);
            return TextCommandResult.Error("");
        }
        public static TextCommandResult CitySetPermissions(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_do_not_have_city");
            }

            if (args.RawArgs.Length < 3)
            {
                return TextCommandResult.Success("claims:more_parameters_needed");
            }

            city.getPermsHandler().setAccessPerm(args.RawArgs);
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED);
            return SuccessWithParams("claims:for_group_perm_set_what", new object[] { args.RawArgs[0], args.RawArgs[1], args.RawArgs[2] });
            //return SuccessWithParams("claims:for_group_perm_set_what", new object[] { args[0], args[1], args[2] });
        }
        public static TextCommandResult CitySetInvMsg(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            if (args.LastArg == null)
            {
                city.invMsg = "";
                city.saveToDatabase();
                return TextCommandResult.Success("claims:city_inv_msg_reset");
            }
            StringBuilder sb = new();
            int lenCounter = 0;

            string filteredName = Filter.filterNameWithSpaces((string)args.LastArg);

            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                return TextCommandResult.Success("claims:invalid_string");
            }
            lenCounter += filteredName.Length;
            if (lenCounter > claims.config.MAX_LENGTH_CITY_INV_MSG)
            {
                return TextCommandResult.Success("claims:inv_msg_too_long");
            }
            sb.Append(" ").Append(filteredName);

            city.invMsg = sb.ToString();
            city.saveToDatabase();
            //if empty reset to empty msg
            return TextCommandResult.Success("claims:city_inv_msg_set");
        }
        public static TextCommandResult CitySetPvP(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            city.getPermsHandler().setPvp((string)args.LastArg);
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED);
            return SuccessWithParams("claims:pvp_flag_set_to", new object[] { (string)args.LastArg });
        }
        public static TextCommandResult CitySetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            city.getPermsHandler().setFire((string)args.LastArg);
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED);
            return SuccessWithParams("claims:fire_flag_set_to", new object[] { (string)args.LastArg });
        }
        public static TextCommandResult CitySetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            city.getPermsHandler().setBlast((string)args.LastArg);
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED);
            return SuccessWithParams("claims:blast_flag_set_to", new object[] { (string)args.LastArg });
        }
        public static TextCommandResult CitySetCitizenPrefix(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if(args.LastArg == null)
            {
                return TextCommandResult.Error("claims:no_paramaters");
            }
            string[] playerName_title = ((string)args.LastArg).Split(' ');
            if (playerName_title.Length < 1)
            {
                return TextCommandResult.Error("claims:no_paramaters");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }

            string filteredName = Filter.filterName(playerName_title[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                return TextCommandResult.Error("claims:invalid_name");
            }

            claims.dataStorage.getPlayerByName(filteredName, out PlayerInfo targetPlayer);

            if (targetPlayer == null)
            {
                return TextCommandResult.Error("claims:invalid_name");
            }

            if(!targetPlayer.hasCity() || !targetPlayer.City.Equals(playerInfo.City))
            {
                return TextCommandResult.Error("claims:invalid_name");
            }

            if (playerName_title.Length < 2)
            {
                targetPlayer.Prefix = "";
                targetPlayer.saveToDatabase();
                return SuccessWithParams("claims:citizen_title_reset", new object[] { targetPlayer.GetPartName() });
            }
            filteredName = Filter.filterName(playerName_title[1]);
            //Length == 0 => empty title
            if (!Filter.checkForBlockedNames(filteredName))
            {
                return TextCommandResult.Error("claims:invalid_title");
            }
            if (filteredName.Length > claims.config.MAX_CITIZEN_TITLE_LENGTH)
            {
                return TextCommandResult.Error("claims:citizen_title_is_too_long");
            }

            targetPlayer.Prefix = filteredName;
            targetPlayer.saveToDatabase();
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.GetPartName(), EnumPlayerRelatedInfo.PLAYER_PREFIX);
            return TextCommandResult.Success();
        }
        public static TextCommandResult CitySetOpen(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            if(city.setCityOpenCloseState((string)args.LastArg))
            {
                city.saveToDatabase();
                return TextCommandResult.Success();
            }
            return TextCommandResult.Error("");
        }
        public static TextCommandResult CitySetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }

            int fee = Convert.ToInt32(args.Parsers[0].GetValue());

            if (fee < 0)
            {
                return TextCommandResult.Success("claims:not_negative");
            }
            if (fee > claims.config.MAX_CITY_FEE)
            {
                fee = (int)claims.config.MAX_CITY_FEE;
            }

            city.fee = fee;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_FEE);
            foreach (var citizen in city.getOnlineCitizens())
            {
                if (claims.dataStorage.GetPlayerByUid(citizen.PlayerUID, out PlayerInfo pi))
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(pi.Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            }
            city.saveToDatabase();
            return SuccessWithParams("claims:city_fee_set_to", new object[] { fee });
        }
        public static TextCommandResult CitySetMayor(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Success;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            if (!city.isMayor(playerInfo))
            {
                return TextCommandResult.Success("claims:you_dont_have_right_for_that_command");
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            City targetCity = targetPlayer.City;
            if (targetCity == null || !targetCity.Equals(city))
            {
                return TextCommandResult.Success("claims:player_should_be_in_same_city");
            }

            city.AddLogEntry(EnumCityLogEvent.MayorChanged, targetPlayer.GetPartName());
            city.FireMayorChanged(targetPlayer);
            city.setMayor(targetPlayer);
            if(city.HasAlliance())
            {
                city.Alliance.Leader = targetPlayer;
            }
            RightsHandler.reapplyRights(playerInfo);
            RightsHandler.reapplyRights(targetPlayer);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_now_is_a_mayor", targetPlayer.GetPartName()));
            playerInfo.saveToDatabase();
            targetPlayer.saveToDatabase();
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.MAYOR_NAME, EnumPlayerRelatedInfo.CITY_LOG);
            return SuccessWithParams("claims:player_now_is_a_mayor", new object[] { targetPlayer.GetPartName() });
        }
        public static TextCommandResult CitySetPlotsColor(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:no_city");
            }

            if (playerInfo.City.isMayor(playerInfo))
            {
                if (!ColorHandling.tryFindColor((string)args.LastArg, out int color))
                {
                    return TextCommandResult.Success("claims:unknown_color");
                }
                playerInfo.City.trySetPlotColor(color);
                return SuccessWithParams("claims:color_was_set_to", new object[] { (string)args.LastArg });
            }
            else
            {
                return TextCommandResult.Success("claims:only_for_mayor");
            }
        }
        public static TextCommandResult CitySetPlotsColorInt(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:no_city");
            }

            if (playerInfo.City.isMayor(playerInfo))
            {
                int colorValue;
                try
                {
                    colorValue = int.Parse((string)args.LastArg);
                }
                catch
                {
                    return TextCommandResult.Success("claims:wrong_value");
                }
                playerInfo.City.trySetPlotColor(colorValue);
                UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, EnumPlayerRelatedInfo.CITY_PLOTS_COLOR);
                return SuccessWithParams("claims:color_was_set_to", new object[] { (string)args.LastArg });
            }
            else
            {
                return TextCommandResult.Success("claims:only_for_mayor");
            }
        }
    }
}
