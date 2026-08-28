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
using claims.src.part.structure.plots;
using claims.src.part.structure.plots.auction;
using claims.src.part.structure.war;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace claims.src.part
{
    public class PartDemolition
    {
        /// <param name="applyVillagePenalty">
        /// False when the caller already handed out its own cooldown - giving a village up
        /// voluntarily costs less than losing it, and the two must not stack.
        /// </param>
        public static void demolishCity (City city, string reason, bool applyVillagePenalty = true)
        {
            // Villages own physical blocks (anchor, granary); unclaiming the plots alone would
            // leave them standing in the world.
            if (city.IsVillage())
            {
                RemoveVillageBlocks(city);
                // Runs before the citizens are detached below - they are the ones being penalised.
                if (applyVillagePenalty) VillageCooldownHelper.RegisterFallenVillage(city);
            }
            // Before anything else: the city's treasury must still exist to receive refunds, so its
            // auction lots are settled while its account is alive.
            AuctionHandler.OnCityRemoved(city);
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
            // Remove this city from other cities' reverse comrade references, otherwise they keep
            // a dangling pointer and persist a stale guid that fails to resolve on next load.
            foreach (City comrade in city.ComradeCities.ToArray())
            {
                comrade.ComradeCities.Remove(city);
                comrade.saveToDatabase();
            }
            // The same for the hostile lists. Ending the conflicts above clears the pairs that had a
            // conflict behind them, but an entry left over from anything else - a city that left an
            // alliance, a war torn down some other way - would outlive the city itself.
            foreach (City hostile in city.HostileCities.ToArray())
            {
                hostile.HostileCities.Remove(city);
                hostile.saveToDatabase();
            }
            // Break vassal/overlord links so no dangling guid survives.
            if (city.IsVassal())
            {
                City overlord = city.GetOverlord();
                if (overlord != null) { overlord.VassalCities.Remove(city); overlord.saveToDatabase(); }
            }
            foreach (City vassal in city.VassalCities.ToArray())
            {
                vassal.OverlordGuid = "";
                vassal.VassalSince = 0;
                vassal.saveToDatabase();
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
            plot.PlotDesc?.OnDeactivated(plot);
            //DataStorage.claimedPlots.TryRemove(plot.chunkLocation, out _);
            claims.getModInstance().getDatabaseHandler().deleteFromDatabasePlot(plot);
            claims.dataStorage.removeClaimedPlot(plot.plotPosition);
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plot.getPos().X);
            tree.SetInt("chZ", plot.getPos().Y);
            if (city != null)
                tree.SetString("name", city.GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotunclaimed", tree);

            // An offer standing on this plot - a price tag or a running auction - is void: the ground
            // it promised is gone, so it is withdrawn and any bids come back.
            AuctionHandler.OnPlotLeftCity(plot);
        }
        /// <summary>
        /// Takes down the blocks a village put into the world: its anchor and its granary. Whatever
        /// was stored is dropped on the ground rather than deleted - the spoils of a raid, or simply
        /// the stock handed back when a village grows into a city.
        /// </summary>
        public static void RemoveVillageBlocks(City village)
        {
            if (village == null) return;
            foreach (Plot plot in village.getCityPlots())
            {
                if (plot.PlotDesc is not PlotDescVillage desc) continue;
                RemoveBlockIfOurs(desc.AnchorPos, VillageBlocks.AnchorCode);
                // Whatever was stored ends up on the ground - the spoils of a successful raid.
                DropGranaryContents(desc.GranaryPos);
                RemoveBlockIfOurs(desc.GranaryPos, VillageBlocks.GranaryCode);
            }
        }

        private static void DropGranaryContents(Vec3i pos)
        {
            if (pos == null) return;
            BlockPos bp = new BlockPos(pos.X, pos.Y, pos.Z);
            if (claims.sapi.World.BlockAccessor.GetBlockEntity(bp) is beb.BlockEntityVillageGranary granary)
            {
                granary.Inventory?.DropAll(new Vec3d(bp.X + 0.5, bp.Y + 0.5, bp.Z + 0.5));
            }
        }

        // Only clears the block when it is still the one we placed: an unloaded chunk returns air
        // here, and leaving an orphan block in that rare case is better than deleting someone's build.
        private static void RemoveBlockIfOurs(Vec3i pos, string blockPath)
        {
            if (pos == null) return;
            BlockPos bp = new BlockPos(pos.X, pos.Y, pos.Z);
            var block = claims.sapi.World.BlockAccessor.GetBlock(bp);
            if (block?.Code != null && block.Code.Path == blockPath)
            {
                claims.sapi.World.BlockAccessor.SetBlock(0, bp);
            }
        }

        // Tears down a single war camp: unclaims the plot (which removes it from campPlots via
        // PlotDescCamp.OnDeactivated) and notifies the owning city.
        public static void DemolishCamp(Plot camp)
        {
            if (camp == null) return;
            City city = camp.hasCity() ? camp.getCity() : null;
            // Remove the physical anchor block if it's still ours and loaded (GetBlock returns
            // null/air for an unloaded chunk, so an orphan block in that rare case is harmless).
            if (camp.PlotDesc is PlotDescCamp campDesc && campDesc.AnchorPos != null)
            {
                var ap = new BlockPos(campDesc.AnchorPos.X, campDesc.AnchorPos.Y, campDesc.AnchorPos.Z);
                var b = claims.sapi.World.BlockAccessor.GetBlock(ap);
                if (b?.Code != null && b.Code.Path == "campanchor")
                    claims.sapi.World.BlockAccessor.SetBlock(0, ap);
            }
            claims.serverPlayerMovementListener.markPlotToWasRemoved(camp.getPos());
            demolishCityPlot(camp);
            if (city != null)
            {
                city.saveToDatabase();
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
                city.FirePlotsMapChanged(EnumPlotsMapChangeReason.Unclaimed);
                MessageHandler.sendMsgInCity(city, Lang.Get("claims:camp_destroyed"));
            }
        }

        public static void DemolishCampsForConflict(City city, string conflictGuid)
        {
            if (city == null) return;
            foreach (Plot camp in city.campPlots
                                      .Where(p => p.PlotDesc is PlotDescCamp pd && pd.ConflictGuid == conflictGuid)
                                      .ToArray())
                DemolishCamp(camp);
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
                // The cities' own comrade lists go with it. Only the link between the alliances was
                // being cut, so the cities stayed on each other's lists - and that list is what an
                // "our ally is at war with them" casus belli is read from, handing out grounds for
                // war on behalf of a union that no longer exists.
                foreach (City comradeCity in comradeAlliance.Cities)
                {
                    foreach (City city in alliance.Cities) comradeCity.ComradeCities.Remove(city);
                    comradeCity.saveToDatabase();
                }
                comradeAlliance.saveToDatabase();
            }

            foreach (City city in alliance.Cities)
            {
                // Whatever comrades this city had, it had them through the alliance being dissolved -
                // the same clearing a city leaving an alliance does.
                city.ComradeCities.Clear();
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
            // Wars the allies of the two sides were called into end with this one. They were never a
            // quarrel of their own - an ally has no terms to agree and nobody to agree them with - so
            // leaving them running kept the allies fighting a war whose principals had made peace.
            // Collected before anything is torn down, and each is ended the same way this one is.
            List<Conflict> calledIn = new List<Conflict>();
            foreach (Conflict it in claims.dataStorage.conflicts)
            {
                if (it != conflict && it.ParentConflictGuid == conflict.Guid) calledIn.Add(it);
            }
            foreach (Conflict it in calledIn)
            {
                // Cleared first: the child must not try to take its parent down in turn.
                it.ParentConflictGuid = "";
                DemolishConflict(it, reason);
            }

            // Close active war window if conflict ends mid-battle
            if (conflict.ActiveWarTime)
            {
                claims.dataStorage.WarsTimes.Remove(conflict.Guid);
                conflict.ActiveWarTime = false;
                RightsHandler.ClearPlayerCachesAndUpdatePlotSavedRightsForClients(conflict);
            }
            // Remove all war camps that belonged to this conflict.
            foreach (City campCity in conflict.First.GetCities())
                DemolishCampsForConflict(campCity, conflict.Guid);
            foreach (City campCity in conflict.Second.GetCities())
                DemolishCampsForConflict(campCity, conflict.Guid);
            // Record the war-end timestamp for the re-declare cooldown (before sides are cleared).
            WarDeclarationHelper.RecordWarEnd(conflict);
            // After-action report to both sides (while the stat counters and sides are still valid).
            WarReportHelper.Generate(conflict);
            // Cancel pending start/end battle callbacks
            events.ModConfigReady.startWarCallbacks.Remove(conflict.Guid);
            events.ModConfigReady.endWarCallbacks.Remove(conflict.Guid);
            events.ModConfigReady.battleWarned.Remove(conflict.Guid);
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
            // Save once per city, not once per removed link - this used to write each city as many
            // times as the other side had cities.
            foreach (var city in first.Cities)
            {
                foreach (var sCity in second.Cities)
                {
                    city.ComradeCities.Remove(sCity);
                }
                city.saveToDatabase();
            }
            foreach (var city in second.Cities)
            {
                foreach (var sCity in first.Cities)
                {
                    city.ComradeCities.Remove(sCity);
                }
                city.saveToDatabase();
            }
            first.ComradAlliancies.Remove(second);
            second.ComradAlliancies.Remove(first);
            first.saveToDatabase();
            second.saveToDatabase();
        }
    }
}
