using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using claims.src.citylog;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.rights;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.structures
{
    public class ClientPlayerInfo 
    {
        public CityInfo CityInfo { get; set; }
        public HashSet<string> Friends { get; set; } = new HashSet<string>();
        public List<ClientToCityInvitation> ReceivedInvitations { get; set; } = new List<ClientToCityInvitation>();
        public List<ClientToPlotsGroupInvitation> ReceivedPlotsGroupInvitations { get; set; } = new List<ClientToPlotsGroupInvitation>();
        public EnumShowPlotMovement ShowPlotMovement { get; set; } = EnumShowPlotMovement.SHOW_NONE;
        public PlayerPermissions PlayerPermissions { get; set; }
        public CurrentPlotInfo CurrentPlotInfo { get; set; }
        public AllianceInfo AllianceInfo { get; set; }
        public Dictionary<string, int> PlayerNextPayments = new();
        public List<ClientCityInfoCellElement> AllCitiesList { get; set; } = new List<ClientCityInfoCellElement>();
        public List<ClientAllianceInfoCellElement> AllAlliancesList { get; set; } = new List<ClientAllianceInfoCellElement>();
        public double PlayerBalance { get; set; } = 0;
        private Dictionary<EnumPlayerRelatedInfo, Action<string>> AcceptChangeHandlers = new();
        public ClientPlayerInfo()
        {
            CityInfo = null;
            ShowPlotMovement = EnumShowPlotMovement.SHOW_HUD;
            PlayerPermissions = new PlayerPermissions();
            CurrentPlotInfo = new CurrentPlotInfo();

            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.MAYOR_NAME, OnMayorName);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_NAME, OnCityName);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, OnCityCreatedTimestamp);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, OnCityMembers);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, OnMaxCountPlots);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, OnClaimedPlots);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, OnPlayerPrefix);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, OnPlayerAfterName);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, OnPlayerCityTitles);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.SHOW_PLOT_MOVEMENT, OnShowPlotMovement);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_INVITE_ADD, OnCityInviteAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_INVITE_REMOVE, OnCityInviteRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.PLAYER_PERMISSIONS, OnPlayerPermissions);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.FRIENDS, OnPlayerFriends);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS, OnCityCitizensRanks);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CITIZEN_RANK_ADDED, OnCityCitizenRankAdded);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CITIZEN_RANK_REMOVED, OnCityCitizenRankRemoved);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOTS_COLOR, OnCityCityPlotsColor);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_BALANCE, OnCityCityBalance);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_DEBT, OnCityCityDebt);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_FEE, OnCityCityFee);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_EMBLEM, OnCityEmblem);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_EMBLEM, OnAllianceEmblem);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CRIMINAL_ADDED, OnCityCityCriminalAdded);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CRIMINAL_REMOVED, OnCityCityCriminalRemoved);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CRIMINALS_LIST, OnCityCityCriminalsList);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PRISON_CELL_ALL, OnCityCityPrisonCellAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_ADD_PRISON_CELL, OnCityCityPrisonCellAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_REMOVE_PRISON_CELL, OnCityCityPrisonCellRemoved);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CELL_PRISON_UPDATE, OnCityCityPrisonCellUpdate);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ALL, OnCityCitySummonPointAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ADD, OnCityCitySummonPointAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_SUMMON_POINT_REMOVE, OnCityCitySummonPointRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_SUMMON_POINT_UPDATE, OnCityCitySummonPointUpdate);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ALL, OnCityCitySummonPlotsgroupAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_REMOVE, OnCityCitySummonPlotsgroupRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_ADD, OnCityCitySummonPlotsgroupAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOTS_GROUPS_UPDATE, OnCityCitySummonPlotsgroupUpdate);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.TO_PLOTS_GROUP_INVITE_ADD, OnCityCitySummonPlotsgroupInviteAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.TO_PLOTS_GROUP_INVITE_REMOVE, OnCityCitySummonPlotsgroupInviteRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL, OnAllianceNewAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE, OnAllianceRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_LIST_ALL, OnCityListAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_LIST_UPDATE, OnCityListUpdate);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_LIST_REMOVE, OnCityListRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_LIST_ALL, OnAllianceListAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_NAME, OnAllianceName);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.TO_ALLIANCE_INVITE_ADD, OnAllianceInviteAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.TO_ALLIANCE_INVITE_REMOVE, OnAllianceInviteRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD, OnAllianceLetterAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE, OnAllianceLetterRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_LETTER_ALL, OnAllianceLetterAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD, OnAllianceConflictAdd);;
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE, OnAllianceConflictRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ALL, OnAllianceConflictAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOT_RECOLOR, OnCityPlotRecolor);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_CASUS_BELLI_ALL, OnCityCasusBelliAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_UNION_BREAKS_ALL, OnAllianceUnionBreaksAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WARRANGES_UPDATED, OnAllianceConflictWarrangesUpdated);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_SCORE_UPDATED, OnAllianceConflictScoreUpdated);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_START, OnAllianceConflictWarTimeMarkStart);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_END, OnAllianceConflictWarTimeMarkEnd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_DAY_PAYMENT, OnCityDayPayment);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED, OnCityPermissionsUpdated);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ADD, OnAllianceUnionLetterAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE, OnAllianceUnionLetterRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ALL, OnAllianceUnionLetterAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_ALLIES_ALL, OnAllianceAlliesAll);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_ALLY_ADDED, OnAllianceAllyAdd);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_ALLY_REMOVED, OnAllianceAllyRemove);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT, OnPlayerNextPaymentDict);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_GUID, OnCityGuid);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_TIER, OnCityTier);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_RAID_WINDOW, OnCityRaidWindow);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_LOG, OnCityLog);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOTS_MAP, OnCityPlotsMap);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY, OnCityPlotMarketHistory);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.CITY_PLOT_AUCTIONS, OnCityPlotAuctions);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.PLAYER_BALANCE, OnPlayerBalance);
            AcceptChangeHandlers.Add(EnumPlayerRelatedInfo.ALLIANCE_BALANCE, OnAllianceBalance);
        }
        public ClientPlayerInfo(string cityName, string mayorName, long timeStampCreated, List<string> citizens, Dictionary<string, int> maxCountPlots, int countPlots, string prefix,
            string afterName, HashSet<string> cityTitles, EnumShowPlotMovement showPlotMovement, int PlotColor, double cityBalance, List<string> criminals)
        {
            CityInfo = new CityInfo(cityName, mayorName, timeStampCreated, citizens, maxCountPlots, countPlots, prefix, afterName, cityTitles, PlotColor, cityBalance, criminals);
            ShowPlotMovement = showPlotMovement;
        }

        public ClientPlayerInfo(CityInfo cityInfo, HashSet<string> friends, List<ClientToCityInvitation> receivedInvitations, EnumShowPlotMovement showPlotMovement)
        {
            CityInfo = cityInfo;
            Friends = friends;
            ReceivedInvitations = receivedInvitations;
            ShowPlotMovement = showPlotMovement;
        }
        public ClientPlayerInfo(string cityName, string mayorName, string timeStampCreated, string citizens, string maxCountPlots, string countPlots, string prefix,
           string afterName, string cityTitles, string showPlotMovement, int PlotColor, double cityBalance, string cityCriminals)
        {
            long.TryParse(timeStampCreated, out long longStamp);

            List<string> citizenList = JsonConvert.DeserializeObject<List<string>>(citizens);

            Dictionary<string, int> maxCountPlotsDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(maxCountPlots);
            int.TryParse(countPlots, out int curCountPlotInt);

            HashSet<string> titles = JsonConvert.DeserializeObject<HashSet<string>>(cityTitles);

            int.TryParse(showPlotMovement, out int showInt);

            List<string> criminals = JsonConvert.DeserializeObject<List<string>>(cityCriminals);

            CityInfo = new CityInfo(cityName, mayorName, longStamp, citizenList, maxCountPlotsDict, curCountPlotInt, prefix, afterName, titles, PlotColor, cityBalance, criminals);
            ShowPlotMovement = (EnumShowPlotMovement)showInt;
        }

        public static ClientPlayerInfo OnJoinAllInfo(string cityName, string mayorName, string timeStampCreated, string citizens, string maxCountPlots, string countPlots, string prefix,
                                                     string afterName, string cityTitles, string showPlotMovement, string friendsList, int PlotColor, double cityBalance, string cityCriminals)
        {
            long.TryParse(timeStampCreated, out long longStamp);

            List<string> citizenList = JsonConvert.DeserializeObject<List<string>>(citizens);

            Dictionary<string, int> maxCountPlotsDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(maxCountPlots);
            int.TryParse(countPlots, out int curCountPlotInt);

            HashSet<string> titles = JsonConvert.DeserializeObject<HashSet<string>>(cityTitles);
            int.TryParse(showPlotMovement, out int showInt);

            HashSet<string> friends = JsonConvert.DeserializeObject<HashSet<string>>(friendsList);

            List<ClientToCityInvitation> receivedInvitations = new List<ClientToCityInvitation>(); //todo

            List<string> criminals = JsonConvert.DeserializeObject<List<string>>(cityCriminals);

            CityInfo cityInfo = new CityInfo(cityName, mayorName, longStamp, citizenList, maxCountPlotsDict, curCountPlotInt, prefix, afterName, titles, PlotColor, cityBalance, criminals);

            return new ClientPlayerInfo(cityInfo, friends, receivedInvitations, (EnumShowPlotMovement)showInt);
        }


        public void AcceptChangedValues(Dictionary<EnumPlayerRelatedInfo, string> valueDict)
        {
            foreach (var pair in valueDict)
            {
                if(AcceptChangeHandlers.TryGetValue(pair.Key, out Action<string> handler))
                {
                    handler(pair.Value);
                }
            }
        }
        private void OnMayorName(string val)
        {
            this.CityInfo.MayorName = val;
        }
        private void OnCityGuid(string val)
        {
            this.CityInfo.Guid = val;
        }
        private void OnCityRaidWindow(string val)
        {
            // "unixStart;minutes" - kept as raw numbers so the client formats them in its own language.
            string[] parts = val?.Split(';');
            if (parts == null || parts.Length != 2) return;
            if (long.TryParse(parts[0], out long start) && int.TryParse(parts[1], out int minutes))
            {
                this.CityInfo.RaidWindowStart = start;
                this.CityInfo.RaidWindowMinutes = minutes;
            }
        }
        private void OnCityTier(string val)
        {
            // Anything unparsable leaves the tier alone, i.e. a full city.
            if (int.TryParse(val, out int tier) && System.Enum.IsDefined(typeof(CityTier), tier))
            {
                this.CityInfo.Tier = (CityTier)tier;
            }
        }
        private void OnCityName(string val)
        {
            this.CityInfo.Name = val;
        }
        private void OnCityCreatedTimestamp(string val)
        {
            long.TryParse(val, out long longStamp);
            CityInfo.TimeStampCreated = longStamp;
        }
        private void OnCityMembers(string val)
        {
            CityInfo.PlayersNames = JsonConvert.DeserializeObject<List<string>>(val);
        }
        private void OnMaxCountPlots(string val)
        {
            CityInfo.MaxCountPlots = JsonConvert.DeserializeObject<Dictionary<string, int>>(val);
        }
        private void OnClaimedPlots(string val)
        {
            int.TryParse(val, out int claimed);
            CityInfo.CountPlots = claimed;
        }
        private void OnPlayerPrefix(string val)
        {
            CityInfo.Prefix = val;
        }
        private void OnPlayerAfterName(string val)
        {
            CityInfo.AfterName = val;
        }
        private void OnPlayerCityTitles(string val)
        {
            CityInfo.CityTitles = JsonConvert.DeserializeObject<HashSet<string>>(val);
        }
        private void OnShowPlotMovement(string val)
        {
            int.TryParse(val, out int showInt);
            ShowPlotMovement = (EnumShowPlotMovement)showInt;
        }
        private void OnCityInviteAdd(string val)
        {
            this.ReceivedInvitations.Add(JsonConvert.DeserializeObject<ClientToCityInvitation>(val));
        }
        private void OnCityInviteRemove(string val)
        {
            var invitationToReemove = this.ReceivedInvitations.Where(invitation => invitation.CityName == val).FirstOrDefault();
            if (invitationToReemove != null)
            {
                this.ReceivedInvitations.Remove(invitationToReemove);
            }
        }
        private void OnPlayerPermissions(string val)
        {
            PlayerPermissions.ClearPermissions();
            PlayerPermissions.AddPermissions(JsonConvert.DeserializeObject<HashSet<EnumPlayerPermissions>>(val));
        }
        private void OnPlayerFriends(string val)
        {
            Friends = JsonConvert.DeserializeObject<List<string>>(val).ToHashSet();
        }
        private void OnCityCitizensRanks(string val)
        {
            Dictionary<string, CustomCityRank> di = JsonConvert.DeserializeObject<Dictionary<string, CustomCityRank>>(val);
            CityInfo.CityRanks.Clear();
            foreach (var it in di)
            {
                CityInfo.CityRanks.Add(new CityRankCellElement(it.Key, it.Value.CitizensNames, it.Value.Permissions));
            }
        }
        private void OnCityCitizenRankAdded(string val)
        {
            Dictionary<string, CustomCityRank> di = JsonConvert.DeserializeObject<Dictionary<string, CustomCityRank>>(val);
            foreach (var it in di)
            {
                var foundCell = CityInfo.CityRanks.FirstOrDefault(cell => cell.Name == it.Key);
                if (foundCell != null)
                {
                    foundCell.Citizens = it.Value.CitizensNames;
                    foundCell.Permissions = it.Value.Permissions;
                }
                else
                {
                    CityInfo.CityRanks.Add(new CityRankCellElement(it.Key, it.Value.CitizensNames, it.Value.Permissions));
                }
            }
        }
        private void OnCityCitizenRankRemoved(string val)
        {
            Dictionary<string, CustomCityRank> di = JsonConvert.DeserializeObject<Dictionary<string, CustomCityRank>>(val);
            foreach (var it in di)
            {
                var foundCell = CityInfo.CityRanks.FirstOrDefault(cell => cell.Name == it.Key);
                if (foundCell != null)
                {
                    CityInfo.CityRanks.Remove(foundCell);
                }
            }
        }
        private void OnCityCityPlotsColor(string val)
        {
            CityInfo.PlotsColor = int.Parse(val, CultureInfo.InvariantCulture);
            claims.clientDataStorage.ClientSetCityPlotsColor(claims.clientDataStorage.clientPlayerInfo.CityInfo.Name, CityInfo.PlotsColor);
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo != null)
            {
                foreach (var it in claims.clientDataStorage.GetCitySavedPlotInfos(claims.clientDataStorage.clientPlayerInfo.CityInfo.Name))
                {
                    claims.clientModInstance.plotsMapLayer.OnResChunkPixels(it, claims.clientDataStorage.clientPlayerInfo?.CityInfo.Name);
                }
            }
        }
        private void OnCityCityBalance(string val)
        {
            CityInfo.CityBalance = (double)decimal.Parse(val, CultureInfo.InvariantCulture);
        }
        private void OnCityCityDebt(string val)
        {
            CityInfo.CityDebt = (double)decimal.Parse(val, CultureInfo.InvariantCulture);
        }
        private void OnCityCityFee(string val)
        {
            CityInfo.CityFee = int.Parse(val);
        }
        private void OnCityEmblem(string val)
        {
            CityInfo.Emblem = val ?? "";
        }
        private void OnAllianceEmblem(string val)
        {
            // Arrives with the city block, which is also sent to citizens of an alliance-less city -
            // there is simply nothing to put it on then.
            if (AllianceInfo != null) AllianceInfo.Emblem = val ?? "";
        }
        private void OnCityCityCriminalAdded(string val)
        {
            HashSet<string> hs = JsonConvert.DeserializeObject<HashSet<string>>(val);
            foreach (var it in hs)
            {
                CityInfo.Criminals.Add(it);
            }
        }
        private void OnCityCityCriminalRemoved(string val)
        {
            HashSet<string> hs = JsonConvert.DeserializeObject<HashSet<string>>(val);
            foreach (var it in hs)
            {
                CityInfo.Criminals.Remove(it);
            }
        }
        private void OnCityCityCriminalsList(string val)
        {
            List<string> hs = JsonConvert.DeserializeObject<List<string>>(val);
            CityInfo.Criminals = hs;
        }
        private void OnCityCityPrisonCellAll(string val)
        {
            HashSet<PrisonCellElement> pc = JsonConvert.DeserializeObject<HashSet<PrisonCellElement>>(val);
            CityInfo.PrisonCells = pc.ToList();
        }
        private void OnCityCityPrisonCellAdd(string val)
        {
            HashSet<PrisonCellElement> pc = JsonConvert.DeserializeObject<HashSet<PrisonCellElement>>(val);
            foreach (var it in pc)
            {
                if (!CityInfo.PrisonCells.Any(existing => existing.SpawnPosition.Equals(it.SpawnPosition)))
                {
                    CityInfo.PrisonCells.Add(it);
                }
            }
        }
        private void OnCityCityPrisonCellRemoved(string val)
        {
            HashSet<PrisonCellElement> pc = JsonConvert.DeserializeObject<HashSet<PrisonCellElement>>(val);
            foreach (var it in pc)
            {
                // Matched on position, the way the summon points are: the element came off the wire,
                // so it is never the same instance the list holds and Remove(it) found nothing - the
                // cell stayed in the GUI although the server had already deleted it.
                foreach (var it_current in CityInfo.PrisonCells.ToArray())
                {
                    if (it_current.SpawnPosition.Equals(it.SpawnPosition))
                    {
                        CityInfo.PrisonCells.Remove(it_current);
                        break;
                    }
                }
            }
        }
        private void OnCityCityPrisonCellUpdate(string val)
        {
            HashSet<PrisonCellElement> pc = JsonConvert.DeserializeObject<HashSet<PrisonCellElement>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in CityInfo.PrisonCells.ToArray())
                {
                    if (it_current.SpawnPosition.Equals(it.SpawnPosition))
                    {
                        CityInfo.PrisonCells.Remove(it_current);
                        CityInfo.PrisonCells.Add(it);
                        break;
                    }
                }
            }
        }
        private void OnCityCitySummonPointAll(string val)
        {
            HashSet<SummonCellElement> pc = JsonConvert.DeserializeObject<HashSet<SummonCellElement>>(val);
            CityInfo.SummonCells = pc.ToList();
        }
        private void OnCityCitySummonPointAdd(string val)
        {
            HashSet<SummonCellElement> pc = JsonConvert.DeserializeObject<HashSet<SummonCellElement>>(val);
            foreach (var it in pc)
            {
                CityInfo.SummonCells.Add(it);
            }
        }
        private void OnCityCitySummonPointRemove(string val)
        {
            HashSet<SummonCellElement> pc = JsonConvert.DeserializeObject<HashSet<SummonCellElement>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in CityInfo.SummonCells.ToArray())
                {
                    if (it_current.SpawnPosition.Equals(it.SpawnPosition))
                    {
                        CityInfo.SummonCells.Remove(it_current);
                        break;
                    }
                }
            }
        }
        private void OnCityCitySummonPointUpdate(string val)
        {
            HashSet<SummonCellElement> pc = JsonConvert.DeserializeObject<HashSet<SummonCellElement>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in CityInfo.SummonCells.ToArray())
                {
                    if (it_current.SpawnPosition.Equals(it.SpawnPosition))
                    {
                        CityInfo.SummonCells.Remove(it_current);
                        CityInfo.SummonCells.Add(it);
                        break;
                    }
                }
            }
        }
        private void OnCityCitySummonPlotsgroupAll(string val)
        {
            HashSet<PlotsGroupCellElement> pc = JsonConvert.DeserializeObject<HashSet<PlotsGroupCellElement>>(val);
            CityInfo.PlotsGroupCells = pc.ToList();
        }
        private void OnCityCitySummonPlotsgroupRemove(string val)
        {
            HashSet<PlotsGroupCellElement> pc = JsonConvert.DeserializeObject<HashSet<PlotsGroupCellElement>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in CityInfo.PlotsGroupCells.ToArray())
                {
                    if (it_current.Guid.Equals(it.Guid))
                    {
                        CityInfo.PlotsGroupCells.Remove(it_current);
                        break;
                    }
                }
            }
        }
        private void OnCityCitySummonPlotsgroupAdd(string val)
        {
            if (CityInfo == null) return;
            HashSet<PlotsGroupCellElement> pc = JsonConvert.DeserializeObject<HashSet<PlotsGroupCellElement>>(val);
            foreach (var it in pc)
            {
                CityInfo.PlotsGroupCells.Add(it);
            }
        }
        private void OnCityCitySummonPlotsgroupUpdate(string val)
        {
            HashSet<PlotsGroupCellElement> pc = JsonConvert.DeserializeObject<HashSet<PlotsGroupCellElement>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in CityInfo.PlotsGroupCells.ToArray())
                {
                    if (it_current.Guid.Equals(it.Guid))
                    {
                        CityInfo.PlotsGroupCells.Remove(it_current);
                        CityInfo.PlotsGroupCells.Add(it);
                    }
                }
            }
        }
        private void OnCityCitySummonPlotsgroupInviteAdd(string val)
        {
            HashSet<ClientToPlotsGroupInvitation> pc = JsonConvert.DeserializeObject<HashSet<ClientToPlotsGroupInvitation>>(val);
            foreach (var it in pc)
            {
                this.ReceivedPlotsGroupInvitations.Add(it);
            }
        }
        private void OnCityCitySummonPlotsgroupInviteRemove(string val)
        {
            HashSet<ClientToPlotsGroupInvitation> pc = JsonConvert.DeserializeObject<HashSet<ClientToPlotsGroupInvitation>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in this.ReceivedPlotsGroupInvitations.ToArray())
                {
                    // Match on both city AND group, otherwise invites to OTHER groups of the same city are lost
                    if (it_current.CityName.Equals(it.CityName) && it_current.PlotsGroupName.Equals(it.PlotsGroupName))
                    {
                        this.ReceivedPlotsGroupInvitations.Remove(it_current);
                        this.ReceivedPlotsGroupInvitations.Add(it);
                    }
                }
            }
        }
        private void OnAllianceNewAll(string val)
        {
            AllianceInfo ai = JsonConvert.DeserializeObject<AllianceInfo>(val);
            claims.clientDataStorage.clientPlayerInfo.AllianceInfo = ai;
        }
        private void OnAllianceRemove(string val)
        {
            claims.clientDataStorage.clientPlayerInfo.AllianceInfo = null;
        }
        private void OnCityListAll(string val)
        {
            List<ClientCityInfoCellElement> ai = JsonConvert.DeserializeObject<List<ClientCityInfoCellElement>>(val);
            claims.clientDataStorage.clientPlayerInfo.AllCitiesList = ai;
        }
        private void OnCityListUpdate(string val)
        {
            List<ClientCityInfoCellElement> ai = JsonConvert.DeserializeObject<List<ClientCityInfoCellElement>>(val);
            foreach (var it in ai)
            {
                var existing = AllCitiesList.FirstOrDefault(c => c.Guid == it.Guid);
                if (existing != null)
                {
                    existing.CitizensAmount = it.CitizensAmount;
                    existing.MayorName = it.MayorName;
                    existing.ClaimedPlotsAmount = it.ClaimedPlotsAmount;
                    existing.AllianceName = it.AllianceName;
                    existing.TimeStampCreated = it.TimeStampCreated;
                    existing.Name = it.Name;
                    existing.Open = it.Open;
                    existing.InvMsg = it.InvMsg;
                    // Without this a village upgraded to a city keeps its old label in the list
                    // until the player rejoins.
                    existing.Tier = it.Tier;
                }
                else
                {
                    AllCitiesList.Add(it);
                }
            }
        }
        private void OnAllianceListAll(string val)
        {
            List<ClientAllianceInfoCellElement> ai = JsonConvert.DeserializeObject<List<ClientAllianceInfoCellElement>>(val);
            claims.clientDataStorage.clientPlayerInfo.AllAlliancesList = ai;
        }
        private void OnCityListRemove(string val)
        {
            List<string> guids = JsonConvert.DeserializeObject<List<string>>(val);
            foreach (var cityGuid in guids)
            {
                var existing = AllCitiesList.FirstOrDefault(c => c.Guid == cityGuid);
                if (existing != null)
                {
                    AllCitiesList.Remove(existing);
                }
            }
        }
        private void OnAllianceName(string val)
        {
            Tuple<string, string> tup = JsonConvert.DeserializeObject<Tuple<string, string>>(val);
            claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Name = tup.Item2;
        }
        private void OnAllianceInviteAdd(string val)
        {
            HashSet<ClientToAllianceInvitationCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientToAllianceInvitationCellElement>>(val);
            foreach (var it in pc)
            {
                this.CityInfo.ClientToAllianceInvitations.Add(it);
            }
        }
        private void OnAllianceInviteRemove(string val)
        {
            if (this.CityInfo == null) return;
            var invitationToRemove = this.CityInfo.ClientToAllianceInvitations.FirstOrDefault(inv => inv.AllianceGuid == val);
            if (invitationToRemove != null)
            {
                this.CityInfo.ClientToAllianceInvitations.Remove(invitationToRemove);
            }
        }
        private void OnAllianceLetterAdd(string val)
        {
            HashSet<ClientConflictLetterCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientConflictLetterCellElement>>(val);
            foreach (var it in pc)
            {
                this.CityInfo.ClientConflictLetterCellElements.Add(it);
            }
        }
        private void OnAllianceUnionLetterAdd(string val)
        {
            HashSet<ClientUnionLetterCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientUnionLetterCellElement>>(val);
            foreach (var it in pc)
            {
                this.CityInfo.ClientUnionLetterCellElements.Add(it);
            }
        }
        private void OnAllianceAllyAdd(string val)
        {
            if (this.AllianceInfo == null) return;
            List<string> pc = JsonConvert.DeserializeObject<List<string>>(val);
            foreach(var it in pc)
            {
                this.AllianceInfo.Allies.Add(it);
            }
        }
        private void OnAllianceLetterRemove(string val)
        {
            HashSet<Tuple<string, LetterPurpose>> pc = JsonConvert.DeserializeObject<HashSet<Tuple<string, LetterPurpose>>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in this.CityInfo.ClientConflictLetterCellElements.ToArray())
                {
                    if (it_current.Guid.ToString().Equals(it.Item1) && it_current.Purpose.Equals(it.Item2))
                    {
                        this.CityInfo.ClientConflictLetterCellElements.Remove(it_current);
                        break;
                    }
                }
            }
        }
        private void OnAllianceUnionLetterRemove(string val)
        {
            HashSet<string> pc = JsonConvert.DeserializeObject<HashSet<string>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in this.CityInfo.ClientUnionLetterCellElements.ToArray())
                {
                    if (it_current.Guid.ToString().Equals(it))
                    {
                        this.CityInfo.ClientUnionLetterCellElements.Remove(it_current);
                        break;
                    }
                }
            }            
        }
        private void OnPlayerNextPaymentDict(string val)
        {
            Dictionary<string, int> pc = JsonConvert.DeserializeObject<Dictionary<string, int>>(val);
            //if (pc.Count > 0)
            {
                this.PlayerNextPayments = pc;
            }
        }
        private void OnAllianceAllyRemove(string val)
        {
            List<string> pc = JsonConvert.DeserializeObject<List<string>>(val);
            foreach (var it in pc)
            {
                this.AllianceInfo.Allies.Remove(it);
            }
        }
        private void OnAllianceConflictAdd(string val)
        {
            HashSet<ClientConflictCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientConflictCellElement>>(val);
            foreach (var it in pc)
            {
                this.CityInfo.ClientConflictCellElements.Add(it);
            }
        }
        private void OnAllianceLetterAll(string val)
        {
            if (this.CityInfo == null) return;
            List<ClientConflictLetterCellElement> pc = JsonConvert.DeserializeObject<List<ClientConflictLetterCellElement>>(val);
            this.CityInfo.ClientConflictLetterCellElements = pc;
        }
        private void OnAllianceUnionLetterAll(string val)
        {
            if (this.CityInfo == null) return;
            List<ClientUnionLetterCellElement> pc = JsonConvert.DeserializeObject<List<ClientUnionLetterCellElement>>(val);
            this.CityInfo.ClientUnionLetterCellElements = pc;
        }
        private void OnAllianceAlliesAll(string val)
        {
            if (this.AllianceInfo == null) return;
            List<string> pc = JsonConvert.DeserializeObject<List<string>>(val);
            // "All" is a full snapshot — replace, don't append, or repeated sends duplicate allies
            this.AllianceInfo.Allies = pc ?? new List<string>();
        }
        private void OnAllianceConflictRemove(string val)
        {
            HashSet<string> pc = JsonConvert.DeserializeObject<HashSet<string>>(val);
            foreach (var it in pc)
            {
                foreach (var it_current in this.CityInfo.ClientConflictCellElements.ToArray())
                {
                    if (it_current.Guid.ToString().Equals(it))
                    {
                        this.CityInfo.ClientConflictCellElements.Remove(it_current);
                        break;
                    }
                }
            }
        }
        private void OnAllianceConflictAll(string val)
        {
            HashSet<ClientConflictCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientConflictCellElement>>(val);
            foreach (var it in pc)
            {
                this.CityInfo.ClientConflictCellElements.Add(it);
            }
        }
        private void OnCityCasusBelliAll(string val)
        {
            if (this.CityInfo == null) return;
            // Full snapshot - replace, never append, or expired reasons would linger.
            this.CityInfo.ClientCasusBelliCellElements =
                JsonConvert.DeserializeObject<List<ClientCasusBelliCellElement>>(val) ?? new List<ClientCasusBelliCellElement>();
        }
        private void OnAllianceUnionBreaksAll(string val)
        {
            if (this.AllianceInfo == null) return;
            // Full snapshot - replace, so a cancelled break disappears.
            this.AllianceInfo.PendingUnionBreaks =
                JsonConvert.DeserializeObject<Dictionary<string, long>>(val) ?? new Dictionary<string, long>();
        }
        private void OnCityPlotRecolor(string val)
        {
            HashSet<Vec2i> pc = JsonConvert.DeserializeObject<HashSet<Vec2i>>(val);
            foreach (var it in pc)
            {
                if (claims.clientDataStorage.getSavedPlot(it, out var plot))
                {
                    claims.clientModInstance.plotsMapLayer.OnResChunkPixels(it, plot.cityName);
                }
            }
        }
        private void OnAllianceConflictWarrangesUpdated(string val)
        {
            HashSet<ClientConflictCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientConflictCellElement>>(val);
            foreach (var it in pc)
            {
                ClientConflictCellElement cell = this.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == it.Guid);
                if (cell != null)
                {
                    cell.WarRanges = it.WarRanges;
                    cell.FirstWarRanges = it.FirstWarRanges;
                    cell.SecondWarRanges = it.SecondWarRanges;
                    cell.NextBattleDateEnd = it.NextBattleDateEnd;
                    cell.NextBattleDateStart = it.NextBattleDateStart;
                }
            }
        }
        private void OnAllianceConflictScoreUpdated(string val)
        {
            HashSet<ClientConflictCellElement> pc = JsonConvert.DeserializeObject<HashSet<ClientConflictCellElement>>(val);
            foreach (var it in pc)
            {
                ClientConflictCellElement cell = this.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == it.Guid);
                if (cell != null)
                {
                    cell.FirstScore = it.FirstScore;
                    cell.SecondScore = it.SecondScore;
                    cell.State = it.State;
                }
            }
        }
        private void OnAllianceConflictWarTimeMarkStart(string val)
        {
            List<string> guids = JsonConvert.DeserializeObject<List<string>>(val);
            if (guids == null || guids.Count == 0) return;
            string conflictGuid = guids[0];
            ClientConflictCellElement cell = this.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == conflictGuid);
            if (cell != null)
            {
                cell.ActiveWarTime = true;
            }
            var player = claims.capi?.World?.Player;
            if (player != null)
            {
                claims.capi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/deepbell"), player.Entity, null, false, 32f, 0.5f);
            }
        }
        private void OnAllianceConflictWarTimeMarkEnd(string val)
        {
            List<string> guids = JsonConvert.DeserializeObject<List<string>>(val);
            if (guids == null || guids.Count == 0) return;
            string conflictGuid = guids[0];
            ClientConflictCellElement cell = this.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == conflictGuid);
            if (cell != null)
            {
                cell.ActiveWarTime = false;
            }
            var player = claims.capi?.World?.Player;
            if (player != null)
            {
                claims.capi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/deepbell"), player.Entity, null, false, 32f, 0.5f);
            }
        }
        private void OnCityLog(string val)
        {
            if (CityInfo == null) return;
            CityInfo.EventLog = JsonConvert.DeserializeObject<List<CityLogEntry>>(val) ?? new List<CityLogEntry>();
        }
        private void OnCityPlotsMap(string val)
        {
            if (CityInfo == null) return;
            CityInfo.PlotsMap = JsonConvert.DeserializeObject<List<CityPlotMiniInfo>>(val) ?? new List<CityPlotMiniInfo>();
        }
        private void OnCityPlotMarketHistory(string val)
        {
            if (CityInfo == null) return;
            CityInfo.PlotMarketHistory = JsonConvert.DeserializeObject<List<PlotSaleRecord>>(val) ?? new List<PlotSaleRecord>();
        }

        private void OnCityPlotAuctions(string val)
        {
            if (CityInfo == null) return;
            CityInfo.PlotAuctions = JsonConvert.DeserializeObject<List<PlotAuctionCellElement>>(val) ?? new List<PlotAuctionCellElement>();
        }

        private void OnPlayerBalance(string val)
        {
            double.TryParse(val, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double balance);
            PlayerBalance = balance;
        }
        private void OnAllianceBalance(string val)
        {
            if (AllianceInfo != null)
                AllianceInfo.Balance = (double)decimal.Parse(val, CultureInfo.InvariantCulture);
        }
        private void OnCityDayPayment(string val)
        {
            CityInfo.CityDayPayment = double.Parse(val, CultureInfo.InvariantCulture);
        }
        private void OnCityPermissionsUpdated(string val)
        {
            PermsHandler ph = JsonConvert.DeserializeObject<PermsHandler>(val);
            this.CityInfo.PermsHandler = ph;
        }
    }
}
