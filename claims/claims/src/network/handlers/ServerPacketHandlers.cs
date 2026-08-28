using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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
                    if (zonesTimestamps == null)
                    {
                        return;
                    }

                    // The client can send this the moment it joins, before the server has given the
                    // player an entity - and the anti-spoof check below reads its position.
                    if (player.Entity == null) return;

                    // Anti-spoof: only allow zones close to player's actual position
                    int zoneBlocks = claims.config.PLOT_SIZE * claims.config.ZONE_PLOTS_LENGTH;
                    Vec2i playerServerPos = new Vec2i((int)player.Entity.Pos.X / zoneBlocks, (int)player.Entity.Pos.Z / zoneBlocks);
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
                    if (player.Entity == null) return;
                    var currentPos = player.Entity.Pos;
                    PlotPosition here = PlotPosition.fromEntityyPos(currentPos);
                    CurrentPlotInfo cpi;
                    if(claims.dataStorage.GetPlot(here, out Plot plot))
                    {
                        claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo plotViewer);
                        cpi = UsefullPacketsSend.BuildCurrentPlotInfo(plot, plotViewer);
                    }
                    else
                    {
                        // Unclaimed ground still gets an answer: staying silent leaves the plot page
                        // describing whichever plot the player walked off, which reads as a page that
                        // never updates.
                        cpi = new CurrentPlotInfo { PlotPosition = here.getPos(), IsClaimed = false };
                    }

                    claims.serverChannel.SendPacket(new SavedPlotsPacket()
                    {
                        type = PacketsContentEnum.CURRENT_PLOT_INFO,
                        data = JsonConvert.SerializeObject(cpi)

                    }, player);
                    if (ServerMain.FrameProfiler.Enabled)
                    {
                        ServerMain.FrameProfiler.Mark("can-claims-packet-current-plot-info");
                    }
                }
                else if (packet.type == PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS)
                {
                    if (!claims.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Contains(player.Role.Code))
                        return;
                    var cities = new System.Collections.Generic.List<AdminCityFlagsItem>();
                    bool hasBalance = claims.economyProvider != null;
                    foreach (var city in claims.dataStorage.getCitiesList())
                    {
                        var ph = city.getPermsHandler();
                        cities.Add(new AdminCityFlagsItem
                        {
                            Guid = city.Guid,
                            Name = city.GetPartName(),
                            Pvp = ph.pvpFlag,
                            Fire = ph.fireFlag,
                            Blast = ph.blastFlag,
                            Technical = city.isTechnicalCity(),
                            Open = city.openCity,
                            CitizenCount = city.getCityCitizens().Count,
                            PlotCount = city.getCityPlots().Count,
                            HasBalance = hasBalance,
                            Balance = hasBalance ? (double)claims.economyProvider.GetBalance(city.MoneyAccountName) : 0
                        });
                    }
                    var wi = claims.dataStorage.getWorldInfo();
                    var worldFlags = new AdminWorldFlags
                    {
                        PvpEverywhere = wi.pvpEverywhere,
                        PvpForbidden = wi.pvpForbidden,
                        FireEverywhere = wi.fireEverywhere,
                        FireForbidden = wi.fireForbidden,
                        BlastEverywhere = wi.blastEverywhere,
                        BlastForbidden = wi.blastForbidden
                    };
                    claims.serverChannel.SendPacket(new SavedPlotsPacket
                    {
                        type = PacketsContentEnum.ADMIN_CITY_FLAGS_ALL,
                        data = JsonConvert.SerializeObject(new AdminDataPacket { Cities = cities, World = worldFlags })
                    }, player);
                }
                else if (packet.type == PacketsContentEnum.CITY_CITIZENS_RANKS_REQUEST)
                {
                    claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo rankRequester);
                    if (rankRequester == null || !rankRequester.hasCity()) return;
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
                }
                else if (packet.type == PacketsContentEnum.CLIENT_SET_RESPAWN_PREFERENCE)
                {
                    if (!int.TryParse(packet.data, out int prefValue)) return;
                    if (!System.Enum.IsDefined(typeof(EnumRespawnPreference), prefValue)) return;

                    EnumRespawnPreference preference = (EnumRespawnPreference)prefValue;
                    RespawnPreference.Write(player, preference);
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:respawn_pref_set",
                        Lang.Get(RespawnPreference.LangKeyOf(preference))));
                }
                else if (packet.type == PacketsContentEnum.CLIENT_SET_BOAT_SHARE)
                {
                    HandleSetBoatShare(player, packet.data);
                }
                else if (packet.type == PacketsContentEnum.CLIENT_CAMP_TELEPORT)
                {
                    claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo tpRequester);
                    if (tpRequester == null) return;

                    string langKey = commands.CityCommand.TryStartCampTeleport(player, tpRequester, out object[] msgParams);
                    MessageHandler.sendMsgToPlayer(player, msgParams == null
                        ? Lang.Get(langKey)
                        : Lang.Get(langKey, msgParams));
                }
                else if (packet.type == PacketsContentEnum.CLIENT_REQUEST_CASUS_BELLI)
                {
                    claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo cbRequester);
                    if (cbRequester == null || !cbRequester.hasCity()) return;
                    UsefullPacketsSend.AddToQueuePlayerInfoUpdate(cbRequester.Guid, EnumPlayerRelatedInfo.CITY_CASUS_BELLI_ALL);
                }
            });
            claims.serverChannel.SetMessageHandler<PlayerGuiRelatedInfoPacket>((player, packet) =>
            {
                claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    return;
                }
                // Refusals are spoken, not swallowed: the submit button gave no answer at all when
                // the sender was not entitled to set a schedule, which reads as a dead button.
                IConflictParty ourParty;
                if (playerInfo.HasAlliance())
                {
                    if (!playerInfo.Alliance.IsLeader(playerInfo))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:warrange-only-alliance-leader"));
                        return;
                    }
                    ourParty = playerInfo.Alliance;
                }
                else if (playerInfo.hasCity())
                {
                    if (!playerInfo.City.isMayor(playerInfo))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:warrange-only-mayor"));
                        return;
                    }
                    // The GUI reaches war ranges without going through the commands, so the
                    // village gate in TryResolveMyParty does not cover this path.
                    if (playerInfo.City.IsVillage())
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:warrange-village-cannot"));
                        return;
                    }
                    ourParty = playerInfo.City;
                }
                else
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:warrange-no-city"));
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

                if (conflict.ActiveWarTime)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:warrange-battle-running"));
                    return;
                }

                // Only a side of the conflict may edit its war ranges
                if (!conflict.First.Equals(ourParty) && !conflict.Second.Equals(ourParty))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:warrange-not-our-conflict"));
                    return;
                }

                // The grid the client sends is whatever it felt like sending, so the day restriction
                // is applied here rather than only in the GUI.
                ccce.FirstWarRanges = WarScheduleHelper.FilterToAllowedDays(ccce.FirstWarRanges);
                ccce.SecondWarRanges = WarScheduleHelper.FilterToAllowedDays(ccce.SecondWarRanges);

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
                        MessageHandler.SendMsgInAlliance(conflict.First,
                            Lang.Get("claims:warrange-agreed", conflict.Second.GetPartName(),
                                TimeFunctions.FormatBattleDate(conflict.NextBattleDateStart)));
                        MessageHandler.SendMsgInAlliance(conflict.Second,
                            Lang.Get("claims:warrange-agreed", conflict.First.GetPartName(),
                                TimeFunctions.FormatBattleDate(conflict.NextBattleDateStart)));
                    }
                    else
                    {
                        // Both sides hear it: the sender learns their proposal was recorded but does
                        // not meet the enemy's, and the enemy learns there is something to answer.
                        IConflictParty otherParty = conflict.First.Equals(ourParty) ? conflict.Second : conflict.First;
                        MessageHandler.sendMsgToPlayer(player,
                            Lang.Get("claims:warrange-no-common-window",
                                claims.config.MIN_WARRANGE_DURATION_MINUTES, otherParty.GetPartName()));
                        MessageHandler.SendMsgInAlliance(otherParty,
                            Lang.Get("claims:warrange-enemy-proposed", ourParty.GetPartName()));
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
                    // No agreement needed on this server, so the sender is told directly what their
                    // hours booked rather than being left to guess from the page.
                    MessageHandler.sendMsgToPlayer(player,
                        Lang.Get("claims:warrange-agreed",
                            (conflict.First.Equals(ourParty) ? conflict.Second : conflict.First).GetPartName(),
                            TimeFunctions.FormatBattleDate(conflict.NextBattleDateStart)));
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
        /// <summary>
        /// Overlap of the two sides' proposed windows. Positions are minutes since the start of the
        /// week; a window that runs past midnight simply runs past the end of the week too, so the
        /// second side's window is also tried one week earlier and later - otherwise a Saturday
        /// 23:00 window could never meet the very same window proposed by the other side.
        /// </summary>
        public static List<SelectedWarRange> FindCommonRanges(List<SelectedWarRange> first, List<SelectedWarRange> second)
        {
            const int week = 7 * 24 * 60;
            List<SelectedWarRange> common = new List<SelectedWarRange>();
            foreach (var firstRange in first)
            {
                int startFirst = GetMinutes(firstRange.StartDay, firstRange.StartTime);
                // End is start plus duration, never GetMinutes(EndDay, EndTime): EndTime already
                // carries the overflow past midnight, so adding EndDay on top counted it twice and
                // made every window that touched midnight look like it ended before it began.
                int endFirst = startFirst + (int)firstRange.Duration.TotalMinutes;
                foreach (var secondRange in second)
                {
                    int startSecondBase = GetMinutes(secondRange.StartDay, secondRange.StartTime);
                    int secondLength = (int)secondRange.Duration.TotalMinutes;

                    for (int shift = -week; shift <= week; shift += week)
                    {
                        int start = Math.Max(startFirst, startSecondBase + shift);
                        int end = Math.Min(endFirst, startSecondBase + shift + secondLength);
                        int diff = end - start;
                        if (diff < claims.config.MIN_WARRANGE_DURATION_MINUTES) continue;

                        int normStart = ((start % week) + week) % week;
                        int normEnd = (normStart + diff) % week;
                        common.Add(new SelectedWarRange
                        (
                            (DayOfWeek)(normStart / (24 * 60)),
                            (DayOfWeek)(normEnd / (24 * 60)),
                            TimeSpan.FromMinutes(normStart % (24 * 60)),
                            TimeSpan.FromMinutes(diff),
                            firstRange.SuggestedAllianceGuid
                        ));
                    }
                }
            }
            // The overlap of two allowed windows is itself allowed, but the minute arithmetic above
            // does not wrap around the week, so re-check rather than trust it.
            return WarScheduleHelper.FilterToAllowedDays(common);
        }
        /// <summary>
        /// Applies the sharing mode picked in the dialog. Re-checks everything the client claims:
        /// that the entity is ownable, that the caller owns it, and that they are standing at it.
        /// </summary>
        private static void HandleSetBoatShare(IServerPlayer player, string data)
        {
            if (!claims.config.BOAT_SHARE_WITH_CITY) return;

            string[] parts = (data ?? "").Split(';');
            if (parts.Length != 2) return;
            if (!long.TryParse(parts[0], out long entityId)) return;
            if (!int.TryParse(parts[1], out int rawMode)) return;
            if (!Enum.IsDefined(typeof(BoatShareMode), rawMode)) return;

            var mode = (BoatShareMode)rawMode;
            if (mode == BoatShareMode.ALLIANCE && !claims.config.BOAT_SHARE_WITH_ALLIANCE) return;

            Entity boat = claims.sapi.World.GetEntityById(entityId);
            if (boat?.GetBehavior<Vintagestory.GameContent.EntityBehaviorOwnable>() == null) return;

            var ownedby = boat.WatchedAttributes.GetTreeAttribute("ownedby");
            if (ownedby == null || ownedby.GetString("uid", "") != player.PlayerUID) return;

            if (player.Entity == null || player.Entity.Pos.DistanceTo(boat.Pos.XYZ) > BoatShareReach) return;

            BoatShareModeHelper.Set(boat, mode);
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:boat-share-set",
                Lang.Get(BoatShareModeHelper.LangKeyOf(mode))));
        }

        /// <summary>How far from a boat its sharing may still be changed, in blocks.</summary>
        private const double BoatShareReach = 12;

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
