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
        public void onPlayerChangePlotEvent(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            Vec2i from = new Vec2i(tree.GetInt("xChO"), tree.GetInt("zChO"));
            Vec2i to = new Vec2i(tree.GetInt("xCh"), tree.GetInt("zCh"));

            IPlayer pl = claims.capi.World.Player;
            if (pl == null)
            {
                return;
            }

            if ((int)(from.X / claims.config.ZONE_PLOTS_LENGTH) != (int)(to.X / claims.config.ZONE_PLOTS_LENGTH) ||
                (int)(from.Y / claims.config.ZONE_PLOTS_LENGTH) != (int)(to.Y / claims.config.ZONE_PLOTS_LENGTH))
            {
                handleZoneChange(to, pl);
            }
        }
        public void checkPlayerMove(float dt)
        {
            player = claims.capi.World.Player;
            if ((playerLastPos.X / PlotPosition.plotSize != (int)(player.Entity.Pos.X / PlotPosition.plotSize)) || playerLastPos.Z / PlotPosition.plotSize != (int)(player.Entity.Pos.Z / PlotPosition.plotSize))
            {
                TreeAttribute tree = new TreeAttribute();
                tree.SetString("playerUID", player.PlayerUID);
                //new plot
                tree.SetInt("xCh", (int)player.Entity.Pos.X / PlotPosition.plotSize);
                tree.SetInt("zCh", (int)player.Entity.Pos.Z / PlotPosition.plotSize);
                //old plot
                tree.SetInt("xChO", (int)playerLastPos.X / PlotPosition.plotSize);
                tree.SetInt("zChO", (int)playerLastPos.Z / PlotPosition.plotSize);
                claims.capi.World.Api.Event.PushEvent("claimsPlayerChangePlot", tree);
                playerLastPos.X = (int)player.Entity.Pos.X;
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
        /// <summary>Writes the zones that changed since the last save. Writing all of them meant
        /// serializing every zone the session ever saw - hundreds of them, up to a thousand plots
        /// each - on the main thread every 30 seconds.</summary>
        public void saveActiveZonesToDb()
        {
            Dictionary<Vec2i, ClientSavedZone> dirty = null;
            foreach (var pair in claims.clientDataStorage.getClientSavedPlots())
            {
                if (pair.Value == null || !pair.Value.Dirty) continue;
                if (dirty == null) dirty = new Dictionary<Vec2i, ClientSavedZone>();
                dirty[pair.Key] = pair.Value;
            }
            if (dirty == null) return;

            mapdb.SetMapPieces(dirty);
            // Only after the write went through - a throw leaves them marked for the next attempt.
            foreach (var pair in dirty)
            {
                pair.Value.Dirty = false;
            }
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
            // Whatever changed since the last periodic save, up to 30 seconds of it.
            try
            {
                saveActiveZonesToDb();
            }
            catch (Exception)
            {
                // Leaving with an unwritten zone only costs a re-fetch from the server next session.
            }
            mapdb.Dispose();
        }

    }
}
