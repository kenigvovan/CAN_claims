using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src
{
    /// <summary>
    /// Public API for other mods. Access via api.ModLoader.GetModSystem&lt;claims&gt;().Api
    ///
    /// For loose coupling (no DLL reference), subscribe to events via the VS event bus:
    ///   sapi.Event.RegisterEventBusListener("claims:citizenJoined", handler);
    ///
    /// Available VS event bus event names:
    ///   claims:cityCreated        - cityGuid, cityName, mayorUid, chX, chZ
    ///   claims:cityDestroyed      - cityGuid, cityName
    ///   claims:citizenJoined      - cityGuid, cityName, playerUid, playerName
    ///   claims:citizenLeft        - cityGuid, cityName, playerUid, playerName, reason (Left/Kicked)
    ///   claims:mayorChanged       - cityGuid, cityName, newMayorUid, newMayorName
    ///   claims:plotTypeChanged    - cityGuid, chX, chZ, newType   [NOTE: declared but not yet fired]
    ///   claims:plotCaptured       - attackerCityGuid, defenderCityGuid, chX, chZ   [NOTE: declared but not yet fired]
    ///   claims:allianceCreated    - allianceGuid, allianceName, founderCityGuid
    ///   claims:allianceDestroyed  - allianceGuid, allianceName
    ///   claims:cityJoinedAlliance - cityGuid, cityName, allianceGuid, allianceName
    ///   claims:cityLeftAlliance   - cityGuid, cityName, allianceGuid, allianceName, reason (Left/Kicked)
    ///   claims:conflictDeclared   - attackerGuid, attackerName, defenderGuid, defenderName
    ///   claims:conflictEnded      - firstGuid, firstName, secondGuid, secondName, reason (Peace/CityDestroyed/AllianceDestroyed)
    ///   plotclaimed               - chX, chZ, name (cityName)  — fired on server when any plot is claimed
    ///   plotunclaimed             - chX, chZ, name (cityName)  — fired on server when a plot is freed
    ///   claimsPlayerChangePlot    - playerUID, xCh, zCh (new plot), xChO, zChO (old plot)  — fired on server and client
    ///
    /// For strongly-typed C# events (requires DLL reference), subscribe directly:
    ///   City.CitizenJoined += (city, player) => ...
    ///   Alliance.ConflictDeclared += (alliance, opponent) => ...
    /// </summary>
    public class ClaimsModApi
    {
        public IDataStorage Data => claims.dataStorage;
        public IConfig Config => claims.config;

        public bool TryGetCityAt(BlockPos pos, out City city)
        {
            city = null;
            var plotPos = PlotPosition.fromBlockPos(pos);
            if (!claims.dataStorage.GetPlot(plotPos, out Plot plot)) return false;
            if (!plot.hasCity()) return false;
            city = plot.getCity();
            return true;
        }

        public bool TryGetPlayerCity(string playerUid, out City city)
        {
            city = null;
            if (!claims.dataStorage.GetPlayerByUid(playerUid, out PlayerInfo playerInfo)) return false;
            city = playerInfo.City;
            return city != null;
        }

        public bool TryGetPlayerAlliance(string playerUid, out Alliance alliance)
        {
            alliance = null;
            if (!TryGetPlayerCity(playerUid, out City city)) return false;
            alliance = city.Alliance;
            return alliance != null;
        }

        public bool TryGetPlayerInfo(string playerUid, out PlayerInfo playerInfo)
        {
            return claims.dataStorage.GetPlayerByUid(playerUid, out playerInfo);
        }

        public bool IsPlotClaimed(BlockPos pos)
        {
            return claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos), out _);
        }

        public bool TryGetPlotAt(BlockPos pos, out Plot plot)
        {
            return claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(pos), out plot);
        }

        public List<City> GetAllCities()
        {
            return claims.dataStorage.getCitiesList();
        }

        public List<Alliance> GetAllAlliances()
        {
            return claims.dataStorage.getAllAlliances();
        }

        public bool AreSameCity(string playerUid1, string playerUid2)
        {
            if (!TryGetPlayerCity(playerUid1, out City city1)) return false;
            if (!TryGetPlayerCity(playerUid2, out City city2)) return false;
            return city1.Guid == city2.Guid;
        }

        public bool AreAllied(string playerUid1, string playerUid2)
        {
            if (!TryGetPlayerCity(playerUid1, out City city1)) return false;
            if (!TryGetPlayerCity(playerUid2, out City city2)) return false;
            if (city1.Guid == city2.Guid) return true;
            if (city1.HasAlliance() && city2.HasAlliance() && city1.Alliance.Guid == city2.Alliance.Guid) return true;
            return city1.ComradeCities.Contains(city2);
        }

        public bool AreInConflict(string playerUid1, string playerUid2)
        {
            if (!TryGetPlayerCity(playerUid1, out City city1)) return false;
            if (!TryGetPlayerCity(playerUid2, out City city2)) return false;
            return city1.HostileCities.Contains(city2);
        }
    }
}
