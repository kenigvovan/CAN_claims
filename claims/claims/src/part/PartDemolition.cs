using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.delayed.invitations;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace claims.src.part
{
    public class PartDemolition
    {
        public static void demolishCity (City city, string reason)
        {
            foreach(var plot in city.getCityPlots())
            {
                claims.serverPlayerMovementListener.markPlotToWasRemoved(plot.getPos());
            }
            demolishCityPlots(city);
            InvitationHandler.deleteAllInvitationsForReceiver(city);
            InvitationHandler.deleteAllInvitationsForSender(city);
            foreach (var it in city.getCityCitizens().ToArray())
            {
                RightsHandler.reapplyRights(it);
                it.clearCity();
                IPlayer player = claims.sapi.World.PlayerByUid(it.Guid);
                if (player != null)
                {
                    claims.serverChannel.SendPacket(new SavedPlotsPacket()
                    {
                        type = PacketsContentEnum.OWN_CITY_DELETED,
                        data = ""

                    }, player as IServerPlayer);
                }
            }
            foreach (var player in claims.sapi.World.AllOnlinePlayers)
            {
                claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    continue;
                }
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, new Dictionary<string, object> { { "value", city.Guid } }, EnumPlayerRelatedInfo.CITY_LIST_REMOVE);
            }
            foreach (Conflict conflict in claims.dataStorage.conflicts.ToArray())
            {
                if (conflict.First.GetCities().Contains(city) || conflict.Second.GetCities().Contains(city))
                {
                    DemolishConflict(conflict, EnumConflictEndReason.CityDestroyed);
                }
            }
            Dictionary<string, ClientCityInfoCellElement> CityStatsCashe =
                ObjectCacheUtil.GetOrCreate<Dictionary<string, ClientCityInfoCellElement>>(claims.sapi,
                "claims:cityinfocache", () => new Dictionary<string, ClientCityInfoCellElement>());
            CityStatsCashe.Remove(city.Guid);
            claims.economyProvider.DeleteAccount(city.MoneyAccountName);
            claims.dataStorage.removeCityByGUID(city.Guid);
            //DataStorage.nameToCityDict.TryRemove(city.getPartName(), out _);
            City.FireCityDestroyed(city);
            claims.getModInstance().getDatabaseHandler().deleteFromDatabaseCity(city);
            MessageHandler.sendDebugMsg(string.Format("City {0} was deleted, ", city.GetPartName()) + reason);
        }
      
        public static void demolishCityPlots(City city)
        {
            foreach(Plot plot in city.getCityPlots().ToArray())
            {
                demolishCityPlot(plot);
            }
        }
        public static void demolishCityPlot(Plot plot)
        {
            PlayerInfo player = plot.getPlayerInfo();
            if (player != null)
            {
                player.PlayerPlots.Remove(plot);
            }
            City city = plot.getCity();
            if(city != null)
            {
                city.getCityPlots().Remove(plot);
            }
            if(plot.Type == PlotType.PRISON || plot.Type == PlotType.SUMMON)
            {
                plot.CleanUpCurrentPlotTypeData();
            }
            //DataStorage.claimedPlots.TryRemove(plot.chunkLocation, out _);
            claims.getModInstance().getDatabaseHandler().deleteFromDatabasePlot(plot);
            claims.dataStorage.removeClaimedPlot(plot.plotPosition);
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plot.getPos().X);
            tree.SetInt("chZ", plot.getPos().Y);
            if (city != null)
                tree.SetString("name", city.GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotunclaimed", tree);
        }
        public static void DemolishAlliance(Alliance alliance)
        {
            InvitationHandler.deleteAllInvitationsForSender(alliance);

            foreach (Conflict conflict in claims.dataStorage.conflicts.ToArray())
            {
                if (conflict.First.Equals(alliance) || conflict.Second.Equals(alliance) ||
                    conflict.First.GetCities().Any(c => alliance.Cities.Contains(c)) ||
                    conflict.Second.GetCities().Any(c => alliance.Cities.Contains(c)))
                {
                    DemolishConflict(conflict, EnumConflictEndReason.AllianceDestroyed);
                }
            }

            //FOR HOSTILE ALLIANCE WE DELETE OUR CITIES FROM HOSTILES FOR THIER CITIES
            foreach (IConflictParty otherParty in alliance.HostileParties)
            {
                foreach (City otherCity in otherParty.GetCities())
                {
                    foreach (City city in alliance.Cities)
                    {
                        otherCity.HostileCities.Remove(city);
                        otherCity.saveToDatabase();
                    }
                }
            }

            foreach (Alliance comradeAlliance in alliance.ComradAlliancies)
            {
                comradeAlliance.ComradAlliancies.Remove(alliance);
                comradeAlliance.saveToDatabase();
            }

            foreach (City city in alliance.Cities)
            {

                city.Alliance = null;
                foreach (var it in city.getCityCitizens())
                {
                    it.ClearAllAllianceTitles();
                    RightsHandler.reapplyRights(it);
                }
                city.Alliance = null;
                city.saveToDatabase();
            }
            foreach (var it in alliance.Cities)
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(it.Guid, EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE);
            }
            claims.economyProvider.DeleteAccount(alliance.MoneyAccountName);
            claims.dataStorage.RemoveAllianceByGUID(alliance.Guid);
            //DataStorage.nameToCityDict.TryRemove(city.getPartName(), out _);
            Alliance.FireAllianceDestroyed(alliance);
            claims.getModInstance().getDatabaseHandler().deleteFromDatabaseAlliance(alliance);
        }
        public static void DemolishConflict(Conflict conflict, EnumConflictEndReason reason = EnumConflictEndReason.Peace)
        {
            // Close active war window if conflict ends mid-battle
            if (conflict.ActiveWarTime)
            {
                claims.dataStorage.WarsTimes.Remove(conflict.Guid);
                conflict.ActiveWarTime = false;
                RightsHandler.ClearPlayerCachesAndUpdatePlotSavedRightsForClients(conflict);
            }
            // Cancel pending start/end battle callbacks
            events.ModConfigReady.startWarCallbacks.Remove(conflict.Guid);
            events.ModConfigReady.endWarCallbacks.Remove(conflict.Guid);
            claims.dataStorage.TryRemoveConflict(conflict);
            foreach (City ourCity in conflict.First.GetCities())
            {
                foreach (City targetCity in conflict.Second.GetCities())
                {
                    ourCity.HostileCities.Remove(targetCity);
                    ourCity.saveToDatabase();
                }
            }
            foreach (City targetCity in conflict.Second.GetCities())
            {
                foreach (City ourCity in conflict.First.GetCities())
                {
                    targetCity.HostileCities.Remove(ourCity);
                    targetCity.saveToDatabase();
                }
            }
            conflict.First.RunningConflicts.Remove(conflict);
            conflict.Second.RunningConflicts.Remove(conflict);
            conflict.First.RemoveHostileParty(conflict.Second);
            conflict.Second.RemoveHostileParty(conflict.First);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First,
                new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second,
                new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE);
            conflict.First.saveToDatabase();
            conflict.Second.saveToDatabase();
            if (conflict.First is Alliance firstAlliance)
                firstAlliance.FireConflictEnded(conflict.Second, reason);
            if (conflict.Second is Alliance secondAlliance)
                secondAlliance.FireConflictEnded(conflict.First, reason);
            claims.getModInstance().getDatabaseHandler().deleteFromDatabaseConflict(conflict);
        }
        public static void DemolishUnion(Alliance first, Alliance second)
        {
            foreach (var city in first.Cities)
            {
                foreach (var sCity in second.Cities)
                {
                    city.ComradeCities.Remove(sCity);
                    city.saveToDatabase();
                }
            }
            foreach (var city in second.Cities)
            {
                foreach (var sCity in first.Cities)
                {
                    city.ComradeCities.Remove(sCity);
                    city.saveToDatabase();
                }
            }
            first.ComradAlliancies.Remove(second);
            second.ComradAlliancies.Remove(first);
            first.saveToDatabase();
            second.saveToDatabase();
        }
    }
}
