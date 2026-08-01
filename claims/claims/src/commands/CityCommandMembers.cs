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
        /*=====================================INVITES==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult InviteToCity(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_city_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            City targetCity = targetPlayer.City;
            if (targetCity != null)
            {
                return TextCommandResult.Success("claims:player_has_city_already");
            }
            if (!Settings.CanAcceptMoreCitizens(city))
            {
                return TextCommandResult.Success("claims:village_is_full");
            }
            if (InvitationHandler.addNewInvite(new Invitation(city, targetPlayer, TimeFunctions.getEpochSeconds() + claims.config.HOUR_TIMEOUT_INVITATION_CITY * 60 * 60,
                () =>
                {
                    // The has-city check at invite time is not enough: the target can found
                    // their own city (becoming its mayor) between invite and accept. Joining
                    // here would overwrite their City via setCity and leave their old city's
                    // mayor pointer dangling, desyncing MAYOR_NAME from their permissions.
                    if (targetPlayer.hasCity())
                    {
                        MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_already_have_city"));
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:player_has_city_already"));
                        return;
                    }
                    city.AddLogEntry(EnumCityLogEvent.CitizenJoined, targetPlayer.GetPartName());
                    city.FireCitizenJoined(targetPlayer);
                    city.getCityCitizens().Add(targetPlayer);
                    targetPlayer.setCity(city);
                    city.saveToDatabase();
                    targetPlayer.saveToDatabase();
                    targetPlayer.PlayerCache.Reset();
                    TreeAttribute tree = new TreeAttribute();
                    tree.SetString("cityname", city.GetPartName());
                    claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
                    UsefullPacketsSend.SendPlayerRelatedInfoOnCityJoined(targetPlayer);
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
                },
                () =>
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:player_disagreed_with_invitation_to_city", targetPlayer.GetPartName(), city.GetPartName()));
                }
                )))
            {
                MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_were_invited_to_city", city.GetPartName()));

                var targetIPlayer = claims.sapi.World.PlayerByUid(targetPlayer.Guid);
                if (targetIPlayer != null)
                {
                    Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                    {
                        { EnumPlayerRelatedInfo.CITY_INVITE_ADD, JsonConvert.SerializeObject(new ClientToCityInvitation(city.GetPartName(),
                                                                 TimeFunctions.getEpochSeconds() + claims.config.HOUR_TIMEOUT_INVITATION_CITY * 60 * 60)) }
                    };


                    claims.serverChannel.SendPacket(
                            new PlayerGuiRelatedInfoPacket()
                            {
                                playerGuiRelatedInfoDictionary = collector
                            }
                            , targetIPlayer as IServerPlayer);
                }
                return SuccessWithParams("claims:invitation_to_city_was_sent", new object[] { targetPlayer.GetPartName() });
            }
            else
            {
                return TextCommandResult.Success("claims:player_already_invited_to_city");
            }
        }
        public static TextCommandResult CityKick(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS);
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS);
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            City targetCity = targetPlayer.City;
            if (targetCity == null || !targetCity.Equals(city))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS);
                return TextCommandResult.Success("claims:player_should_be_in_same_city");
            }
            if (city.isMayor(targetPlayer))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS);
                return TextCommandResult.Success("claims:can_not_kick_mayor");
            }
            if (playerInfo.Equals(targetPlayer))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS);
                return TextCommandResult.Success("claims:can_not_kick_yourself");
            }
            city.AddLogEntry(EnumCityLogEvent.CitizenKicked, targetPlayer.GetPartName());
            city.FireCitizenLeft(targetPlayer, EnumCityLeaveReason.Kicked);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_was_kicked", targetPlayer.GetPartName()));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_were_kicked_from_city", city.getPartNameReplaceUnder()));
            targetPlayer.clearCity();
            TreeAttribute tree = new();
            tree.SetString("cityname", city.GetPartName());
            claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
            targetPlayer.PlayerCache.Reset();
            UsefullPacketsSend.SendPlayerRelatedInfoOnKickFromCity(targetPlayer);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            return TextCommandResult.Success();
        }
        public static TextCommandResult CityLeave(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            if (city.isMayor(playerInfo))
            {
                return TextCommandResult.Success("claims:you_are_mayor");
            }
            city.AddLogEntry(EnumCityLogEvent.CitizenLeft, playerInfo.GetPartName());
            city.FireCitizenLeft(playerInfo, EnumCityLeaveReason.Left);
            playerInfo.clearCity();
            TreeAttribute tree = new();
            tree.SetString("cityname", city.GetPartName());
            playerInfo.PlayerCache.Reset();
            claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
            UsefullPacketsSend.SendPlayerRelatedInfoOnKickFromCity(playerInfo);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            return SuccessWithParams("claims:player_left_city", new object[] { playerInfo.getPartNameReplaceUnder() });
        }
        public static TextCommandResult UninviteToCity(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
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
            if (InvitationHandler.removeInvitationIfExists(city, targetPlayer))
            {
                Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                    {
                        { EnumPlayerRelatedInfo.CITY_INVITE_REMOVE, city.GetPartName() }
                    };


                IServerPlayer onlineTarget = claims.sapi.World.PlayerByUid(targetPlayer.Guid) as IServerPlayer;
                if (onlineTarget != null)
                {
                    claims.serverChannel.SendPacket(
                            new PlayerGuiRelatedInfoPacket()
                            {
                                playerGuiRelatedInfoDictionary = collector
                            }
                            , onlineTarget);
                }
                return SuccessWithParams("claims:invitation_for_player_to_city_was_removed", new object[] { city.GetPartName(), targetPlayer.GetPartName() });
            }
            else
            {
                return TextCommandResult.Success("claims:no_invitation");
            }
        }
        public static TextCommandResult ShowInvitesSent(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Success("claims:you_dont_have_city");
            if (args.LastArg == null)
            {
                return TextCommandResult.Success(StringFunctions.getNthPageOf(playerInfo.City.GetSentInvitations(), 1));
            }

            int page = (int)args.LastArg;

            var sentInvites = playerInfo.City.GetSentInvitations();
            if (sentInvites.Count < 1)
            {
                return TextCommandResult.Success("claims:no_invitations");
            }
            return TextCommandResult.Success(StringFunctions.getNthPageOf(playerInfo.City.GetSentInvitations(), page));
        }
        public static TextCommandResult CityJoin(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:has_city_or_village");
            }
            string targetCity = Filter.filterName((string)args.LastArg);
            if (targetCity.Length == 0 || !Filter.checkForBlockedNames(targetCity))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.GetCityByName(targetCity, out City city);
            if (city == null)
            {
                return TextCommandResult.Success("claims:no_such_city");
            }

            if (!city.openCity)
            {
                return TextCommandResult.Success("claims:not_open_city");
            }
            if (!Settings.CanAcceptMoreCitizens(city))
            {
                return TextCommandResult.Success("claims:village_is_full");
            }
            city.AddLogEntry(EnumCityLogEvent.CitizenJoined, playerInfo.GetPartName());
            city.FireCitizenJoined(playerInfo);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_joined_city", playerInfo.getPartNameReplaceUnder()));
            city.getPlayerInfos().Add(playerInfo);
            playerInfo.setCity(city);
            city.saveToDatabase();
            playerInfo.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS);
            UsefullPacketsSend.SendPlayerRelatedInfoOnCityJoined(playerInfo);
            return TextCommandResult.Success();
        }
    }
}
