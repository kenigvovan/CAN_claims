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
        public Dictionary<string, HashSet<Vec2i>> alreadySentZonesToPlayers;

        //after plot is claimed/unclaimed/bought/flags/ownership/plotgroups changes/changed name/cost etc.
        //we add it's pos here and once in N seconds we send
        //for every player in some radius update about this plots
        //flags decide if player can use/brake something
        //plotgroup as well
        //ownershipment of plot gives hime all rights
        //also innerclaims should as well be passed there as some sort of cuboids 
        //and require resend of data when changed
        //TODO
        public Dictionary<Vec2i, HashSet<Vec2i>> PlotWhichShouldBeUpdated;
        public Dictionary<Vec2i, HashSet<Vec2i>> PlotWhichShouldBeRemoved;
        public PlayerMovementsListnerServer()
        {
            alreadySentZonesToPlayers = new Dictionary<string, HashSet<Vec2i>>();
            PlotWhichShouldBeUpdated = new Dictionary<Vec2i, HashSet<Vec2i>>();
            PlotWhichShouldBeRemoved = new Dictionary<Vec2i, HashSet<Vec2i>>();

            claims.sapi.Event.Timer(checkAndSendUpdates, 10);
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
            
            if (playerInfo.PlayerCache.LastChunk == null)
            {
                events.OnBlockAction.InitPlayerCache((IServerPlayer)pl);
            }
            else
            {
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
                    Vec3i playerCurrentPos = it.Entity.Pos.XYZInt;
                    if ((lastPlayerPos.X != playerCurrentPos.X || lastPlayerPos.Z != playerCurrentPos.Z))
                    {
                        //Player moved
                        claims.dataStorage.GetPlayerByUid(it.PlayerUID, out PlayerInfo playerInfo);
                        if (playerInfo == null)
                        {
                            return;
                        }
                        if (claims.config.PLAYER_MOVEMENT_CANCEL_TELEPORTATION
                                                                 && playerInfo.AwaitForTeleporation
                                                                 && it.WorldData.CurrentGameMode != EnumGameMode.Creative)
                        {
                            TeleportationHandler.removeTeleportation(playerInfo);
                            MessageHandler.sendMsgToPlayer(it as IServerPlayer, Lang.Get("claims:summon_canceled"));
                        }

                        //If player is now in a different plot
                        if (lastPlayerPos != null && (lastPlayerPos.X / PlotPosition.plotSize != (playerCurrentPos.X / PlotPosition.plotSize)) || lastPlayerPos.Z / PlotPosition.plotSize != (playerCurrentPos.Z / PlotPosition.plotSize))
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
                        return;
                    }
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
        /// Send plots' zones around player
        /// if zone already has been sent we skip it
        /// for now we resend on every reconnect
        /// </summary>
        /// <param name="playersPlot"></param>
        /// <param name="playerInfo"></param>
        /// <param name="player"></param>
        public void sendZoneToPlayer(Vec2i playersPlot, PlayerInfo playerInfo, IServerPlayer player)
        {
            Vec2i centerZoneCoords = new Vec2i(playersPlot.X / claims.config.ZONE_PLOTS_LENGTH, playersPlot.Y / claims.config.ZONE_PLOTS_LENGTH);
            Vec2i tmpZoneCoords = new Vec2i();
            List<KeyValuePair<Vec2i, List<KeyValuePair<Vec2i, SavedPlotInfo>>>> savedZones = new List<KeyValuePair<Vec2i, List<KeyValuePair<Vec2i, SavedPlotInfo>>>>();
            List<KeyValuePair<Vec2i, SavedPlotInfo>> savedPlots;
            for (int i = -1; i < 2; i++)
            {
                savedPlots = new List<KeyValuePair<Vec2i, SavedPlotInfo>>();
                for (int j = -1; j < 2; j++) 
                {
                    tmpZoneCoords.X = centerZoneCoords.X + i;
                    tmpZoneCoords.Y = centerZoneCoords.Y + j;

                    if (alreadySentZonesToPlayers.TryGetValue(playerInfo.GetPartName(), out HashSet<Vec2i> alreadySentZones))
                    {
                        //we already sent this zone to player
                        if (alreadySentZones.Contains(tmpZoneCoords))
                        {
                            continue;
                        }
                    }
                    //for zone in which player is and around we collect plots
                    //if zone exists then it has plots, so we add it to "already sent" dict
                    if (claims.dataStorage.getZone(tmpZoneCoords, out ServerZoneInfo zone))
                    {                        
                        foreach (Plot plot in zone.zonePlots)
                        {
                            savedPlots.Add(new KeyValuePair<Vec2i, SavedPlotInfo>(plot.getPos(), new SavedPlotInfo((int)plot.Price, plot.getPermsHandler().pvpFlag,
                                player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockDestroyWithOutCacheUpdate(playerInfo, plot),
                                player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockUseWithOutCacheUpdate(playerInfo, plot),
                                player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canAttackAnimalsWithOutCacheUpdate(playerInfo, plot),
                                plot.getCity().GetPartName(), plot.GetPartName(),
                                plot.hasCityPlotsGroup()
                                    ? plot.getPlotGroup().GetPartName()
                                    : "",
                               plot.Type == PlotType.TAVERN
                                   ? plot.GetClientInnerClaimFromDefault(playerInfo)
                                   : null,
                               plot.getCity().Alliance?.Guid ?? "")));
                        }
                        if (!alreadySentZonesToPlayers.ContainsKey(playerInfo.GetPartName()))
                        {
                            alreadySentZonesToPlayers.Add(playerInfo.GetPartName(),
                                                        new HashSet<Vec2i> { tmpZoneCoords });
                        }
                        else
                        {
                            alreadySentZones.Add(tmpZoneCoords);
                        }
                    }                   
                    
                }
                if (savedPlots.Count > 0)
                {
                    savedZones.Add(new KeyValuePair<Vec2i, List<KeyValuePair<Vec2i, SavedPlotInfo>>>(tmpZoneCoords.Copy(), savedPlots));
                }
            }
            //List < KeyValuePair < Vec2i, List<KeyValuePair<Vec2i, SavedPlotInfo>>>>
            if (savedZones.Count > 0)
            {
                //we collected all zones' plots and send packet
                string serializedPlots = JsonConvert.SerializeObject(savedZones);

                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.ON_JOIN,
                    data = serializedPlots

                }, player);
            }
        }
        //in collected "removed" plots and "updated" plots dict we saved coords of such plots
        //later we come through them and decide to which players we need to send this info
        //now in hardcoded radius around player's zone
        public void checkAndSendUpdates()
        {
            var sapi = claims.sapi;
            if(sapi == null)
            {
                return;
            }
            if (PlotWhichShouldBeRemoved.Count > 0)
            {
                Vec2i playerZone = new Vec2i();
                foreach (var pl in claims.sapi.World.AllOnlinePlayers)
                {
                    playerZone.X = (int)pl.Entity.Pos.X / claims.config.ZONE_BLOCKS_LENGTH;
                    playerZone.Y = (int)pl.Entity.Pos.Z / claims.config.ZONE_BLOCKS_LENGTH;

                    HashSet<Vec2i> removeForPlyaer = new HashSet<Vec2i>();
                    foreach(var zone in PlotWhichShouldBeRemoved)
                    {
                        if ((playerZone.X - 2 < zone.Key.X && playerZone.X + 2 > zone.Key.X) &&
                            (playerZone.Y - 2 < zone.Key.Y && playerZone.Y + 2 > zone.Key.Y))
                        {
                            foreach(var it in zone.Value)
                            {
                                removeForPlyaer.Add(it);
                            }                           
                        }                          
                    }
                    if(removeForPlyaer.Count == 0)
                    {
                        continue;
                    }
                    string serializedZones = JsonConvert.SerializeObject(removeForPlyaer);
                    claims.serverChannel.SendPacket(new SavedPlotsPacket()
                    {
                        type = PacketsContentEnum.SERVER_REMOVE_COLLECTED_PLOTS,
                        data = serializedZones

                    }, pl as IServerPlayer);
                }
                PlotWhichShouldBeRemoved.Clear();
            }

            if(PlotWhichShouldBeUpdated.Count > 0)
            {
                Vec2i playerZone = new Vec2i();
                foreach (IServerPlayer pl in claims.sapi.World.AllOnlinePlayers)
                {
                    claims.dataStorage.GetPlayerByUid(pl.PlayerUID, out PlayerInfo playerInfo);

                    if (playerInfo == null)
                    {
                        continue;
                    }
                    playerZone.X = (int)pl.Entity.Pos.X / claims.config.ZONE_BLOCKS_LENGTH;
                    playerZone.Y = (int)pl.Entity.Pos.Z / claims.config.ZONE_BLOCKS_LENGTH;

                    List<Tuple<Vec2i, SavedPlotInfo>> updatePlotsForPlayer = new List<Tuple<Vec2i, SavedPlotInfo>>();
                    PlotPosition tmpPlotPosition = new PlotPosition();
                    foreach (var zone in PlotWhichShouldBeUpdated)
                    {
                        if ((playerZone.X - 2 < zone.Key.X && playerZone.X + 2 > zone.Key.X) &&
                            (playerZone.Y - 2 < zone.Key.Y && playerZone.Y + 2 > zone.Key.Y))
                        {
                            foreach (var it in zone.Value)
                            {
                                tmpPlotPosition.setXY(it);
                                if (claims.dataStorage.GetPlot(tmpPlotPosition, out Plot plot))
                                {
                                    
                                    updatePlotsForPlayer.Add(new Tuple<Vec2i, SavedPlotInfo>(plot.getPos(), new SavedPlotInfo((int)plot.Price, plot.getPermsHandler().pvpFlag,
                                    pl.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockDestroyWithOutCacheUpdate(playerInfo, plot),
                                    pl.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockUseWithOutCacheUpdate(playerInfo, plot),
                                    pl.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canAttackAnimalsWithOutCacheUpdate(playerInfo, plot),
                                    plot.getCity().GetPartName(), plot.GetPartName(),
                                    plot.hasCityPlotsGroup()
                                        ? plot.getPlotGroup().GetPartName()
                                        : "",
                                   plot.Type == PlotType.TAVERN
                                       ? plot.GetClientInnerClaimFromDefault(playerInfo)
                                       : null,
                                   plot.getCity().Alliance?.Guid ?? "")));
                                }
                            }
                        }
                    }
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
