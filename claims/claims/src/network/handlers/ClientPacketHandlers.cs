using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.gui.playerGui.structures;
using claims.src.network.packets;
using claims.src.playerMovements;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using claims.src;
using claims.src.part.structure.plots;
using claims.src.claimsext.map;
using Vintagestory.GameContent;

namespace claims.src.network.handlers
{
    public static class ClientPacketHandlers
    {
        public static void RegisterHandlers()
        {
            claims.clientChannel.SetMessageHandler<SavedPlotsPacket>((packet) =>
            {
                switch (packet.type)
                {
                    case PacketsContentEnum.ADD_SINGLE_PLOT:
                        Tuple<Vec2i, SavedPlotInfo> savedPlotTuple = JsonConvert.DeserializeObject<Tuple<Vec2i, SavedPlotInfo>>(packet.data);
                        // Guard against a malformed payload or a packet arriving before the client is ready.
                        if (savedPlotTuple?.Item1 == null || claims.clientDataStorage == null) break;
                        claims.clientDataStorage.addClientSavedPlots(savedPlotTuple.Item1, savedPlotTuple.Item2);
                        ResolvePlotsMapLayer()?.OnResChunkPixels(savedPlotTuple.Item1, savedPlotTuple.Item2?.cityName);
                        break;
                    case PacketsContentEnum.REMOVE_SINGLE_PLOT:
                        //try to send saved plot as null without creating object
                        Tuple<Vec2i, SavedPlotInfo> savedPlotTupleRemove = JsonConvert.DeserializeObject<Tuple<Vec2i, SavedPlotInfo>>(packet.data);
                        if (savedPlotTupleRemove?.Item1 == null || claims.clientDataStorage == null) break;
                        claims.clientDataStorage.removeClientSavedPlots(savedPlotTupleRemove.Item1);
                        ResolvePlotsMapLayer()?.OnResChunkPixels(savedPlotTupleRemove.Item1, "");
                        break;
                    case PacketsContentEnum.ALL_CITY_EMBLEMS:
                        var emblems = JsonConvert.DeserializeObject<Dictionary<string, string>>(packet.data);
                        if (claims.clientDataStorage == null) break;
                        // Only when something actually changed: dropping the textures makes the map
                        // recompose every icon, and this packet is broadcast on every emblem edit.
                        if (claims.clientDataStorage.ClientSetCityEmblems(emblems))
                        {
                            gui.playerGui.GuiElements.EmblemCache.Invalidate();
                        }
                        break;
                    case PacketsContentEnum.ALL_CITY_COLORS:
                        Dictionary<string, int> colors = JsonConvert.DeserializeObject<Dictionary<string, int>>(packet.data);
                        claims.clientDataStorage.ClientSetCityNameToColorDict(colors);
                        //claims.getModInstance().plotsMapLayer.RedrawPlots();
                        break;
                    case PacketsContentEnum.SERVER_UPDATED_ZONES_ANSWER:
                        HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>> updatedZones = JsonConvert.DeserializeObject<HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>>>(packet.data);
                        //we got new zones info from server
                        // Bail out if the payload or the client storage isn't ready yet; this packet can arrive
                        // before the client is fully initialized.
                        if (updatedZones == null || claims.clientDataStorage == null) break;
                        // The map layer may not be resolved yet if this answer arrives before onPlayerJoin;
                        // resolve it lazily and guard the draw calls so zone data still updates without it.
                        PlotsMapLayer zonesMapLayer = ResolvePlotsMapLayer();
                        foreach (var tup in updatedZones)
                        {
                            if (tup?.Item1 == null) continue;
                            Dictionary<Vec2i, SavedPlotInfo> zonePlots = (tup.Item3 ?? new List<KeyValuePair<Vec2i, SavedPlotInfo>>())
                                .GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Last().Value);
                            //if zone was already known, but updated info from server arrived
                            if (claims.clientDataStorage.getClientSavedZone(tup.Item1, out var savedZone) && savedZone != null)
                            {
                                zonesMapLayer?.clearZoneSavedPlotsFromMap(tup.Item1);
                                savedZone.savedPlots = zonePlots;
                                savedZone.timestamp = tup.Item2;
                                zonesMapLayer?.generateFromZoneSavedPlotsOnMap(tup.Item1);
                            }
                            //no such zone, reset it
                            else
                            {
                                ClientSavedZone newZone = new ClientSavedZone(zonePlots);
                                newZone.timestamp = tup.Item2;
                                claims.clientDataStorage.addClientSavedZone(tup.Item1, newZone);
                                zonesMapLayer?.generateFromZoneSavedPlotsOnMap(tup.Item1);
                            }
                        }
                        break;
                    case PacketsContentEnum.SERVER_REMOVE_COLLECTED_PLOTS:
                        HashSet<Vec2i> plotsToRemove = JsonConvert.DeserializeObject<HashSet<Vec2i>>(packet.data);
                        if (plotsToRemove == null || claims.clientDataStorage == null) break;
                        PlotsMapLayer removeMapLayer = ResolvePlotsMapLayer();
                        foreach (var savedPlot in plotsToRemove)
                        {
                            if (savedPlot == null) continue;
                            claims.clientDataStorage.removeClientSavedPlots(savedPlot);
                            removeMapLayer?.OnResChunkPixels(savedPlot, "");
                        }
                        break;
                    case PacketsContentEnum.SERVER_UPDATE_COLLECTED_PLOTS:
                        List<Tuple<Vec2i, SavedPlotInfo>> plotsToUpdate = JsonConvert.DeserializeObject<List<Tuple<Vec2i, SavedPlotInfo>>>(packet.data);
                        if (plotsToUpdate == null || claims.clientDataStorage == null) break;
                        PlotsMapLayer updateMapLayer = ResolvePlotsMapLayer();
                        foreach (var savedPlot in plotsToUpdate)
                        {
                            if (savedPlot?.Item1 == null) continue;
                            claims.clientDataStorage.addClientSavedPlots(savedPlot.Item1, savedPlot.Item2);
                            // Redraw here as well: CITY_PLOT_RECOLOR travels on a separate queue and may arrive
                            // BEFORE this packet, in which case it would repaint the plot (and its borders) from
                            // the stale city name and nothing would ever refresh it again.
                            updateMapLayer?.OnResChunkPixels(savedPlot.Item1, savedPlot.Item2?.cityName);
                        }
                        break;
                    case PacketsContentEnum.OWN_CITY_DELETED:
                        claims.clientDataStorage.clientPlayerInfo.CityInfo = null;
                        if(claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.AGREE_NEEDED_ON_NEW_CITY_CREATION:
                        //TODO Delete after
                        claims.capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = gui.prettyGui.EnumSecondaryWindowTab.NEED_AGREE;
                        if (claims.CANCityGui != null)
                        {
                            // The proposed city name rides along in Text so the dialog can name it.
                            claims.CANCityGui.OpenDialog(gui.playerGui.EnumUpperWindowSelectedState.NEED_AGREE,
                                args => args.Text = packet.data);
                            if (claims.CANCityGui.IsOpened())
                            {
                                claims.CANCityGui.BuildMainWindow();
                            }
                        }
                        break;
                    case PacketsContentEnum.OWN_CITY_INFO_ON_JOIN:
                    case PacketsContentEnum.OWN_NEW_CITY_CREATED:
                        var cityValuesDict = JsonConvert.DeserializeObject<Dictionary<EnumPlayerRelatedInfo, string>>(packet.data);
                        claims.clientDataStorage.clientPlayerInfo = new ClientPlayerInfo();

                        //if player has city there will be name of it and we need to create instance of lists for cityinfo
                        if(cityValuesDict.ContainsKey(EnumPlayerRelatedInfo.CITY_NAME) && claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
                        {
                            claims.clientDataStorage.clientPlayerInfo.CityInfo = new CityInfo();
                        }
                        claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(cityValuesDict);
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ON_CITY_JOINED:
                        var joinedValues = JsonConvert.DeserializeObject<Dictionary<EnumPlayerRelatedInfo, string>>(packet.data);
                        if(claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
                        {
                            claims.clientDataStorage.clientPlayerInfo.CityInfo = new CityInfo();
                        }
                        claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(joinedValues);
                        claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Clear();
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ON_KICKED_FROM_CITY:
                        claims.clientDataStorage.clientPlayerInfo.CityInfo = null;
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED:
                        var someUpdateDict = JsonConvert.DeserializeObject<Dictionary<EnumPlayerRelatedInfo, string>>(packet.data);
                        if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
                        {
                            claims.clientDataStorage.clientPlayerInfo.CityInfo = new CityInfo();
                        }
                        claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(someUpdateDict);
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            if(claims.CANCityGui.SelectedTab == gui.playerGui.EnumSelectedTab.ConflictInfoPage)
                            {
                                claims.CANCityGui.SelectRangeAndFill();
                            }
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.CURRENT_PLOT_INFO:
                        claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo = JsonConvert.DeserializeObject<CurrentPlotInfo>(packet.data);
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ADMIN_CITY_FLAGS_ALL:
                        var adminData = JsonConvert.DeserializeObject<AdminDataPacket>(packet.data);
                        AdminClientState.Accept(adminData);
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                }
            });
            claims.clientChannel.SetMessageHandler<PlayerGuiRelatedInfoPacket>((packet) =>
            {
                claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(packet.playerGuiRelatedInfoDictionary);
                if (claims.CANCityGui?.IsOpened() ?? false)
                {
                    claims.CANCityGui.BuildMainWindow();
                }
            });
            claims.clientChannel.SetMessageHandler<ConfigUpdateValuesPacket>((packet) =>
            {
                claims.config.NEW_CITY_COST = packet.NewCityCost;
                claims.config.PLOT_CLAIM_PRICE = packet.NewPlotClaimCost;
                claims.config.COINS_VALUES_TO_CODE = packet.COINS_VALUES_TO_CODE;
                claims.config.ID_TO_COINS_VALUES = packet.ID_TO_COINS_VALUES;
                claims.config.COIN_DENOMINATIONS = packet.COIN_DENOMINATIONS;
                claims.config.CITY_NAME_CHANGE_COST = packet.CITY_NAME_CHANGE_COST;
                claims.config.CITY_BASE_CARE = packet.CITY_BASE_CARE;
                claims.config.PLOT_COLORS = packet.PLOTS_COLORS;
                claims.config.NEW_ALLIANCE_COST = packet.NewAllianceCost;
                claims.config.SUMMON_PAYMENT = packet.SummonPayment;
                claims.config.ALWAYS_ACCESS_BLOCKS = packet.ALWAYS_ACCESS_BLOCKS;
                claims.config.AVAILABLE_CITY_PERMISSIONS = packet.AVAILABLE_CITY_PERMISSIONS;
                claims.config.SELECTED_ECONOMY_HANDLER = packet.SELECTED_ECONOMY_HANDLER;
                claims.config.GUI_SHOW_DEBT = packet.GUI_SHOW_DEBT;
                claims.config.CITY_AREA_VISIBILITY_STATE = packet.CITY_AREA_VISIBILITY_STATE;
                claims.config.SHOW_BALANCE_HUD_DEFAULT = packet.SHOW_BALANCE_HUD_DEFAULT;

                claims.config.DEFAULT_PLOT_COST = packet.DEFAULT_PLOT_COST;
                claims.config.TOURNAMENT_PLOT_COST = packet.TOURNAMENT_PLOT_COST;
                claims.config.CAMP_PLOT_COST = packet.CAMP_PLOT_COST;
                claims.config.TEMPLE_PLOT_COST = packet.TEMPLE_PLOT_COST;
                claims.config.FARM_PLOT_COST = packet.FARM_PLOT_COST;
                claims.config.SUMMON_PLOT_COST = packet.SUMMON_PLOT_COST;
                claims.config.EMBASSY_PLOT_COST = packet.EMBASSY_PLOT_COST;
                claims.config.TAVERN_PLOT_COST = packet.TAVERN_PLOT_COST;
                claims.config.MAIN_CITYPLOT_COST = packet.MAIN_CITYPLOT_COST;
                claims.config.PRISON_PLOT_COST = packet.PRISON_PLOT_COST;

                claims.config.DISABLED_PLOT_TYPES = packet.DISABLED_PLOT_TYPES ?? new HashSet<string>();

                claims.config.OUTPOST_PLOT_COST = packet.OUTPOST_PLOT_COST;
                claims.config.EXTRA_PLOT_COST = packet.EXTRA_PLOT_COST;
                claims.config.PLOT_NO_PVP_FLAG_COST = packet.PLOT_NO_PVP_FLAG_COST;

                claims.config.RANSOM_FOR_NO_CITIZEN = packet.RANSOM_FOR_NO_CITIZEN;
                claims.config.RANSOM_FOR_CITIZEN = packet.RANSOM_FOR_CITIZEN;
                claims.config.RANSOM_FOR_MAYOR = packet.RANSOM_FOR_MAYOR;
                claims.config.RANSOM_FOR_LEADER = packet.RANSOM_FOR_LEADER;
                claims.config.RANSOM_FOR_CHIEF = packet.RANSOM_FOR_CHIEF;

                claims.config.ALLIANCE_RENAME_COST = packet.ALLIANCE_RENAME_COST;
                claims.config.ALLIANCE_BASE_CARE = packet.ALLIANCE_BASE_CARE;
                claims.config.ALLIANCE_MAX_FEE = packet.ALLIANCE_MAX_FEE;
                claims.config.NEUTRAL_ALLANCE_PAYMENT = packet.NEUTRAL_ALLANCE_PAYMENT;

                claims.config.MAX_CITY_FEE = packet.MAX_CITY_FEE;
                claims.config.CITY_MAX_DEBT = packet.CITY_MAX_DEBT;

                claims.config.MIN_RANGE_CELL_DURATION_MINUTES = packet.MIN_RANGE_CELL_DURATION_MINUTES;

                claims.config.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED = packet.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED;
                claims.config.WAR_FLAG_DEFENDER_RADIUS = packet.WAR_FLAG_DEFENDER_RADIUS;
                claims.config.WAR_FLAG_REGRESS_MULTIPLIER = packet.WAR_FLAG_REGRESS_MULTIPLIER;

                claims.config.WAR_SCORE_ENABLED = packet.WAR_SCORE_ENABLED;
                claims.config.WAR_SCORE_TO_WIN = packet.WAR_SCORE_TO_WIN;
                claims.config.WAR_SCORE_PER_PLOT_CAPTURE = packet.WAR_SCORE_PER_PLOT_CAPTURE;
                claims.config.WAR_SCORE_PER_KILL = packet.WAR_SCORE_PER_KILL;
                claims.config.WAR_SCORE_PER_HOLD_TICK = packet.WAR_SCORE_PER_HOLD_TICK;
                claims.config.WAR_SCORE_HOLD_TICK_SECONDS = packet.WAR_SCORE_HOLD_TICK_SECONDS;

                claims.config.WAR_PILLAGE_ENABLED = packet.WAR_PILLAGE_ENABLED;
                claims.config.WAR_PILLAGE_PERCENT = packet.WAR_PILLAGE_PERCENT;

                claims.config.WAR_CAMP_ENABLED = packet.WAR_CAMP_ENABLED;
                claims.config.WAR_MAX_CAMPS_PER_CONFLICT = packet.WAR_MAX_CAMPS_PER_CONFLICT;
                claims.config.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY = packet.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY;
                claims.config.WAR_CAMP_ANCHOR_BREAKS = packet.WAR_CAMP_ANCHOR_BREAKS;
                claims.config.WAR_NAP_ENABLED = packet.WAR_NAP_ENABLED;
                claims.config.WAR_NAP_DEFAULT_DAYS = packet.WAR_NAP_DEFAULT_DAYS;
                claims.config.WAR_NAP_MAX_DAYS = packet.WAR_NAP_MAX_DAYS;
                claims.config.WAR_NAP_BREAK_PENALTY = packet.WAR_NAP_BREAK_PENALTY;
                claims.config.WAR_BATTLE_WARN_MINUTES = packet.WAR_BATTLE_WARN_MINUTES;
                claims.config.WAR_ULTIMATUM_ENABLED = packet.WAR_ULTIMATUM_ENABLED;
                if (packet.WAR_ULTIMATUM_EXPIRE_HOURS > 0)
                    claims.config.WAR_ULTIMATUM_EXPIRE_HOURS = packet.WAR_ULTIMATUM_EXPIRE_HOURS;

                claims.config.WAR_RESPAWN_SAFEZONE_ENABLED = packet.WAR_RESPAWN_SAFEZONE_ENABLED;
                claims.config.WAR_RESPAWN_SAFEZONE_RADIUS = packet.WAR_RESPAWN_SAFEZONE_RADIUS;
                claims.config.WAR_RESPAWN_SAFEZONE_SECONDS = packet.WAR_RESPAWN_SAFEZONE_SECONDS;

                claims.config.WAR_SIEGE_ENABLED = packet.WAR_SIEGE_ENABLED;
                claims.config.WAR_SIEGE_RAM_TICK_SECONDS = packet.WAR_SIEGE_RAM_TICK_SECONDS;
                claims.config.WAR_SIEGE_RAM_RANGE = packet.WAR_SIEGE_RAM_RANGE;
                claims.config.WAR_SIEGE_RAM_COST = packet.WAR_SIEGE_RAM_COST;

                claims.config.FLAG_CAPTURE_DURATION_SECONDS = packet.FLAG_CAPTURE_DURATION_SECONDS;
                claims.config.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE = packet.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE;
                claims.config.FLAG_REINFORCEMENT_AMOUNT = packet.FLAG_REINFORCEMENT_AMOUNT;
                claims.config.MINIMUM_DAYS_BETWEEN_BATTLES = packet.MINIMUM_DAYS_BETWEEN_BATTLES;

                claims.config.WAR_DECLARATION_COST = packet.WAR_DECLARATION_COST;
                claims.config.WAR_REDECLARE_COOLDOWN_DAYS = packet.WAR_REDECLARE_COOLDOWN_DAYS;
                claims.config.WAR_REQUIRE_CASUS_BELLI = packet.WAR_REQUIRE_CASUS_BELLI;
                claims.config.WAR_CASUS_BELLI_GRACE_DAYS = packet.WAR_CASUS_BELLI_GRACE_DAYS;

                claims.config.WAR_PEACE_TERMS_ENABLED = packet.WAR_PEACE_TERMS_ENABLED;
                claims.config.WAR_VASSAL_TRIBUTE = packet.WAR_VASSAL_TRIBUTE;
                claims.config.WAR_VASSAL_DURATION_DAYS = packet.WAR_VASSAL_DURATION_DAYS;

                claims.config.WAR_BOUNTY_ENABLED = packet.WAR_BOUNTY_ENABLED;
                claims.config.WAR_BOUNTY_MIN = packet.WAR_BOUNTY_MIN;
                claims.config.WAR_PLUNDER_ON_KILL_ENABLED = packet.WAR_PLUNDER_ON_KILL_ENABLED;
                claims.config.WAR_PLUNDER_ON_KILL_PERCENT = packet.WAR_PLUNDER_ON_KILL_PERCENT;

                claims.config.WAR_REPORT_ENABLED = packet.WAR_REPORT_ENABLED;
                claims.config.WAR_HUD_ENABLED = packet.WAR_HUD_ENABLED;

                // World grid geometry must follow the server; 0 = packet from an older
                // server that doesn't send it, keep the local config value then.
                bool gridChanged = false;
                if (packet.PLOT_SIZE > 0 && packet.PLOT_SIZE != claims.config.PLOT_SIZE)
                {
                    claims.config.PLOT_SIZE = packet.PLOT_SIZE;
                    gridChanged = true;
                }
                if (packet.ZONE_PLOTS_LENGTH > 0 && packet.ZONE_PLOTS_LENGTH != claims.config.ZONE_PLOTS_LENGTH)
                {
                    claims.config.ZONE_PLOTS_LENGTH = packet.ZONE_PLOTS_LENGTH;
                    gridChanged = true;
                }
                if (gridChanged)
                {
                    // Anything already drawn used the old grid — rebuild map textures.
                    ResolvePlotsMapLayer()?.RedrawPlots();
                }

                // Plot type costs are captured into dictPlotTypes at client startup
                // (before this packet arrives), so rebuild it with the synced values.
                PlotInfo.initDicts();

                if (!claims.config.BalanceHudOverride.HasValue)
                    gui.hud.ClaimsHudState.ShowBalance = packet.SHOW_BALANCE_HUD_DEFAULT;
                if (claims.config.AVAILABLE_CITY_PERMISSIONS == null)
                {
                    claims.config.AVAILABLE_CITY_PERMISSIONS = new();
                }
                if(claims.config.ALWAYS_ACCESS_BLOCKS.Count > 0)
                {
                    claims.FindAlwaysUseBlocks(claims.capi);
                }
            });
            claims.clientChannel.SetMessageHandler<BountyBoardPacket>((packet) =>
            {
                gui.hud.ClaimsHudState.BountyBoard = packet.Entries ?? new System.Collections.Generic.List<BountyBoardEntry>();
            });
        }

        // Resolves the client plots map layer, lazily fetching it from the WorldMapManager when the
        // mod instance hasn't cached it yet (e.g. a plots packet arrives before onPlayerJoin). Returns
        // null when the map system isn't available yet; callers must null-guard the result.
        private static PlotsMapLayer ResolvePlotsMapLayer()
        {
            PlotsMapLayer layer = claims.clientModInstance?.plotsMapLayer;
            if (layer == null)
            {
                layer = claims.capi?.ModLoader?.GetModSystem<WorldMapManager>()?.MapLayers?.OfType<PlotsMapLayer>().FirstOrDefault();
                if (layer != null && claims.clientModInstance != null)
                    claims.clientModInstance.plotsMapLayer = layer;
            }
            return layer;
        }
    }
}
