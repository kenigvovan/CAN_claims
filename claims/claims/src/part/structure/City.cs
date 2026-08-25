using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.cityplotsgroups;
using claims.src.delayed.invitations;
using claims.src.gui.playerGui.structures;
using claims.src.part.interfaces;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.perms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace claims.src.part
{
    public class City : Part, ISender, IReceiver, IGetStatus, ICooldown, IConflictParty
    {
        HashSet<PlayerInfo> cityCitizens = new HashSet<PlayerInfo>();
        PermsHandler permsHandler = new PermsHandler();
        PlayerInfo mayor;
        List<Plot> cityPlots = new List<Plot>();
        public double DebtBalance { get; set; }
        public long TimeStampCreated {  get; set; }
        List<Invitation> listSentInvitations = new List<Invitation>();
        List<Invitation> listReceivedInvitations = new List<Invitation>();      
        List<CityPlotsGroup> cityPlotsGroups = new List<CityPlotsGroup>();
        List<Prison> prisons = new List<Prison>();
        int defaultPlotCost = 0;
        public Alliance Alliance { get; set; }
        public List<PlayerInfo> criminals = new List<PlayerInfo>();
        public string invMsg { get; set; } = "";
        public bool openCity { get; set; } = false;
        public List<CityPlotsGroupInvitation> groupInvitations = new List<CityPlotsGroupInvitation>();
        public int fee { get; set; } = 0;
        bool isTechnical { get; set; } = false;
        // Village or full city. Independent of isTechnical (an admin-owned city skipped by the
        // day timer) and of PlotType.CAMP (a war camp).
        public CityTier Tier { get; set; } = CityTier.CITY;
        public bool IsVillage() => Tier == CityTier.VILLAGE;

        /// <summary>
        /// The main plot of a village - the one carrying its anchor, granary and raid state.
        /// False for a city, and for a village that somehow lost it (an admin-made one, until
        /// PartInits.EnsureVillageAnchor gives it one).
        /// </summary>
        public bool TryGetVillageMain(out Plot mainPlot, out PlotDescVillage desc)
        {
            if (IsVillage())
            {
                foreach (Plot plot in cityPlots)
                {
                    if (plot.Type == PlotType.VILLAGE_MAIN && plot.PlotDesc is PlotDescVillage villageDesc)
                    {
                        mainPlot = plot;
                        desc = villageDesc;
                        return true;
                    }
                }
            }
            mainPlot = null;
            desc = null;
            return false;
        }
        int bonusPlots { get; set; } = 0;
        public HashSet<Plot> summonPlots = new HashSet<Plot>();
        // War camps (PlotType.CAMP) this city owns; forward respawn points during active wars.
        public HashSet<Plot> campPlots = new HashSet<Plot>();
        public int Extrachunksbought { get; set; } = 0;
        public int cityColor { get; set; } = -992222222;
        // Coat of arms: ';'-separated texture layers, see Emblem. Empty means the city has none yet.
        public string Emblem { get; set; } = "";
        public string MoneyAccountName => claims.config.CITY_ACCOUNT_STRING_PREFIX + Guid;
        public Dictionary<Vec2i, Vec3i> TempleRespawnPoints { get; } = new Dictionary<Vec2i, Vec3i>();
        public List<City> HostileCities { get; set; } = new List<City>();
        public List<City> ComradeCities { get; set; } = new List<City>();
        // War re-declare cooldowns: opponent party guid -> unix seconds when the war with them ended.
        public Dictionary<string, long> WarCooldowns { get; set; } = new();
        // Casus belli grievances: offender party guid -> unix seconds of the latest grievance.
        public Dictionary<string, long> Grievances { get; set; } = new();
        // Non-aggression pacts: partner party guid -> unix seconds when the pact expires.
        public Dictionary<string, long> NonAggressionPacts { get; set; } = new();
        // Free-war justifications from refused/expired ultimatums: target party guid -> unix seconds when it lapses.
        public Dictionary<string, long> WarJustifications { get; set; } = new();
        // Vassalage: this city's overlord (empty = independent) and its vassals.
        public string OverlordGuid { get; set; } = "";
        public List<City> VassalCities { get; set; } = new List<City>();
        public long VassalSince { get; set; } = 0;
        public bool IsVassal() => !string.IsNullOrEmpty(OverlordGuid);
        public City GetOverlord() => claims.dataStorage.getCityByGUID(OverlordGuid, out City o) ? o : null;
        public bool Dirty { get; set; } = false;
        /// <summary>
        /// Declared neutral: cannot declare war, cannot be declared upon, and pays
        /// NEUTRAL_CITY_PAYMENT a day on top of its upkeep. Read through <see cref="IsNeutral"/>,
        /// which also honours the server switch - a host turning neutrality off should not leave
        /// cities that already bought it unattackable.
        /// </summary>
        public bool Neutral { get; set; } = false;
        public bool IsNeutral => Neutral && claims.config.NEUTRALITY_ENABLED;
        /// <summary>Unix seconds when neutrality was last given up; 0 if it never was.</summary>
        public long NeutralDroppedAt { get; set; } = 0;
        public HashSet<Conflict> RunningConflicts { get; } = new HashSet<Conflict>();
        public Dictionary<string, CustomCityRank> CustomCityRanks { get; set; } = new();
        public List<CityLogEntry> EventLog { get; set; } = new List<CityLogEntry>();
        public City(string valName, string guid, bool isTechnical = false) : base(valName, guid)
        {
            this.isTechnical = isTechnical;
        }

        public void AddLogEntry(EnumCityLogEvent eventType, params string[] args)
        {
            EventLog.Add(new CityLogEntry(TimeFunctions.getEpochSeconds(), eventType, args));
            int maxEntries = claims.config.CITY_LOG_MAX_ENTRIES;
            if (EventLog.Count > maxEntries)
                EventLog.RemoveRange(0, EventLog.Count - maxEntries);
        }


        /*****************************************************************/
        public bool HasCityRank(string rankName)
        {
            return CustomCityRanks.ContainsKey(rankName);
        }
        public bool AddNewCityRank(string rankName, CustomCityRank rank)
        {
            if (CustomCityRanks.ContainsKey(rankName)) return false;
            CustomCityRanks.Add(rankName, rank);
            return true;
        }
        public bool GrantPlayerRank(string rankName, PlayerInfo playerInfo)
        {
            if(CustomCityRanks.TryGetValue(rankName, out var rankInfo))
            {
                if(rankInfo.CitizensNames.Contains(playerInfo.GetPartName()))
                {
                    return false;
                }
                return rankInfo.CitizensNames.Add(playerInfo.GetPartName());
            }
            return false;
        }
        public bool RevokePlayerRank(string rankName, PlayerInfo playerInfo)
        {
            if (CustomCityRanks.TryGetValue(rankName, out var rankInfo))
            {
                if (rankInfo.CitizensNames.Contains(playerInfo.GetPartName()))
                {
                    return rankInfo.CitizensNames.Remove(playerInfo.GetPartName());
                }
                return false;
            }
            return false;
        }
        public bool RemoveCityRank(string rankName)
        {
            return CustomCityRanks.Remove(rankName);
        }
        /// <summary>
        /// Clears the safety marks of plots whose flag is off again. Runs right after the payment,
        /// so a flag that was on at any point during the day has already been billed.
        /// </summary>
        public void UpdateSafetyFlagMarks()
        {
            foreach (var it in cityPlots)
            {
                bool changed = false;
                if (it.MarkedNoPvp && it.getPermsHandler().pvpFlag)
                {
                    it.MarkedNoPvp = false;
                    changed = true;
                }
                if (it.MarkedNoMobSpawn && !it.getPermsHandler().noMobSpawnFlag)
                {
                    it.MarkedNoMobSpawn = false;
                    changed = true;
                }
                // Persisted right away: a mark that only cleared in memory would come back on the
                // next server start and be billed a second time.
                if (changed) it.saveToDatabase();
            }
        }
        /// <summary>
        /// What the city pays daily for the safety flags of its plots. Keeping players out (no pvp)
        /// and keeping hostile mobs out (nomobspawn) are separate flags but one bill: a second
        /// counter per flag is how this grows into getFireCost() next.
        ///
        /// Only the city's own land is counted here: a plot with an owner is billed to that owner
        /// with the rest of their fee (<see cref="Plot.SafetyFlagsPaidByOwner"/>), so whoever turns
        /// the flag on is the one who pays for it.
        /// </summary>
        public double GetSafetyFlagsCost()
        {
            double sum = 0;
            foreach (Plot it in cityPlots)
            {
                if (it.SafetyFlagsPaidByOwner()) continue;
                sum += it.GetSafetyFlagsCost();
            }
            return sum;
        }
        public List<PlayerInfo> getCriminals()
        {
            return criminals;
        }
        public int getBonusPlots()
        {
            return bonusPlots;
        }
        public void setBonusPlots(int val)
        {
            this.bonusPlots = val;
        }
        public bool isTechnicalCity()
        {
            return isTechnical;
        }
        public bool HasAlliance()
        {
            return Alliance != null;
        }
        public void setIsTechnicalCity(bool val)
        {
            this.isTechnical = val;
        }
        public void setIsTechnicalCity(string val, TextCommandResult tcr)
        {
            if (val.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                isTechnical = false;
                tcr.StatusMessage = "claims:city_is_not_technical";
            }
            else if (val.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                isTechnical = true;
                tcr.StatusMessage = "claims:city_is_now_technical";
            }
            else
            {
                tcr.Status = EnumCommandStatus.Error;
            }
        }
        public int getDefaultPlotCost()
        {
            return defaultPlotCost;
        }
        public void setDefaultPlotCost(int cost)
        {
            this.defaultPlotCost = cost;
        }
        public List<CityPlotsGroup> getCityPlotsGroups()
        {
            return cityPlotsGroups;
        }
        public List<Prison> getPrisons()
        {
            return prisons;
        }

        public bool TryGetRandomPrisonWithCell(out Prison prison)
        {
            Random random = claims.dataStorage.r;
            int[] cellNumber = Enumerable.Range(0, prisons.Count).OrderBy(x => claims.dataStorage.r.Next()).ToArray();
            foreach(var it in cellNumber)
            {
                if (prisons[it].getPrisonCells().Count > 0)
                {
                    prison = prisons[it];
                    return true;
                }
            }
            prison = null;
            return false;
        }

        public bool hasPrison()
        {
            return prisons.Count > 0;
        }

        public bool isCitizen(PlayerInfo playerInfo)
        {
            return cityCitizens.Contains(playerInfo);
        }  

        public HashSet<PlayerInfo> getPlayerInfos()
        {
            return cityCitizens;
        }

        public bool isMayor(PlayerInfo playerInfo)
        {
            if (mayor == null)
            {
                return false;
            }
            if(mayor.Equals(playerInfo))
            {
                return true;
            }
            return false;
        }

        public List<IServerPlayer> getOnlineCitizens()
        {
            List<IServerPlayer> outList = new List<IServerPlayer>();
            IServerPlayer tmp;
            foreach (var it in getCityCitizens())
            {
                tmp = (IServerPlayer)claims.sapi.World.PlayerByUid(it.Guid);
                if (tmp != null && claims.sapi.World.AllOnlinePlayers.Contains(tmp))
                {
                    outList.Add(tmp);
                }
            }
            return outList;
        }
        public List<Plot> getCityPlots()
        {
            return cityPlots;
        }

        public static event Action<string, EnumPlotsMapChangeReason> PlotsMapChanged;
        public void FirePlotsMapChanged(EnumPlotsMapChangeReason reason) => PlotsMapChanged?.Invoke(this.Guid, reason);

        public static event Action<City> CityCreated;
        public static event Action<City> CityDestroyed;
        public static event Action<City, PlayerInfo> CitizenJoined;
        public static event Action<City, PlayerInfo, EnumCityLeaveReason> CitizenLeft;
        public static event Action<City, PlayerInfo> MayorChanged;

        public static void FireCityCreated(City city) => CityCreated?.Invoke(city);
        public static void FireCityDestroyed(City city) => CityDestroyed?.Invoke(city);
        public void FireCitizenJoined(PlayerInfo player) => CitizenJoined?.Invoke(this, player);
        public void FireCitizenLeft(PlayerInfo player, EnumCityLeaveReason reason) => CitizenLeft?.Invoke(this, player, reason);
        public void FireMayorChanged(PlayerInfo newMayor) => MayorChanged?.Invoke(this, newMayor);

        public HashSet<PlayerInfo> getCityCitizens()
        {
            return cityCitizens;
        }

        public PlayerInfo getMayor()
        {
            return mayor;
        }
        public bool HasMayor()
        {
            return mayor != null;
        }

        public bool setMayor(PlayerInfo player)
        {
            this.mayor = player;
            // Leader is checked by Alliance.IsLeader, keep it in sync with the capital's mayor
            if (HasAlliance() && this.Equals(Alliance.MainCity))
            {
                Alliance.Leader = player;
            }
            return true;
        }

        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().saveCity(this, update);
        }

        public List<Invitation> GetSentInvitations()
        {
            return listSentInvitations;
        }

        public List<Invitation> getReceivedInvitations()
        {
            return listReceivedInvitations;
        }

        public void deleteSentInvitation(Invitation invitation)
        {
            this.listSentInvitations.Remove(invitation);
        }

        public void addSentInvitation(Invitation invitation)
        {
            this.listSentInvitations.Add(invitation);
        }

        public int getMaxSentInvitations()
        {
            return claims.config.MAX_SENT_INVITATIONS_CITY;
        }

        public void deleteReceivedInvitation(Invitation invitation)
        {
            this.listReceivedInvitations.Remove(invitation);
        }

        public void addReceivedInvitation(Invitation invitation)
        {
            this.listReceivedInvitations.Add(invitation);
        }

        public int getMaxReceivedInvitations()
        {
            return claims.config.MAX_RECEIVED_INVITATIONS_CITY;
        }

        public double getExpense()
        {
            double outSum = 0;
            foreach (Plot plot in getCityPlots())
            {
                if (!PlotInfo.dictPlotTypes.TryGetValue(plot.Type, out PlotInfo plotInfo))
                    continue;
                outSum += plotInfo.getCost();
            }
            return outSum;
        }
        public void AddTempleRespawnPoint(Plot plot, BlockPos blockPos)
        {
            if(TempleRespawnPoints.ContainsKey(plot.getPos()))
            {
                TempleRespawnPoints[plot.getPos()] = blockPos.ToVec3i();
            }
            else
            {
                TempleRespawnPoints[plot.getPos()] = blockPos.ToVec3i();
            }
        }

        public void AddTempleRespawnPoint(Vec2i rPlotPos, Vec3i blockPos)
        {
            if (TempleRespawnPoints.ContainsKey(rPlotPos))
            {
                TempleRespawnPoints[rPlotPos] = blockPos;
            }
            else
            {
                TempleRespawnPoints[rPlotPos] = blockPos;
            }
        }

        public bool RemoveTempleRespawnPoint(Plot plot)
        {
            return TempleRespawnPoints.Remove(plot.getPos());
        }

        public bool HasTempleRespawnPoints()
        {
            return TempleRespawnPoints.Count > 0;
        }

        public List<string> getStatus(PlayerInfo forPlayer = null)
        {
            List<string> outStrings = new List<string>
            {
                "[" + getPartNameReplaceUnder() + "]" + (this.openCity
                                                                        ? Lang.Get("claims:open_city")
                                                                        : "") + "\n"
            };

            if(isMayor(forPlayer) && getCityPlots().Count > 0)
            {
                Plot plot = getCityPlots()[0];
                outStrings.Add(plot.getPos().ToString() + "\n");
            }

            if (invMsg.Length != 0)
            {
                outStrings.Add($"{Lang.Get("claims:invite_msg_city", invMsg)}\n");
            }
            if(this.mayor != null)
                outStrings.Add($"{Lang.Get("claims:mayor")}  {getMayor().getPartNameReplaceUnder()}\n");

            // A village has no treasury, no debt and no upkeep, so none of that is printed for one.
            if (!IsVillage())
            {
                if (claims.economyProvider.SupportsPlayerWallet)
                    outStrings.Add(Lang.Get("claims:bank_status", claims.economyProvider.GetBalance(this.MoneyAccountName)));
                outStrings.Add(DebtBalance > 0 ? Lang.Get("claims:city_debt_status") + DebtBalance + "\n" : "\n");
            }
            CityLevelInfo cityLevelInfo = Settings.getCityLevelInfo(getCityCitizens().Count);
            // Villages have a flat plot limit of their own, not the citizen-count levels.
            outStrings.Add($"{Lang.Get("claims:city_claimed_amount_status", this.getCityPlots().Count, Settings.getMaxNumberOfPlotForCity(this)) + (!IsVillage() && cityLevelInfo.Maxextrachunksbought > 0 ? " " + Lang.Get("claims:city_claimed_extra_amount_status", this.Extrachunksbought, cityLevelInfo.Maxextrachunksbought) + "\n" : "\n")}");
            outStrings.Add($"{Lang.Get("claims:created")} {TimeFunctions.getDateFromEpochSeconds(TimeStampCreated)}\n");
            // Said plainly, as an alliance says it: it decides whether this place can be warred.
            if (IsNeutral) outStrings.Add(Lang.Get("claims:neutral") + "\n");
            // Its own people know when the village is open to attack; outsiders have to come and
            // find out on the spot.
            if (IsVillage() && forPlayer != null && isCitizen(forPlayer))
            {
                outStrings.Add(VillageRaidHelper.DescribeSchedule(this) + "\n");
            }
            if (!IsVillage())
            {
                double safetyFlagsCost = GetSafetyFlagsCost();
                outStrings.Add(Lang.Get("claims:city_outgo") + (getExpense() + cityLevelInfo.UnconditionalPayment).ToString() + (safetyFlagsCost > 0 ? "(+" + safetyFlagsCost.ToString() + ")" : "") + "\n");
            }
            outStrings.Add($"{Lang.Get("claims:citizens")} {StringFunctions.makeStringPlayersName(this.getCityCitizens(), ", ")}");

            outStrings.Add("\n");
            outStrings.Add(permsHandler.getStringForChat() + "\n");
            
            return outStrings;
        }

        public PermsHandler getPermsHandler()
        {
            return permsHandler;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Guid.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            if(obj is City)
            {
                return this.Guid.Equals(((City)obj).Guid);
            }
            return false;
        }

        public string getNameSender()
        {
            return GetPartName();
        }

        public string getNameReceiver()
        {
            return GetPartName();
        }

        public bool setCityOpenCloseState(string newVal)
        {
            if (newVal.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                openCity = false;
                return true;
            }
            else if (newVal.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                openCity = true;
                return true;
            }
            return false;
        }
        public double GetDayPaymentAmount()
        {
            // A village pays nothing: it lives off the supplies in its granary, and the day timer
            // skips it entirely. Returning 0 also keeps the treasury out of its GUI.
            if (IsVillage()) return 0;

            double sumToPay = claims.config.CITY_BASE_CARE;
            // Neutrality is bought by the day, the way an alliance buys it.
            if (IsNeutral) sumToPay += claims.config.NEUTRAL_CITY_PAYMENT;
            // What the plots pay for being safe: no pvp, no hostile spawns. The marks behind it are
            // cleared by the day timer, not here - this is also called to show the figure in the GUI.
            sumToPay += this.GetSafetyFlagsCost();

            foreach (Plot plot in this.getCityPlots())
            {
                if (!PlotInfo.dictPlotTypes.TryGetValue(plot.Type, out PlotInfo plotInfo))
                    continue;
                sumToPay += plotInfo.getCost();
            }
            CityLevelInfo cityLevelInfo = Settings.getCityLevelInfo(this.getCityCitizens().Count);
            int cityOutGo = cityLevelInfo.UnconditionalPayment;

            sumToPay += cityOutGo;
            claims.sapi.Logger.Debug(string.Format("[claims] processCityCare, withdraw {0} from city {1} account. Balance before is {2}, debt is {3}.",
                sumToPay, this.GetPartName(), claims.economyProvider.GetBalance(this.MoneyAccountName), this.DebtBalance));
            return sumToPay;
        }

        /// <summary>
        /// Check if name of the city can be changed to provided.
        /// Change it if can be, change also account name, save to db, set tcr value.
        /// Do NOT check for permissions or money.
        /// </summary>
        /// <param name="tcr"></param>
        /// <param name="newCityName"></param>
        public bool rename(string newCityName)
        {
            string filteredName = Filter.filterName(newCityName);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                return false;
            }
            if (filteredName.Length > claims.config.MAX_LENGTH_CITY_NAME)
            {
                return false;
            }
            if (claims.dataStorage.nameForCityOrVillageIsTaken(filteredName))
            {
                return false;
            }

            if (claims.economyProvider.UpdateAccount(this.MoneyAccountName, new Dictionary<string, object> { { "lastknownname", filteredName } }))
            {
                if (!claims.dataStorage.changeCityName(this, filteredName)) return false;
                SetPartName(filteredName);
                saveToDatabase();
                UsefullPacketsSend.AddToQueueCityInfoUpdate(Guid, EnumPlayerRelatedInfo.CITY_NAME);
                return true;
            }
            return false;                   
        }
        public void trySetPlotColor(int color)
        {
            cityColor = color;
            this.saveToDatabase();
        }
        /// <summary>Replaces the coat of arms, normalized so that what is stored can be drawn.</summary>
        public void SetEmblem(string emblem)
        {
            Emblem = EmblemHandler.Normalize(emblem);
            this.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(Guid, EnumPlayerRelatedInfo.CITY_EMBLEM);
            // Everyone, not just the citizens: banners of this city stand where anybody may walk past.
            UsefullPacketsSend.BroadcastCityEmblems();
        }
        public void setAllCityPlotsMarkedAsUpdated()
        {
            //for all plots set new mark and add to queue for send
            foreach(var plot in getCityPlots())
            {
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            }
        }

        // IConflictParty
        public List<City> GetCities() => new List<City> { this };
        public void AddHostileParty(IConflictParty party)
        {
            foreach (City c in party.GetCities())
            {
                if (!HostileCities.Contains(c))
                    HostileCities.Add(c);
            }
        }
        public void RemoveHostileParty(IConflictParty party)
        {
            foreach (City c in party.GetCities())
                HostileCities.Remove(c);
        }
        public IEnumerable<IConflictParty> GetHostileParties() => HostileCities.Cast<IConflictParty>();
    }
}
