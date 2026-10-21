using claims.src.auxialiry;
using claims.src.economy;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.war;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Security.Claims;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.timers
{
    public class DayTimer
    {
        public static Dictionary<City, List<Plot>> citiesPlots = new();
        public static List<(City, string)> toDeleteCities = new();
        static List<Alliance> toDeleteAlliancies = new();

        private class DayTimerState
        {
            public long LastProcessedDayStart;
        }
        private static DayTimerState state;

        private static string StateFilePath()
        {
            return claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0
                ? Path.Combine(GamePaths.ModConfig, "claims_daytimer.json")
                : Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, "claims_daytimer.json");
        }
        private static DayTimerState LoadState()
        {
            try
            {
                string path = StateFilePath();
                if (!File.Exists(path)) return null;
                return JsonConvert.DeserializeObject<DayTimerState>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                claims.sapi.Logger.Warning("[claims] DayTimer: can't read state file: " + ex.Message);
                return null;
            }
        }
        private static void SaveState()
        {
            try
            {
                File.WriteAllText(StateFilePath(), JsonConvert.SerializeObject(state));
            }
            catch (Exception ex)
            {
                claims.sapi.Logger.Error("[claims] DayTimer: can't write state file: " + ex.Message);
            }
        }

        /// <summary>
        /// Runs the day if a new cycle has begun. The marker is written before the run, so a crash
        /// in the middle never charges the same cycle twice.
        /// </summary>
        public static void CheckNewDay(bool afterStart)
        {
            long current = TimeFunctions.GetCurrentDayStart(TimeFunctions.getEpochSeconds());
            // Static survives a world switch in singleplayer.
            if (afterStart) state = null;
            if (state == null)
            {
                state = LoadState();
                if (state == null)
                {
                    state = new DayTimerState { LastProcessedDayStart = current };
                    SaveState();
                    return;
                }
            }
            if (current <= state.LastProcessedDayStart) return;

            long runs = CyclesToRun(state.LastProcessedDayStart, current, afterStart,
                TimeFunctions.DayLength, claims.config.DAY_MAX_MISSED_CYCLES_TO_PROCESS);

            state.LastProcessedDayStart = current;
            SaveState();
            for (long i = 0; i < runs; i++)
            {
                new DayTimer().Run();
            }
        }

        /// <summary>
        /// How many day cycles to run now. A running server catches one cycle at a time; on start
        /// the cycles missed while it was down are caught up, at most <paramref name="maxMissed"/>
        /// of them - every cycle charges fees and can evict whoever cannot pay, so a long downtime
        /// must not turn into a week of charges at once. Zero means the missed cycles are skipped.
        /// </summary>
        public static long CyclesToRun(long lastProcessedDayStart, long currentDayStart, bool afterStart,
                                       long dayLength, int maxMissed)
        {
            if (currentDayStart <= lastProcessedDayStart) return 0;
            if (!afterStart) return 1;
            long passed = Math.Max(1, (currentDayStart - lastProcessedDayStart) / dayLength);
            return Math.Min(passed, Math.Max(0, maxMissed));
        }

        public void Run()
        {
            MessageHandler.sendDebugMsg("[claims] DayTimer::start collecting");
            // Clear static state at the top — if a previous run threw, the dicts stay
            // populated and the next Add() throws ArgumentException on duplicate key
            citiesPlots.Clear();
            toDeleteCities.Clear();
            toDeleteAlliancies.Clear();

            foreach (City city in claims.dataStorage.getCitiesList())
            {
                citiesPlots.Add(city, new List<Plot>());
            }

            //FILL LISTS OF CITIES' PLOTS
            foreach(Plot plot in claims.dataStorage.getClaimedPlots().Values)
            {
                if(plot.hasCity() && citiesPlots.TryGetValue(plot.getCity(), out List<Plot> plots))
                {
                    plots.Add(plot);
                }
            }
            MessageHandler.sendDebugMsg("[claims] DayTimer::from cities" + StringFunctions.concatStringsWithDelim(citiesPlots.Keys.ToArray(), ','));
            //All players processed
            Isolated("citizens' fees", processCitiesFee);
            Isolated("alliance fees", ProcessAlliancesFee);
            Isolated("vassal tribute", ProcessVassalTribute);

            //All cities
            Isolated("city upkeep", processCitiesCare);

            citiesPlots.Clear();

            Isolated("alliance upkeep", ProcessAlliancesCare);

            MessageHandler.sendGlobalMsg(Lang.Get("claims:new_day_log_msg"));
            MessageHandler.sendDebugMsg("[claims] DayTimer::new day here");
        }
        /// <summary>
        /// Runs one piece of the day so that its failure is logged and the rest goes on. The day's
        /// marker is written before the run, so a day cut short by one throwing city was never
        /// retried: every city after it in the list went that day without upkeep or fees.
        /// </summary>
        private static void Isolated(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                claims.sapi.Logger.Error("[claims] DayTimer: {0} failed, the rest of the day goes on: {1}", what, e);
            }
        }

        public static void processCitiesCare()
        {
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                if(city.isTechnicalCity())
                {
                    MessageHandler.sendErrorMsg(Lang.Get("claims:city_istechnical_no_care_processing", city.GetPartName()));
                    continue;
                }
                // A village is kept alive by supplies in its granary, not by money.
                if (city.IsVillage())
                {
                    continue;
                }
                Isolated("upkeep of " + city.GetPartName(), () => processCityCare(city));
            }
            //DELETE CITIES WHICH WERE MARKED
            foreach (var city in toDeleteCities)
            {
                Isolated("demolition of " + city.Item1.GetPartName(), () => PartDemolition.demolishCity(city.Item1, city.Item2));
            }

            toDeleteCities.Clear();
        }
        public static void processCityCare(City city)
        {
            decimal sumToPay = (decimal)city.GetDayPaymentAmount();
            // The safety flags of this day are now billed - both here and, for owned plots, in
            // processCityFee above - so plots whose flag is off again stop counting from now on.
            city.UpdateSafetyFlagMarks();
            string capturedGuid = city.Guid;
            // The only field of the city this method touches. A solvent city with no debt changes
            // nothing here - the money itself lives in the economy provider - and used to be written
            // back every single day regardless.
            double debtBefore = city.DebtBalance;
            // Cities pay whole coins only: the check, the payment and the debt all use the rounded-down sum.
            decimal today = Math.Floor(sumToPay);
            decimal due = today + (decimal)city.DebtBalance;
            decimal balance = claims.economyProvider.GetBalance(city.MoneyAccountName);
            if (balance < due)
            {
                // Pay what the treasury holds, the rest becomes debt.
                if (balance > 0 && claims.economyProvider.Withdraw(city.MoneyAccountName, balance) == MoneyOperationResult.Success)
                {
                    city.DebtBalance = (double)(due - balance);
                }
                else
                {
                    city.DebtBalance += (double)today;
                }
                OnDebtGrown(city, capturedGuid);
            }
            else
            {
                // The whole bill decides whether there is anything to collect, not the daily part of
                // it alone: a city whose daily charge rounds to nothing still owes what it failed to
                // pay earlier, and returning here left that debt standing forever however full the
                // treasury was - the branch is only reached when the money for it is there.
                if (due < 1)
                {
                    return;
                }
                if(claims.economyProvider.Withdraw(city.MoneyAccountName, due) == MoneyOperationResult.Success )
                {
                    if (city.DebtBalance > 0)
                    {
                        city.DebtBalance = 0;
                    }
                    claims.sapi.Event.RegisterCallback(_ =>
                        UsefullPacketsSend.AddToQueueCityInfoUpdate(capturedGuid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DEBT), 0);
                }
                else
                {
                    // The money is there but the withdrawal failed: today's bill becomes debt
                    // instead of being forgiven.
                    city.DebtBalance += (double)today;
                    OnDebtGrown(city, capturedGuid);
                }
            }
            claims.sapi.Logger.Debug(string.Format("[claims] processCityCare, withdrew {0} from city {1} account. Balance after is {2}, debt is {3}.",
                sumToPay, city.GetPartName(), claims.economyProvider.GetBalance(city.MoneyAccountName), city.DebtBalance));
            if (city.DebtBalance != debtBefore)
            {
                city.saveToDatabase();
            }
        }
        private static void OnDebtGrown(City city, string capturedGuid)
        {
            if (claims.config.DELETE_CITY_IF_DOESN_PAY_FEE)
            {
                if (city.DebtBalance > claims.config.CITY_MAX_DEBT)
                {
                    // A fire sale takes precedence over the wrecking ball: while the city's land
                    // is on the block it stays standing, and it is demolished only if the sale
                    // brought nothing. Begin() answers false whenever bankruptcy sales are off -
                    // by the mode, by the auction switch or by a stubbed economy - so the old
                    // behaviour needs no separate check here.
                    if (!part.structure.plots.auction.BankruptcyHelper.Begin(city))
                    {
                        toDeleteCities.Add((city, Lang.Get("claims:city_delete_reason_debt_is_too_high", city.DebtBalance)));
                    }
                }
            }
            claims.sapi.Event.RegisterCallback(_ =>
                UsefullPacketsSend.AddToQueueCityInfoUpdate(capturedGuid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DEBT), 0);
        }
        public static void ProcessAllianceCare(Alliance alliance)
        {
            double toPay = claims.config.ALLIANCE_BASE_CARE;

            if (alliance.IsNeutral)
            {
                toPay += claims.config.NEUTRAL_ALLANCE_PAYMENT;
            }

            if (claims.economyProvider.GetBalance(alliance.MoneyAccountName) < (decimal)toPay)
            {
                toDeleteAlliancies.Add(alliance);
            }
            else
            {
                claims.economyProvider.Withdraw(alliance.MoneyAccountName, (decimal)toPay);
            }
        } 
        public static void processCitiesFee()
        {
            foreach(City city in citiesPlots.Keys)
            {
                // Villages collect no fee and own no treasury to collect it into.
                if (city.IsVillage()) continue;
                Isolated("fees of " + city.GetPartName(), () => processCityFee(city));
            }
        }
        /// <summary>What a person paying this city owes today.</summary>
        public enum FeeOutcome
        {
            /// <summary>Less than one coin: not worth a transfer.</summary>
            Nothing,
            Pay,
            /// <summary>Cannot pay and is this city's citizen, with eviction switched on.</summary>
            Kick,
            /// <summary>Cannot pay but stays: loses their plots in this city instead.</summary>
            TakePlots
        }

        /// <summary>
        /// The decision for one bill. Plots group membership is dropped in both Kick and TakePlots.
        /// </summary>
        public static FeeOutcome DecideFee(decimal toPay, decimal balance, bool isOwnCitizen, bool kickNonPayers)
        {
            if (toPay < 1) return FeeOutcome.Nothing;
            if (balance >= toPay) return FeeOutcome.Pay;
            return kickNonPayers && isOwnCitizen ? FeeOutcome.Kick : FeeOutcome.TakePlots;
        }

        /// <summary>
        /// Everyone this city charges today and how much: custom plot taxes and safety flags on the
        /// plots they own, the fee of each plots group that holds land, and the city fee for its own
        /// citizens. Every citizen is on the bill, at zero if nothing applies, since the city fee is
        /// added to it. The mayor pays nothing.
        /// </summary>
        public static Dictionary<PlayerInfo, decimal> BuildFeeBill(City city, List<Plot> cityPlotsList)
        {
            Dictionary<PlayerInfo, decimal> playerSumFee = new();
            // Groups that actually hold land in this city, gathered while the plots are walked
            // anyway - a group is charged once, below, however many plots it turns out to have.
            HashSet<CityPlotsGroup> groupsWithPlots = new HashSet<CityPlotsGroup>();
            foreach(Plot plot in cityPlotsList ?? new List<Plot>())
            {
                // Before the owner branch below, which skips the rest of the loop for a mayor's plot:
                // whose plot it is has no bearing on whether the group holds land.
                if (plot.hasCityPlotsGroup())
                {
                    groupsWithPlots.Add(plot.getPlotGroup());
                }
                if(plot.hasPlotOwner())
                {
                    // Only this city's mayor is exempt; a mayor of another city pays for an embassy here.
                    if(city.isMayor(plot.getPlotOwner()))
                    {
                        continue;
                    }
                    if(plot.hasCutomTax())
                    {
                        if (playerSumFee.TryGetValue(plot.getPlotOwner(), out decimal val))
                        {
                            playerSumFee[plot.getPlotOwner()] += (decimal)plot.getCustomTax();
                        }
                        else
                        {
                            playerSumFee[plot.getPlotOwner()] = (decimal)plot.getCustomTax();
                        }
                    }
                    // Safety flags of an owned plot are the owner's bill, not the treasury's - the
                    // city only pays for its own land, see City.GetSafetyFlagsCost.
                    double safetyFlagsCost = plot.GetSafetyFlagsCost();
                    if (safetyFlagsCost > 0)
                    {
                        if (playerSumFee.TryGetValue(plot.getPlotOwner(), out decimal current))
                        {
                            playerSumFee[plot.getPlotOwner()] = current + (decimal)safetyFlagsCost;
                        }
                        else
                        {
                            playerSumFee[plot.getPlotOwner()] = (decimal)safetyFlagsCost;
                        }
                    }
                    /*else
                    {
                        PlotInfo.dictPlotTypes.TryGetValue(plot.getType(), out PlotInfo plotInfo);
                        if(plotInfo != null)
                        {
                            if (playerSumFee.TryGetValue(plot.getPlotOwner(), out double val))
                            {
                                playerSumFee[plot.getPlotOwner()] += plotInfo.getCost();
                            }
                            else
                            {
                                playerSumFee[plot.getPlotOwner()] = plotInfo.getCost();
                            }
                        }
                    }*/
                }
            }

            // Sub-plot taxes, on the same terms as a bought plot's custom tax.
            foreach (var subPlot in subplots.SubPlotEditor.OfCity(city))
            {
                // A sub-plot is land of its group as much as a plot is.
                if (subPlot.Group != null) groupsWithPlots.Add(subPlot.Group);
                if (subPlot.Owner == null || subPlot.Tax <= 0 || city.isMayor(subPlot.Owner)) continue;
                playerSumFee[subPlot.Owner] = (playerSumFee.TryGetValue(subPlot.Owner, out decimal owed) ? owed : 0)
                    + (decimal)subPlot.Tax;
            }

            // Membership is charged once a day, not once per plot: a group of six used to cost its
            // members six times what the mayor typed, so a fee anyone would call reasonable ruined
            // the very people it was meant to keep. A group holding no land is charged nothing -
            // there is nothing to be a member of yet.
            foreach (CityPlotsGroup group in groupsWithPlots)
            {
                if (!group.HasFee()) continue;
                foreach (PlayerInfo player in group.PlayersList)
                {
                    if (city.isMayor(player)) continue;
                    if (playerSumFee.TryGetValue(player, out decimal val))
                    {
                        playerSumFee[player] = val + (decimal)group.PlotsGroupFee;
                    }
                    else
                    {
                        playerSumFee[player] = (decimal)group.PlotsGroupFee;
                    }
                }
            }
            //So all citizen will pay city's fee
            foreach(PlayerInfo citizen in city.getCityCitizens())
            {
                //mayor shouldn't pay city's fee
                if (citizen.hasCity() && citizen.City.isMayor(citizen))
                {
                    continue;
                }
                if (!playerSumFee.ContainsKey(citizen))
                {
                    playerSumFee[citizen] = 0;
                }
            }
            foreach (PlayerInfo it in playerSumFee.Keys.ToArray())
            {
                if (it.hasCity() && it.City.Equals(city))
                {
                    playerSumFee[it] += (decimal)city.fee;
                }
            }
            return playerSumFee;
        }

        /// <summary>
        /// Removes a citizen who could not pay. Everything /city kick does, plus a log entry the
        /// mayor is shown on their next login: this used to be a bare clearCity, so the evicted
        /// player's client still showed the city, their cached rights outlived the membership, mods
        /// listening for CitizenLeft never heard of it, and an offline mayor had no way to find out.
        /// </summary>
        private static void EvictForUnpaidFee(City city, PlayerInfo it, decimal owed)
        {
            MessageHandler.sendDebugMsg("processCityFee:kicking player " + it.GetPartName());
            string owedText = owed.ToString(System.Globalization.CultureInfo.InvariantCulture);
            city.AddLogEntry(citylog.EnumCityLogEvent.CitizenEvictedUnpaid, it.GetPartName(), owedText);
            city.FireCitizenLeft(it, EnumCityLeaveReason.Kicked);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:citizen_didnt_pay_kicked", it.getPartNameReplaceUnder()));
            MessageHandler.sendMsgToPlayerInfo(it, Lang.Get("claims:you_were_evicted_unpaid", city.getPartNameReplaceUnder(), owedText));
            it.clearCity(true);
            var tree = new Vintagestory.API.Datastructures.TreeAttribute();
            tree.SetString("cityname", city.GetPartName());
            claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
            it.PlayerCache.Reset();
            UsefullPacketsSend.SendPlayerRelatedInfoOnKickFromCity(it);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_MEMBERS,
                gui.playerGui.structures.EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG);
        }

        public static void processCityFee(City city)
        {
            citiesPlots.TryGetValue(city, out List<Plot> cityPlotsList);
            Dictionary<PlayerInfo, decimal> playerSumFee = BuildFeeBill(city, cityPlotsList);
            foreach (KeyValuePair<PlayerInfo, decimal> bill in playerSumFee)
            {
                // One person whose eviction throws must not leave everyone after them unbilled.
                Isolated("fee of " + bill.Key.GetPartName() + " in " + city.GetPartName(),
                    () => ChargeFee(city, cityPlotsList, bill.Key, bill.Value));
            }
        }

        private static void ChargeFee(City city, List<Plot> cityPlotsList, PlayerInfo it, decimal toPay)
        {
            // The balance is only asked for a bill worth paying.
            if (toPay < 1)
            {
                return;
            }
            FeeOutcome outcome = DecideFee(toPay, claims.economyProvider.GetBalance(it.MoneyAccountName),
                it.hasCity() && it.City.Equals(city), claims.config.DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE);
            if (outcome != FeeOutcome.Pay)
            {
                //WE DELETE PLAYER FROM EVERY PLOTGROUP IN THIS CITY
                List<CityPlotsGroup> droppedFrom = new List<CityPlotsGroup>();
                foreach(CityPlotsGroup cpg in city.getCityPlotsGroups())
                {
                    foreach(PlayerInfo playerInfoHere in cpg.PlayersList.ToArray())
                    {
                        if (playerInfoHere.Equals(it))
                        {
                            cpg.PlayersList.Remove(playerInfoHere);
                            cityplotsgroups.PlotsGroupFeeHelper.OnMemberLeft(cpg, it);
                            cpg.saveToDatabase();
                            droppedFrom.Add(cpg);
                        }
                    }
                }
                // Being thrown out of every group at once used to happen in complete silence:
                // the player found out by walking onto ground they could no longer build on.
                if (droppedFrom.Count > 0)
                {
                    MessageHandler.sendMsgToPlayerInfo(it, Lang.Get("claims:plotsgroup_left_no_money",
                        StringFunctions.concatGroupsNames(droppedFrom, ','),
                        city.getPartNameReplaceUnder()));
                    // Their rights came from the groups and are cached per player and per
                    // plot; without this they keep building on that land until something
                    // else happens to evict the cache.
                    it.PlayerCache?.Reset();
                    foreach (CityPlotsGroup cpg in droppedFrom)
                    {
                        cityplotsgroups.PlotsGroupFeeHelper.SendGroupUpdate(cpg);
                    }
                    foreach (Plot groupPlot in cityPlotsList ?? new List<Plot>())
                    {
                        if (groupPlot.hasPlotGroup() && droppedFrom.Contains(groupPlot.getPlotGroup()))
                        {
                            claims.serverPlayerMovementListener.markPlotToWasReUpdated(groupPlot.getPos());
                        }
                    }
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(it.Guid,
                        gui.playerGui.structures.EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
                }
                // Only this city's own citizens can be kicked; anyone else (an embassy owner)
                // loses the plots here instead - kicking used to skip them and leave the plots.
                if (outcome == FeeOutcome.Kick)
                {
                    EvictForUnpaidFee(city, it, toPay);
                }
                else
                {
                    //HE DIDN'T PAY BUT WE ARE NOT SAVAGIES AFTER ALL, LET HIM BE IN THE CITY
                    foreach (Plot plot in it.PlayerPlots.ToArray())
                    {
                        //BUT NOT AT OTHER CITIES, THAT WILL BE TAKEN CARE IN DIFFERENT ITERATION FOR ANOTHER CITY
                        if (!plot.hasCity() || !plot.getCity().Equals(city))
                        {
                            continue;
                        }
                        plot.resetOwner();
                        it.PlayerPlots.Remove(plot); // R27: not left in the former owner's list
                        plot.Price = -1;
                        // Through setNewType, not by assigning Type: OnDeactivated is what
                        // takes a temple off the city's respawn points and a summon point off
                        // its list. Assigning the type skipped that, so a plot taken back for
                        // non-payment went on serving as the temple it no longer was.
                        plot.setNewType(new TextCommandResult(), "default", null, true);
                        plot.saveToDatabase();
                    }
                    subplots.SubPlotLifecycle.TakeBackFrom(it, city);
                    it.saveToDatabase();
                }
            }
            else
            {
                bool paymentSuccessfull = claims.economyProvider.Transfer(it.MoneyAccountName, city.MoneyAccountName, toPay) == MoneyOperationResult.Success;
                if (paymentSuccessfull)
                {
                    MessageHandler.sendMsgToPlayerInfo(it, Lang.Get("claims:you_paid_fee_to_city", toPay.ToString(), city.getPartNameReplaceUnder()));
                }
                else
                {
                    //todo workaround for failed state
                    MessageHandler.sendMsgToPlayerInfo(it, Lang.Get("claims:economy_money_transaction_error"));
                }
            }
        }
        public static void ProcessAlliancesFee()
        {
            foreach (Alliance alliance in claims.dataStorage.getAllAlliances())
            {
                if (alliance.AllianceFee > 0)
                {
                    foreach (City city in alliance.Cities.ToArray())
                    {
                        Isolated("alliance fee of " + city.GetPartName(), () => ChargeAllianceFee(alliance, city));
                    }
                }
            }
        }
        private static void ChargeAllianceFee(Alliance alliance, City city)
        {
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < alliance.AllianceFee)
            {
                alliance.Cities.Remove(city);
                city.Alliance = null;
                city.saveToDatabase();
                MessageHandler.SendMsgInAlliance(alliance, Lang.Get("claims:city_kicked_from_alliance_no_fee", city.getPartNameReplaceUnder()));
            }
            else
            {
                if (claims.economyProvider.Transfer(city.MoneyAccountName, alliance.MoneyAccountName, (decimal)alliance.AllianceFee) != MoneyOperationResult.Success)
                    claims.sapi.Logger.Warning("[claims] Alliance fee transfer failed: {0} -> {1}, amount {2}", city.MoneyAccountName, alliance.MoneyAccountName, alliance.AllianceFee);
            }
        }
        public static void ProcessVassalTribute()
        {
            double tribute = claims.config.WAR_VASSAL_TRIBUTE;
            long durationSec = (long)claims.config.WAR_VASSAL_DURATION_DAYS * 86400;
            long now = TimeFunctions.getEpochSeconds();

            // Vassalage is imposed on a whole losing side, so a subdued alliance would otherwise pay
            // the full tribute once per member city. Collect the still-valid vassals first, then
            // split the tribute per side (overlord + the vassal's alliance).
            List<City> payingVassals = new List<City>();
            foreach (City vassal in claims.dataStorage.getCitiesList().ToArray())
            {
                if (!vassal.IsVassal()) continue;
                City overlord = vassal.GetOverlord();
                if (overlord == null)
                {
                    PeaceTermsHelper.ReleaseVassal(vassal);
                    continue;
                }
                // Auto-release after the configured duration.
                if (durationSec > 0 && now - vassal.VassalSince > durationSec)
                {
                    PeaceTermsHelper.ReleaseVassal(vassal);
                    MessageHandler.sendMsgInCity(vassal, Lang.Get("claims:vassalage_ended"));
                    MessageHandler.sendMsgInCity(overlord, Lang.Get("claims:vassal_freed", vassal.getPartNameReplaceUnder()));
                    continue;
                }
                payingVassals.Add(vassal);
            }

            if (tribute <= 0) return;

            // Cities of one alliance under the same overlord form a single paying side; a vassal
            // without an alliance is a side of its own.
            Dictionary<string, int> sideSizes = new Dictionary<string, int>();
            foreach (City vassal in payingVassals)
            {
                string sideKey = TributeSideKey(vassal);
                sideSizes[sideKey] = sideSizes.TryGetValue(sideKey, out int count) ? count + 1 : 1;
            }

            foreach (City vassal in payingVassals)
            {
                City overlord = vassal.GetOverlord();
                if (overlord == null) continue;

                int sideSize = sideSizes[TributeSideKey(vassal)];
                decimal share = (decimal)tribute / (claims.config.WAR_VASSAL_TRIBUTE_PER_CITY ? 1 : sideSize);
                if (share <= 0) continue;

                Isolated("tribute of " + vassal.GetPartName(), () =>
                {
                    if (claims.economyProvider.Transfer(vassal.MoneyAccountName, overlord.MoneyAccountName, share) != MoneyOperationResult.Success)
                        claims.sapi.Logger.Warning("[claims] Vassal tribute transfer failed: {0} -> {1}", vassal.MoneyAccountName, overlord.MoneyAccountName);
                });
            }
        }
        private static string TributeSideKey(City vassal)
        {
            return vassal.OverlordGuid + "|" + (vassal.HasAlliance() ? vassal.Alliance.Guid : vassal.Guid);
        }
        public static void ProcessAlliancesCare()
        {
            foreach (Alliance alliance in claims.dataStorage.getAllAlliances())
            {
                Isolated("upkeep of " + alliance.GetPartName(), () => ProcessAllianceCare(alliance));
            }

            foreach (Alliance alliance in toDeleteAlliancies)
            {
                Isolated("demolition of " + alliance.GetPartName(), () => PartDemolition.DemolishAlliance(alliance));
            }
            toDeleteAlliancies.Clear();
        }
    }
}
