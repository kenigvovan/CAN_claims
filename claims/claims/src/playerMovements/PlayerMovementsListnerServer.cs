using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.delayed.teleportation;
using claims.src.events;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src
{
    public class PlayerMovementsListnerServer
    {
        public static int playerPositionControlDelta = 100;
        // uid -> set of zones this player is subscribed to
        public Dictionary<string, HashSet<Vec2i>> playerSubscriptions;
        // zone -> set of uids subscribed to this zone (reverse index for fast push)
        public Dictionary<Vec2i, HashSet<string>> zoneSubscribers;

        public Dictionary<Vec2i, HashSet<Vec2i>> PlotWhichShouldBeUpdated;
        public Dictionary<Vec2i, HashSet<Vec2i>> PlotWhichShouldBeRemoved;
        public PlayerMovementsListnerServer()
        {
            playerSubscriptions = new Dictionary<string, HashSet<Vec2i>>();
            zoneSubscribers = new Dictionary<Vec2i, HashSet<string>>();
            PlotWhichShouldBeUpdated = new Dictionary<Vec2i, HashSet<Vec2i>>();
            PlotWhichShouldBeRemoved = new Dictionary<Vec2i, HashSet<Vec2i>>();

            claims.sapi.Event.Timer(checkAndSendUpdates, 10);
        }
        private void Subscribe(string uid, Vec2i zone)
        {
            if (!playerSubscriptions.TryGetValue(uid, out var zones))
            {
                zones = new HashSet<Vec2i>();
                playerSubscriptions[uid] = zones;
            }
            zones.Add(zone);

            if (!zoneSubscribers.TryGetValue(zone, out var subs))
            {
                subs = new HashSet<string>();
                zoneSubscribers[zone] = subs;
            }
            subs.Add(uid);
        }
        private void Unsubscribe(string uid, Vec2i zone)
        {
            if (playerSubscriptions.TryGetValue(uid, out var zones))
            {
                zones.Remove(zone);
                if (zones.Count == 0) playerSubscriptions.Remove(uid);
            }
            if (zoneSubscribers.TryGetValue(zone, out var subs))
            {
                subs.Remove(uid);
                if (subs.Count == 0) zoneSubscribers.Remove(zone);
            }
        }
        public void RemovePlayerFromAllSubscriptions(string uid)
        {
            if (!playerSubscriptions.TryGetValue(uid, out var zones)) return;
            foreach (var zone in zones)
            {
                if (zoneSubscribers.TryGetValue(zone, out var subs))
                {
                    subs.Remove(uid);
                    if (subs.Count == 0) zoneSubscribers.Remove(zone);
                }
            }
            playerSubscriptions.Remove(uid);
        }
        public HashSet<string> GetSubscribers(Vec2i zone)
        {
            return zoneSubscribers.TryGetValue(zone, out var subs) ? subs : null;
        }
        //Player change chunk position
        /*public static string getMsgForChunkChange(Plot fromPlot, Plot toPlot, int state, PlayerInfo playerInfo)
        {
            StringBuilder stringBuilder = new StringBuilder();
            //state - 
            //0 both empty
            //1 from has smth
            //2 to has smth
            //3 both has smth

            //JUST FROM ONE EMPTY TO ANOTHER
            if(state == 0)
            {
                return "";
            }

            //FROM VILLAGE OR CITY TO EMPTY
            if(state == 1)
            {
                stringBuilder.Append("To wild lands.");
                if(playerInfo.isPrisoned())
                {
                    MessageHandler.sendMsgInCity(
                    playerInfo.PrisonedIn.getCity(),
                    Lang.Get("claims:player_escaped_prison", playerInfo.GetPartName()));
                    MessageHandler.sendMsgToPlayerInfo(playerInfo, Lang.Get("claims:you_escaped_prison"));
                    
                    playerInfo.PrisonedIn = null;
                    playerInfo.PrisonHoursLeft = 0;
                    (claims.sapi.World.PlayerByUid(playerInfo.Guid) as IServerPlayer).SetSpawnPosition(new PlayerSpawnPos((int)claims.sapi.World.DefaultSpawnPosition.X, (int)claims.sapi.World.DefaultSpawnPosition.Y, (int)claims.sapi.World.DefaultSpawnPosition.Z));
                    

                }
                return stringBuilder.ToString();
            }

            //FROM EMPTY TO CITY
            if (state == 2)
            {
                if (toPlot.hasCity())
                {                   
                    stringBuilder.Append(StringFunctions.setStringColor(toPlot.getCity().getPartNameReplaceUnder(), ColorsClaims.DARK_GRAY));                                                           
                }


                if (toPlot.hasPlotGroup())
                {
                    stringBuilder.Append(" ").Append(toPlot.getPlotGroup().getPartNameReplaceUnder());
                }
                else if (toPlot.hasPlotOwner())
                {
                    stringBuilder.Append(" ").Append(toPlot.getPlotOwner().GetPartName());
                }
                if(toPlot.GetPartName() != "")
                {
                    stringBuilder.Append(" ").Append("~").Append(toPlot.GetPartName()).Append("~");
                }
                if (toPlot.getPrice() >= 0)
                {
                    stringBuilder.Append(" ").Append("To sell: " + toPlot.getPrice().ToString());
                }
                if (toPlot.getPermsHandler().pvpFlag)
                {
                    stringBuilder.Append(" ").Append(StringFunctions.setBold(StringFunctions.setStringColor("PVP", ColorsClaims.DARK_RED)));
                }
                else
                {
                    stringBuilder.Append(" ").Append(StringFunctions.setBold(StringFunctions.setStringColor("NO-PVP", ColorsClaims.WHITE)));
                }
            }

            //BOTH HAVE CITY 
            if(state == 3)
            {
                if (toPlot.hasCity() && !toPlot.getCity().Equals(fromPlot.getCity()))
                {
                    if (playerInfo.hasCity())
                    {
                        stringBuilder.Append(StringFunctions.setStringColor(toPlot.getCity().getPartNameReplaceUnder(), ColorsClaims.DARK_GREEN) + " ");
                    }
                    else
                    {
                        stringBuilder.Append(StringFunctions.setStringColor(toPlot.getPartNameReplaceUnder(), ColorsClaims.DARK_GRAY) + " ");
                    }
                }

            

                if (toPlot.hasPlotGroup())
                {
                    stringBuilder.Append(toPlot.getPlotGroup().getPartNameReplaceUnder() + " ");
                }
                else if (toPlot.hasPlotOwner())
                {
                    stringBuilder.Append(toPlot.getPlotOwner().GetPartName() + " ");
                }

                if (toPlot.GetPartName() != "")
                {
                    stringBuilder.Append(" ").Append("~").Append(toPlot.GetPartName()).Append("~");
                }

                if (toPlot.getPrice() >= 0)
                {
                    stringBuilder.Append("To sell: " + toPlot.getPrice().ToString() + " ");
                }
                if (toPlot.getPermsHandler().pvpFlag)
                {
                    StringFunctions.setBold(StringFunctions.setStringColor("PVP", ColorsClaims.DARK_RED) + " ");
                }
                else
                {
                    StringFunctions.setBold(StringFunctions.setStringColor("NO-PVP", ColorsClaims.WHITE) + " ");
                }
            }

            return stringBuilder.ToString();
        }*/
        public void onPlayerChangePlotEvent(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            claims.dataStorage.GetPlayerByUid(tree.GetString("playerUID"), out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return;
            }
            Vec2i fromv = new Vec2i(tree.GetInt("xChO"), tree.GetInt("zChO"));
            Vec2i tov = new Vec2i(tree.GetInt("xCh"), tree.GetInt("zCh"));

            PlotPosition to = new PlotPosition(tov);
            IServerPlayer pl = claims.sapi.World.PlayerByUid(playerInfo.Guid) as IServerPlayer;
            if (pl == null) return;

            if (playerInfo.PlayerCache.LastChunk == null)
            {
                events.OnBlockAction.InitPlayerCache(pl);
            }
            else
            {
                if (pl.Entity == null) return;
                playerInfo.PlayerCache.setPlotPosition(PlotPosition.fromXZ((int)pl.Entity.Pos.X, (int)pl.Entity.Pos.Z));
                playerInfo.PlayerCache.Reset();
            }

            claims.dataStorage.GetPlot(to, out Plot toPlot);

            //To empty plot, so he escaped
            if (toPlot == null)
            {
                if (playerInfo.isPrisoned())
                {
                    MessageHandler.sendMsgInCity(
                    playerInfo.PrisonedIn.City,
                    Lang.Get("claims:player_escaped_prison", playerInfo.GetPartName()));
                    MessageHandler.sendMsgToPlayerInfo(playerInfo, Lang.Get("claims:you_escaped_prison"));
                    if(playerInfo.PrisonedIn.TryGetCellInWhichPlayer(playerInfo, out var cell))
                    {
                        // Out of the cell before the roster is sent: it used to go out still listing
                        // the player who had just escaped, and nothing ever took them off it.
                        cell.RemovePlayer(playerInfo);
                        UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.PrisonedIn.City.Guid, new Dictionary<string, object> { { "value", new PrisonCellElement(cell.spawnPostion, cell.playerNames) } },
                            EnumPlayerRelatedInfo.CITY_CELL_PRISON_UPDATE);
                    }
                    playerInfo.PrisonedIn = null;
                    playerInfo.PrisonHoursLeft = 0;
                    pl.SetSpawnPosition(new PlayerSpawnPos((int)claims.sapi.World.DefaultSpawnPosition.X, (int)claims.sapi.World.DefaultSpawnPosition.Y, (int)claims.sapi.World.DefaultSpawnPosition.Z));
                    
                    
                }
            }
            if (playerInfo.showBorders)
            {
                PlotPosition.makeChunkHighlight(claims.sapi.World, claims.sapi.World.PlayerByUid(playerInfo.Guid), toPlot);
            }
            /* if((int)(fromv.X / Config.Current.ZONE_PLOTS_LENGTH.Val) != (int)(tov.X / Config.Current.ZONE_PLOTS_LENGTH.Val) ||
                 (int)(fromv.Y / Config.Current.ZONE_PLOTS_LENGTH.Val) != (int)(tov.Y / Config.Current.ZONE_PLOTS_LENGTH.Val))
             {
                 sendZoneToPlayer(tov, playerInfo, pl);
             }*/

        }        
        public void checkPlayerMove(float dt)
        {
            foreach (var it in claims.sapi.World.AllOnlinePlayers)
            {
                if (it == null)
                {
                    MessageHandler.sendErrorMsg("checkPlayerMove::null player");
                    continue;
                }

                //If we have last player pos saved
                if (claims.dataStorage.getLastPlayerPos(it.PlayerUID, out Vec3i lastPlayerPos))
                {
                    if (it.Entity == null) continue;
                    Vec3i playerCurrentPos = it.Entity.Pos.XYZInt;
                    if ((lastPlayerPos.X != playerCurrentPos.X || lastPlayerPos.Z != playerCurrentPos.Z))
                    {
                        //Player moved
                        claims.dataStorage.GetPlayerByUid(it.PlayerUID, out PlayerInfo playerInfo);
                        if (playerInfo == null)
                        {
                            continue;
                        }
                        if (claims.config.PLAYER_MOVEMENT_CANCEL_TELEPORTATION
                                                                 && playerInfo.AwaitForTeleporation
                                                                 && it.WorldData.CurrentGameMode != EnumGameMode.Creative)
                        {
                            TeleportationHandler.removeTeleportation(playerInfo);
                            MessageHandler.sendMsgToPlayer(it as IServerPlayer, Lang.Get("claims:summon_canceled"));
                        }

                        //If player is now in a different plot
                        if (lastPlayerPos != null && (lastPlayerPos.X / PlotPosition.plotSize != (playerCurrentPos.X / PlotPosition.plotSize) || lastPlayerPos.Z / PlotPosition.plotSize != (playerCurrentPos.Z / PlotPosition.plotSize)))
                        {
                            TreeAttribute tree = new TreeAttribute();
                            tree.SetString("playerUID", it.PlayerUID);
                            //new plot
                            tree.SetInt("xCh", playerCurrentPos.X / PlotPosition.plotSize);
                            tree.SetInt("zCh", playerCurrentPos.Z / PlotPosition.plotSize);
                            //old plot
                            tree.SetInt("xChO", (int)lastPlayerPos.X / PlotPosition.plotSize);
                            tree.SetInt("zChO", (int)lastPlayerPos.Z / PlotPosition.plotSize);

                            playerInfo.PlayerCache.Reset();
                            playerInfo.PlayerCache.setPlotPosition(PlotPosition.fromXZ((int)it.Entity.Pos.X, (int)it.Entity.Pos.Z));

                            claims.sapi.World.Api.Event.PushEvent("claimsPlayerChangePlot", tree);
                        }
                        lastPlayerPos.X = playerCurrentPos.X;
                        lastPlayerPos.Y = playerCurrentPos.Y;
                        lastPlayerPos.Z = playerCurrentPos.Z;

                    }
                }
                else
                {
                    claims.dataStorage.GetPlayerByUid(it.PlayerUID, out PlayerInfo playerInfo);
                    if (playerInfo == null)
                    {
                        continue;
                    }
                    if (it.Entity == null) continue;
                    Vec3i playerCurrentPos = it.Entity.Pos.XYZInt;
                    claims.dataStorage.setLastPlayerPos(it.PlayerUID, playerCurrentPos.Clone());

                    //player probably just logged in
                    TreeAttribute tree = new TreeAttribute();
                    tree.SetString("playerUID", it.PlayerUID);
                    //new plot
                    tree.SetInt("xCh", playerCurrentPos.X / PlotPosition.plotSize);
                    tree.SetInt("zCh", playerCurrentPos.Z / PlotPosition.plotSize);
                    //old plot
                    tree.SetInt("xChO", playerCurrentPos.X / PlotPosition.plotSize);
                    tree.SetInt("zChO", playerCurrentPos.Z / PlotPosition.plotSize);
                    if (playerInfo.PlayerCache.LastChunk == null)
                    {
                        //events.OnBlockAction.InitPlayerCache((IServerPlayer)it);
                    }
                    else
                    {
                        playerInfo.PlayerCache.Reset();
                        playerInfo.PlayerCache.setPlotPosition(PlotPosition.fromXZ(playerCurrentPos.X, playerCurrentPos.Z));
                    }
                    claims.sapi.World.Api.Event.PushEvent("claimsPlayerChangePlot", tree);
                }
            }
        }
        /// <summary>
        /// Sync player subscriptions with the requested zone set.
        /// Subscribes to zones not previously tracked, unsubscribes from zones no longer requested.
        /// Sends snapshot only for newly subscribed zones (delta).
        /// </summary>
        public void SyncSubscriptionsAndSendSnapshot(IServerPlayer player, PlayerInfo playerInfo, IEnumerable<Vec2i> requestedZones)
        {
            string uid = player.PlayerUID;
            HashSet<Vec2i> requested = new HashSet<Vec2i>(requestedZones);

            // Compute diff
            HashSet<Vec2i> current = playerSubscriptions.TryGetValue(uid, out var existing)
                ? new HashSet<Vec2i>(existing)
                : new HashSet<Vec2i>();

            HashSet<Vec2i> toUnsubscribe = new HashSet<Vec2i>(current);
            toUnsubscribe.ExceptWith(requested);
            HashSet<Vec2i> toSubscribe = new HashSet<Vec2i>(requested);
            toSubscribe.ExceptWith(current);

            foreach (var zone in toUnsubscribe)
            {
                Unsubscribe(uid, zone);
            }

            // Build snapshot for newly subscribed zones only
            HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>> snapshot
                = new HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>>();
            foreach (var zone in toSubscribe)
            {
                Subscribe(uid, zone);
                if (claims.dataStorage.getZone(zone, out ServerZoneInfo serverZoneInfo))
                {
                    List<KeyValuePair<Vec2i, SavedPlotInfo>> preparedSavedPlots = new List<KeyValuePair<Vec2i, SavedPlotInfo>>();
                    foreach (Plot plot in serverZoneInfo.zonePlots)
                    {
                        preparedSavedPlots.Add(new KeyValuePair<Vec2i, SavedPlotInfo>(plot.getPos(),
                            PlotStateHandling.BuildSavedPlotInfo(plot, player, playerInfo)));
                    }
                    snapshot.Add(new Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>(zone, 0L, preparedSavedPlots));
                }
                else
                {
                    // Zone exists but is empty (or doesn't exist) - send empty so client clears any stale data
                    snapshot.Add(new Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>(zone, 0L, new List<KeyValuePair<Vec2i, SavedPlotInfo>>()));
                }
            }

            if (snapshot.Count > 0)
            {
                string serialized = JsonConvert.SerializeObject(snapshot);
                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.SERVER_UPDATED_ZONES_ANSWER,
                    data = serialized
                }, player);
            }
        }
        //in collected "removed" plots and "updated" plots dict we saved coords of such plots
        //we send each subscribed player the deltas for zones they're subscribed to
        public void checkAndSendUpdates()
        {
            var sapi = claims.sapi;
            if (sapi == null)
            {
                return;
            }
            if (PlotWhichShouldBeRemoved.Count > 0)
            {
                // For each subscriber, collect the plots in zones they're subscribed to
                Dictionary<string, HashSet<Vec2i>> perPlayerRemoves = new Dictionary<string, HashSet<Vec2i>>();
                foreach (var zoneEntry in PlotWhichShouldBeRemoved)
                {
                    if (!zoneSubscribers.TryGetValue(zoneEntry.Key, out var subs)) continue;
                    foreach (var uid in subs)
                    {
                        if (!perPlayerRemoves.TryGetValue(uid, out var set))
                        {
                            set = new HashSet<Vec2i>();
                            perPlayerRemoves[uid] = set;
                        }
                        foreach (var coord in zoneEntry.Value) set.Add(coord);
                    }
                }
                foreach (var pair in perPlayerRemoves)
                {
                    IServerPlayer pl = claims.sapi.World.PlayerByUid(pair.Key) as IServerPlayer;
                    if (pl == null) continue;
                    string serializedZones = JsonConvert.SerializeObject(pair.Value);
                    claims.serverChannel.SendPacket(new SavedPlotsPacket()
                    {
                        type = PacketsContentEnum.SERVER_REMOVE_COLLECTED_PLOTS,
                        data = serializedZones
                    }, pl);
                }
                PlotWhichShouldBeRemoved.Clear();
            }

            if (PlotWhichShouldBeUpdated.Count > 0)
            {
                Dictionary<string, List<Vec2i>> perPlayerUpdates = new Dictionary<string, List<Vec2i>>();
                foreach (var zoneEntry in PlotWhichShouldBeUpdated)
                {
                    if (!zoneSubscribers.TryGetValue(zoneEntry.Key, out var subs)) continue;
                    foreach (var uid in subs)
                    {
                        if (!perPlayerUpdates.TryGetValue(uid, out var list))
                        {
                            list = new List<Vec2i>();
                            perPlayerUpdates[uid] = list;
                        }
                        foreach (var coord in zoneEntry.Value) list.Add(coord);
                    }
                }
                PlotPosition tmpPlotPosition = new PlotPosition();
                foreach (var pair in perPlayerUpdates)
                {
                    IServerPlayer pl = claims.sapi.World.PlayerByUid(pair.Key) as IServerPlayer;
                    if (pl == null) continue;
                    if (!claims.dataStorage.GetPlayerByUid(pair.Key, out PlayerInfo playerInfo)) continue;

                    List<Tuple<Vec2i, SavedPlotInfo>> updatePlotsForPlayer = new List<Tuple<Vec2i, SavedPlotInfo>>();
                    foreach (var coord in pair.Value)
                    {
                        tmpPlotPosition.setXY(coord);
                        if (claims.dataStorage.GetPlot(tmpPlotPosition, out Plot plot))
                        {
                            updatePlotsForPlayer.Add(new Tuple<Vec2i, SavedPlotInfo>(plot.getPos(),
                                PlotStateHandling.BuildSavedPlotInfo(plot, pl, playerInfo)));
                        }
                    }
                    if (updatePlotsForPlayer.Count == 0) continue;
                    string serializedZones = JsonConvert.SerializeObject(updatePlotsForPlayer);
                    claims.serverChannel.SendPacket(new SavedPlotsPacket()
                    {
                        type = PacketsContentEnum.SERVER_UPDATE_COLLECTED_PLOTS,
                        data = serializedZones
                    }, pl);
                }
                PlotWhichShouldBeUpdated.Clear();
            }
        }

        public void markPlotToWasRemoved(Vec2i vec)
        {
            var tmpVec = new Vec2i(vec.X / claims.config.ZONE_PLOTS_LENGTH, vec.Y / claims.config.ZONE_PLOTS_LENGTH);
            if (PlotWhichShouldBeRemoved.TryGetValue(tmpVec,
                out HashSet<Vec2i> hs))
            {
                hs.Add(vec);            
            }
            else
            {
                PlotWhichShouldBeRemoved.Add(tmpVec, new HashSet<Vec2i> { vec });
            }
        }
        public void markPlotToWasReUpdated(Vec2i vec)
        {
            var tmpVec = new Vec2i(vec.X / claims.config.ZONE_PLOTS_LENGTH, vec.Y / claims.config.ZONE_PLOTS_LENGTH);
            if (PlotWhichShouldBeUpdated.TryGetValue(tmpVec,
                out HashSet<Vec2i> hs))
            {
                hs.Add(vec);
            }
            else
            {
                PlotWhichShouldBeUpdated.Add(tmpVec, new HashSet<Vec2i> { vec });
            }
        }
    }
}
