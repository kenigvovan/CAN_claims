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
using claims.src.part.structure.plots.auction;
using claims.src.part.structure.war;
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
        /// <summary>Sends the coat of arms of every city and alliance, keyed by guid - what the
        /// drawers hold, and unlike a name it does not change.</summary>
        public static void sendAllCityEmblemsToPlayer(IServerPlayer player)
        {
            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.ALL_CITY_EMBLEMS,
                data = JsonConvert.SerializeObject(BuildEmblemDict())
            }, player);
        }

        /// <summary>Pushes the emblems to everyone. Sent whole, not as a delta - a few hundred short
        /// strings at most, and a missed update would otherwise persist until reconnect.</summary>
        public static void BroadcastCityEmblems()
        {
            string data = JsonConvert.SerializeObject(BuildEmblemDict());
            foreach (var p in claims.sapi.World.AllOnlinePlayers)
            {
                if (p is not IServerPlayer sp) continue;
                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.ALL_CITY_EMBLEMS,
                    data = data
                }, sp);
            }
        }

        private static Dictionary<string, string> BuildEmblemDict()
        {
            var emblems = new Dictionary<string, string>();
            foreach (City cityItem in claims.dataStorage.getCitiesList())
            {
                if (string.IsNullOrEmpty(cityItem.Emblem)) continue;
                emblems[cityItem.Guid] = cityItem.Emblem;
            }
            foreach (Alliance alliance in claims.dataStorage.getAllAlliances())
            {
                if (string.IsNullOrEmpty(alliance.Emblem)) continue;
                emblems[alliance.Guid] = alliance.Emblem;
            }
            return emblems;
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

                infoToUpdateCity.AddRange([EnumPlayerRelatedInfo.CITY_GUID, EnumPlayerRelatedInfo.CITY_TIER, EnumPlayerRelatedInfo.CITY_RAID_WINDOW,
                                           EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, EnumPlayerRelatedInfo.CITY_MEMBERS,
                                           EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CLAIMED_PLOTS,
                                           EnumPlayerRelatedInfo.CITY_PLOTS_COLOR, EnumPlayerRelatedInfo.CITY_DEBT, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT,
                                           EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED, EnumPlayerRelatedInfo.CITY_BALANCE, EnumPlayerRelatedInfo.CITY_FEE, EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST,
                                           EnumPlayerRelatedInfo.CITY_PRISON_CELL_ALL, EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ALL, EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ALL,
                                           EnumPlayerRelatedInfo.CITY_LOG, EnumPlayerRelatedInfo.CITY_PLOTS_MAP,
                                           EnumPlayerRelatedInfo.CITY_EMBLEM, EnumPlayerRelatedInfo.ALLIANCE_EMBLEM,
                                           EnumPlayerRelatedInfo.CITY_PLOT_AUCTIONS, EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY]);
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
                    collector.Add(EnumPlayerRelatedInfo.CITY_TIER, ((int)city.Tier).ToString());
                    collector.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, city.TimeStampCreated.ToString());
                    collector.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)));
                    collector.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, JsonConvert.SerializeObject(Settings.getPossibleAmountOfPlotsDictForCity(city)));
                    collector.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, city.getCityPlots().Count.ToString());
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, playerInfo.Prefix);
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, playerInfo.AfterName);
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(playerInfo.getCityTitles()));
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT, JsonConvert.SerializeObject(playerInfo.GetNextPaymentsDict()));
                    // The land market is not part of this packet, and the joiner's CityInfo was just
                    // rebuilt empty - without this their market tab stays blank until some other
                    // city happens to change a listing. Queued rather than inlined so the snapshot is
                    // built in the one place that knows how.
                    AddToQueueCityInfoUpdate(city.Guid,
                        EnumPlayerRelatedInfo.CITY_PLOT_AUCTIONS, EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY);
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
                       COIN_DENOMINATIONS = claims.config.COIN_DENOMINATIONS,
                       CITY_NAME_CHANGE_COST = claims.config.CITY_NAME_CHANGE_COST,
                       CITY_BASE_CARE = claims.config.CITY_BASE_CARE,
                       PLOTS_COLORS = Settings.colors,
                       NewAllianceCost = claims.config.NEW_ALLIANCE_COST,
                       SummonPayment = claims.config.SUMMON_PAYMENT,
                       ALWAYS_ACCESS_BLOCKS = claims.config.ALWAYS_ACCESS_BLOCKS,
                       AVAILABLE_CITY_PERMISSIONS = claims.config.AVAILABLE_CITY_PERMISSIONS,
                       DISABLED_PLOT_TYPES = claims.config.DISABLED_PLOT_TYPES,
                       SELECTED_ECONOMY_HANDLER = claims.config.SELECTED_ECONOMY_HANDLER,
                       GUI_SHOW_DEBT = claims.config.GUI_SHOW_DEBT,
                       CITY_AREA_VISIBILITY_STATE = claims.config.CITY_AREA_VISIBILITY_STATE,
                       SHOW_BALANCE_HUD_DEFAULT = claims.config.SHOW_BALANCE_HUD_DEFAULT,

                       DEFAULT_PLOT_COST = claims.config.DEFAULT_PLOT_COST,
                       TOURNAMENT_PLOT_COST = claims.config.TOURNAMENT_PLOT_COST,
                       CAMP_PLOT_COST = claims.config.CAMP_PLOT_COST,
                       TEMPLE_PLOT_COST = claims.config.TEMPLE_PLOT_COST,
                       FARM_PLOT_COST = claims.config.FARM_PLOT_COST,
                       SUMMON_PLOT_COST = claims.config.SUMMON_PLOT_COST,
                       EMBASSY_PLOT_COST = claims.config.EMBASSY_PLOT_COST,
                       TAVERN_PLOT_COST = claims.config.TAVERN_PLOT_COST,
                       MAIN_CITYPLOT_COST = claims.config.MAIN_CITYPLOT_COST,
                       PRISON_PLOT_COST = claims.config.PRISON_PLOT_COST,

                       OUTPOST_PLOT_COST = claims.config.OUTPOST_PLOT_COST,
                       EXTRA_PLOT_COST = claims.config.EXTRA_PLOT_COST,
                       PLOT_NO_PVP_FLAG_COST = claims.config.PLOT_NO_PVP_FLAG_COST,
                       PLOT_NO_MOBSPAWN_FLAG_COST = claims.config.PLOT_NO_MOBSPAWN_FLAG_COST,

                       RANSOM_FOR_NO_CITIZEN = claims.config.RANSOM_FOR_NO_CITIZEN,
                       RANSOM_FOR_CITIZEN = claims.config.RANSOM_FOR_CITIZEN,
                       RANSOM_FOR_MAYOR = claims.config.RANSOM_FOR_MAYOR,
                       RANSOM_FOR_LEADER = claims.config.RANSOM_FOR_LEADER,
                       RANSOM_FOR_CHIEF = claims.config.RANSOM_FOR_CHIEF,

                       ALLIANCE_RENAME_COST = claims.config.ALLIANCE_RENAME_COST,
                       ALLIANCE_BASE_CARE = claims.config.ALLIANCE_BASE_CARE,
                       ALLIANCE_MAX_FEE = claims.config.ALLIANCE_MAX_FEE,
                       NEUTRAL_ALLANCE_PAYMENT = claims.config.NEUTRAL_ALLANCE_PAYMENT,

                       MAX_CITY_FEE = claims.config.MAX_CITY_FEE,
                       CITY_MAX_DEBT = claims.config.CITY_MAX_DEBT,

                       MIN_RANGE_CELL_DURATION_MINUTES = claims.config.MIN_RANGE_CELL_DURATION_MINUTES,

                       PLOT_SIZE = claims.config.PLOT_SIZE,
                       ZONE_PLOTS_LENGTH = claims.config.ZONE_PLOTS_LENGTH,

                       WAR_FLAG_DEFENDER_INTERRUPT_ENABLED = claims.config.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED,
                       WAR_FLAG_DEFENDER_RADIUS = claims.config.WAR_FLAG_DEFENDER_RADIUS,
                       WAR_FLAG_REGRESS_MULTIPLIER = claims.config.WAR_FLAG_REGRESS_MULTIPLIER,

                       WAR_SCORE_ENABLED = claims.config.WAR_SCORE_ENABLED,
                       WAR_SCORE_TO_WIN = claims.config.WAR_SCORE_TO_WIN,
                       WAR_SCORE_PER_PLOT_CAPTURE = claims.config.WAR_SCORE_PER_PLOT_CAPTURE,
                       WAR_SCORE_PER_KILL = claims.config.WAR_SCORE_PER_KILL,
                       WAR_SCORE_PER_HOLD_TICK = claims.config.WAR_SCORE_PER_HOLD_TICK,
                       WAR_SCORE_HOLD_TICK_SECONDS = claims.config.WAR_SCORE_HOLD_TICK_SECONDS,

                       WAR_PILLAGE_ENABLED = claims.config.WAR_PILLAGE_ENABLED,
                       WAR_PILLAGE_PERCENT = claims.config.WAR_PILLAGE_PERCENT,

                       WAR_CAMP_ENABLED = claims.config.WAR_CAMP_ENABLED,
                       WAR_MAX_CAMPS_PER_CONFLICT = claims.config.WAR_MAX_CAMPS_PER_CONFLICT,
                       WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY = claims.config.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY,
                       WAR_CAMP_ANCHOR_BREAKS = claims.config.WAR_CAMP_ANCHOR_BREAKS,
                       WAR_NAP_ENABLED = claims.config.WAR_NAP_ENABLED,
                       WAR_NAP_DEFAULT_DAYS = claims.config.WAR_NAP_DEFAULT_DAYS,
                       WAR_NAP_MAX_DAYS = claims.config.WAR_NAP_MAX_DAYS,
                       WAR_NAP_BREAK_PENALTY = claims.config.WAR_NAP_BREAK_PENALTY,
                       WAR_BATTLE_WARN_MINUTES = claims.config.WAR_BATTLE_WARN_MINUTES,
                       WAR_ULTIMATUM_ENABLED = claims.config.WAR_ULTIMATUM_ENABLED,
                       WAR_ULTIMATUM_EXPIRE_HOURS = claims.config.WAR_ULTIMATUM_EXPIRE_HOURS,
                       VILLAGE_ENABLED = claims.config.VILLAGE_ENABLED,
                       VILLAGE_FOOD_ITEMS = claims.config.VILLAGE_FOOD_ITEMS,
                       VILLAGE_FUEL_ITEMS = claims.config.VILLAGE_FUEL_ITEMS,
                       VILLAGE_SUPPLY_HOURS_PER_ITEM = claims.config.VILLAGE_SUPPLY_HOURS_PER_ITEM,

                       CITY_PLOT_TRADE_ENABLED = claims.config.CITY_PLOT_TRADE_ENABLED,
                       CITY_PLOT_TRADE_GUI = claims.config.CITY_PLOT_TRADE_GUI,
                       CITY_PLOT_TRADE_REMOTE_BUY = claims.config.CITY_PLOT_TRADE_REMOTE_BUY,

                       WAR_RESPAWN_SAFEZONE_ENABLED = claims.config.WAR_RESPAWN_SAFEZONE_ENABLED,
                       WAR_RESPAWN_SAFEZONE_RADIUS = claims.config.WAR_RESPAWN_SAFEZONE_RADIUS,
                       WAR_RESPAWN_SAFEZONE_SECONDS = claims.config.WAR_RESPAWN_SAFEZONE_SECONDS,

                       WAR_SIEGE_ENABLED = claims.config.WAR_SIEGE_ENABLED,
                       WAR_SIEGE_RAM_TICK_SECONDS = claims.config.WAR_SIEGE_RAM_TICK_SECONDS,
                       WAR_SIEGE_RAM_RANGE = claims.config.WAR_SIEGE_RAM_RANGE,
                       WAR_SIEGE_RAM_COST = claims.config.WAR_SIEGE_RAM_COST,

                       FLAG_CAPTURE_DURATION_SECONDS = claims.config.FLAG_CAPTURE_DURATION_SECONDS,
                       MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE = claims.config.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE,
                       FLAG_REINFORCEMENT_AMOUNT = claims.config.FLAG_REINFORCEMENT_AMOUNT,
                       MINIMUM_DAYS_BETWEEN_BATTLES = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES,

                       WAR_DECLARATION_COST = claims.config.WAR_DECLARATION_COST,
                       WAR_REDECLARE_COOLDOWN_DAYS = claims.config.WAR_REDECLARE_COOLDOWN_DAYS,
                       WAR_REQUIRE_CASUS_BELLI = claims.config.WAR_REQUIRE_CASUS_BELLI,
                       WAR_CASUS_BELLI_GRACE_DAYS = claims.config.WAR_CASUS_BELLI_GRACE_DAYS,

                       WAR_PEACE_TERMS_ENABLED = claims.config.WAR_PEACE_TERMS_ENABLED,
                       WAR_VASSAL_TRIBUTE = claims.config.WAR_VASSAL_TRIBUTE,
                       WAR_VASSAL_DURATION_DAYS = claims.config.WAR_VASSAL_DURATION_DAYS,

                       WAR_BOUNTY_ENABLED = claims.config.WAR_BOUNTY_ENABLED,
                       WAR_BOUNTY_MIN = claims.config.WAR_BOUNTY_MIN,
                       WAR_PLUNDER_ON_KILL_ENABLED = claims.config.WAR_PLUNDER_ON_KILL_ENABLED,
                       WAR_PLUNDER_ON_KILL_PERCENT = claims.config.WAR_PLUNDER_ON_KILL_PERCENT,

                       WAR_REPORT_ENABLED = claims.config.WAR_REPORT_ENABLED,
                       WAR_HUD_ENABLED = claims.config.WAR_HUD_ENABLED
                   }
                   , player);
        }
        /// <summary>
        /// Merges a payload into one of the delayed-info collectors. The city and the player queue used
        /// to carry two hand-copied versions of this; they differ only in whether a list value is stored
        /// as one entry or unpacked into entries.
        /// </summary>
        private static void EnqueueInfo(
            ConcurrentDictionary<string, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>>> collector,
            string key, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate, bool unpackLists)
        {
            // field name -> the values collected for it since the last send
            Dictionary<string, List<object>> build(Dictionary<string, object> info) => info.ToDictionary(
                k => k.Key,
                k => unpackLists && k.Value is System.Collections.IList list
                        ? list.Cast<object>().ToList()
                        : new List<object> { k.Value });

            if (collector.TryGetValue(key, out var byEnum))
            {
                // Such enum was queued before - just add the new values to it.
                if (byEnum.TryGetValue(toUpdate, out var storedValues) && storedValues != null)
                {
                    foreach (var pair in additionalInfo)
                    {
                        if (!storedValues.TryGetValue(pair.Key, out var values)) continue;
                        if (unpackLists && pair.Value is List<object> listValue) values.AddRange(listValue);
                        else values.Add(pair.Value);
                    }
                }
                else
                {
                    byEnum[toUpdate] = build(additionalInfo);
                }
            }
            else
            {
                collector.TryAdd(key,
                    new Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>> { { toUpdate, build(additionalInfo) } });
            }
        }

        /// <summary>Queues enums with no payload: the sender rebuilds their value at send time.</summary>
        private static void EnqueueInfo(
            ConcurrentDictionary<string, Dictionary<EnumPlayerRelatedInfo, Dictionary<string, List<object>>>> collector,
            string key, EnumPlayerRelatedInfo[] toUpdate)
        {
            if (collector.TryGetValue(key, out var byEnum))
            {
                foreach (var it in toUpdate) byEnum.TryAdd(it, null);
            }
            else
            {
                collector.TryAdd(key, toUpdate.ToDictionary(k => k, k => (Dictionary<string, List<object>>)null));
            }
        }

        public static void AddToQueueCityInfoUpdate(string cityName, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
            => EnqueueInfo(cityDelayedInfoCollector, cityName, additionalInfo, toUpdate, unpackLists: false);

        public static void AddToQueueCityInfoUpdate(string cityName, params EnumPlayerRelatedInfo[] toUpdate)
            => EnqueueInfo(cityDelayedInfoCollector, cityName, toUpdate);

        public static void AddToQueuePlayerInfoUpdate(string playerName, Dictionary<string, object> additionalInfo, EnumPlayerRelatedInfo toUpdate)
            => EnqueueInfo(playerDelayedInfoCollector, playerName, additionalInfo, toUpdate, unpackLists: true);

        public static void AddToQueuePlayerInfoUpdate(string playerName, params EnumPlayerRelatedInfo[] toUpdate)
            => EnqueueInfo(playerDelayedInfoCollector, playerName, toUpdate);
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
        /// <summary>
        /// Builds what one player is told about one plot. Both the periodic update and the client's
        /// explicit request go through here, so neither can be left behind when a field is added.
        /// Lives on the sending side rather than on CurrentPlotInfo: that type is the wire contract
        /// the client deserialises, and it has no business reaching into the data storage.
        /// </summary>
        public static CurrentPlotInfo BuildCurrentPlotInfo(Plot plot, PlayerInfo viewer)
        {
            var info = new CurrentPlotInfo(plot.GetPartName(), plot.getPlotOwner()?.GetPartName() ?? "",
                plot.Type, plot.getCustomTax(), plot.Price, plot.getPermsHandler(), plot.extraBought, plot.getPos())
            {
                CityName = plot.hasCity() ? plot.getCity().GetPartName() : ""
            };

            // The listing is told to the selling city - that is how its own mayor sees what is on
            // offer - and to whoever the offer is open to. To anyone else the plot reads as not for
            // sale: an offer addressed to one city is not something a passing enemy gets to price.
            City viewerCity = viewer != null && viewer.hasCity() ? viewer.City : null;
            bool ours = viewerCity != null && plot.hasCity() && plot.getCity().Equals(viewerCity);

            if (AuctionRegistry.TryGetRunningFor(plot, out PlotAuction lot)
                && (ours || AuctionRules.IsVisibleTo(lot, viewerCity)))
            {
                info.SaleAudience = lot.Audience;
                if (lot.Audience == EnumPlotSaleAudience.SPECIFIC_CITY
                    && claims.dataStorage.getCityByGUID(lot.TargetCityGuid, out City target))
                {
                    info.SaleTargetCityName = target.GetPartName();
                }
                // A price tag fills the price; a lot with bids fills the auction fields instead, so
                // the page can tell one offer from the other without knowing how lots work.
                if (lot.IsFixedPrice)
                {
                    info.PriceForCityBuy = (int)lot.BuyoutPrice;
                }
                else
                {
                    info.AuctionEndsAt = lot.EndsAt;
                    info.AuctionCurrentBid = lot.CurrentBid;
                    info.AuctionMinNextBid = lot.MinNextBid;
                    info.AuctionBuyout = lot.BuyoutPrice;
                }

                // The server's own verdict, so the page does not re-judge war, limits or money. The
                // refusal travels with it: a button that quietly disappears leaves the mayor guessing
                // which of a dozen rules stopped them.
                string blocked = null;
                bool canTake = viewerCity != null && AuctionRules.CanBid(lot, viewerCity, lot.MinNextBid, out blocked);
                info.CanBuyAsCity = canTake && lot.IsFixedPrice;
                info.CanBidAsCity = canTake && !lot.IsFixedPrice;
                // Not shown to the seller: "you cannot bid on your own lot" is not news to them.
                if (!canTake && !ours) info.CityBuyBlockedReason = blocked ?? "";
            }
            return info;
        }

        public static void SendCurrentPlotUpdate(IServerPlayer player, Plot plot)
        {
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo plotViewer);
            CurrentPlotInfo cpi = BuildCurrentPlotInfo(plot, plotViewer);
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
        /// <summary>Announced union breaks of the city's alliance, keyed by the ally's display name.</summary>
        private static Dictionary<string, long> BuildPendingUnionBreaks(City city)
        {
            var result = new Dictionary<string, long>();
            if (city == null || !city.HasAlliance()) return result;
            foreach (var pair in city.Alliance.PendingUnionBreaks)
            {
                if (!claims.dataStorage.GetAllianceByGUID(pair.Key, out Alliance other)) continue;
                result[other.GetPartName()] = pair.Value;
            }
            return result;
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
                )
                { Emblem = alliance.Emblem ?? "" });
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
                        case EnumPlayerRelatedInfo.CITY_TIER:
                            result[pair.Key] = ((int)city.Tier).ToString();
                            break;
                        case EnumPlayerRelatedInfo.CITY_RAID_WINDOW:
                            // Raw numbers, formatted client-side; empty for anything but a raidable village.
                            result[pair.Key] = city.IsVillage() && claims.config.VILLAGE_RAIDABLE
                                ? part.structure.VillageRaidHelper.NextWindowStart(city) + ";" + claims.config.VILLAGE_RAID_DURATION_SECONDS / 60
                                : "0;0";
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
                        case EnumPlayerRelatedInfo.CITY_EMBLEM:
                            result[pair.Key] = city.Emblem ?? "";
                            break;
                        case EnumPlayerRelatedInfo.ALLIANCE_EMBLEM:
                            result[pair.Key] = city.HasAlliance() ? (city.Alliance.Emblem ?? "") : "";
                            break;
                        case EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST:
                            result[pair.Key] = JsonConvert.SerializeObject(StringFunctions.getNamesOfCriminals(city));
                            break;
                        // Built here rather than passed in: it's a full snapshot of the party's state,
                        // so it must be current at send time, not at queue time.
                        case EnumPlayerRelatedInfo.CITY_CASUS_BELLI_ALL:
                            result[pair.Key] = JsonConvert.SerializeObject(CasusBelliHelper.BuildForCity(city));
                            break;
                        case EnumPlayerRelatedInfo.ALLIANCE_UNION_BREAKS_ALL:
                            result[pair.Key] = JsonConvert.SerializeObject(BuildPendingUnionBreaks(city));
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
                        case EnumPlayerRelatedInfo.CITY_PLOT_AUCTIONS:
                            result[pair.Key] = JsonConvert.SerializeObject(
                                AuctionRules.GetLotsFor(city)
                                    .Select(a =>
                                    {
                                        a.TryGetPlot(out Plot lotPlot);
                                        return new PlotAuctionCellElement(a, lotPlot, city,
                                            AuctionRules.CanBid(a, city, a.MinNextBid, out _));
                                    })
                                    .ToList());
                            break;
                        case EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY:
                            // Newest first, capped by config - the table keeps every row regardless.
                            result[pair.Key] = JsonConvert.SerializeObject(
                                claims.dataStorage.PlotSaleHistory
                                    .Where(r => r.Involves(city))
                                    .Reverse()
                                    .Take(claims.config.CITY_PLOT_TRADE_HISTORY_SHOWN)
                                    .ToList());
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
                                        cityplotsgroups.PlotsGroupFeeHelper.ToCell(it,
                                            it.PlayersList.Select(pl => pl.GetPartName()).ToList()));
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
                        // Same snapshot as the city queue builds, for pushes addressed to a single player.
                        case EnumPlayerRelatedInfo.CITY_CASUS_BELLI_ALL:
                            if (playerInfo.hasCity())
                                result[pair.Key] = JsonConvert.SerializeObject(CasusBelliHelper.BuildForCity(playerInfo.City));
                            break;
                        case EnumPlayerRelatedInfo.ALLIANCE_UNION_BREAKS_ALL:
                            if (playerInfo.hasCity())
                                result[pair.Key] = JsonConvert.SerializeObject(BuildPendingUnionBreaks(playerInfo.City));
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
