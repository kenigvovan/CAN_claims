using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class AcceptCommand: BaseCommand
    {
        //FOR PLAYERS INVITATIONS
        public static TextCommandResult onCommand(TextCommandCallingArgs args)
        {
            claims.dataStorage.GetPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if(playerInfo == null)
            {
                return TextCommandResult.Error("");
            }

            //try to accept the first and only one invite
            if (args.LastArg == null)
            {
                int invitationsCount = playerInfo.getReceivedInvitations().Count;
                if (invitationsCount == 1)
                {
                    playerInfo.getReceivedInvitations()[0].accept();
                }
                return TextCommandResult.Success();
            }

            //cityName arg is not null, we try to accept invite from that city
            foreach(var invitation in playerInfo.getReceivedInvitations())
            {
                if((invitation.getSender() as City).GetPartName().Equals(args[0]))
                {
                    invitation.accept();
                    return TextCommandResult.Success();
                }
            }
            return TextCommandResult.Success();
        }     
        public static TextCommandResult onAcceptPlotGroup(TextCommandCallingArgs args)
        {
            //FROM ONE CITY PLAYER CAN GET ONLY ONE INVITATION TO GROUP AT THE TIME
            claims.dataStorage.GetPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("");
            }
            if (args.LastArg == null)
            {
                MessageHandler.sendMsgToPlayer(args.Caller.Player as IServerPlayer, StringFunctions.makeFeasibleStringFromNames(
                        StringFunctions.getNamesOfCitiesFromInvitations(Lang.Get("claims:you_have_plotsgroup_invites"), playerInfo.groupInvitations),
                    ','));
                return TextCommandResult.Success();
            }
            string cityName = (string)args.Parsers[0].GetValue();
            string groupName = (string)args.Parsers[1].GetValue();
            foreach (var invitation in playerInfo.groupInvitations)
            {
                if (invitation.Sender.GetPartName().Equals(cityName) && invitation.GroupName.Equals(groupName))
                {
                    invitation.Receiver.groupInvitations.Remove(invitation);
                    invitation.Sender.groupInvitations.Remove(invitation);
                    invitation.accept();
                    return TextCommandResult.Success();
                }
            }
            return TextCommandResult.Success();
        }
        public static TextCommandResult onDenyPlotGroup(TextCommandCallingArgs args)
        {
            claims.dataStorage.GetPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null) return TextCommandResult.Error("");

            string cityName = (string)args.Parsers[0].GetValue();
            string groupName = (string)args.Parsers[1].GetValue();
            foreach (var invitation in playerInfo.groupInvitations)
            {
                if (invitation.Sender.GetPartName().Equals(cityName) && invitation.GroupName.Equals(groupName))
                {
                    invitation.Receiver.groupInvitations.Remove(invitation);
                    invitation.Sender.groupInvitations.Remove(invitation);
                    invitation.reject();
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid,
                        new Dictionary<string, object> { { "value", new ClientToPlotsGroupInvitation(cityName, groupName, 0) } },
                        EnumPlayerRelatedInfo.TO_PLOTS_GROUP_INVITE_REMOVE);
                    return TextCommandResult.Success();
                }
            }
            return TextCommandResult.Success();
        }
        public static TextCommandResult onLeavePlotGroup(TextCommandCallingArgs args)
        {
            claims.dataStorage.GetPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("");
            }
            if (args.LastArg == null)
            {
                List<string> names = new List<string>
                {
                    Lang.Get("claims:your_plotsgroups")
                };
                foreach (var it in claims.dataStorage.getCityPlotsGroupsDict().Values)
                {
                    if(it.PlayersList.Contains(playerInfo))
                    {
                        names.Add(it.City.GetPartName() + ":" + it.GetPartName());
                    }
                }
                return TextCommandResult.Success(StringFunctions.makeFeasibleStringFromNames(names, ','));
            }
            if(!((string)args.LastArg).Contains(":"))
            {
                return TextCommandResult.Success("claims:wrong_format");
            }
            string[] splitted = ((string)args.LastArg).Split(':');
            string cityName = Filter.filterName(splitted[0]);
            if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
            {
                return TextCommandResult.Success("claims:no_city_found");
            }

            claims.dataStorage.GetCityByName(cityName, out City targetCity);

            if(targetCity == null)
            {
                return TextCommandResult.Success("claims:no_city_found");
            }
            string groupName = Filter.filterName(splitted[1]);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                return TextCommandResult.Success("claims:invlaid_group_name");
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in targetCity.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }

            if (searchedGroup == null)
            {
                return TextCommandResult.Success("claims:no_such_group");
            }

            if(!searchedGroup.PlayersList.Contains(playerInfo))
            {
                return TextCommandResult.Success("claims:you_are_not_in_the_group");
            }
            searchedGroup.PlayersList.Remove(playerInfo);
            searchedGroup.saveToDatabase();
            return SuccessWithParams("claims:you_left_plotsgroup", new object[] { searchedGroup.GetPartName() });
        }
        public static TextCommandResult AcceptToAllianceInvitation(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return tcr;
            }
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:player_has_no_city";
                return tcr;
            }
            if (playerInfo.HasAlliance())
            {
                tcr.StatusMessage = "claims:has_alliance_already";
                return tcr;
            }

            int invitationsCount = playerInfo.City.getReceivedInvitations().Count;
            if (args.LastArg == null)
            {
                if (invitationsCount == 0)
                {
                    tcr.StatusMessage = "claims:no_invitations";
                    return tcr;
                }

                MessageHandler.sendMsgToPlayer(player, StringFunctions.makeFeasibleStringFromNames(
                         StringFunctions.getNamesOfAllianciesFromInvitations(
                             Lang.Get("claims:invitations_to_alliance"), playerInfo.City.getReceivedInvitations()),
                    ','));
                return tcr;
            }
            foreach (var invitation in playerInfo.City.getReceivedInvitations())
            {
                if ((invitation.getSender() as Alliance).GetPartName().Equals(args.LastArg))
                {
                    invitation.accept();
                    return tcr;
                }
            }
            tcr.StatusMessage = "claims:no_invitations";
            return tcr;
        }
    }
}
