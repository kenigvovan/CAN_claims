using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part
{
    public class PartInits
    {
        public static void initNewCity(PlayerInfo creator, PlotPosition pos, string cityName,
            CityTier tier = CityTier.CITY)
        {
            string guid;

            while (true)
            {
                guid = Guid.NewGuid().ToString();
                if (claims.dataStorage.checkGuidForCityVillage(guid))
                    break;
            }
            City city = new City(cityName, guid) { Tier = tier };
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
            newPlot.Type = tier == CityTier.VILLAGE ? PlotType.VILLAGE_MAIN : PlotType.MAIN_CITY_PLOT;
            if (tier == CityTier.VILLAGE)
            {
                PlaceVillageAnchor(city, newPlot);
            }
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

            city.AddLogEntry(tier == CityTier.VILLAGE ? EnumCityLogEvent.VillageFounded : EnumCityLogEvent.CityCreated,
                creator?.GetPartName() ?? "");
            City.FireCityCreated(city);
            city.saveToDatabase();
            newPlot.saveToDatabase();
            claims.dataStorage.ClearCacheForPlayersInPlot(newPlot);
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(newPlot.getPos());

            MessageHandler.sendGlobalMsg(Lang.Get(tier == CityTier.VILLAGE ? "claims:new_village_created" : "claims:new_city_created",
                StringFunctions.replaceUnderscore(cityName), creator != null ? creator.GetPartName() : ""));
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
                    { EnumPlayerRelatedInfo.CITY_NAME, city.GetPartName() },
                    // This packet rebuilds CityInfo from nothing, and the guid is only ever sent on
                    // login otherwise: without it the client cannot tell that a letter addressed to
                    // this city is addressed to us, and the answer buttons go missing.
                    { EnumPlayerRelatedInfo.CITY_GUID, city.Guid }
                };
                if (city.getMayor() != null)
                {
                    collector.Add(EnumPlayerRelatedInfo.MAYOR_NAME, city.getMayor().GetPartName());
                }
                collector.Add(EnumPlayerRelatedInfo.CITY_TIER, ((int)city.Tier).ToString());
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

                // The founder's CityInfo is built fresh by this packet, and the land market is not
                // part of it - without this their market tab stays empty until some other city
                // changes a listing.
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                    EnumPlayerRelatedInfo.CITY_PLOT_AUCTIONS, EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY);
            }
            claims.economyProvider.NewAccount(city.MoneyAccountName, new Dictionary<string, object> { { "lastknownname", city.GetPartName() } });
            return;
        }
        /// <summary>
        /// Puts the village anchor in the middle of the main plot, on the surface, and registers it
        /// as a respawn point of the settlement - that way the vanilla respawn patch picks villages
        /// up without knowing anything about them.
        /// </summary>
        /// <summary>
        /// Gives a village an anchor when it has none - the case after an admin downgrades a city
        /// with /cadmin city set tier. Without it the settlement would be immortal (the supply timer
        /// skips it) and yet permanently exposed, because the raid window is computed from the
        /// founding time alone.
        /// </summary>
        public static bool EnsureVillageAnchor(City village)
        {
            if (village == null || !village.IsVillage()) return false;
            if (village.TryGetVillageMain(out _, out _)) return true;

            Plot mainPlot = null;
            foreach (Plot plot in village.getCityPlots())
            {
                if (plot.Type == PlotType.MAIN_CITY_PLOT) { mainPlot = plot; break; }
                mainPlot ??= plot;
            }
            if (mainPlot == null) return false;

            mainPlot.Type = PlotType.VILLAGE_MAIN;
            PlaceVillageAnchor(village, mainPlot);
            mainPlot.saveToDatabase();
            village.saveToDatabase();
            return true;
        }

        private static void PlaceVillageAnchor(City village, Plot mainPlot)
        {
            int plotSize = claims.config.PLOT_SIZE;
            int ax = mainPlot.getPos().X * plotSize + plotSize / 2;
            int az = mainPlot.getPos().Y * plotSize + plotSize / 2;
            int ay = claims.sapi.World.BlockAccessor.GetTerrainMapheightAt(new BlockPos(ax, 0, az)) + 1;
            Vec3i anchor = new Vec3i(ax, ay, az);
            // Granary right next to the anchor, on its own surface height - the ground next to the
            // anchor is not necessarily flat.
            int gx = ax + 1;
            int gy = claims.sapi.World.BlockAccessor.GetTerrainMapheightAt(new BlockPos(gx, 0, az)) + 1;
            Vec3i granary = new Vec3i(gx, gy, az);

            PlotDescVillage desc = new PlotDescVillage(anchor, granary);
            // Founded with a full stock, so a new village is not starving from its first hour.
            desc.SupplyHours = claims.config.VILLAGE_SUPPLY_HOURS_PER_ITEM;
            mainPlot.PlotDesc = desc;

            PlaceBlock(VillageBlocks.Anchor, anchor);
            PlaceBlock(VillageBlocks.Granary, granary);

            village.AddTempleRespawnPoint(mainPlot.getPos(), anchor);
            VillageSupplyHelper.SyncBlockEntities(desc);
            // Arms the callback for this village's first raid window, a week from now.
            VillageRaidHelper.Schedule(village);
        }

        private static void PlaceBlock(AssetLocation blockCode, Vec3i pos)
        {
            Block block = claims.sapi.World.GetBlock(blockCode);
            if (block == null) return;
            claims.sapi.World.BlockAccessor.SetBlock(block.Id, new BlockPos(pos.X, pos.Y, pos.Z));
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
            Alliance.FireAllianceCreated(newAlliace);
            claims.economyProvider.NewAccount(newAlliace.MoneyAccountName, new Dictionary<string, object> { { "lastknownname", newAlliace.GetPartName() } });
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
            // Added only once, the way SetPartiesHostile guards its lists. These are plain Lists and
            // DemolishUnion removes a single entry each, so a duplicated link would survive breaking
            // the union - leaving the two permanently allied and unable to ever declare war.
            if (!first.ComradAlliancies.Contains(second)) first.ComradAlliancies.Add(second);
            if (!second.ComradAlliancies.Contains(first)) second.ComradAlliancies.Add(first);
            // Save once per city, not once per added link (mirrors PartDemolition.DemolishUnion).
            foreach (var city in first.Cities)
            {
                foreach(var sCity in second.Cities)
                {
                    if (!city.ComradeCities.Contains(sCity)) city.ComradeCities.Add(sCity);
                }
                city.saveToDatabase();
            }
            foreach (var city in second.Cities)
            {
                foreach(var sCity in first.Cities)
                {
                    if (!city.ComradeCities.Contains(sCity)) city.ComradeCities.Add(sCity);
                }
                city.saveToDatabase();
            }
            first.saveToDatabase();
            second.saveToDatabase();
            //UsefullPacketsSend.AddToQueueUnionInfoUpdate(newUnion.Guid, new Dictionary<string, object> { { "value", newUnion.Guid } }, EnumPlayerRelatedInfo.NEW_UNION_ALL);
        }
    }
}
