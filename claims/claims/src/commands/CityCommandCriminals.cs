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
        /*=====================================CRIMINAL=================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CityCriminalList(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            tcr.StatusMessage = "claims:criminals";
            tcr.MessageParams = new object[] { StringFunctions.makeStringPlayersName(city.getCriminals(), ',') };

            return tcr;
        }
        public static TextCommandResult CityCriminalAdd(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            if (args.LastArg == null)
            {
                tcr.StatusMessage = "claims:need_player_name";
                return tcr;
            }
            if (!helperFunctionCriminal(player, (string)args.LastArg, out City targetCity, out PlayerInfo targetPlayer, tcr))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST);
                return tcr;
            }
            if (city.getCriminals().Contains(targetPlayer))
            {
                tcr.StatusMessage = "claims:already_added_as_criminal";
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST);
                return tcr;
            }
            city.getCriminals().Add(targetPlayer);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_has_been_added_to_criminals", targetPlayer.GetPartName()));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("criminals:you_were_added_criminals_in_city", city.GetPartName()));
            city.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST);
            return tcr;
        }
        public static TextCommandResult CityCriminalRemove(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            if (args.LastArg == null)
            {
                tcr.StatusMessage = "claims:need_player_name";
                return tcr;
            }
            if (!helperFunctionCriminal(player, (string)args.LastArg, out City targetCity, out PlayerInfo targetPlayer, tcr))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST);
                return tcr;
            }
            if (!city.getCriminals().Contains(targetPlayer))
            {
                tcr.StatusMessage = "claims:not_criminal_here";
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST);
                return tcr;
            }
            city.getCriminals().Remove(targetPlayer);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_has_been_removed_from_criminals", targetPlayer.GetPartName()));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("criminals:you_were_removed_criminals_in_city", city.GetPartName()));
            city.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST);
            return tcr;
        }
        public static bool helperFunctionCriminal(IServerPlayer player, string targetPlayerName, out City targetCity, out PlayerInfo targetPlayer, TextCommandResult tcr)
        {
            targetCity = null;
            targetPlayer = null;
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return false;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }

            string name = Filter.filterName(targetPlayerName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            claims.dataStorage.getPlayerByName(name, out targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            targetCity = targetPlayer.City;
            if (targetCity != null && city.Equals(targetCity))
            {
                tcr.StatusMessage = "claims:kick_before_add";
                return false;
            }
            return true;
        }
    }
}
