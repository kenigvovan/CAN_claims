using System.Text.RegularExpressions;
using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace claims.src.events
{
    public class OnPlayerChat
    {
        public static void onPlayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed)
        {

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            ClaimsChatType chat;
            claims.dataStorage.getPlayerChatDict().TryGetValue(player.PlayerUID, out chat);
            if (chat == ClaimsChatType.LOCAL)
            {
                MessageHandler.sendLocalMsg(player.Entity.Pos.XYZ, message);
                consumed.value = true;
                return;
            }

           //message = string.Format("<font color=\"" + VtmlUtil.toHexColor(new double[] {0.8, 0.1, .5, 255}) + "\"><strong>{0}:</strong></font> {1}", "KenigVovan", "hello");

            if (channelId != claims.dataStorage.getModChatGroup().Uid && chat == ClaimsChatType.NONE)
            {
                if (playerInfo.hasCity())
                {
                    //message = "<font color=#FFFFFF><strong>PREFIX</strong></font>" + message;
                    Match somePrefix = Regex.Match(message, "<font(.+)/font>");
                    
                    Match onlyMsg = Regex.Match(message, "> (.+)");


                    if (somePrefix.Success)
                    {
                        message = string.Format("{0}{1}{2}{3}",
                        claims.config.SHOW_ALLIANCE_PREFIX_IN_CHAT && playerInfo.HasAlliance() && playerInfo.Alliance.Prefix.Length > 0
                            ? StringFunctions.setStringColor("[", ColorsClaims.WHITE) +
                                StringFunctions.setBold(StringFunctions.setStringColor(playerInfo.Alliance.Prefix, claims.config.ALLIANCE_COLOR_NAME)) +
                                StringFunctions.setStringColor("]", ColorsClaims.WHITE)
                            : "",
                        claims.config.SHOW_CITY_NAME_IN_CHAT
                            ? StringFunctions.setStringColor("[", ColorsClaims.WHITE) +
                                StringFunctions.setBold(StringFunctions.setStringColor(StringFunctions.replaceUnderscore(playerInfo.City.GetPartName()), claims.config.CITY_COLOR_NAME)) +
                                StringFunctions.setStringColor("]", ColorsClaims.WHITE)
                             : "",
                        somePrefix.Success
                             ? somePrefix.Value + " "
                             : playerInfo.getNameForChat() + " ",
                        onlyMsg.Groups[1].Value
                        );
                    }
                    else
                    {

                        //prefix[cityName] playerName message
                        message = string.Format("{0}{1}{2}{3}{4}",
                            somePrefix.Success
                                ? somePrefix.Value
                                : "",
                            claims.config.SHOW_ALLIANCE_PREFIX_IN_CHAT && playerInfo.HasAlliance() && playerInfo.Alliance.Prefix.Length > 0
                                ? StringFunctions.setStringColor("[", ColorsClaims.WHITE) +
                                    StringFunctions.setBold(StringFunctions.setStringColor(playerInfo.Alliance.Prefix, claims.config.ALLIANCE_COLOR_NAME)) +
                                    StringFunctions.setStringColor("]", ColorsClaims.WHITE)
                                : "",
                            claims.config.SHOW_CITY_NAME_IN_CHAT
                                ? StringFunctions.setStringColor("[", ColorsClaims.WHITE) +
                                    StringFunctions.setBold(StringFunctions.setStringColor(StringFunctions.replaceUnderscore(playerInfo.City.GetPartName()), claims.config.CITY_COLOR_NAME)) +
                                    StringFunctions.setStringColor("]", ColorsClaims.WHITE)
                                 : "",
                            somePrefix.Success
                                 ? " "
                                 : playerInfo.getNameForChat() + " ",
                            onlyMsg.Groups[1].Value
                            );
                    }
                    //var c =  Regex.Match(message, "> (.+)");
                }
                else
                {

                }
                return;
            }

            if (chat == ClaimsChatType.CITY)
            {
                MessageHandler.sendMsgInCity(playerInfo.City, message, false);
                consumed.value = true;
            }
            else if (chat == ClaimsChatType.ALLIANCE)
            {
                MessageHandler.SendMsgInAlliance(playerInfo.Alliance, message);
                consumed.value = true;
            }

            if (channelId == claims.dataStorage.getModChatGroup()?.Uid)
            {
                consumed.value = true;
                return;
            }
        }
    }
}
