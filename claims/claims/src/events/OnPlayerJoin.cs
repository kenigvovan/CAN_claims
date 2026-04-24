using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure.conflict;
using claims.src.part.structure.union;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace claims.src.events
{
    public class OnPlayerJoin
    {
        public static void Event_OnPlayerJoin(IServerPlayer player)
        {
            //ADD CHAT WINDOWS
            PlayerGroup modChatGroup = claims.sapi.Groups.GetPlayerGroupByName(claims.config.CHAT_WINDOW_NAME);
            PlayerGroupMembership playerClaimsGroup = player.GetGroup(modChatGroup.Uid);
            if (playerClaimsGroup == null)
            {
                PlayerGroupMembership newChatGroup = new PlayerGroupMembership()
                {
                    GroupName = modChatGroup.Name,
                    GroupUid = modChatGroup.Uid,
                    Level = EnumPlayerGroupMemberShip.Member
                };
                player.ServerData.PlayerGroupMemberships.Add(modChatGroup.Uid, newChatGroup);
                modChatGroup.OnlinePlayers.Add(player);
            }

            claims.dataStorage.addToPlayerChatDict(player.PlayerUID, ClaimsChatType.NONE);
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);            
            //NEW PLAYERINFO
            if (playerInfo == null)
            {
                playerInfo = new PlayerInfo(player.PlayerName, player.PlayerUID);
                claims.dataStorage.addPlayer(playerInfo);
                playerInfo.TimeStampLasOnline = TimeFunctions.getEpochSeconds();
                playerInfo.TimeStampFirstJoined = TimeFunctions.getEpochSeconds();
                //MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:new_player_greetings", player.PlayerName));
            }
            else
            {
                processExistedPlayerInfoOnLogin(playerInfo, player);
            }
            
            RightsHandler.reapplyRights(playerInfo);
            //playerInfo.PlayerCache.Reset();
            playerInfo.saveToDatabase();

            UsefullPacketsSend.sendAllCitiesColorsToPlayer(player);
            UsefullPacketsSend.SendPlayerCityRelatedInfo(player);
            UsefullPacketsSend.SendUpdatedConfigValues(player);
            if(playerInfo.HasAlliance())
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", playerInfo.Alliance.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);

                List<ClientConflictLetterCellElement> li = new List<ClientConflictLetterCellElement>();
                foreach(var it in ConflictHandler.GetAllLettersForParty(playerInfo.Alliance))
                {
                    li.Add(new ClientConflictLetterCellElement(it.From.GetPartName(), it.From.Guid,
                        WarTargetTypeHelper.FromConflictParty(it.From),
                        it.To.GetPartName(), it.To.Guid,
                        WarTargetTypeHelper.FromConflictParty(it.To),
                        it.Purpose, it.TimeStampExpire, it.Guid));
                }
                if (li.Count > 0)
                {
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", li } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ALL);
                }

                List<ClientConflictCellElement> lic = new List<ClientConflictCellElement>();
                foreach (var it in ConflictHandler.GetAllConflictsForParty(playerInfo.Alliance))
                {
                    lic.Add(ClientConflictCellElement.FromConflict(it));
                }
                if (lic.Count > 0)
                {
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", lic } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ALL);
                }

                List<ClientUnionLetterCellElement> unionList = new();
                foreach (var it in UnionHander.GetAllLettersForAlliance(playerInfo.Alliance))
                {
                    unionList.Add(new ClientUnionLetterCellElement(it.From.GetPartName(), it.From.Guid, it.To.GetPartName(), it.To.Guid,
                        it.TimeStampExpire, it.Guid));
                }
                if (unionList.Count > 0)
                {
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", unionList } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ALL);
                }
            }
            else if (playerInfo.hasCity())
            {
                List<ClientConflictLetterCellElement> li = new List<ClientConflictLetterCellElement>();
                foreach (var it in ConflictHandler.GetAllLettersForParty(playerInfo.City))
                {
                    li.Add(new ClientConflictLetterCellElement(it.From.GetPartName(), it.From.Guid,
                        WarTargetTypeHelper.FromConflictParty(it.From),
                        it.To.GetPartName(), it.To.Guid,
                        WarTargetTypeHelper.FromConflictParty(it.To),
                        it.Purpose, it.TimeStampExpire, it.Guid));
                }
                if (li.Count > 0)
                {
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", li } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ALL);
                }

                List<ClientConflictCellElement> lic = new List<ClientConflictCellElement>();
                foreach (var it in ConflictHandler.GetAllConflictsForParty(playerInfo.City))
                {
                    lic.Add(ClientConflictCellElement.FromConflict(it));
                }
                if (lic.Count > 0)
                {
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", lic } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ALL);
                }
            }

            Dictionary<string, ClientCityInfoCellElement> CityStatsCashe =
                ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientCityInfoCellElement>>(claims.sapi,
                "claims:cityinfocache", () => new Dictionary<string, ClientCityInfoCellElement>());
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", CityStatsCashe.Values.ToList() } }, EnumPlayerRelatedInfo.CITY_LIST_ALL);

            Dictionary<string, ClientAllianceInfoCellElement> AllianceStatsCashe =
                ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientAllianceInfoCellElement>>(claims.sapi,
                "claims:allianceinfocache", () => new Dictionary<string, ClientAllianceInfoCellElement>());
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", AllianceStatsCashe.Values.ToList() } }, EnumPlayerRelatedInfo.ALLIANCE_LIST_ALL);
        }
        public static void processExistedPlayerInfoOnLogin(PlayerInfo playerInfo, IServerPlayer player)
        {
            playerInfo.TimeStampLasOnline = TimeFunctions.getEpochSeconds();
            if(player.PlayerName.Equals(playerInfo.GetPartName()))
            {
                playerInfo.SetPartName(player.PlayerName);
            }
        }
    }
}
