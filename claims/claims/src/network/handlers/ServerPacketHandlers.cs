using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Server;

namespace claims.src.network.handlers
{
    public static class ServerPacketHandlers
    {
        public static void RegisterHandlers()
        {
            claims.serverChannel.SetMessageHandler<SavedPlotsPacket>((player, packet) =>
            {
                if (packet.type == PacketsContentEnum.CLIENT_INFORM_ZONES_TIMESTAMPS)
                {
                    claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                    if (playerInfo == null)
                    {
                        return;
                    }
                    List<Tuple<Vec2i, long>> zonesTimestamps = JsonConvert.DeserializeObject<List<Tuple<Vec2i, long>>>(packet.data);

                    // Anti-spoof: only allow zones close to player's actual position
                    Vec2i playerServerPos = new Vec2i((int)player.Entity.Pos.X / 512, (int)player.Entity.Pos.Z / 512);
                    List<Vec2i> requestedZones = new List<Vec2i>();
                    foreach (var zoneItem in zonesTimestamps)
                    {
                        if (zoneItem.Item1.X > playerServerPos.X + 3 || zoneItem.Item1.X < playerServerPos.X - 3 ||
                            zoneItem.Item1.Y > playerServerPos.Y + 3 || zoneItem.Item1.Y < playerServerPos.Y - 3)
                        {
                            continue;
                        }
                        requestedZones.Add(zoneItem.Item1);
                    }

                    claims.serverPlayerMovementListener.SyncSubscriptionsAndSendSnapshot(player, playerInfo, requestedZones);

                    if (ServerMain.FrameProfiler.Enabled)
                    {
                        ServerMain.FrameProfiler.Mark("can-claims-packet-city-zone");
                    }
                }
                else if(packet.type == PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST)
                {
                    var currentPos = player.Entity.Pos;
                    if(claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(currentPos), out Plot plot))
                    {
                        CurrentPlotInfo cpi = new CurrentPlotInfo(plot.GetPartName(), plot.getPlotOwner()?.GetPartName() ?? "",
                            plot.Type, plot.getCustomTax(), plot.Price, plot.getPermsHandler(), plot.extraBought, plot.getPos());
                        string serializedZones = JsonConvert.SerializeObject(cpi);
                        
                        claims.serverChannel.SendPacket(new SavedPlotsPacket()
                        {
                            type = PacketsContentEnum.CURRENT_PLOT_INFO,
                            data = serializedZones

                        }, player);
                        if (ServerMain.FrameProfiler.Enabled)
                        {
                            ServerMain.FrameProfiler.Mark("can-claims-packet-current-plot-info");
                        }
                    }                                      
                }
                else if (packet.type == PacketsContentEnum.CITY_CITIZENS_RANKS_REQUEST)
                {

                    //get player
                    //city
                    //skip if not mayor
                    //send dict with ranks
                    //add handler on client
                    var currentPos = player.Entity.Pos;
                    if (claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(currentPos), out Plot plot))
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
                }
            });
            claims.serverChannel.SetMessageHandler<PlayerGuiRelatedInfoPacket>((player, packet) =>
            {
                claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    return;
                }
                IConflictParty ourParty;
                if (playerInfo.HasAlliance())
                {
                    if (!playerInfo.Alliance.IsLeader(playerInfo))
                        return;
                    ourParty = playerInfo.Alliance;
                }
                else if (playerInfo.hasCity())
                {
                    if (!playerInfo.City.isMayor(playerInfo))
                        return;
                    ourParty = playerInfo.City;
                }
                else
                {
                    return;
                }

                Dictionary<EnumPlayerRelatedInfo, string> collector = packet.playerGuiRelatedInfoDictionary;
                if(!collector.TryGetValue(EnumPlayerRelatedInfo.CLIENT_CONFLICT_SUGGESTED_WARRANGE, out var clientConflictString))
                {
                    return;
                }
                ClientConflictCellElement ccce = JsonConvert.DeserializeObject<ClientConflictCellElement>(clientConflictString);
                if(ccce == null)
                {
                    return;
                }

                if (!ConflictHandler.TryGetConflictByGuid(ccce.Guid, out var conflict))
                {
                    return;
                }

                bool getFirst = true;
                if(conflict.First.Equals(ourParty))
                {
                    conflict.FirstWarRanges = ccce.FirstWarRanges;
                }
                else
                {
                    conflict.SecondWarRanges = ccce.SecondWarRanges;
                    getFirst = false;
                }

                if (claims.config.NEED_AGREE_FOR_WAR_RANGES)
                {
                    var commonRanges = FindCommonRanges(conflict.FirstWarRanges, conflict.SecondWarRanges);
                    if(commonRanges.Count > 0)
                    {
                        conflict.WarRanges = commonRanges;
                        ccce.WarRanges = commonRanges;
                        conflict.FirstWarRanges.Clear();
                        conflict.SecondWarRanges.Clear();
                        conflict.CalculateNextBattleDate();
                        conflict.State = ConflictState.ACTIVE;
                    }
                }
                else
                {
                    //remove all ranges for this alliance
                    string usedAllianceGuid = getFirst ? conflict.First.Guid : conflict.Second.Guid;
                    //if()
                    foreach(var it in conflict.WarRanges.ToArray())
                    {
                        if(it.SuggestedAllianceGuid.Equals(usedAllianceGuid))
                        {
                            conflict.WarRanges.Remove(it);
                        }
                    }

                    //allow only n war ranges for alliance
                    List<SelectedWarRange> usedAllianceRanges = getFirst ? ccce.FirstWarRanges : ccce.SecondWarRanges;
                    while(usedAllianceRanges.Count() > claims.config.WARRANGE_PER_ALLIANCE && usedAllianceRanges.Count() > 0)
                    {
                        usedAllianceRanges.Remove(usedAllianceRanges.Last());
                    }
                    
                    foreach (var it in usedAllianceRanges)
                    {
                        conflict.WarRanges.Add(it);
                    }
     
                    conflict.CalculateNextBattleDate();
                    conflict.State = ConflictState.ACTIVE;
                }
                ModConfigReady.CheckForWarToStart();
                conflict.saveToDatabase();
                ccce.FirstWarRanges = conflict.FirstWarRanges;
                ccce.SecondWarRanges = conflict.SecondWarRanges;
                ccce.WarRanges = conflict.WarRanges;
                ccce.NextBattleDateEnd = conflict.NextBattleDateEnd;
                ccce.NextBattleDateStart = conflict.NextBattleDateStart;
                ccce.State = conflict.State;
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First, new Dictionary<string, object> { { "value", ccce } } , EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WARRANGES_UPDATED);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second, new Dictionary<string, object> { { "value", ccce } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WARRANGES_UPDATED);
            });
        }
        public static List<SelectedWarRange> FindCommonRanges(List<SelectedWarRange> first, List<SelectedWarRange> second)
        {
            List<SelectedWarRange> common = new List<SelectedWarRange>();
            foreach (var firstRange in first)
            {
                int startFirst = GetMinutes(firstRange.StartDay, firstRange.StartTime);
                int endFirst = GetMinutes(firstRange.EndDay, firstRange.EndTime);
                foreach (var secondRange in second)
                {
                    int startSecond = GetMinutes(secondRange.StartDay, secondRange.StartTime);
                    int endSecond = GetMinutes(secondRange.EndDay, secondRange.EndTime);

                    int start = Math.Max(startFirst, startSecond);
                    int end = Math.Min(endFirst, endSecond);

                    int diff = end - start;

                    if (diff >= claims.config.MIN_WARRANGE_DURATION_MINUTES)
                    {
                        common.Add(new SelectedWarRange
                        (
                            (DayOfWeek)(start / (24 * 60)),
                            (DayOfWeek)(end / (24 * 60)),
                            TimeSpan.FromMinutes(start % (24 * 60)),
                            TimeSpan.FromMinutes(diff),
                            firstRange.SuggestedAllianceGuid
                        ));
                        //found common range
                        /*TimeSpan startTime = TimeSpan.FromMinutes(start % (24 * 60));
                        TimeSpan endTime = TimeSpan.FromMinutes(end % (24 * 60));
                        DayOfWeek startDay = (DayOfWeek)(start / (24 * 60));
                        DayOfWeek endDay = (DayOfWeek)(end / (24 * 60));
                        common.Add(new SelectedWarRange(startDay, endDay, startTime, endTime - startTime, firstRange.SuggestedAllianceGuid));*/
                    }
                }
            }
            return common;
        }
        public static int GetMinutes(DayOfWeek day, TimeSpan time)
        {
            return (int)day * 24 * 60 + (int)time.TotalMinutes;
        }
        public static int GetWarRangesAmountFromAlliacne(Alliance alliance, List<SelectedWarRange> ranges)
        {
            int result = 0;
            foreach(var it in ranges)
            {
                if(it.SuggestedAllianceGuid.Equals(alliance.Guid))
                {
                    result++;
                }
            }
            return result;
        }
    }
}
