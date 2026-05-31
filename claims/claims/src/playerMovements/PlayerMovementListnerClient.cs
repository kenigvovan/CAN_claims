using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using claims.src.auxialiry;
using claims.src.claimsext.map;
using claims.src.clientMapHandling;
using claims.src.commands.register;
using claims.src.network.packets;
using claims.src.part;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace claims.src.playerMovements
{
    public class PlayerMovementListnerClient
    {
        public Vec3i playerLastPos;
        IClientPlayer player;
        ClientMapDB mapdb;
        public PlayerMovementListnerClient()
        {
            playerLastPos = new Vec3i(-1, -1, -1);
            mapdb = new ClientMapDB(claims.capi.World.Logger);
            string errorMessage = null;
            string mapdbfilepath = this.getMapDbFilePath();
            this.mapdb.OpenOrCreate(mapdbfilepath, ref errorMessage, true, true, false);
            if (errorMessage != null)
            {
                throw new Exception(string.Format("Cannot open {0}, possibly corrupted. Please fix manually or delete this file to continue playing", mapdbfilepath));
            }
        }
        public string getMapDbFilePath()
        {
            string text = Path.Combine(GamePaths.DataPath, "clientSavedZones");
            GamePaths.EnsurePathExists(text);
            return Path.Combine(text, claims.capi.World.SavegameIdentifier + ".db");
        }
        public string getMsgForChunkChange(SavedPlotInfo fromPlot, SavedPlotInfo toPlot, int state)
        {
            StringBuilder stringBuilder = new StringBuilder();
            //state - 
            //0 both empty
            //1 from has smth
            //2 to has smth
            //3 both has smth

            //JUST FROM ONE EMPTY TO ANOTHER
            if (state == 0)
            {
                return "";
            }

            //FROM VILLAGE OR CITY TO EMPTY
            if (state == 1)
            {
                stringBuilder.Append("To wild lands.");
                return stringBuilder.ToString();
            }

            //FROM EMPTY TO VILLAGE OR CITY
            if (state == 2)
            {
                if (toPlot.cityName.Length > 0)
                {
                    stringBuilder.Append(StringFunctions.setStringColor(toPlot.cityName, ColorsClaims.DARK_GRAY));
                }


                if (toPlot.groupName.Length > 0)
                {
                    stringBuilder.Append(" ").Append(toPlot.groupName);
                }

                if (toPlot.plotName.Length > 0)
                {
                    stringBuilder.Append(" ").Append("~").Append(toPlot.plotName).Append("~");
                }
                if (toPlot.price != -1)
                {
                    stringBuilder.Append(" ").Append("To sell: " + toPlot.price.ToString());
                }
                if (toPlot.PvPIsOn)
                {
                    stringBuilder.Append(" ").Append(StringFunctions.setBold(StringFunctions.setStringColor("PVP", ColorsClaims.DARK_RED)));
                }
                else
                {
                    stringBuilder.Append(" ").Append(StringFunctions.setBold(StringFunctions.setStringColor("NO-PVP", ColorsClaims.WHITE)));
                }
            }

            //BOTH HAS VILLAGE OR CITY 
            if (state == 3)
            {
                if (toPlot.cityName.Equals(fromPlot.cityName))
                {
                    stringBuilder.Append(StringFunctions.setStringColor(toPlot.cityName, ColorsClaims.DARK_GRAY) + " ");
                }

                if (toPlot.groupName.Length > 0)
                {
                    stringBuilder.Append(" ").Append(toPlot.groupName);
                }

                if (toPlot.plotName.Length > 0)
                {
                    stringBuilder.Append(" ").Append("~").Append(toPlot.plotName).Append("~");
                }
                if (toPlot.price != -1)
                {
                    stringBuilder.Append(" ").Append("To sell: " + toPlot.price.ToString());
                }
                if (toPlot.PvPIsOn)
                {
                    stringBuilder.Append(" ").Append(StringFunctions.setBold(StringFunctions.setStringColor("PVP", ColorsClaims.DARK_RED)));
                }
                else
                {
                    stringBuilder.Append(" ").Append(StringFunctions.setBold(StringFunctions.setStringColor("NO-PVP", ColorsClaims.WHITE)));
                }
            }
            return stringBuilder.ToString();
        }
        public void onPlayerChangePlotEvent(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            Vec3i from = new Vec3i(tree.GetInt("xChO"), tree.GetInt("yChO"), tree.GetInt("zChO"));
            Vec3i to   = new Vec3i(tree.GetInt("xCh"),  tree.GetInt("yCh"),  tree.GetInt("zCh"));

            IPlayer pl = claims.capi.World.Player;
            if (pl == null)
            {
                return;
            }

            if ((int)(from.X / claims.config.ZONE_PLOTS_LENGTH) != (int)(to.X / claims.config.ZONE_PLOTS_LENGTH) ||
                (int)(from.Z / claims.config.ZONE_PLOTS_LENGTH) != (int)(to.Z / claims.config.ZONE_PLOTS_LENGTH))
            {
                handleZoneChange(new Vec2i(to.X, to.Z), pl);
            }

            claims.clientDataStorage.getSavedPlot(to, out SavedPlotInfo toPlot);
            claims.clientDataStorage.getSavedPlot(from, out SavedPlotInfo fromPlot);

            //TODO
            /*if (playerInfo.showBorders)
            {
                plotPosition.makeChunkHighlight(claims.sapi.World, claims.sapi.World.PlayerByUid(playerInfo.getGuid()), toPlot);
            }*/
            //STILL IN THE WILD LANDS

            string st;
            if (toPlot == null && fromPlot == null)
            {
                /*st = getMsgForChunkChange(fromPlot, toPlot, 0, playerInfo);
                if (st.Length != 0)
                {
                    if (playerInfo.showBorderMsgs)
                        MessageHandler.sendMsgToPlayerInfo(playerInfo, st);
                }*/
                return;
            }

            if (claims.clientDataStorage.clientPlayerInfo != null)
            { 
                if (toPlot != null && fromPlot != null)
                {
                    st = getMsgForChunkChange(fromPlot, toPlot, 3);
                    if (st.Length != 0)
                    {
                        if (claims.clientDataStorage.clientPlayerInfo.ShowPlotMovement == EnumShowPlotMovement.SHOW_MESSAGE 
                            || claims.clientDataStorage.clientPlayerInfo.ShowPlotMovement == EnumShowPlotMovement.SHOW_MESSAGE_HUD)
                        {
                            claims.capi.ShowChatMessage(st);
                        }

                    }
                    //SAME CITY
                    if (toPlot.cityName.Equals(fromPlot.cityName))
                    {
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                if (toPlot != null && fromPlot == null)
                {
                    st = getMsgForChunkChange(fromPlot, toPlot, 2);
                    if (st.Length != 0)
                    {
                        if (claims.clientDataStorage.clientPlayerInfo.ShowPlotMovement == EnumShowPlotMovement.SHOW_MESSAGE
                            || claims.clientDataStorage.clientPlayerInfo.ShowPlotMovement == EnumShowPlotMovement.SHOW_MESSAGE_HUD)
                        {
                            claims.capi.ShowChatMessage(st);
                        }
                    }
                    //claims.sapi.World.Api.Event.PushEvent("claimsPlayerEnterCity", tree);
                    return;
                }

                st = getMsgForChunkChange(fromPlot, toPlot, 1);
                if (st.Length != 0)
                {
                    if (claims.clientDataStorage.clientPlayerInfo.ShowPlotMovement == EnumShowPlotMovement.SHOW_MESSAGE 
                        || claims.clientDataStorage.clientPlayerInfo.ShowPlotMovement == EnumShowPlotMovement.SHOW_MESSAGE_HUD)
                    {
                        claims.capi.ShowChatMessage(st);
                    }
                }
             }
        }
        public void checkPlayerMove(float dt)
        {
            player = claims.capi.World.Player;
            bool _plotChanged = (playerLastPos.X / PlotPosition.plotSize != (int)(player.Entity.Pos.X / PlotPosition.plotSize))
                             || (playerLastPos.Z / PlotPosition.plotSize != (int)(player.Entity.Pos.Z / PlotPosition.plotSize))
                             || (claims.config.ENABLE_3D_PLOTS
                                 && playerLastPos.Y / PlotPosition.plotSize != (int)(player.Entity.Pos.Y / PlotPosition.plotSize));
            if (_plotChanged)
            {
                TreeAttribute tree = new TreeAttribute();
                tree.SetString("playerUID", player.PlayerUID);
                //new plot
                tree.SetInt("xCh",  (int)player.Entity.Pos.X / PlotPosition.plotSize);
                tree.SetInt("zCh",  (int)player.Entity.Pos.Z / PlotPosition.plotSize);
                tree.SetInt("yCh",  (int)player.Entity.Pos.Y / PlotPosition.plotSize);
                //old plot
                tree.SetInt("xChO", playerLastPos.X / PlotPosition.plotSize);
                tree.SetInt("zChO", playerLastPos.Z / PlotPosition.plotSize);
                tree.SetInt("yChO", playerLastPos.Y / PlotPosition.plotSize);
                claims.capi.World.Api.Event.PushEvent("claimsPlayerChangePlot", tree);
                playerLastPos.X = (int)player.Entity.Pos.X;
                playerLastPos.Y = (int)player.Entity.Pos.Y;
                playerLastPos.Z = (int)player.Entity.Pos.Z;
            }
        }
        // Send the 3x3 zone window around the player to the server.
        // Server diffs against player's current subscriptions and sends only newly subscribed zones.
        public void handleZoneChange(Vec2i toPlot, IPlayer player)
        {
            Vec2i centerZoneCoords = new Vec2i(toPlot.X / claims.config.ZONE_PLOTS_LENGTH,
                                               toPlot.Y / claims.config.ZONE_PLOTS_LENGTH);
            List<Tuple<Vec2i, long>> requestedZones = new List<Tuple<Vec2i, long>>();
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    requestedZones.Add(new Tuple<Vec2i, long>(new Vec2i(centerZoneCoords.X + i, centerZoneCoords.Y + j), 0));
                }
            }
            claims.clientChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.CLIENT_INFORM_ZONES_TIMESTAMPS,
                data = JsonConvert.SerializeObject(requestedZones)
            });
        }
        public void saveActiveZonesToDb()
        {
            mapdb.SetMapPieces(claims.clientDataStorage.getClientSavedPlots());         
        }

        //on join event player should check for already saved zones and load them from db
        //and send zones timestamps to server
        public void onPlayerJoin(/*IClientPlayer byPlayer*/)
        {
            IClientPlayer byPlayer = claims.capi.World.Player;
            if (byPlayer != null && claims.capi.World.Player == byPlayer)
            {
                claims.clientModInstance.plotsMapLayer = claims.capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<PlotsMapLayer>().FirstOrDefault();

                // Warm cache: load all previously saved zones from local SQLite into in-memory state
                // and draw them on the map. Server will overwrite zones in the current 3x3 window via subscription.
                LoadCachedZonesAndDraw();

                handleZoneChange(new Vec2i((int)byPlayer.Entity.Pos.X / claims.config.PLOT_SIZE,
                    (int)byPlayer.Entity.Pos.Z / claims.config.PLOT_SIZE), byPlayer);
                ClientCommands.RegisterCommands(claims.capi);
            }
        }
        private void LoadCachedZonesAndDraw()
        {
            if (mapdb == null) return;
            Dictionary<Vec2i, ClientSavedZone> cached;
            try
            {
                cached = mapdb.GetAllMapPieces();
            }
            catch (Exception)
            {
                return;
            }
            foreach (var pair in cached)
            {
                if (pair.Value == null) continue;
                if (claims.clientDataStorage.getClientSavedZone(pair.Key, out _)) continue;
                claims.clientDataStorage.addClientSavedZone(pair.Key, pair.Value);
                if (claims.clientModInstance.plotsMapLayer != null)
                {
                    claims.clientModInstance.plotsMapLayer.generateFromZoneSavedPlotsOnMap(pair.Key);
                }
            }
        }
        public void PeriodicSave(float dt)
        {
            if (mapdb == null) return;
            try
            {
                saveActiveZonesToDb();
            }
            catch (Exception)
            {
                // best effort; next tick will retry
            }
        }
        public void OnShutDown()
        {
            if (mapdb == null)
			{
				return;
			}
            mapdb.Dispose();
        }

    }
}
