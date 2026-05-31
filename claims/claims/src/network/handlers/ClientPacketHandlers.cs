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
                        Tuple<Vec3i, SavedPlotInfo> savedPlotTuple = JsonConvert.DeserializeObject<Tuple<Vec3i, SavedPlotInfo>>(packet.data);
                        claims.clientDataStorage.addClientSavedPlots(savedPlotTuple.Item1, savedPlotTuple.Item2);
                        claims.clientModInstance.plotsMapLayer.OnResChunkPixels(new Vec2i(savedPlotTuple.Item1.X, savedPlotTuple.Item1.Z), savedPlotTuple.Item2.cityName);
                        break;
                    case PacketsContentEnum.REMOVE_SINGLE_PLOT:
                        Tuple<Vec3i, SavedPlotInfo> savedPlotTupleRemove = JsonConvert.DeserializeObject<Tuple<Vec3i, SavedPlotInfo>>(packet.data);
                        claims.clientDataStorage.removeClientSavedPlots(savedPlotTupleRemove.Item1);
                        claims.clientModInstance.plotsMapLayer.OnResChunkPixels(new Vec2i(savedPlotTupleRemove.Item1.X, savedPlotTupleRemove.Item1.Z), "");
                        break;
                    case PacketsContentEnum.ALL_CITY_COLORS:
                        Dictionary<string, int> colors = JsonConvert.DeserializeObject<Dictionary<string, int>>(packet.data);
                        claims.clientDataStorage.ClientSetCityNameToColorDict(colors);
                        //claims.getModInstance().plotsMapLayer.RedrawPlots();
                        break;
                    case PacketsContentEnum.SERVER_UPDATED_ZONES_ANSWER:
                        HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec3i, SavedPlotInfo>>>> updatedZones = JsonConvert.DeserializeObject<HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec3i, SavedPlotInfo>>>>>(packet.data);
                        foreach (var tup in updatedZones)
                        {
                            if (claims.clientDataStorage.getClientSavedZone(tup.Item1, out var savedZone))
                            {
                                claims.clientModInstance.plotsMapLayer.clearZoneSavedPlotsFromMap(tup.Item1);
                                savedZone.savedPlots = tup.Item3.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Last().Value);
                                savedZone.timestamp = tup.Item2;
                                claims.clientModInstance.plotsMapLayer.generateFromZoneSavedPlotsOnMap(tup.Item1);
                            }
                            else
                            {
                                ClientSavedZone newZone = new ClientSavedZone(tup.Item3.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Last().Value));
                                newZone.timestamp = tup.Item2;
                                claims.clientDataStorage.addClientSavedZone(tup.Item1, newZone);
                                claims.clientModInstance.plotsMapLayer.generateFromZoneSavedPlotsOnMap(tup.Item1);
                            }
                        }
                        break;
                    case PacketsContentEnum.SERVER_REMOVE_COLLECTED_PLOTS:
                        HashSet<Vec3i> plotsToRemove = JsonConvert.DeserializeObject<HashSet<Vec3i>>(packet.data);
                        foreach (var savedPlot in plotsToRemove)
                        {
                            claims.clientDataStorage.removeClientSavedPlots(savedPlot);
                            claims.clientModInstance.plotsMapLayer.OnResChunkPixels(new Vec2i(savedPlot.X, savedPlot.Z), "");
                        }
                        break;
                    case PacketsContentEnum.SERVER_UPDATE_COLLECTED_PLOTS:
                        List<Tuple<Vec3i, SavedPlotInfo>> plotsToUpdate = JsonConvert.DeserializeObject<List<Tuple<Vec3i, SavedPlotInfo>>>(packet.data);
                        foreach (var savedPlot in plotsToUpdate)
                        {
                            claims.clientDataStorage.addClientSavedPlots(savedPlot.Item1, savedPlot.Item2);
                            claims.clientModInstance.plotsMapLayer.OnResChunkPixels(new Vec2i(savedPlot.Item1.X, savedPlot.Item1.Z), savedPlot.Item2?.cityName ?? "");
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
                        claims.CANCityGui.CreateNewCityState = gui.playerGui.CANClaimsGui.EnumUpperWindowSelectedState.NEED_AGREE;
                        claims.CANCityGui.collectedNewCityName = packet.data;
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
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
                            if(claims.CANCityGui.SelectedTab == gui.playerGui.CANClaimsGui.EnumSelectedTab.ConflictInfoPage)
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
                if (!claims.config.BalanceHudOverride.HasValue)
                    claimsGui.showBalanceHud = packet.SHOW_BALANCE_HUD_DEFAULT;
                if (claims.config.AVAILABLE_CITY_PERMISSIONS == null)
                {
                    claims.config.AVAILABLE_CITY_PERMISSIONS = new();
                }
                if(claims.config.ALWAYS_ACCESS_BLOCKS.Count > 0)
                {
                    claims.FindAlwaysUseBlocks(claims.capi);
                }
            });          
        }
    }
}
