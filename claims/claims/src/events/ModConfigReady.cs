using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.commands;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using claims.src.rights;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace claims.src.events
{
    public class ModConfigReady
    {
        public static Dictionary<string, long> startWarCallbacks = new Dictionary<string, long>();
        // conflict guid -> NextBattleDateStart we already fired the pre-battle warning for (once per window)
        public static Dictionary<string, DateTime> battleWarned = new Dictionary<string, DateTime>();
        public static void onModsAndConfigReady()
        {
            claims.loadDatabase();
            claims.getModInstance().getDatabaseHandler().loadEveryThing();
            MarkBorderClaimPlots();
            Settings.loadAll();
            foreach (var plot in claims.dataStorage.getClaimedPlots().Values)
            {
                claims.dataStorage.addPlotToZoneSet(plot);
            }
            var world = claims.dataStorage.getWorldInfo();
            if (world == null)
            {
                world = new WorldInfo(claims.sapi.World.Seed.ToString(), Guid.NewGuid().ToString());
                claims.dataStorage.setWorldInfo(world);
                world.saveToDatabase(update: false);
            }
            {
                var parsers = claims.sapi.ChatCommands.Parsers;

                claims.sapi.ChatCommands.Get("city")
                                            .BeginSub("balance")
                                                .HandleWith(commands.MoneyCommands.OnCityBalance)
                                                .WithDesc("Show city balance.")
                                            .EndSub()
                                            .BeginSub("deposit")
                                                .HandleWith(commands.MoneyCommands.OnCityDeposit)
                                                .WithDesc("Deposit money to city account.")
                                            .EndSub()
                                            .BeginSub("withdraw")
                                                .WithPreCondition((TextCommandCallingArgs args) => {
                                                    if (args.Caller.Player is IServerPlayer player)
                                                    {
                                                        if (BaseCommand.CheckForPlayerPermissions(player, new EnumPlayerPermissions[] { EnumPlayerPermissions.CITY_WITHDRAW_MONEY }))
                                                        {
                                                            return TextCommandResult.Success();
                                                        }
                                                        else
                                                        {
                                                            return TextCommandResult.Error(Lang.Get("claims:you_dont_have_right_for_that_command"));
                                                        }

                                                    }
                                                    return TextCommandResult.Error("");
                                                })
                                                .HandleWith(commands.MoneyCommands.OnCityWithdraw)
                                                .WithDesc("Withdraw money from city account.")
                                                .WithArgs(parsers.Int("amount"))
                                            .EndSub()

                                            ;
                claims.sapi.ChatCommands.Get("alliance")
                                            .BeginSub("balance")
                                                .HandleWith(commands.MoneyCommands.OnAllianceBalance)
                                                .WithDesc("Show alliance balance.")
                                            .EndSub()
                                            .BeginSub("deposit")
                                                .HandleWith(commands.MoneyCommands.OnAllianceDeposit)
                                                .WithDesc("Deposit money to alliance account.")
                                            .EndSub()
                                            .BeginSub("withdraw")
                                             .WithPreCondition((TextCommandCallingArgs args) => {
                                                 if (args.Caller.Player is IServerPlayer player)
                                                 {
                                                     if (BaseCommand.CheckForPlayerPermissions(player, new EnumPlayerPermissions[] { EnumPlayerPermissions.ALLIANCE_WITHDRAW_MONEY }))
                                                     {
                                                         return TextCommandResult.Success();
                                                     }
                                                     else
                                                     {
                                                         return TextCommandResult.Error(Lang.Get("claims:you_dont_have_right_for_that_command"));
                                                     }

                                                 }
                                                 return TextCommandResult.Error("");
                                             })
                                                .HandleWith(commands.MoneyCommands.OnAllianceWithdraw)
                                                .WithDesc("Withdraw money from alliance account.")
                                                .WithArgs(parsers.Int("amount"))
                                            .EndSub()

                                            ;
            }
            Dictionary<string, ClientCityInfoCellElement> CityStatsCashe =
                ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientCityInfoCellElement>>(claims.sapi,
                "claims:cityinfocache", () => new Dictionary<string, ClientCityInfoCellElement>());
            foreach (var it in claims.dataStorage.getCitiesList())
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
            Dictionary<string, ClientAllianceInfoCellElement> AllianceStatsCashe =
                ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientAllianceInfoCellElement>>(claims.sapi,
                "claims:allianceinfocache", () => new Dictionary<string, ClientAllianceInfoCellElement>());
            foreach (var it in claims.dataStorage.getAllAlliances())
            {
                if (AllianceStatsCashe.TryGetValue(it.Guid, out var stat))
                {
                    stat.UpdateFrom(it);
                }
                else
                {
                    AllianceStatsCashe.Add(it.Guid, ClientAllianceInfoCellElement.FromAlliance(it));
                }
            }
            ReculculateNextBattleTimes();
            claims.sapi.Event.Timer(UsefullPacketsSend.CheckCitisUpdatedAndSend, claims.config.SEND_CITY_UPDATES_EVERY_N_SECONDS);
            claims.sapi.Event.Timer(CheckForWarToStart, claims.config.CHECK_FOR_WAR_TO_START_EVERY_N_SECONDS);
            claims.sapi.Event.RegisterCallback((float dt) =>
            {
                CheckForWarToStart();
            }, 10 * 1000);
            claims.sapi.Event.Timer(CheckWarToEnd, claims.config.CHECK_FOR_WAR_TO_START_EVERY_N_SECONDS);
        }        
        public static void MarkBorderClaimPlots()
        {
            PlotPosition posTmp = new PlotPosition(0, 0);
            foreach (var it in claims.dataStorage.getClaimedPlots())
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if ((Math.Abs(i) + Math.Abs(j)) != 1) continue;
                        posTmp.X = it.Value.getPos().X + i;
                        posTmp.Z = it.Value.getPos().Y + j;
                        if (!claims.dataStorage.GetPlot(posTmp, out var nearPlot))
                        {
                            it.Value.BorderPlot = true;
                            goto notBorderplot;

                        }
                        else
                        {
                            if (!nearPlot.getCity().Equals(it.Value.getCity()))
                            {
                                it.Value.BorderPlot = true;
                                goto notBorderplot;
                            }
                        }
                    }
                    it.Value.BorderPlot = false;
                }
            notBorderplot:
                ;
            }
        }
        public static void CheckForWarToStart()
        {
            foreach(var conflict in claims.dataStorage.conflicts)
            {
                if (conflict.State == ConflictState.ACTIVE && conflict.WarRanges.Count > 0)
                {
                    //if second to conflict date start is lower than configured or negative
                    var secondsToStart = conflict.NextBattleDateStart - DateTime.Now;
                    if(secondsToStart.TotalSeconds < claims.config.CHECK_FOR_WAR_TO_START_CALLBACK_EVERY_N_SECONDS && conflict.NextBattleDateEnd > DateTime.Now)
                    {
                        if(!claims.dataStorage.WarsTimes.ContainsKey(conflict.Guid) && !startWarCallbacks.TryGetValue(conflict.Guid, out var _))
                        {
                            long savedLong = claims.sapi.Event.RegisterCallback((float dt) =>
                            {
                                if (!claims.dataStorage.WarsTimes.ContainsKey(conflict.Guid))
                                {
                                    claims.dataStorage.WarsTimes.Add(conflict.Guid, new WarTime(conflict.Guid, conflict.NextBattleDateStart, conflict.NextBattleDateEnd));
                                    conflict.ActiveWarTime = true;
                                    if(startWarCallbacks.TryGetValue(conflict.Guid, out var savedCallback))
                                    {
                                        startWarCallbacks.Remove(conflict.Guid);
                                    }
                                    RightsHandler.ClearPlayerCachesAndUpdatePlotSavedRightsForClients(conflict);
                                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First, new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_START);
                                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second, new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_START);
                                    MessageHandler.SendMsgInAlliance(conflict.First, Lang.Get("claims:battle_started_with", conflict.Second.GetPartName()));
                                    MessageHandler.SendMsgInAlliance(conflict.Second, Lang.Get("claims:battle_started_with", conflict.First.GetPartName()));
                                    MessageHandler.SendDiscoveryToAlliance(conflict.First, "ingamediscovery-battle-start", Lang.Get("claims:ingamediscovery-battle-start", conflict.Second.GetPartName()), new object[] { });
                                    MessageHandler.SendDiscoveryToAlliance(conflict.Second, "ingamediscovery-battle-start", Lang.Get("claims:ingamediscovery-battle-start", conflict.First.GetPartName()), new object[] { });
                                }
                            }, (int)(secondsToStart.TotalSeconds < 0 ? 2 : secondsToStart.TotalSeconds) * 1000);
                            startWarCallbacks[conflict.Guid] = savedLong;
                        }

                        // Pre-battle warning: alert both sides WAR_BATTLE_WARN_MINUTES before the window opens,
                        // once per window (keyed on NextBattleDateStart so a recalculated window warns again).
                        int warnMinutes = claims.config.WAR_BATTLE_WARN_MINUTES;
                        if (warnMinutes > 0 && conflict.NextBattleDateStart > DateTime.Now
                            && (!battleWarned.TryGetValue(conflict.Guid, out var warnedFor) || warnedFor != conflict.NextBattleDateStart))
                        {
                            battleWarned[conflict.Guid] = conflict.NextBattleDateStart;
                            Conflict warnConflict = conflict;
                            DateTime warnStart = conflict.NextBattleDateStart;
                            double secondsToWarn = secondsToStart.TotalSeconds - warnMinutes * 60;
                            claims.sapi.Event.RegisterCallback((float dt) =>
                            {
                                // Skip if the window was recalculated or the battle already started.
                                if (warnConflict.NextBattleDateStart != warnStart || warnConflict.ActiveWarTime) return;
                                int minsLeft = Math.Max(1, (int)Math.Round((warnStart - DateTime.Now).TotalMinutes));
                                MessageHandler.SendMsgInAlliance(warnConflict.First, Lang.Get("claims:battle_incoming", warnConflict.Second.GetPartName(), minsLeft));
                                MessageHandler.SendMsgInAlliance(warnConflict.Second, Lang.Get("claims:battle_incoming", warnConflict.First.GetPartName(), minsLeft));
                            }, (int)(secondsToWarn < 0 ? 2 : secondsToWarn) * 1000);
                        }
                    }
                }
            }
        }
        public static Dictionary<string, long> endWarCallbacks = new Dictionary<string, long>();
        public static void CheckWarToEnd()
        {
            foreach(var wartime in claims.dataStorage.WarsTimes)
            {
                if (!endWarCallbacks.ContainsKey(wartime.Value.ConflictGuid))
                {
                    int delayMs = (int)(wartime.Value.BattleDateEnd - DateTime.Now).TotalMilliseconds;
                    if (delayMs < 2000) delayMs = 2000;

                    var capturedWartime = wartime.Value;
                    long callbackId = claims.sapi.Event.RegisterCallback((float dt) =>
                    {
                        endWarCallbacks.Remove(capturedWartime.ConflictGuid);
                        if (claims.dataStorage.WarsTimes.ContainsKey(capturedWartime.ConflictGuid))
                        {
                            claims.dataStorage.WarsTimes.Remove(capturedWartime.ConflictGuid);
                        }
                        if (claims.dataStorage.TryGetConflict(capturedWartime.ConflictGuid, out var conflict))
                        {
                            conflict.ActiveWarTime = false;
                            // Update last battle dates so MinimumDaysBetweenBattles is respected
                            conflict.LastBattleDateStart = capturedWartime.BattleDateStart;
                            conflict.LastBattleDateEnd   = capturedWartime.BattleDateEnd;
                            conflict.CalculateNextBattleDate();
                            conflict.saveToDatabase();
                            RightsHandler.ClearPlayerCachesAndUpdatePlotSavedRightsForClients(conflict);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First, new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_END);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second, new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_END);
                            MessageHandler.SendMsgInAlliance(conflict.First, Lang.Get("claims:battle_ended_with", conflict.Second.GetPartName()));
                            MessageHandler.SendMsgInAlliance(conflict.Second, Lang.Get("claims:battle_ended_with", conflict.First.GetPartName()));
                            MessageHandler.SendDiscoveryToAlliance(conflict.First, "ingamediscovery-battle-end", Lang.Get("claims:ingamediscovery-battle-end", conflict.Second.GetPartName()), new object[] { });
                            MessageHandler.SendDiscoveryToAlliance(conflict.Second, "ingamediscovery-battle-end", Lang.Get("claims:ingamediscovery-battle-end", conflict.First.GetPartName()), new object[] { });

                            // A camp is a battle-time forward base: without this it would survive
                            // until the whole war ends, leaving an enemy plot sitting in peacetime.
                            if (claims.config.WAR_CAMP_REMOVE_AFTER_BATTLE)
                            {
                                foreach (City campCity in conflict.First.GetCities())
                                    PartDemolition.DemolishCampsForConflict(campCity, conflict.Guid);
                                foreach (City campCity in conflict.Second.GetCities())
                                    PartDemolition.DemolishCampsForConflict(campCity, conflict.Guid);
                            }
                        }
                        else
                        {
                            claims.sapi.World.Logger.Warning("[claims] CheckWarToEnd: conflict {0} not found, likely already demolished.", capturedWartime.ConflictGuid);
                        }
                    }, delayMs);
                    endWarCallbacks[wartime.Value.ConflictGuid] = callbackId;
                }
            }
        }
        public static void ReculculateNextBattleTimes()
        {
            foreach (var conflict in claims.dataStorage.conflicts)
            {
                if (conflict.State == ConflictState.ACTIVE && conflict.WarRanges.Count > 0)
                {
                    if(conflict.NextBattleDateEnd < DateTime.Now)
                    {
                        conflict.CalculateNextBattleDate();
                    }
                }
            }
        }
    }
}
