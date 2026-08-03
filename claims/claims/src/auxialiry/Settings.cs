using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using claims.src.part;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.auxialiry
{
    public class Settings
    {
        public static HashSet<string> blockedNames;
        public static HashSet<string> blockedCommandsForPrison;
        public static HashSet<string> protectedAnimals;
        public static HashSet<AssetLocation> protectedAnimalCodes;
        public static SortedDictionary<int, CityLevelInfo> cityLevelsDict;
        public static SortedDictionary<int, AllianceLevelInfo> AllianceLevelsDict;
        public static int[] colors = new int[0];
        public static void loadAll()
        {
            blockedNames = new HashSet<string>();
            blockedCommandsForPrison = new HashSet<string>();
            protectedAnimals = new HashSet<string>();
            protectedAnimalCodes = new HashSet<AssetLocation>();
            cityLevelsDict = new SortedDictionary<int, CityLevelInfo>();
            AllianceLevelsDict = new SortedDictionary<int, AllianceLevelInfo>();
            loadBlockedCommandsForPrison();
            loadProtectedAnimals();
            LoadCityLevelsInfo();
            LoadAllianceLevelsInfo();
            InitColors();
        }
        public static void clearAll()
        {
            blockedNames = null;
            blockedCommandsForPrison = null;
            protectedAnimals = null;
            protectedAnimalCodes = null;
            cityLevelsDict = null;
            colors = null;

        }
        public static bool LoadCityLevelsInfo()
        {
            string filePath;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
                filePath = @"" + Path.Combine(GamePaths.ModConfig, "city_level_info.json");
            else
                filePath = Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, "city_level_info.json");

            string json = "";
            if (File.Exists(filePath))
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    json = r.ReadToEnd();
                }
                try
                {
                    Dictionary<int, Dictionary<String, Object>> levelsDict = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<String, Object>>>(json);
                    foreach (var it in levelsDict)
                    {
                        cityLevelsDict.Add(it.Key, new CityLevelInfo(int.Parse(it.Value["AmountOfPlots"].ToString()),
                                int.Parse(it.Value["UnconditionalPayment"].ToString()),
                                int.Parse(it.Value["SummonPlots"].ToString()),
                                int.Parse(it.Value["Maxextrachunksbought"].ToString())
                                ));
                    }
                }catch
                {
                    cityLevelsDict.Clear();
                    createDefaultCityLevels(filePath);
                }
            }
            // Dict is still empty if the file was missing, empty or corrupted
            // (the catch above may have already filled it with defaults)
            if (cityLevelsDict.Count == 0)
            {
                createDefaultCityLevels(filePath);
            }
            return true;
        }
        public static bool LoadAllianceLevelsInfo()
        {
            string filePath;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
                filePath = @"" + Path.Combine(GamePaths.ModConfig, "alliance_level_info.json");
            else
                filePath = Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, "alliance_level_info.json");

            string json = "";
            if (File.Exists(filePath))
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    json = r.ReadToEnd();
                }
                try
                {
                    Dictionary<int, Dictionary<String, Object>> levelsDict = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<String, Object>>>(json);
                    foreach (var it in levelsDict)
                    {
                        AllianceLevelsDict.Add(it.Key, new AllianceLevelInfo(int.Parse(it.Value["AdditionalAmountOfPlots"].ToString()),
                                int.Parse(it.Value["MaxCampsAmount"].ToString()),
                                int.Parse(it.Value["UnconditionalPayment"].ToString())
                                ));
                    }
                }
                catch
                {
                    AllianceLevelsDict.Clear();
                    CreateDefaultAllianceLevels(filePath);
                }
            }
            // Dict is still empty if the file was missing, empty or corrupted
            // (the catch above may have already filled it with defaults)
            if (AllianceLevelsDict.Count == 0)
            {
                CreateDefaultAllianceLevels(filePath);
            }
            return true;
        }
        public static CityLevelInfo getCityLevelInfo(int count)
        {
            foreach (int level in cityLevelsDict.Keys.Reverse()) //CHECK
            {
                if(count >= level)
                {
                    return cityLevelsDict[level];
                }
            }
            // Custom configs may not define level 1 — fall back to the lowest defined level
            return cityLevelsDict.Values.First();
        }
        public static AllianceLevelInfo GetAllianceLevelInfo(int count)
        {
            foreach (int level in AllianceLevelsDict.Keys.Reverse()) //CHECK
            {
                if (count >= level)
                {
                    return AllianceLevelsDict[level];
                }
            }
            // Custom configs may not define level 1 — fall back to the lowest defined level
            return AllianceLevelsDict.Values.First();
        }
        public static void createDefaultCityLevels(string path)
        {
            //plot amount, unconditionalPayment, summon plot, extra plots
            cityLevelsDict.Add(1, new CityLevelInfo(2, 0, 0, 2));
            cityLevelsDict.Add(2, new CityLevelInfo(4, 0, 0, 4));
            cityLevelsDict.Add(3, new CityLevelInfo(8, 0, 0, 8));
            cityLevelsDict.Add(4, new CityLevelInfo(16, 0, 0, 16));
            cityLevelsDict.Add(8, new CityLevelInfo(24, 0, 1, 24));
            cityLevelsDict.Add(16, new CityLevelInfo(30, 0, 1, 30));
            cityLevelsDict.Add(20, new CityLevelInfo(38, 0, 1, 38));
            cityLevelsDict.Add(24, new CityLevelInfo(44, 0, 1, 44));
            cityLevelsDict.Add(30, new CityLevelInfo(50, 0, 2, 50));
            cityLevelsDict.Add(36, new CityLevelInfo(56, 0, 2, 56));
            using (StreamWriter r = new StreamWriter(path))
            {
                string b = JsonConvert.SerializeObject(cityLevelsDict, Formatting.Indented);
                r.WriteLine(b);
            }
        }
        public static void CreateDefaultAllianceLevels(string path)
        {
            //plot amount, unconditionalPayment, summon plot, extra plots
            AllianceLevelsDict.Add(1, new AllianceLevelInfo(12, 1, 4));
            AllianceLevelsDict.Add(2, new AllianceLevelInfo(24, 1, 5));
            using (StreamWriter r = new StreamWriter(path))
            {
                string b = JsonConvert.SerializeObject(AllianceLevelsDict, Formatting.Indented);
                r.WriteLine(b);
            }
        }
        public static void loadBlockedNames()
        {
            foreach (var it in claims.config.BLOCKED_NAMES)
            {
                blockedNames.Add(it);
            }
        }
        public static void loadBlockedCommandsForPrison()
        {
            foreach(var it in claims.config.BLOCKED_COMMANDS_PRISON)
            {
                blockedCommandsForPrison.Add(it);
            }
        }
        public static void loadProtectedAnimals()
        {
            foreach (string it in claims.config.PROTECTED_MOB_TYPES)
            {
                if (it.Trim().Length == 0)
                    continue;
                protectedAnimals.Add(it.Trim());
            }

            bool PatternMatches(string pattern, AssetLocation code)
            {
                if (pattern.EndsWith("-*"))
                {
                    var prefix = pattern[..^1]; // "pig-*" → "pig-"
                    return pattern.Contains(':')
                        ? code.ToString().StartsWith(prefix)
                        : code.PathStartsWith(prefix);
                }
                return pattern.Contains(':')
                    ? code.ToString() == pattern
                    : code.Path == pattern;
            }

            foreach (var entityType in claims.sapi.World.EntityTypes)
            {
                foreach (var pattern in protectedAnimals)
                {
                    if (PatternMatches(pattern, entityType.Code))
                    {
                        protectedAnimalCodes.Add(entityType.Code);
                        break;
                    }
                }
            }
        }

        public static bool IsProtectedMob(AssetLocation code)
        {
            return protectedAnimalCodes.Contains(code);
        }
        public static bool isPvpTime()
        {
            return IsWithinDailyWindow(claims.sapi.World.Calendar.HourOfDay,
                claims.config.PVP_TIME_START, claims.config.PVP_TIME_END);
        }

        /// <summary>
        /// Whether <paramref name="hour"/> falls into the daily [start, end) window.
        /// A window whose start is past its end wraps around midnight - 19 -> 6 is the night,
        /// which is what the default PVP_TIME_START/END mean.
        /// </summary>
        public static bool IsWithinDailyWindow(float hour, float start, float end)
        {
            if (start == end) return false;
            if (start < end) return hour >= start && hour < end;
            return hour >= start || hour < end;
        }
        public static int getMaxNumberOfPlotForCity(City city)
        {
            // A village has a flat limit instead of the citizen-count levels; the bonus plots an
            // admin granted still apply, alliance bonuses cannot (villages have no alliance).
            if (city.IsVillage())
            {
                return city.getBonusPlots() + claims.config.VILLAGE_MAX_PLOTS;
            }
            CityLevelInfo cityLevel = getCityLevelInfo(city.getCityCitizens().Count);
            return city.getBonusPlots() + cityLevel.AmountOfPlots +
                                                                    (city.HasAlliance()
                                                                        ? GetAllianceLevelInfo(city.Alliance.Cities.Count).AdditionalAmountOfPlots
                                                                        : 0);
        }
        public static Dictionary<string, int> getPossibleAmountOfPlotsDictForCity(City city)
        {
            Dictionary<string, int> res = new Dictionary<string, int>();
            if (city.IsVillage())
            {
                res["base"] = claims.config.VILLAGE_MAX_PLOTS;
                res["bonus"] = city.getBonusPlots();
                res["alliance"] = 0;
                return res;
            }
            CityLevelInfo cityLevel = getCityLevelInfo(city.getCityCitizens().Count);
            res["base"] = cityLevel.AmountOfPlots;
            res["bonus"] = city.getBonusPlots();
            res["alliance"] = city.HasAlliance() ? GetAllianceLevelInfo(city.Alliance.Cities.Count).AdditionalAmountOfPlots : 0;
            return res;
        }
        public static int getMaxNumberOfExtraChunksBought(City city)
        {
            // Villages cannot buy extra plots at all.
            if (city.IsVillage()) return 0;
            CityLevelInfo cityLevel = getCityLevelInfo(city.getCityCitizens().Count);
            return cityLevel.Maxextrachunksbought;
        }
        /// <summary>Whether the settlement may still take in another citizen.</summary>
        public static bool CanAcceptMoreCitizens(City city)
        {
            if (!city.IsVillage()) return true;
            return city.getCityCitizens().Count < claims.config.VILLAGE_MAX_CITIZENS;
        }

        public static void InitColors()
        {
            HashSet<int> tmpSet = new();
            foreach(var it in claims.config.CITY_PLOTS_COLOR_AVAILABLE_COLORS_GUI)
            {
                ColorHandling.tryFindColor(it, out var colorInt);
                if(colorInt != 0)
                {
                    tmpSet.Add(colorInt);
                }
            }
            colors = tmpSet.ToArray();
        }
    }
}
