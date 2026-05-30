using claims.src.auxialiry;
using claims.src.economy;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
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
                    new Thread(new ThreadStart(() =>
                    {
                        new DayTimer().Run(true);
                    })).Start();
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
            string capturedGuid = city.Guid;
            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < sumToPay + (decimal)city.DebtBalance)
            {
                city.DebtBalance += (double)sumToPay;

                if (claims.config.DELETE_CITY_IF_DOESN_PAY_FEE)
                {
                    if (city.DebtBalance > claims.config.CITY_MAX_DEBT)
                    {
                        toDeleteCities.Add((city, Lang.Get("claims:city_delete_reason_debt_is_too_high", city.DebtBalance)));
                    }
                }
                claims.sapi.Event.RegisterCallback(_ =>
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(capturedGuid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_DEBT), 0);
            }
            else
            {
                if (sumToPay < 1)
                {
                    return;
                }
                if(claims.economyProvider.Withdraw(city.MoneyAccountName, Math.Floor(sumToPay) + (decimal)city.DebtBalance) == MoneyOperationResult.Success )
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

            if (alliance.Neutral)
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
                processCityFee(city);
            }           
        }
        public static void processCityFee(City city)
        {
            citiesPlots.TryGetValue(city, out List<Plot> cityPlotsList);
            foreach(Plot plot in cityPlotsList)
            {          
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
                else if (plot.hasCityPlotsGroup())
                {
                    if (plot.getPlotGroup().HasFee())
                    {
                        foreach(PlayerInfo player in plot.getPlotGroup().PlayersList)
                        {
                            if (plot.getCity().isMayor(player))
                            {
                                continue;
                            }
                            if (playerSumFee.TryGetValue(player, out decimal val))
                            {
                                playerSumFee[player] += (decimal)plot.getPlotGroup().PlotsGroupFee;
                            }
                            else
                            {
                                playerSumFee[player] = (decimal)plot.getPlotGroup().PlotsGroupFee;
                            }
                        }                       
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
                        foreach(CityPlotsGroup cpg in city.getCityPlotsGroups())
                        {
                            foreach(PlayerInfo playerInfoHere in cpg.PlayersList.ToArray())
                            {
                                if (playerInfoHere.Equals(it))
                                {
                                    cpg.PlayersList.Remove(playerInfoHere);
                                }
                            }
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
                                if (!plot.getCity().Equals(city))
                                {
                                    continue;
                                }
                                plot.resetOwner();
                                plot.Price = -1;
                                plot.Type = PlotType.DEFAULT;
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
