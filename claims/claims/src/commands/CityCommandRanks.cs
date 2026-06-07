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
        /*=====================================RANKS====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CityRankList(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            if (args.LastArg == null)
            {
                return TextCommandResult.Success(StringFunctions.concatStringsWithPrefixAndDelim(
                    Lang.Get("claims:your_ranks"),
                    playerInfo.getCityTitles(),
                    ", "));
            }

            string targetNames = Filter.filterName((string)args.LastArg); ;
            if (targetNames.Length == 0 || !Filter.checkForBlockedNames(targetNames))
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            claims.dataStorage.getPlayerByName(targetNames, out PlayerInfo targetPlayerInfo);
            if (targetPlayerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (playerInfo.hasCity() && playerInfo.City.Equals(targetPlayerInfo.City))
            {
                return TextCommandResult.Success(StringFunctions.concatStringsWithPrefixAndDelim(
                    Lang.Get("claims:player_ranks", targetPlayerInfo.GetPartName()),
                    targetPlayerInfo.getCityTitles(),
                    ", "));
            }
            return TextCommandResult.Success();

        }
        public static TextCommandResult CityRankAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            string rank_name = (string)args.Parsers[0].GetValue();
            string player_name = (string)args.Parsers[1].GetValue();

            if (!HelperFunctionRank(player, rank_name, player_name, out City city, out PlayerInfo targetPlayer, tcr))
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                return tcr;
            }
            if (targetPlayer.getCityTitles().Contains(rank_name))
            {
                tcr.StatusMessage = "claims:player_already_has_rank";
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                return tcr;
            }
            city.GrantPlayerRank(rank_name, targetPlayer);
            targetPlayer.addCityTitle(rank_name);
            RightsHandler.reapplyRights(targetPlayer);
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:rank_added_to_player", targetPlayer.GetPartName(), rank_name));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_got_now_rank", rank_name));
            targetPlayer.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_CITY_TITLES);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);

            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult CityRankRemove(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (args.LastArg == null)
            {
                return tcr;
            }
            string[] rank_and_player_name = ((string)args.LastArg).Split(' ');
            if (rank_and_player_name.Length < 2)
            {
                return tcr;
            }

            if (!HelperFunctionRank(player, rank_and_player_name[0], rank_and_player_name[1], out City city, out PlayerInfo targetPlayer, tcr))
            {
                return tcr;
            }
            if (!targetPlayer.getCityTitles().Contains(rank_and_player_name[0]))
            {
                tcr.StatusMessage = "claims:player_doesnt_have_this_title";
                return tcr;
            }
            city.RevokePlayerRank(rank_and_player_name[0], targetPlayer);
            targetPlayer.removeCityTitle(rank_and_player_name[0]);
            RightsHandler.reapplyRights(targetPlayer);
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:rank_was_deleted", rank_and_player_name[0], targetPlayer.GetPartName()));
            targetPlayer.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_CITY_TITLES);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
            return SuccessWithParams("claims:rank_removed_from_player", new object[] { rank_and_player_name[0], targetPlayer.GetPartName() });
        }
        /*NEW RANKS*/
        public static TextCommandResult CityRankCreateCustom(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            string rank_name = (string)args.Parsers[0].GetValue();

            if(!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            City city = playerInfo.City;

           /* if(!city.isMayor(playerInfo))
            {
                tcr.StatusMessage = "claims:not_a_mayor";
                return tcr;
            }*/

            if (city.HasCityRank(rank_name))
            {
                tcr.StatusMessage = "claims:rank_already_exists";
                return tcr;
            }

            city.AddNewCityRank(rank_name, new CustomCityRank() { Name = rank_name , Permissions = new()});
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
            tcr.StatusMessage = "claims:rank_added";
            return tcr;
        }
        public static TextCommandResult CityRankDeleteCustom(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            string rank_name = (string)args.Parsers[0].GetValue();

            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }

            City city = playerInfo.City;

            /* if(!city.isMayor(playerInfo))
             {
                 tcr.StatusMessage = "claims:not_a_mayor";
                 return tcr;
             }*/

            if (!city.HasCityRank(rank_name))
            {
                tcr.StatusMessage = "claims:no_such_rank";
                return tcr;
            }

            city.RemoveCityRank(rank_name);
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
            tcr.StatusMessage = "claims:rank_removed";
            return tcr;
        }
        public static TextCommandResult CityRankAddPermissions(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            if(!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            string rankName = args.Parsers[0].GetValue().ToString();
            if(!city.CustomCityRanks.TryGetValue(rankName, out var foundRank))
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                return TextCommandResult.Success("claims:no_such_rank");
            }
            string[] allPermissions = args.Parsers[1].GetValue().ToString().Split(' ');
            List<EnumPlayerPermissions> permissionList = new();
            foreach(var it in allPermissions)
            {
                try
                {
                    var tmpVal = (EnumPlayerPermissions)Enum.Parse(typeof(EnumPlayerPermissions), it);
                    permissionList.Add(tmpVal);
                }
                catch(ArgumentException)
                {
                    continue;
                }

            }
            bool newPermWasAdded = false;
            foreach(var it in permissionList)
            {
                newPermWasAdded |= foundRank.Permissions.Add(it);
            }
            if(newPermWasAdded)
            {
                foreach(var citizen in city.getCityCitizens())
                {
                    if(citizen.hasCityTitle(rankName))
                    {
                        RightsHandler.reapplyRights(citizen);
                    }
                }
            }
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult CityRankRemovePermissions(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            string rankName = args.Parsers[0].GetValue().ToString();
            if (!city.CustomCityRanks.TryGetValue(rankName, out var foundRank))
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                return TextCommandResult.Success("claims:no_such_rank");
            }
            string[] allPermissions = args.Parsers[1].GetValue().ToString().Split(' ');
            List<EnumPlayerPermissions> permissionList = new();
            foreach (var it in allPermissions)
            {
                try
                {
                    var tmpVal = (EnumPlayerPermissions)Enum.Parse(typeof(EnumPlayerPermissions), it);
                    permissionList.Add(tmpVal);
                }
                catch (ArgumentException)
                {
                    continue;
                }

            }
            bool permWasRemove = false;
            foreach (var it in permissionList)
            {
                permWasRemove |= foundRank.Permissions.Remove(it);
            }
            if (permWasRemove)
            {
                foreach (var citizen in city.getCityCitizens())
                {
                    if (citizen.hasCityTitle(rankName))
                    {
                        RightsHandler.reapplyRights(citizen);
                    }
                }
            }
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
            city.saveToDatabase();
            return tcr;
        }
    }
}
