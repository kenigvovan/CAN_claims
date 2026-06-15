using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using claims.src.delayed.invitations;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace claims.src.auxialiry
{
    public static class UsefullPacketsSend
    {
        public static ConcurrentDictionary<string, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>>> cityDelayedInfoCollector =
            new ConcurrentDictionary<string, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>>>();

        public static ConcurrentDictionary<string, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>>> playerDelayedInfoCollector =
            new ConcurrentDictionary<string, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>>>();

        public static void sendAllCitiesColorsToPlayer(IServerPlayer player)
        {
            Dictionary<string, int> cityColors = new Dictionary<string, int>();
            foreach (City cityItem in claims.dataStorage.getCitiesList())
            {
                cityColors.Add(cityItem.GetPartName(), cityItem.cityColor);
            }
            string serializedPlots = JsonConvert.SerializeObject(cityColors);

            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.ALL_CITY_COLORS,
                data = serializedPlots
            }, player);
        }       
        public static void SendPlayerCityRelatedInfo(IServerPlayer player)
        {
            Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>();

            if(!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return;
            }

            List<EnumPlayerRelatedInfo> infoToUpdateCity = new();
            List<EnumPlayerRelatedInfo> infoToUpdatePlayer = new();
            City city = null;
            if (playerInfo.hasCity())
            {
                infoToUpdateCity.Add(EnumPlayerRelatedInfo.CITY_NAME);
                city = playerInfo.City;
                if (city.HasMayor())
                {
                    infoToUpdateCity.Add(EnumPlayerRelatedInfo.MAYOR_NAME);
                    if (city.isMayor(playerInfo))
                    {
                        infoToUpdateCity.Add(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                    }
                }

                infoToUpdateCity.AddRange([EnumPlayerRelatedInfo.CITY_GUID, EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, EnumPlayerRelatedInfo.CITY_MEMBERS,
                                           EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CLAIMED_PLOTS,
                                           EnumPlayerRelatedInfo.CITY_PLOTS_COLOR, EnumPlayerRelatedInfo.CITY_DEBT, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT,
                                           EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED, EnumPlayerRelatedInfo.CITY_BALANCE, EnumPlayerRelatedInfo.CITY_FEE, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST,
                                           EnumPlayerRelatedInfo.CITY_PRISON_CELL_ALL, EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ALL, EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ALL,
                                           EnumPlayerRelatedInfo.CITY_LOG, EnumPlayerRelatedInfo.CITY_PLOTS_MAP]);
            }
            infoToUpdatePlayer.AddRange([EnumPlayerRelatedInfo.SHOW_PLOT_MOVEMENT, EnumPlayerRelatedInfo.FRIENDS, EnumPlayerRelatedInfo.TO_CITY_INVITES,
                                         EnumPlayerRelatedInfo.PLAYER_PREFIX, EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, EnumPlayerRelatedInfo.PLAYER_CITY_TITLES,
                                         EnumPlayerRelatedInfo.PLAYER_BALANCE]);
            if(city != null)
            {
                AddToQueueCityInfoUpdate(city.Guid, infoToUpdateCity.ToArray());
            }
                       
            AddToQueuePlayerInfoUpdate(playerInfo.Guid, infoToUpdatePlayer.ToArray());
            
        }
        public static void SendCityRelatedInfoToAllOnlineCitizensOnPlayerJoinCity(City city, List<string> exceptListPlayersUIDs)
        {
            foreach(var onlinePlayer in city.getOnlineCitizens())
            {
                if(exceptListPlayersUIDs.Contains(onlinePlayer.PlayerUID))
                {
                    continue;
                }
                Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                {
                    { EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)) },
                    { EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, Settings.getMaxNumberOfPlotForCity(city).ToString() }
                };
                claims.serverChannel.SendPacket(
                    new SavedPlotsPacket()
                    {
                        data = JsonConvert.SerializeObject(collector),
                        type = PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED
                    }
                    , onlinePlayer as IServerPlayer);
            }
        }
        public static void SendPlayerRelatedInfoOnCityJoined(PlayerInfo playerInfo)
        {
            Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>();
            var player = claims.sapi.World.PlayerByUid(playerInfo.Guid);
            if (player != null)
            {
                if (playerInfo.hasCity())
                {
                    City city = playerInfo.City;
                    collector.Add(EnumPlayerRelatedInfo.CITY_NAME, city.GetPartName());
                    if (city.getMayor() != null)
                    {
                        collector.Add(EnumPlayerRelatedInfo.MAYOR_NAME, city.getMayor().GetPartName());
                    }
                    collector.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, city.TimeStampCreated.ToString());
                    collector.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)));
                    collector.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, JsonConvert.SerializeObject(Settings.getPossibleAmountOfPlotsDictForCity(city)));
                    collector.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, city.getCityPlots().Count.ToString());
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, playerInfo.Prefix);
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, playerInfo.AfterName);
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(playerInfo.getCityTitles()));
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT, JsonConvert.SerializeObject(playerInfo.GetNextPaymentsDict()));
                }
                claims.serverChannel.SendPacket(
                    new SavedPlotsPacket()
                    {
                        data = JsonConvert.SerializeObject(collector),
                        type = PacketsContentEnum.ON_CITY_JOINED
                    }
                    , player as IServerPlayer);
            }
        }
        public static void SendPlayerRelatedInfoOnKickFromCity(PlayerInfo playerInfo)
        {
            var player = claims.sapi.World.PlayerByUid(playerInfo.Guid);
            if (player != null)
            {
                claims.serverChannel.SendPacket(
                    new SavedPlotsPacket()
                    {
                        data = "",
                        type = PacketsContentEnum.ON_KICKED_FROM_CITY
                    }
                    , player as IServerPlayer);
            }
        }
        public static void SendUpdatedConfigValues(IServerPlayer player)
        {
            claims.serverChannel.SendPacket(
                   new ConfigUpdateValuesPacket()
                   {
                       NewCityCost = claims.config.NEW_CITY_COST,
                       NewPlotClaimCost = claims.config.PLOT_CLAIM_PRICE,
                       COINS_VALUES_TO_CODE = claims.config.COINS_VALUES_TO_CODE,
                       ID_TO_COINS_VALUES = claims.config.ID_TO_COINS_VALUES,
                       CITY_NAME_CHANGE_COST = claims.config.CITY_NAME_CHANGE_COST,
                       CITY_BASE_CARE = claims.config.CITY_BASE_CARE,
                       PLOTS_COLORS = Settings.colors,
                       NewAllianceCost = claims.config.NEW_ALLIANCE_COST,
                       SummonPayment = claims.config.SUMMON_PAYMENT,
                       ALWAYS_ACCESS_BLOCKS = claims.config.ALWAYS_ACCESS_BLOCKS,
                       AVAILABLE_CITY_PERMISSIONS = claims.config.AVAILABLE_CITY_PERMISSIONS,
                       SELECTED_ECONOMY_HANDLER = claims.config.SELECTED_ECONOMY_HANDLER,
                       GUI_SHOW_DEBT = claims.config.GUI_SHOW_DEBT,
                       CITY_AREA_VISIBILITY_STATE = claims.config.CITY_AREA_VISIBILITY_STATE,
                       SHOW_BALANCE_HUD_DEFAULT = claims.config.SHOW_BALANCE_HUD_DEFAULT
                   }
                   , player);
        }
        public static void AddToQueueCityInfoUpdate(string cityName, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
        {
            if (cityDelayedInfoCollector.TryGetValue(cityName, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> cityHashSet))
            {
                //such enum was added before, just add new additional info to it
                if (cityHashSet.TryGetValue(toUpdate, out var already_stored_dict))
                {
                    foreach (var value_pair in additionalInfo)
                    {
                        if (already_stored_dict.TryGetValue(value_pair.Key, out var inner_value))
                        {
                            inner_value.Add(value_pair.Value);
                        }
                    }
                }
                else
                {
                    cityHashSet.Add(toUpdate, additionalInfo.ToDictionary(k => k.Key, k => new List<object> { k.Value }));
                }                
            }
            else
            {               
                    cityDelayedInfoCollector.TryAdd(cityName,
                        new Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> { { toUpdate, additionalInfo.ToDictionary(k => k.Key, k => new List<object> { k.Value }) } });
            }
        }
        public static void AddToQueueCityInfoUpdate(string cityName, params EnumPlayerRelatedInfo[] toUpdate)
        {
            if (cityDelayedInfoCollector.TryGetValue(cityName, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> cityHashSet))
            {               
                foreach (var it in toUpdate)
                {
                    cityHashSet.TryAdd(it, null);
                }
            }
            else
            {
                cityDelayedInfoCollector.TryAdd(cityName, toUpdate.ToDictionary(k => k, k => (Dictionary<string, List<object>>)null));
            }
        }
        public static void AddToQueuePlayerInfoUpdate(string playerName, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
        {
            if (playerDelayedInfoCollector.TryGetValue(playerName, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> playerHashSet))
            {
                //such enum was added before, just add new additional info to it
                if (playerHashSet.TryGetValue(toUpdate, out var already_stored_dict))
                {
                    foreach (var value_pair in additionalInfo)
                    {
                        if (already_stored_dict.TryGetValue(value_pair.Key, out var inner_value))
                        {
                            if(value_pair.Value is List<object> listValue)
                            {
                                inner_value.AddRange(listValue);
                            }
                            else
                            {
                                inner_value.Add(value_pair.Value);
                            }
                        }
                    }
                }
                else
                {
                    playerHashSet.Add(toUpdate, additionalInfo.ToDictionary(k => k.Key,
                                                                            k => k.Value is System.Collections.IList list
                                                                                    ? list.Cast<object>().ToList()
                                                                                    : new List<object> { k.Value }));
                }
            }
            else
            {
                playerDelayedInfoCollector.TryAdd(playerName,
                    new Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> { { toUpdate, additionalInfo.ToDictionary(k => k.Key, k => new List<object> { k.Value }) } });
            }
        }
        public static void AddToQueuePlayerInfoUpdate(string playerName, params EnumPlayerRelatedInfo[] toUpdate)
        {
            if (playerDelayedInfoCollector.TryGetValue(playerName, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> playerHashSet))
            {
                foreach (var it in toUpdate)
                {
                    if (!playerHashSet.ContainsKey(it))
                    {
                        playerHashSet.Add(it, null);
                    }
                }
            }
            else
            {
                playerDelayedInfoCollector.TryAdd(playerName, toUpdate.ToDictionary(k => k, k => (Dictionary<string, List<object>>)null));
            }
        }
        public static void AddToQueueAllPlayersInfoUpdate(Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
        {
            foreach(var pl in claims.sapi.World.AllOnlinePlayers)
            {
                AddToQueuePlayerInfoUpdate(pl.PlayerUID, additionalInfo, toUpdate);
            }
        }
        public static void SendAllCollectedCityUpdatesToCitizens()
        {
            while(!cityDelayedInfoCollector.IsEmpty)
            {
                string currentCityGuid = cityDelayedInfoCollector.ElementAt(0).Key;

                if(!cityDelayedInfoCollector.Remove(currentCityGuid, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> listToUpdate)) continue;
                if (!claims.dataStorage.getCityByGUID(currentCityGuid, out City city)) continue;

                var onlinePlayersFromCity = city.getOnlineCitizens();
                //nobody need this info since nobody from the city is online
                if(onlinePlayersFromCity.Count == 0) continue;

                // If a full "ALL" packet is present, incremental adds are redundant
                if (listToUpdate.ContainsKey(EnumPlayerRelatedInfo.CITY_PRISON_CELL_ALL))
                    listToUpdate.Remove(EnumPlayerRelatedInfo.CITY_ADD_PRISON_CELL);
                if (listToUpdate.ContainsKey(EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ALL))
                    listToUpdate.Remove(EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ADD);
                if (listToUpdate.ContainsKey(EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ALL))
                    listToUpdate.Remove(EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ADD);

                Dictionary<EnumPlayerRelatedInfo, string> collector = CollectFullInfo(null, city, listToUpdate);
                
                //collector now contains only general info for all citizens

                foreach (var citizen in onlinePlayersFromCity)
                {
                    var playerCollector = new Dictionary<EnumPlayerRelatedInfo, string>();
                    if (playerDelayedInfoCollector.Remove(citizen.PlayerUID, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> dictInfo))
                    {
                        if (claims.dataStorage.GetPlayerByUid(citizen.PlayerUID, out PlayerInfo playerInfo))
                        {
                            playerCollector = CollectFullInfo(playerInfo, null, dictInfo);

                            if (!playerInfo.PlayerPermissionsHandler.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                            {
                                playerCollector.Remove(EnumPlayerRelatedInfo.CITY_BALANCE);
                            }
                            if (!playerInfo.PlayerPermissionsHandler.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_CITY_RANKS))
                            {
                                playerCollector.Remove(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                            }
                        }
                    }

                    var mergedCollector = new Dictionary<EnumPlayerRelatedInfo, string>(collector);
                    foreach (var kv in playerCollector)
                        mergedCollector.TryAdd(kv.Key, kv.Value);

                    claims.serverChannel.SendPacket(
                        new SavedPlotsPacket
                        {
                            data = JsonConvert.SerializeObject(mergedCollector),
                            type = PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED
                        },
                        citizen as IServerPlayer
                    );
                    if (ServerMain.FrameProfiler.Enabled)
                    {
                        ServerMain.FrameProfiler.Mark("can-claims-packet-city-updates");
                    }
                }
            }

            

            while(!playerDelayedInfoCollector.IsEmpty)
            {
                string currentPlayerUid = playerDelayedInfoCollector.ElementAt(0).Key;
                if (!playerDelayedInfoCollector.Remove(currentPlayerUid,
                                                                    out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> listToUpdate)) continue;

                if (!claims.dataStorage.GetPlayerByUid(currentPlayerUid, out PlayerInfo playerInfo)) continue;
              
                Dictionary<EnumPlayerRelatedInfo, string> collector = CollectFullInfo(playerInfo, playerInfo.hasCity() ? playerInfo.City : null, listToUpdate);
            
                if (collector.Count > 0)
                {
                    var player = claims.sapi.World.PlayerByUid(currentPlayerUid);
                    if (player != null)
                    {
                        claims.serverChannel.SendPacket(
                            new SavedPlotsPacket()
                            {
                                data = JsonConvert.SerializeObject(collector),
                                type = PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED
                            }
                            , player as IServerPlayer);
                    }
                    if (ServerMain.FrameProfiler.Enabled)
                    {
                        ServerMain.FrameProfiler.Mark("can-claims-packet-player-updates");
                    }
                }                                  
            }
        }
        public static void SendCurrentPlotUpdate(IServerPlayer player, Plot plot)
        {
            CurrentPlotInfo cpi = new CurrentPlotInfo(plot.GetPartName(), plot.getPlotOwner()?.GetPartName() ?? "",
                plot.Type, plot.getCustomTax(), plot.Price, plot.getPermsHandler(), plot.extraBought, plot.getPos());
            string serializedZones = JsonConvert.SerializeObject(cpi);

            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.CURRENT_PLOT_INFO,
                data = serializedZones

            }, player);            
        }
        public static void AddToQueueAllianceInfoUpdate(string allianceGuid, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
        {
            if (claims.dataStorage.GetAllianceByGUID(allianceGuid, out var alliance))
            {
                foreach (var city in alliance.Cities)
                {
                    if (cityDelayedInfoCollector.TryGetValue(city.Guid, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> cityHashSet))
                    {
                        //such enum was added before, just add new additional info to it
                        if (cityHashSet.TryGetValue(toUpdate, out var already_stored_dict))
                        {
                            foreach (var value_pair in additionalInfo)
                            {
                                if (already_stored_dict.TryGetValue(value_pair.Key, out var inner_value))
                                {
                                    inner_value.Add(value_pair.Value);
                                }
                            }
                        }
                        else
                        {
                            cityHashSet.Add(toUpdate, additionalInfo.ToDictionary(k => k.Key, k => new List<object> { k.Value }));
                        }
                    }
                    else
                    {
                        cityDelayedInfoCollector.TryAdd(city.Guid,
                            new Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> { { toUpdate, additionalInfo.ToDictionary(k => k.Key, k => new List<object> { k.Value }) } });
                    }
                }
            }
        }
        public static void AddToQueueAllianceInfoUpdate(string allianceGuid, params EnumPlayerRelatedInfo[] toUpdate)
        {
            if (claims.dataStorage.GetAllianceByGUID(allianceGuid, out var alliance))
            {
                foreach (var city in alliance.Cities)
                {
                    if (cityDelayedInfoCollector.TryGetValue(city.Guid, out Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> cityHashSet))
                    {
                        foreach (var it in toUpdate)
                        {
                            cityHashSet.TryAdd(it, null);
                        }
                    }
                    else
                    {
                        cityDelayedInfoCollector.TryAdd(city.Guid, toUpdate.ToDictionary(k => k, k => (Dictionary<string, List<object>>)null));
                    }
                }
            }
        }
        public static void AddToQueueConflictPartyInfoUpdate(IConflictParty party, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
        {
            foreach (var city in party.GetCities())
            {
                AddToQueueCityInfoUpdate(city.Guid, additionalInfo, toUpdate);
            }
        }
        public static void CheckCitisUpdatedAndSend()
        {
            if(claims.dataStorage == null)
            {
                return;
            }
            List<City> updatedCities = new List<City>();
            foreach (var it in claims.dataStorage.getCitiesList())
            {
                if (it.Dirty)
                {
                    updatedCities.Add(it);
                    it.Dirty = false;
                }
            }
            if (updatedCities.Count == 0)
            {
                return;
            }
            Dictionary<string, ClientCityInfoCellElement> CityStatsCashe =
                ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientCityInfoCellElement>>(claims.sapi,
                "claims:cityinfocache", () => new Dictionary<string, ClientCityInfoCellElement>());
            foreach (var it in updatedCities)
            {
                if (CityStatsCashe.TryGetValue(it.Guid, out var stat))
                {
                    stat.UpdateFrom(it);
                }
                else
                {
                    CityStatsCashe.Add(it.Guid, ClientCityInfoCellElement.FromCity(it));
                }
            }
            List<ClientCityInfoCellElement> elToSend = new List<ClientCityInfoCellElement>();
            foreach (var it in updatedCities)
            {
                if (CityStatsCashe.TryGetValue(it.Guid, out var stat))
                {
                    elToSend.Add(stat);
                }
            }
            foreach (var player in claims.sapi.World.AllOnlinePlayers)
            {
                claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    continue;
                }
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", elToSend } }, EnumPlayerRelatedInfo.CITY_LIST_UPDATE);
            }
        }     
        private static string SerializeAllianceInfo(string guid)
        {
            if (claims.dataStorage.GetAllianceByGUID(guid, out var alliance))
            {
                return JsonConvert.SerializeObject(new AllianceInfo(
                    alliance.GetPartName(),
                    alliance.Leader?.GetPartName() ?? "",
                    alliance.TimeStampCreated,
                    alliance.Prefix,
                    StringFunctions.GetPartsNames(alliance.Cities),
                    (double)claims.economyProvider.GetBalance(alliance.MoneyAccountName),
                    alliance.Guid,
                    StringFunctions.GetPartsNames(alliance.ComradAlliancies)
                ));
            }
            return null;
        }
        private static Dictionary<EnumPlayerRelatedInfo, string> CollectFullInfo(PlayerInfo playerInfo, City city, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> info)
        {
            var result = new Dictionary<EnumPlayerRelatedInfo, string>();

            //Add info only connected to a city
            if (city != null)
            {
                foreach (var pair in info)
                {
                    switch (pair.Key)
                    {
                        case EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP:
                            result[pair.Key] = city.TimeStampCreated.ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_MEMBERS:
                            result[pair.Key] = JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city));
                            break;
                        case EnumPlayerRelatedInfo.MAYOR_NAME:
                            result[pair.Key] = city.getMayor()?.GetPartName() ?? "";
                            break;
                        case EnumPlayerRelatedInfo.CITY_NAME:
                            result[pair.Key] = city.GetPartName();
                            break;
                        case EnumPlayerRelatedInfo.CITY_GUID:
                            result[pair.Key] = city.Guid;
                            break;
                        case EnumPlayerRelatedInfo.MAX_COUNT_PLOTS:
                            result[pair.Key] = JsonConvert.SerializeObject(Settings.getPossibleAmountOfPlotsDictForCity(city));
                            break;
                        case EnumPlayerRelatedInfo.CLAIMED_PLOTS:
                            result[pair.Key] = city.getCityPlots().Count.ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_BALANCE:
                            result[pair.Key] = claims.economyProvider.GetBalance(city.MoneyAccountName).ToString(CultureInfo.InvariantCulture);
                            break;
                        case EnumPlayerRelatedInfo.CITY_DEBT:
                            result[pair.Key] = city.DebtBalance.ToString(CultureInfo.InvariantCulture);
                            break;
                        case EnumPlayerRelatedInfo.CITY_FEE:
                            result[pair.Key] = city.fee.ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_PLOTS_COLOR:
                            result[pair.Key] = city.cityColor.ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST:
                            result[pair.Key] = JsonConvert.SerializeObject(StringFunctions.getNamesOfCriminals(city));
                            break;
                        case EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE:
                            result[pair.Key] = null;
                            break;
                        case EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL:
                        case EnumPlayerRelatedInfo.ALLIANCE_NAME:
                            if (pair.Value.TryGetValue("value", out var allianceList) && allianceList.Count > 0)
                            {
                                var allianceJson = SerializeAllianceInfo((string)allianceList[0]);
                                if (allianceJson != null)
                                    result[pair.Key] = allianceJson;
                            }
                            break;
                        case EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS:
                            result[pair.Key] = JsonConvert.SerializeObject(city.CustomCityRanks);
                            break;
                        case EnumPlayerRelatedInfo.ALLIANCE_BALANCE:
                            if (city.Alliance != null)
                                result[pair.Key] = claims.economyProvider.GetBalance(city.Alliance.MoneyAccountName).ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_DAY_PAYMENT:
                            result[pair.Key] = city.GetDayPaymentAmount().ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED:
                            result[pair.Key] = JsonConvert.SerializeObject(city.getPermsHandler());
                            break;
                        case EnumPlayerRelatedInfo.CITY_LOG:
                            result[pair.Key] = JsonConvert.SerializeObject(city.EventLog);
                            break;
                        case EnumPlayerRelatedInfo.CITY_PLOTS_MAP:
                            result[pair.Key] = JsonConvert.SerializeObject(city.getCityPlots().Select(p => new CityPlotMiniInfo(p.plotPosition.X, p.plotPosition.Z, p.Type)).ToList());
                            break;
                        case EnumPlayerRelatedInfo.CITY_PRISON_CELL_ALL:
                            if (city.hasPrison())
                            {
                                HashSet<PrisonCellElement> prisonCellElements = new HashSet<PrisonCellElement>();
                                foreach (var it in city.getPrisons())
                                {
                                    foreach (PrisonCellInfo cell in it.getPrisonCells())
                                    {
                                        prisonCellElements.Add(new PrisonCellElement(cell.getSpawnPosition(), cell.GetPlayersNames()));
                                    }
                                }
                                result[pair.Key] = JsonConvert.SerializeObject(prisonCellElements);
                            }
                            break;
                        case EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ALL:
                            if (city.summonPlots.Count > 0)
                            {
                                HashSet<SummonCellElement> summonCellElements = new HashSet<SummonCellElement>();
                                foreach (var it in city.summonPlots)
                                {
                                    if (it.PlotDesc is not PlotDescSummon desc) continue;
                                    summonCellElements.Add(new SummonCellElement(desc.SummonPoint.AsVec3i.Clone(), desc.Name));
                                }
                                result[pair.Key] = JsonConvert.SerializeObject(summonCellElements);
                            }
                            break;
                        case EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ALL:
                            if (city.getCityPlotsGroups().Count > 0)
                            {
                                HashSet<PlotsGroupCellElement> plotsgroupCellElements = new HashSet<PlotsGroupCellElement>();
                                foreach (var it in city.getCityPlotsGroups())
                                {
                                    plotsgroupCellElements.Add(
                                        new PlotsGroupCellElement(it.Guid, it.GetPartName(), it.City.GetPartName(),
                                                                  it.PlayersList.Select(pl => pl.GetPartName()).ToList(),
                                                                  it.PermsHandler, it.PlotsGroupFee));
                                }
                                result[pair.Key] = JsonConvert.SerializeObject(plotsgroupCellElements);
                            }
                            break;
                        //PLAYER_NEXT_PAYMENT
                        case EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT:
                            if (playerInfo == null) break;
                            var dict = playerInfo.GetNextPaymentsDict();
                            result[pair.Key] = JsonConvert.SerializeObject(dict);
                            break;
                        default:
                            if (pair.Value?.TryGetValue("value", out var list) ?? false)
                                result[pair.Key] = JsonConvert.SerializeObject(list);
                            break;
                    }
                }
            }

            if (playerInfo != null)
            {
                if (!playerInfo.PlayerPermissionsHandler.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                {
                    info.Remove(EnumPlayerRelatedInfo.CITY_BALANCE);
                }
                if (!playerInfo.PlayerPermissionsHandler.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_CITY_RANKS))
                {
                    info.Remove(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                }
                foreach (var pair in info)
                {
                    switch (pair.Key)
                    {
                        case EnumPlayerRelatedInfo.PLAYER_PERMISSIONS:
                            result[pair.Key] = JsonConvert.SerializeObject(playerInfo.PlayerPermissionsHandler.GetPermissions());
                            break;
                        case EnumPlayerRelatedInfo.PLAYER_PREFIX:
                            result[pair.Key] = playerInfo.Prefix;
                            break;
                        case EnumPlayerRelatedInfo.PLAYER_AFTER_NAME:
                            result[pair.Key] = playerInfo.AfterName;
                            break;
                        case EnumPlayerRelatedInfo.PLAYER_CITY_TITLES:
                            result[pair.Key] = JsonConvert.SerializeObject(playerInfo.getCityTitles());
                            break;
                        case EnumPlayerRelatedInfo.FRIENDS:
                            result[pair.Key] = JsonConvert.SerializeObject(StringFunctions.getNamesOfFriends(playerInfo));
                            break;
                        case EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE:
                            result[pair.Key] = null;
                            break;
                        case EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL:
                        case EnumPlayerRelatedInfo.ALLIANCE_NAME:
                            if (pair.Value.TryGetValue("value", out var allianceList) && allianceList.Count > 0)
                            {
                                var allianceJson = SerializeAllianceInfo((string)allianceList[0]);
                                if (allianceJson != null)
                                    result[pair.Key] = allianceJson;
                            }
                            break;
                        case EnumPlayerRelatedInfo.CITY_PLOT_RECOLOR:
                            if (pair.Value.TryGetValue("value", out var plotToRecolor) && plotToRecolor.Count > 0)
                            {
                                var allianceJson = JsonConvert.SerializeObject(plotToRecolor);
                                if (allianceJson != null)
                                    result[pair.Key] = allianceJson;
                            }
                            break;
                        case EnumPlayerRelatedInfo.TO_CITY_INVITES:
                            result[pair.Key] = JsonConvert.SerializeObject(InvitationHandler.getInvitesForReceiver(playerInfo));
                            break;
                        case EnumPlayerRelatedInfo.SHOW_PLOT_MOVEMENT:
                            result[pair.Key] = ((int)playerInfo.showPlotMovement).ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_LIST_ALL:
                            Dictionary<string, ClientCityInfoCellElement> CityStatsCashe =
                            ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientCityInfoCellElement>>(claims.sapi,
                            "claims:cityinfocache", () => new Dictionary<string, ClientCityInfoCellElement>());
                            if (CityStatsCashe.Count > 0)
                            {
                                result[pair.Key] = JsonConvert.SerializeObject(CityStatsCashe.Values.ToList());
                            }
                            break;
                        case EnumPlayerRelatedInfo.ALLIANCE_LIST_ALL:
                            Dictionary<string, ClientAllianceInfoCellElement> AllianceStatsCashe =
                            ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientAllianceInfoCellElement>>(claims.sapi,
                            "claims:allianceinfocache", () => new Dictionary<string, ClientAllianceInfoCellElement>());
                            if (AllianceStatsCashe.Count > 0)
                            {
                                result[pair.Key] = JsonConvert.SerializeObject(AllianceStatsCashe.Values.ToList());
                            }
                            break;
                        case EnumPlayerRelatedInfo.PLAYER_BALANCE:
                            result[pair.Key] = claims.economyProvider.GetBalance(playerInfo.Guid)
                                .ToString(System.Globalization.CultureInfo.InvariantCulture);
                            break;
                        default:
                            if (pair.Value?.TryGetValue("value", out var list) ?? false)
                                result[pair.Key] = JsonConvert.SerializeObject(list);
                            break;
                    }
                }
            }

            return result;
        }
    }
}
