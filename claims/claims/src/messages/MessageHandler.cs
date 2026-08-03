using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.messages
{
    public class MessageHandler
    {

        public static void sendGlobalMsg(string msg)
        {
            foreach (IPlayer player in claims.sapi.World.AllOnlinePlayers)
            {
                 ((IServerPlayer)player).SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("claims:mod_default_mark") + msg, EnumChatType.Notification);
            }
        }
        public static void sendMsgInCity(City city, string msg, bool useCityName = true)
        {
            foreach(IServerPlayer citizen in city.getOnlineCitizens())
            {
                if (claims.config.USE_MOD_CHAT_WINDOW)
                {
                    citizen.SendMessage(claims.dataStorage.getModChatGroup().Uid,
                        "|C|" + 
                        (useCityName
                            ? StringFunctions.setStringColor("[", ColorsClaims.WHITE) +  StringFunctions.setBold(StringFunctions.setStringColor(StringFunctions.replaceUnderscore(city.GetPartName()), ColorsClaims.CITY_NAME) + StringFunctions.setStringColor("]", ColorsClaims.WHITE))
                            : "" ) +
                        msg, EnumChatType.Notification);
                }
                else
                {
                    citizen.SendMessage(GlobalConstants.GeneralChatGroup, "|C|" + StringFunctions.setStringColor("[", ColorsClaims.WHITE) +
                       StringFunctions.setBold(StringFunctions.setStringColor(StringFunctions.replaceUnderscore(city.GetPartName()),
                       ColorsClaims.CITY_NAME)) + StringFunctions.setStringColor("]", ColorsClaims.WHITE) +
                       msg, EnumChatType.Notification);
                }
            }
        }
        public static void SendMsgInAlliance(Alliance alliance, string msg)
        {
            foreach (IPlayer player in alliance.getOnlineCitizens())
            {
                if (claims.config.USE_MOD_CHAT_WINDOW)
                    ((IServerPlayer)player).SendMessage(claims.dataStorage.getModChatGroup().Uid, msg, EnumChatType.Notification);
                else
                    ((IServerPlayer)player).SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
            }
        }
        public static void SendMsgInAlliance(IConflictParty party, string msg)
        {
            foreach (City city in party.GetCities())
                sendMsgInCity(city, msg, false);
        }
        public static void sendLocalMsg(Vec3d pos, string msg)
        {
            foreach (var it in claims.sapi.World.AllOnlinePlayers)
            {
                if (it.Entity.Pos.DistanceTo(pos) <= claims.config.LOCAL_CHAT_DISTANCE)
                {
                    sendMsgToPlayer(it as IServerPlayer, "|L|" + msg);
                }
            }
        }
        public static void sendMsgToPlayerInfo(PlayerInfo receiver, string msg)
        {
            sendMsgToPlayer(claims.sapi.World.PlayerByUid(receiver.Guid) as IServerPlayer, msg);
        }
        public static void sendMsgToPlayer(IServerPlayer receiver, string msg)
        {
            if (receiver == null)
            {
                return;
            }
            if(claims.config.USE_MOD_CHAT_WINDOW)
            {
                receiver.SendMessage(claims.dataStorage.getModChatGroup().Uid, msg, EnumChatType.Notification);
            }
            else
            {
                // Without this fallback every personal mod message is silently dropped when the
                // dedicated chat window is disabled, unlike sendMsgInCity / SendMsgInAlliance.
                receiver.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
            }
        }
        public static void sendDebugMsg(string msg)
        {
            claims.sapi.Logger.Debug(msg);
        }
        public static void sendErrorMsg(string msg)
        {
            claims.sapi.Logger.Error(msg);
        }
        public static void SendDiscoveryToAlliance(Alliance alliance, string discoveryCode, string text, object[] args)
        {
            if (alliance == null)
            {
                return;
            }
            foreach (IPlayer player in alliance.getOnlineCitizens())
            {
                claims.sapi.SendIngameDiscovery(player as IServerPlayer, discoveryCode, text, args);
                claims.sapi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/deepbell"), player.Entity, null, false, 5f, 0.5f);
            }
        }
        public static void SendDiscoveryToAlliance(IConflictParty party, string discoveryCode, string text, object[] args)
        {
            if (party == null) return;
            foreach (City city in party.GetCities())
                foreach (IServerPlayer player in city.getOnlineCitizens())
                {
                    claims.sapi.SendIngameDiscovery(player, discoveryCode, text, args);
                    claims.sapi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/deepbell"), player.Entity, null, false, 5f, 0.5f);
                }
        }
    }
}
