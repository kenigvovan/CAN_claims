using claims.src.auxialiry;
using claims.src.economy;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.war;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.timers
{
    public class DayTimer
    {
        public static Dictionary<City, List<Plot>> citiesPlots = new();
        public static Dictionary<PlayerInfo, decimal> playerSumFee = new();
        public static List<(City, string)> toDeleteCities = new();
        static List<Alliance> toDeleteAlliancies = new();

        public void Run(bool scheduleNewDayAfter)
        {
            MessageHandler.sendDebugMsg("[claims] DayTimer::start collecting");
            // Clear static state at the top — if a previous run threw, the dicts stay
            // populated and the next Add() throws ArgumentException on duplicate key
            citiesPlots.Clear();
            playerSumFee.Clear();
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
            processCitiesFee();
            ProcessAlliancesFee();
            ProcessVassalTribute();

            //All cities
            processCitiesCare();

            citiesPlots.Clear();

            ProcessAlliancesCare();

            MessageHandler.sendGlobalMsg(Lang.Get("claims:new_day_log_msg"));
            MessageHandler.sendDebugMsg("[claims] DayTimer::new day here");
            if (scheduleNewDayAfter)
            {
                claims.sapi.Event.RegisterCallback((dt =>
                {
                    new DayTimer().Run(true);
                }), (int)TimeFunctions.getSecondsBeforeNextDayStart() * 1000);
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
                processCityCare(city);
            }
            //DELETE CITIES WHICH WERE MARKED
            foreach (var city in toDeleteCities)
            {
                PartDemolition.demolishCity(city.Item1, city.Item2);
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
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < sumToPay + (decimal)city.DebtBalance)
            {
                city.DebtBalance += (double)sumToPay;

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
            else
            {
                // The whole bill decides whether there is anything to collect, not the daily part of
                // it alone: a city whose daily charge rounds to nothing still owes what it failed to
                // pay earlier, and returning here left that debt standing forever however full the
                // treasury was - the branch is only reached when the money for it is there.
                decimal due = Math.Floor(sumToPay) + (decimal)city.DebtBalance;
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
            }
            claims.sapi.Logger.Debug(string.Format("[claims] processCityCare, withdrew {0} from city {1} account. Balance after is {2}, debt is {3}.",
                sumToPay, city.GetPartName(), claims.economyProvider.GetBalance(city.MoneyAccountName), city.DebtBalance));
            city.saveToDatabase();
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
                processCityFee(city);
            }
        }
        public static void processCityFee(City city)
        {
            citiesPlots.TryGetValue(city, out List<Plot> cityPlotsList);
            // Groups that actually hold land in this city, gathered while the plots are walked
            // anyway - a group is charged once, below, however many plots it turns out to have.
            HashSet<CityPlotsGroup> groupsWithPlots = new HashSet<CityPlotsGroup>();
            foreach(Plot plot in cityPlotsList)
            {
                // Before the owner branch below, which skips the rest of the loop for a mayor's plot:
                // whose plot it is has no bearing on whether the group holds land.
                if (plot.hasCityPlotsGroup())
                {
                    groupsWithPlots.Add(plot.getPlotGroup());
                }
                if(plot.hasPlotOwner())
                {
                    if(plot.getPlotOwner().hasCity() && plot.getPlotOwner().City.isMayor(plot.getPlotOwner()))
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
            foreach (PlayerInfo it in playerSumFee.Keys)
            {
                if (playerSumFee.TryGetValue(it, out decimal toPay))
                {
                    if(it.hasCity() && it.City.Equals(city))
                    {
                        toPay += city.fee;
                    }
                    if (toPay < 1)
                    {
                        continue;
                    }
                    if (claims.economyProvider.GetBalance(it.MoneyAccountName) < toPay)
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
                        if (claims.config.DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE)
                        {
                            //IF HE HAS EMBASSY WE DON'T WANT TO KICK HIM FROM HIS CITY OR IF HE DOESN'T HAVE ONE
                            if (it.hasCity() && it.City.Equals(city))
                            {
                                MessageHandler.sendDebugMsg("processCityFee:kicking player " + it.GetPartName());
                                MessageHandler.sendMsgInCity(city, Lang.Get("claims:citizen_didnt_pay_kicked", city.getPartNameReplaceUnder()));
                                it.clearCity(true);
                            }
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
                                plot.Price = -1;
                                // Through setNewType, not by assigning Type: OnDeactivated is what
                                // takes a temple off the city's respawn points and a summon point off
                                // its list. Assigning the type skipped that, so a plot taken back for
                                // non-payment went on serving as the temple it no longer was.
                                plot.setNewType(new TextCommandResult(), "default", null, true);
                                plot.saveToDatabase();
                            }
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
            }

            playerSumFee.Clear();
        }
        public static void ProcessAlliancesFee()
        {
            foreach (Alliance alliance in claims.dataStorage.getAllAlliances())
            {
                if (alliance.AllianceFee > 0)
                {
                    foreach (City city in alliance.Cities.ToArray())
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
                }
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

                if (claims.economyProvider.Transfer(vassal.MoneyAccountName, overlord.MoneyAccountName, share) != MoneyOperationResult.Success)
                    claims.sapi.Logger.Warning("[claims] Vassal tribute transfer failed: {0} -> {1}", vassal.MoneyAccountName, overlord.MoneyAccountName);
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
                ProcessAllianceCare(alliance);
            }

            foreach (Alliance alliance in toDeleteAlliancies)
            {
                PartDemolition.DemolishAlliance(alliance);
            }
            toDeleteAlliancies.Clear();
        }
    }
}
