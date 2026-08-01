using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.rights;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src
{
    public class RightsHandler
    {
        public static HashSet<EnumPlayerPermissions> GetPermsByGroup(string group)
        {
            if (PlayerPermissionsByGroups.TryGetValue(group, out HashSet<EnumPlayerPermissions> list))
            {
                return list;
            }
            return null;
        }
        static Dictionary<string, HashSet<EnumPlayerPermissions>> PlayerPermissionsByGroups = new();        
        private static Dictionary<string, HashSet<EnumPlayerPermissions>> getDefaultRankPermsDict()
        {
            Dictionary<string, HashSet<EnumPlayerPermissions>> outDict = new()
            {
                 { "DEFAULT", new HashSet<EnumPlayerPermissions> 
                    { 
                        EnumPlayerPermissions.PLOT_CLAIM,
                        EnumPlayerPermissions.PLOT_UNCLAIM,
                        EnumPlayerPermissions.CITY_HERE,
                        EnumPlayerPermissions.CITY_INFO

                    } 
                 },

                 { "MAYOR", new HashSet<EnumPlayerPermissions>
                    {
                        EnumPlayerPermissions.CITY_CLAIM_PLOT,
                        EnumPlayerPermissions.CITY_UNCLAIM_PLOT,
                        EnumPlayerPermissions.CITY_UNINVITE,
                        EnumPlayerPermissions.CITY_INVITE,
                        EnumPlayerPermissions.CITY_KICK,
                        EnumPlayerPermissions.CITY_SET_ALL,
                        EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS,
                        EnumPlayerPermissions.CITY_CRIMINAL_ALL,
                        EnumPlayerPermissions.CITY_PRISON_ALL,
                        EnumPlayerPermissions.CITY_SET_OTHERS_PREFIX,
                        EnumPlayerPermissions.CITY_SHOW_RANK_OTHERS,
                        EnumPlayerPermissions.CITY_SET_RANK,
                        EnumPlayerPermissions.CITY_REMOVE_RANK,
                        EnumPlayerPermissions.CITY_SET_PLOTS_COLOR,
                        EnumPlayerPermissions.CITY_SEE_BALANCE,
                        EnumPlayerPermissions.CITY_BUY_EXTRA_PLOT,
                        EnumPlayerPermissions.CITY_REMOVE_CRIMINAL,
                        EnumPlayerPermissions.CITY_ADD_CRIMINAL,
                        EnumPlayerPermissions.CITY_SET_SUMMON,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLAYER,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_ADD_PLOT,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE_PLOT, 
                        EnumPlayerPermissions.CITY_PLOTSGROUP_LIST, 
                        EnumPlayerPermissions.CITY_PLOTSGROUP_SET,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_SET_FIRE,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_SET_PVP,
                        EnumPlayerPermissions.CITY_PLOTSGROUP_SET_BLAST,
                        EnumPlayerPermissions.CITY_WITHDRAW_MONEY,
                        EnumPlayerPermissions.CITY_CREATE_CITY_RANK,
                        EnumPlayerPermissions.CITY_DELETE_CITY_RANK,
                        EnumPlayerPermissions.CITY_SEE_CITY_RANKS,
                        EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK,
                        EnumPlayerPermissions.CITY_REMOVE_PERMISSION_FROM_RANK,
                        EnumPlayerPermissions.CITY_SET_PLOT_ACCESS_PERMISSIONS,
                        EnumPlayerPermissions.CITY_BUY_OUTPOST,
                        EnumPlayerPermissions.CITY_SET_EMBLEM
                    }
                },
                 // What a village head gets instead of MAYOR. Deliberately a whitelist: a village
                 // has no treasury, alliances, wars, prisons, summons, plot groups or ranks, and a
                 // future city permission must not leak into villages just because nobody
                 // remembered to deny it. CITY_SET_ALL is not here on purpose - it also covers the
                 // fee and emblem setters.
                 { "VILLAGE_MAYOR", new HashSet<EnumPlayerPermissions>
                    {
                        EnumPlayerPermissions.CITY_CLAIM_PLOT,
                        EnumPlayerPermissions.CITY_UNCLAIM_PLOT,
                        EnumPlayerPermissions.CITY_INVITE,
                        EnumPlayerPermissions.CITY_UNINVITE,
                        EnumPlayerPermissions.CITY_KICK,
                        EnumPlayerPermissions.SHOW_INVITES_SENT,
                        EnumPlayerPermissions.CITY_SET_NAME,
                        EnumPlayerPermissions.CITY_SET_OPEN_STATE,
                        EnumPlayerPermissions.CITY_SET_PVP,
                        EnumPlayerPermissions.CITY_SET_FIRE,
                        EnumPlayerPermissions.CITY_SET_BLAST,
                        EnumPlayerPermissions.CITY_SET_DAILY_MSG,
                        EnumPlayerPermissions.CITY_SET_INV_MSG,
                        EnumPlayerPermissions.CITY_SET_PLOT_ACCESS_PERMISSIONS,
                        EnumPlayerPermissions.CITY_SET_PLOTS_COLOR,
                        EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS
                    }
                },
                {
                    "LEADER", new HashSet<EnumPlayerPermissions>
                    {
                        EnumPlayerPermissions.ALLIANCE_ACCEPT_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_DECLARE_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_REVOKE_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_DENY_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_OFFER_STOP_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_ACCEPT_STOP_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_DENY_STOP_CONFLICT,
                        EnumPlayerPermissions.ALLIANCE_WITHDRAW_MONEY,
                        EnumPlayerPermissions.ALLIANCE_ACCEPT_UNION,
                        EnumPlayerPermissions.ALLIANCE_DECLARE_UNION,
                        EnumPlayerPermissions.ALLIANCE_DENY_UNION,
                        EnumPlayerPermissions.ALLIANCE_REVOKE_UNION,
                        EnumPlayerPermissions.ALLIANCE_SET_EMBLEM
                    }
                }

            };
            return outDict;
        }
        public static void reapplyRights(PlayerInfo playerInfo)
        {
            if (claims.sapi.World.PlayerByUid(playerInfo.Guid) is not IServerPlayer player)
            {
                return;
            }
            playerInfo.PlayerPermissionsHandler.ClearPermissions();
            if (PlayerPermissionsByGroups.TryGetValue("DEFAULT", out HashSet<EnumPlayerPermissions> strangerPerms))
            {
                playerInfo.PlayerPermissionsHandler.AddPermissions(strangerPerms);

            }
            City city = playerInfo.City;
            if (city != null)
            {
                if (city.isMayor(playerInfo))
                {
                    // A village head gets its own, much smaller group; see getDefaultRankPermsDict.
                    string mayorGroup = city.IsVillage() ? "VILLAGE_MAYOR" : "MAYOR";
                    if (PlayerPermissionsByGroups.TryGetValue(mayorGroup, out HashSet<EnumPlayerPermissions> mayorPerms))
                    {
                        playerInfo.PlayerPermissionsHandler.AddPermissions(mayorPerms);
                    }
                }
                // Custom ranks are a city feature; a village must not grant permissions through
                // ranks it kept from before an admin downgraded it.
                if (playerInfo.hasCity() && !city.IsVillage())
                {
                    foreach (string str in playerInfo.getCityTitles())
                    {
                        if(playerInfo.City.CustomCityRanks.TryGetValue(str, out var rank))
                        {
                            foreach(var it in rank.Permissions)
                            {
                                playerInfo.PlayerPermissionsHandler.AddPermission(it);
                            }
                        }
                    }
                }
            }
            //TODO
            //REMOVE ALLIANCE ON CITY REMOVE
            if (playerInfo.hasCity())
            {
                Alliance alliance = playerInfo.Alliance;
                if (alliance != null)
                {
                    if (alliance.IsLeader(playerInfo))
                    {
                        if (PlayerPermissionsByGroups.TryGetValue("LEADER", out HashSet<EnumPlayerPermissions> leaderPerms))
                        {
                            playerInfo.PlayerPermissionsHandler.AddPermissions(leaderPerms);
                        }
                    }
                }
            }
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
        }
        public class StringEnumConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType.IsEnum;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string enumString = reader.Value.ToString();
                    if (Enum.IsDefined(objectType, enumString))
                    {
                        return Enum.Parse(objectType, enumString);
                    }
                    else
                    {
                        throw new JsonSerializationException($"Unknown enum value: {enumString}");
                    }
                }
                throw new JsonSerializationException("Expected string token");
            }
        }
        public static void readOrCreateRightPerms()
        {
            string h = Directory.GetCurrentDirectory();
            MessageHandler.sendErrorMsg(h);
            string filePath;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
            {
                filePath = @"" + Path.Combine(GamePaths.ModConfig, claims.config.PERMS_FILE_NAME);
            }
            else
            {
                filePath = @"" + Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, claims.config.PERMS_FILE_NAME);
            }
            string json;
            if (File.Exists(filePath))
            {
                using (StreamReader r = new(filePath))
                {
                    json = r.ReadToEnd();
                    JsonSerializerSettings settings = new();
                    settings.Converters.Add(new StringEnumConverter());
                    PlayerPermissionsByGroups = JsonConvert.DeserializeObject<Dictionary<string, HashSet<EnumPlayerPermissions>>>(json, settings);
                }
                AddMissingGroups(filePath);
            }
            else
            {
                Dictionary<string, HashSet<EnumPlayerPermissions>> ranksPerms = getDefaultRankPermsDict();
                using (StreamWriter r = new(filePath))
                {
                    JsonSerializerSettings settings = new();
                    settings.Converters.Add(new StringEnumConverter());
                    settings.Formatting = Formatting.Indented;
                    string b = JsonConvert.SerializeObject(ranksPerms, settings);
                    r.WriteLine(b);
                    PlayerPermissionsByGroups = ranksPerms;
                }
            }
        }
        /// <summary>
        /// Adds groups the current code knows about but an existing permissions file predates
        /// (VILLAGE_MAYOR is the first such case). Groups the server admin already tuned are left
        /// untouched; the file is only rewritten when something was actually missing.
        /// </summary>
        private static void AddMissingGroups(string filePath)
        {
            if (PlayerPermissionsByGroups == null)
            {
                PlayerPermissionsByGroups = getDefaultRankPermsDict();
            }
            bool added = false;
            foreach (var group in getDefaultRankPermsDict())
            {
                if (PlayerPermissionsByGroups.ContainsKey(group.Key)) continue;
                PlayerPermissionsByGroups.Add(group.Key, group.Value);
                added = true;
            }
            if (!added) return;
            try
            {
                JsonSerializerSettings settings = new();
                settings.Converters.Add(new StringEnumConverter());
                settings.Formatting = Formatting.Indented;
                using StreamWriter w = new(filePath);
                w.WriteLine(JsonConvert.SerializeObject(PlayerPermissionsByGroups, settings));
            }
            catch (Exception ex)
            {
                // The in-memory dict already has the group, so rights work either way this session.
                claims.sapi?.Logger.Warning("[claims] Could not write new permission groups to {0}: {1}", filePath, ex.Message);
            }
        }

        public static bool hasRight(IServerPlayer player, string right)
        {
            return player.ServerData.PermaPrivileges.Contains(right) 
                || player.WorldData.CurrentGameMode == Vintagestory.API.Common.EnumGameMode.Creative;
        }
        public void initRightsDict()
        {
            readOrCreateRightPerms();
        }
        public static void clearAll()
        {
            //rightsByGroupDict.Clear();
        }
        public static void ClearPlayerCachesAndUpdatePlotSavedRightsForClients(Conflict conflict)
        {
            foreach (var city in conflict.First.GetCities())
            {
                foreach (var pl in city.getCityCitizens())
                {
                    RightsHandler.reapplyRights(pl);
                }
            }

            foreach (var city in conflict.Second.GetCities())
            {
                foreach (var pl in city.getCityCitizens())
                {
                    RightsHandler.reapplyRights(pl);
                }
            }

            foreach (var it in conflict.First.GetCities())
            {
                foreach (var plot in it.getCityPlots())
                {
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                }
            }

            foreach (var it in conflict.Second.GetCities())
            {
                foreach (var plot in it.getCityPlots())
                {
                    claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                    claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
                }
            }
        }
        public static void SetPartiesHostile(IConflictParty first, IConflictParty second, Conflict conflict)
        {
            first.RunningConflicts.Add(conflict);
            second.RunningConflicts.Add(conflict);
            foreach (City ourCity in first.GetCities())
            {
                foreach (City targetCity in second.GetCities())
                {
                    if (!ourCity.HostileCities.Contains(targetCity))
                    {
                        ourCity.HostileCities.Add(targetCity);
                        ourCity.saveToDatabase();
                    }
                }
            }
            foreach (City targetCity in second.GetCities())
            {
                foreach (City ourCity in first.GetCities())
                {
                    if (!targetCity.HostileCities.Contains(ourCity))
                    {
                        targetCity.HostileCities.Add(ourCity);
                        targetCity.saveToDatabase();
                    }
                }
            }
            first.AddHostileParty(second);
            second.AddHostileParty(first);
        }
        // Keep Alliance-typed overload for backward compatibility with alliance conflict code
        public static void SetAllianciesHostile(Alliance first, Alliance second, Conflict conflict)
            => SetPartiesHostile(first, second, conflict);
        public static void AllianceAllySetHostileOnNewConflictStarted(IConflictParty first, IConflictParty second, Conflict conflict)
        {
            if (first is not Alliance firstAlliance) return;
            foreach(var it in firstAlliance.ComradAlliancies)
            {
                if(it != second && !it.HostileParties.Contains(second))
                {
                    SetPartiesHostile(it, second, conflict);
                    string newConflictGuid = ConflictLetter.GetUnusedGuid().ToString();
                    Conflict newConflict = new Conflict("", newConflictGuid);
                    claims.dataStorage.TryAddConflict(newConflict);
                    newConflict.First = it;
                    newConflict.Second = second;
                    newConflict.StartedBy = first;
                    newConflict.State = ConflictState.CREATED;
                    newConflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
                    newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;

                    var allyConflictCell = ClientConflictCellElement.FromConflict(newConflict);
                    UsefullPacketsSend.AddToQueueAllianceInfoUpdate(it.Guid,
                                new Dictionary<string, object> { { "value", allyConflictCell } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                    UsefullPacketsSend.AddToQueueAllianceInfoUpdate(second.Guid,
                        new Dictionary<string, object> { { "value", allyConflictCell } },
                        EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);

                    it.saveToDatabase();
                    second.saveToDatabase();
                    newConflict.saveToDatabase(false);
                }

            }
           
        }
        public static void AddCityHostilesInAlliance(City city, Alliance alliance)
        {
            foreach(var runConflict in alliance.RunningConflicts)
            {
                IConflictParty foeParty = runConflict.First.Guid == alliance.Guid ? runConflict.Second : runConflict.First;
                foreach (City targetCity in foeParty.GetCities())
                {
                    if (!targetCity.HostileCities.Contains(city))
                        targetCity.HostileCities.Add(city);
                    if (!city.HostileCities.Contains(targetCity))
                        city.HostileCities.Add(targetCity);
                }
            }
        }
        public static void RemoveCityHostilesInAlliance(City city, Alliance alliance)
        {
            foreach (var runConflict in alliance.RunningConflicts)
            {
                IConflictParty foeParty = runConflict.First.Guid == alliance.Guid ? runConflict.Second : runConflict.First;
                foreach (City targetCity in foeParty.GetCities())
                {
                    targetCity.HostileCities.Remove(city);
                    city.HostileCities.Remove(targetCity);
                }
            }
        }
    }
}
