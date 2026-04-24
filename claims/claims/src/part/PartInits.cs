using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part
{
    public class PartInits
    {
        public static void initNewCity(PlayerInfo creator, PlotPosition pos, string cityName)
        {
            string guid;

            while (true)
            {
                guid = Guid.NewGuid().ToString();
                if (claims.dataStorage.checkGuidForCityVillage(guid))
                    break;
            }
            City city = new City(cityName, guid);
            claims.dataStorage.addCity(city);
            // DataStorage.nameToCityDict.TryAdd(cityName, city);

            if (creator != null)
            {
                creator.setCity(city);
                city.setMayor(creator);
                city.getCityCitizens().Add(creator);
                city.setIsTechnicalCity(false);
            }
            else
            {
                city.setIsTechnicalCity(true);
            }
            city.TimeStampCreated = TimeFunctions.getEpochSeconds();

            Plot newPlot = new Plot(pos);
            newPlot.Price = -1;
            newPlot.setCity(city);
            newPlot.Type = PlotType.MAIN_CITY_PLOT;
            claims.dataStorage.addClaimedPlot(newPlot.plotPosition, newPlot);
            city.getCityPlots().Add(newPlot);
            if (creator != null)
            {
                creator.saveToDatabase();
            }

            var perms = city.getPermsHandler();
            perms.CitizenPerms[0] = true;
            perms.CitizenPerms[1] = true;
            perms.CitizenPerms[2] = true;

            city.saveToDatabase();
            newPlot.saveToDatabase();
            claims.dataStorage.ClearCacheForPlayersInPlot(newPlot);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(newPlot.getPos());
            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(newPlot.getPos());

            MessageHandler.sendGlobalMsg(Lang.Get("claims:new_city_created", StringFunctions.replaceUnderscore(cityName), creator != null ? creator.GetPartName() : ""));
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", newPlot.getPos().X);
            tree.SetInt("chZ", newPlot.getPos().Y);
            tree.SetString("name", newPlot.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            if (creator != null)
            {
                RightsHandler.reapplyRights(creator);
            }

            var player = creator != null ? claims.sapi.World.PlayerByUid(creator.Guid) : null;
            if (player != null)
            {

                Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                {
                    { EnumPlayerRelatedInfo.CITY_NAME, city.GetPartName() }
                };
                if (city.getMayor() != null)
                {
                    collector.Add(EnumPlayerRelatedInfo.MAYOR_NAME, city.getMayor().GetPartName());
                }
                collector.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, city.TimeStampCreated.ToString());
                collector.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)));
                collector.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, JsonConvert.SerializeObject(Settings.getPossibleAmountOfPlotsDictForCity(city)));
                collector.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, city.getCityPlots().Count.ToString());
                collector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, creator.Prefix);
                collector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, creator.AfterName);
                collector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(creator.getCityTitles()));
                collector.Add(EnumPlayerRelatedInfo.SHOW_PLOT_MOVEMENT, ((int)creator.showPlotMovement).ToString());
                collector.Add(EnumPlayerRelatedInfo.CITY_PERMISSIONS_UPDATED, JsonConvert.SerializeObject(city.getPermsHandler()));
                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.OWN_NEW_CITY_CREATED,
                    data = JsonConvert.SerializeObject(collector)

                }, player as IServerPlayer);
            }
            if (caneconomy.caneconomy.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
            {
                claims.economyHandler.newAccount(city.MoneyAccountName, new Dictionary<string, object> { { "lastknownname", city.GetPartName() } });
            }
            return;
        }
        public static void initPrison(Plot plot, City city, IServerPlayer creator)
        {
            Guid guid;
            while (true)
            {
                guid = Guid.NewGuid();
                if (claims.dataStorage.prisonExistsByGUID(guid.ToString()))
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            plot.Type = PlotType.PRISON;
            plot.Prison = new Prison("", guid.ToString());
            claims.dataStorage.addPrison(plot.Prison);
            plot.Prison.addPrisonCell(new PrisonCellInfo(new Vec3i((int)creator.Entity.Pos.X, (int)creator.Entity.Pos.Y, (int)creator.Entity.Pos.Z)));
            plot.getCity().getPrisons().Add(plot.Prison);
            plot.Prison.Plot = plot;
            plot.Prison.City = plot.getCity();
            PlotDescPrison pdp = new PlotDescPrison(plot.Prison.Guid);
            plot.PlotDesc = pdp;

            plot.getCity().saveToDatabase();
            plot.saveToDatabase();
            plot.Prison.saveToDatabase();
        }
        public static void InitNewAlliance(PlayerInfo creator, string allianceName)
        {
            Guid guid;
            while (true)
            {
                guid = Guid.NewGuid();
                if (claims.dataStorage.AllianceExistsByGUID(guid.ToString()))
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            Alliance newAlliace = new Alliance(allianceName, guid.ToString());
            //DataStorage.nameToAllianceDict.TryAdd(newAlliace.getPartName(), newAlliace);
            claims.dataStorage.addAlliance(newAlliace);
            newAlliace.Leader = creator;
            newAlliace.MainCity = creator.City;
            newAlliace.Cities.Add(creator.City);
            creator.City.Alliance = newAlliace;
            newAlliace.TimeStampCreated = TimeFunctions.getEpochSeconds();
            if (caneconomy.caneconomy.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
            {
                claims.economyHandler.newAccount(newAlliace.MoneyAccountName, new Dictionary<string, object> { { "lastknownname", newAlliace.GetPartName() } });
            }
            foreach (var city in newAlliace.Cities)
            {
                foreach (var it in city.getCityCitizens())
                {
                    RightsHandler.reapplyRights(it);
                }
            }
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(newAlliace.Guid, new Dictionary<string, object> { { "value", newAlliace.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);
            creator.City.saveToDatabase();
            newAlliace.saveToDatabase(false);
        }
        public static void InitNewUnion(Alliance first, Alliance second)
        {
            first.ComradAlliancies.Add(second);
            second.ComradAlliancies.Add(first);
            foreach (var city in first.Cities)
            {
                foreach(var sCity in second.Cities)
                {
                    city.ComradeCities.Add(sCity);
                    city.saveToDatabase();
                }
            }
            foreach (var city in second.Cities)
            {
                foreach(var sCity in first.Cities)
                {
                    city.ComradeCities.Add(sCity);
                    city.saveToDatabase();
                }
            }
            first.saveToDatabase();
            second.saveToDatabase();
            //UsefullPacketsSend.AddToQueueUnionInfoUpdate(newUnion.Guid, new Dictionary<string, object> { { "value", newUnion.Guid } }, EnumPlayerRelatedInfo.NEW_UNION_ALL);
        }
    }
}
