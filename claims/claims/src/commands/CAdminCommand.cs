using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.events;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.part.structure.war;
using claims.src.timers;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class CAdminCommand : BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult triggerNextDay(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult
            {
                Status = EnumCommandStatus.Success
            };

            claims.sapi.Event.RegisterCallback((dt =>
            {
                new DayTimer().Run(false);
            }), 0);


            return tcr;
        }
        public static TextCommandResult triggerNextHour(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult
            {
                Status = EnumCommandStatus.Success
            };

            claims.sapi.Event.RegisterCallback((dt =>
            {
                new HourTimer().Run();
            }), 0);

            return tcr;
        }
        public static TextCommandResult DiagPlayer(TextCommandCallingArgs args)
        {
            IServerPlayer caller = args.Caller.Player as IServerPlayer;
            string targetName = (string)args.Parsers[0].GetValue();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            void Line(string s) { sb.Append(s).Append('\n'); }

            if (!claims.dataStorage.getPlayerByName(targetName, out PlayerInfo pi))
            {
                Line("no PlayerInfo for name '" + targetName + "' in nameToPlayerDict");
                bool foundByUidScan = false;
                foreach (var kv in claims.dataStorage.getPlayersDict())
                {
                    if (kv.Value.GetPartName() == targetName)
                    {
                        Line("but found in uidToPlayerDict by scan: uid=" + kv.Key);
                        pi = kv.Value;
                        foundByUidScan = true;
                        break;
                    }
                }
                if (!foundByUidScan)
                {
                    MessageHandler.sendMsgToPlayer(caller, sb.ToString());
                    return TextCommandResult.Success();
                }
            }

            Line("name='" + pi.GetPartName() + "' uid='" + pi.Guid + "'");
            IServerPlayer onlinePlayer = claims.sapi.World.PlayerByUid(pi.Guid) as IServerPlayer;
            Line("online=" + (onlinePlayer != null) +
                 (onlinePlayer != null ? " sapi.PlayerName='" + onlinePlayer.PlayerName + "'" : ""));

            Line("hasCity=" + pi.hasCity() +
                 (pi.hasCity() ? " city='" + pi.City.GetPartName() + "' (" + pi.City.Guid + ")" : ""));
            if (pi.hasCity())
            {
                var city = pi.City;
                var mayor = city.getMayor();
                Line("city.mayor=" + (mayor == null ? "null"
                    : mayor.GetPartName() + " uid=" + mayor.Guid));
                Line("ReferenceEquals(mayor,pi)=" + ReferenceEquals(mayor, pi) +
                     " mayor.Equals(pi)=" + (mayor != null && mayor.Equals(pi)) +
                     " isMayor()=" + city.isMayor(pi));
                Line("cityCitizens.Contains(pi)=" + city.getCityCitizens().Contains(pi) +
                     " count=" + city.getCityCitizens().Count);
                Line("cityTitles=[" + string.Join(",", pi.getCityTitles()) + "]");
                Line("CustomCityRanks keys=[" + string.Join(",", city.CustomCityRanks.Keys) + "]");
                foreach (var t in pi.getCityTitles())
                {
                    if (city.CustomCityRanks.TryGetValue(t, out var rank))
                    {
                        Line("  rank '" + t + "' perms=[" + string.Join(",", rank.Permissions) + "]");
                        Line("  rank '" + t + "' CitizensNames=[" + string.Join(",", rank.CitizensNames) + "]");
                    }
                    else
                    {
                        Line("  rank '" + t + "' NOT FOUND in CustomCityRanks");
                    }
                }
            }

            Line("PlayerPermissions=[" + string.Join(",", pi.PlayerPermissionsHandler.GetPermissions()) + "]");
            Line("PermsHandler=" + pi.PermsHandler.ToString());

            MessageHandler.sendMsgToPlayer(caller, sb.ToString());
            return TextCommandResult.Success();
        }
        /*==============================================================================================*/
        /*=====================================SET======================================================*/
        /*==============================================================================================*/
        public static TextCommandResult plotPermissions(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult
            {
                Status = EnumCommandStatus.Success
            };

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setAccessPerm(((string)args.Parsers[0].GetValue()), ((string)args.Parsers[1].GetValue()), ((string)args.Parsers[2].GetValue()));
            plot.saveToDatabase();
            claims.dataStorage.ClearCacheForPlayersInPlot(plot);
            return tcr;
        }

        public static TextCommandResult plotPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setPvp((string)args.LastArg);
            plot.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setFire((string)args.LastArg);
            plot.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setBlast((string)args.LastArg);
            plot.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotType(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plotHere);
            if (plotHere == null)
            {
                return tcr;
            }
            if (PlotInfo.nameToPlotType.ContainsKey((string)args.LastArg))
            {
                plotHere.setNewType(tcr, (string)args.LastArg, player, true);
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:no_such_plot_type";
                return tcr;
            }
        }
        public static TextCommandResult plotFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plotHere);
            if (plotHere == null)
            {
                return tcr;
            }

            int tax = (int)args.LastArg;

            if (tax < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player";
                return tcr;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }

            plotHere.setCustomTax(tax);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotForSale(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            if (!plot.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }

            int price = (int)args.LastArg;

            if (price < 0)
            {
                tcr.StatusMessage = "claims:try_pos";
                return tcr;
            }

            plot.Price = price;
            plot.saveToDatabase();
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================CITY=====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult cCreateCity(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_already_claimed"));
                return tcr;
            }
            string newCityName = Filter.filterName((string)args.LastArg);

            if (newCityName.Length == 0 || !Filter.checkForBlockedNames(newCityName))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_new_city_name"));
                return tcr;
            }
            if (claims.dataStorage.cityExistsByName(newCityName))
            {
                return TextCommandResult.Error("claims:city_name_is_already_taken");
            }
            if (newCityName.Length > claims.config.MAX_LENGTH_CITY_NAME)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:city_name_is_too_long"));
                return tcr;
            }
            PartInits.initNewCity(null, currentPlotPosition, newCityName);
            return tcr;
        }
        public static TextCommandResult cityDelete(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            if(city.HasAlliance())
            {
                if (city.Equals(city.Alliance.MainCity))
                {
                    PartDemolition.DemolishAlliance(city.Alliance);
                }
                else
                {
                    var alliance = city.Alliance;
                    alliance.Cities.Remove(city);
                    city.Alliance = null;
                    alliance.saveToDatabase();
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE);
                }
            }

            PartDemolition.demolishCity(city, string.Format("Deleted by admin player {0}", player.PlayerName));

            return tcr;
        }
        public static TextCommandResult cityClaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            cCityClaim(player, null, city, tcr, true);
            return tcr;
        }
        public static TextCommandResult cityRadiusClaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            if (args.ArgCount < 2)
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            int radius = 0;
            try
            {
                radius = (int)args.Parsers[1].GetValue();
            }
            catch
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            if (radius < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }

            for(int i = -radius; i <= radius; i++)
            {
                for(int j = -radius; j <= radius; j++)
                {
                    cCityClaimByChankOffset(player, null, city, tcr, i, j, true);
                }
            }



            //cCityClaim(player, null, city, tcr, true);
            return tcr;
        }
        public static TextCommandResult cityUnclaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            cCityUnclaim(player, null, city, tcr);
            return tcr;
        }
        public static TextCommandResult cCityPlayerKick(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            string targetPlayerName = Filter.filterName((string)args.Parsers[1].GetValue());
            if (targetPlayerName.Length == 0 || !Filter.checkForBlockedNames(targetPlayerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(targetPlayerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            if (city.isMayor(targetPlayer))
            {
                city.setMayor(null);
            }
            targetPlayer.clearCity();
            return tcr;
        }
        public static TextCommandResult cCityPlayerAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            string targetPlayerName = Filter.filterName((string)args.Parsers[1].GetValue());
            if (targetPlayerName.Length == 0 || !Filter.checkForBlockedNames(targetPlayerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(targetPlayerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            // Adding a player who already has a city would overwrite their City via setCity and,
            // if they are a mayor, leave their old city's mayor pointer dangling (same as
            // cadmin setmayor / CityJoin, both of which guard this).
            if (targetPlayer.hasCity())
            {
                tcr.StatusMessage = "claims:player_has_city";
                return tcr;
            }
            city.getCityCitizens().Add(targetPlayer);
            targetPlayer.setCity(city);
            city.saveToDatabase();
            targetPlayer.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult cCitySetName(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.rename((string)args.Parsers[1].GetValue());
            return tcr;
        }
        public static TextCommandResult citySetPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.getPermsHandler().setPvp((string)args.Parsers[1].GetValue());
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.getPermsHandler().setFire((string)args.LastArg);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.getPermsHandler().setBlast((string)args.Parsers[1].GetValue());
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetOpenClosed(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.setCityOpenCloseState((string)args.Parsers[1].GetValue());
            city.saveToDatabase();
            return tcr;
        }
        /// <summary>
        /// Moves a settlement between the two tiers by hand. Upgrading goes through the normal
        /// upgrade path (anchor and granary taken down); downgrading only flips the flag, so an
        /// admin-made village has no anchor until one is placed for it.
        /// </summary>
        public static TextCommandResult citySetTier(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            string tier = ((string)args.Parsers[1].GetValue()).ToLowerInvariant();
            if (tier == "city")
            {
                if (city.IsVillage()) VillageUpgradeHelper.UpgradeToCity(city);
            }
            else if (tier == "village")
            {
                city.Tier = CityTier.VILLAGE;
                city.saveToDatabase();
                // A city has no anchor of its own; a village without one never starves and is
                // exposed every day with nothing to break.
                PartInits.EnsureVillageAnchor(city);
                foreach (PlayerInfo citizen in city.getCityCitizens())
                {
                    RightsHandler.reapplyRights(citizen);
                }
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_TIER,
                    EnumPlayerRelatedInfo.MAX_COUNT_PLOTS);
                VillageRaidHelper.Schedule(city);
            }
            else
            {
                tcr.Status = EnumCommandStatus.Error;
                return tcr;
            }

            tcr.StatusMessage = "claims:city_tier_set";
            tcr.MessageParams = new object[] { city.getPartNameReplaceUnder(), tier };
            return tcr;
        }

        public static TextCommandResult citySetTechnical(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.setIsTechnicalCity((string)args.Parsers[1].GetValue(), tcr);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            int fee = 0;
            try
            {
                fee = int.Parse((string)args.Parsers[1].GetValue());
            }
            catch
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            if (fee < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }
            if (fee > claims.config.MAX_CITY_FEE)
            {
                fee = (int)claims.config.MAX_CITY_FEE;
            }

            city.fee = fee;
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetMayor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            string filteredName = Filter.filterName(args.Parsers[0].GetValue().ToString());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            filteredName = Filter.filterName(args.Parsers[1].GetValue().ToString());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(filteredName, out PlayerInfo playerInfo);

            if (playerInfo == null)
            {
                tcr.Status = EnumCommandStatus.Error;
                tcr.StatusMessage = "claims:no_such_player";
                return tcr;
            }

            // Promoting a citizen of this very city is the common case - only a member of a
            // DIFFERENT city has to leave that one first.
            bool alreadyCitizen = playerInfo.hasCity() && playerInfo.City.Equals(city);
            if (playerInfo.hasCity() && !alreadyCitizen)
            {
                tcr.Status = EnumCommandStatus.Error;
                tcr.StatusMessage = "claims:player_has_city";
                return tcr;
            }

            if (city.isMayor(playerInfo))
            {
                tcr.StatusMessage = "claims:player_is_mayor_already";
                return tcr;
            }

            city.AddLogEntry(EnumCityLogEvent.MayorChanged, playerInfo.GetPartName());
            city.FireMayorChanged(playerInfo);

            PlayerInfo oldMayor = city.HasMayor() ? city.getMayor() : null;
            if (oldMayor != null)
            {
                // Stepping down from mayor does not kick the player out of the city
                city.setMayor(null);
                RightsHandler.reapplyRights(oldMayor);
            }

            if (!alreadyCitizen)
            {
                city.getPlayerInfos().Add(playerInfo);
                playerInfo.setCity(city);
            }

            city.setMayor(playerInfo);
            RightsHandler.reapplyRights(playerInfo);

            playerInfo.saveToDatabase();
            oldMayor?.saveToDatabase();
            city.saveToDatabase();

            if (alreadyCitizen)
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
            }
            else
            {
                UsefullPacketsSend.SendPlayerRelatedInfoOnCityJoined(playerInfo);
            }
            if (oldMayor != null)
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(oldMayor.Guid, EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
            }
            // Every citizen has to see the new mayor name, not just the two players involved
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.MAYOR_NAME,
                EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.CITY_LOG);

            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_now_is_a_mayor", playerInfo.GetPartName()));
            return SuccessWithParams("claims:player_now_is_a_mayor", new object[] { playerInfo.GetPartName() });
        }
        public static TextCommandResult citySetBonusPlots(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.Parsers[0].GetValue());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            int bonusPlots = 0;
            try
            {
                bonusPlots = (int)args.Parsers[1].GetValue();
            }
            catch
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            if (bonusPlots < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }

            city.setBonusPlots(bonusPlots);
            city.saveToDatabase();
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================WORLD=====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult worldInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            tcr.StatusMessage = string.Join("", claims.dataStorage.getWorldInfo().getStatus());
            return tcr;
        }
        public static TextCommandResult worldSet(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string param = ((string)args.Parsers[0].GetValue());
            string state = ((string)args.Parsers[1].GetValue());
            bool newVal = state.Equals("on", StringComparison.OrdinalIgnoreCase);
            var wi = claims.dataStorage.getWorldInfo();

            if (param.Equals("blastew", StringComparison.OrdinalIgnoreCase))
            {
                wi.blastEverywhere = newVal;
            }
            else if (param.Equals("pvpew", StringComparison.OrdinalIgnoreCase))
            {
                wi.pvpEverywhere = newVal;
            }
            else if (param.Equals("fireew", StringComparison.OrdinalIgnoreCase))
            {
                wi.fireEverywhere = newVal;
            }
            else if (param.Equals("pvpfb", StringComparison.OrdinalIgnoreCase))
            {
                wi.pvpForbidden = newVal;
            }
            else if (param.Equals("firefb", StringComparison.OrdinalIgnoreCase))
            {
                wi.fireForbidden = newVal;
            }
            else if (param.Equals("blastfb", StringComparison.OrdinalIgnoreCase))
            {
                wi.blastForbidden = newVal;
            }
            else
            {
                tcr.Status = EnumCommandStatus.Error;
                tcr.StatusMessage = "Unknown param: " + param;
                return tcr;
            }

            wi.saveToDatabase();
            tcr.StatusMessage = string.Join("", wi.getStatus());
            return tcr;
        }
        public static TextCommandResult setCfg(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string key = ((string)args.Parsers[0].GetValue());
            string value = ((string)args.Parsers[1].GetValue());

            if (!config.WarConfigEditor.TrySet(key, value, out string error))
            {
                tcr.Status = EnumCommandStatus.Error;
                tcr.StatusMessage = "setcfg failed: " + error;
                return tcr;
            }

            // Persist so the edit survives a restart (StoreModConfig is otherwise only
            // called on load), then push the new values to every online client so the
            // change applies without a reconnect.
            claims.sapi.StoreModConfig<Config>(claims.config, "claims.json");
            foreach (var p in claims.sapi.World.AllOnlinePlayers)
            {
                if (p is IServerPlayer sp)
                {
                    UsefullPacketsSend.SendUpdatedConfigValues(sp);
                }
            }

            tcr.StatusMessage = "set " + key + " = " + value;
            return tcr;
        }
        public static TextCommandResult processBackup(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.sapi.Event.RegisterCallback((dt =>
            {
                claims.getModInstance().getDatabaseHandler().makeBackup(claims.config.MANUALLY_BACKUP_FILE_NAME);
            }), 0);
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================HELPER===================================================*/
        /*==============================================================================================*/
        public static void cCityUnclaim(IServerPlayer player, CmdArgs args, City city, TextCommandResult res)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_not_claimed"));
                return;
            }

            if (!plotHere.hasCity())
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_city_here"));
                return;
            }

            if (!plotHere.getCity().Equals(city))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:player_should_be_in_same_city"));
                return;
            }
            if (plotHere.getCity().getCityPlots().Count == 1)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:last_city_plot"));
                return;
            }
            PartDemolition.demolishCityPlot(plotHere);
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_has_been_unclaimed", currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y));
        }
        public static void cCityClaim(IServerPlayer player, CmdArgs args, City city, TextCommandResult res, bool force = false)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_already_claimed"));
                return;
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (!force && !claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:too_close_to_another_city"));
                return;
            }
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_has_been_claimed", currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, 0));
            plotHere.setCity(city);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.Price = -1;
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            return;
        }
        public static void cCityClaimByChankOffset(IServerPlayer player, CmdArgs args, City city, TextCommandResult res, int xOffset, int yOffset, bool force = false)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            var curPos = currentPlotPosition.getPos();
            currentPlotPosition.setX(curPos.X + xOffset); 
            currentPlotPosition.setY(curPos.Y + yOffset);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_already_claimed"));
                return;
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (!force && !claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:too_close_to_another_city"));
                return;
            }
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_has_been_claimed", currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, 0));
            plotHere.setCity(city);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.Price = -1;
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            return;
        }
        /*public static TextCommandResult onCommand(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            //player.Role.Privileges
            if (!player.Role.Code.Equals("admin"))        
            {             
                MessageHandler.sendMsgToPlayer(player, Lang.GetL(player.LanguageCode, "claims:you_dont_have_right_for_that_command"));
                return tcr;
            }
            if (args.RawArgs.Length == 0)
            {

            }

            string firstArg = args.RawArgs.PopWord().ToLower();

            return tcr;
        }
      
        public static TextCommandResult processSetCityPlotsColor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 2)
            {
                tcr.StatusMessage = "claims:need_name_and_color";
                return tcr;
            }

            if(!ColorHandling.tryFindColor(args.RawArgs[1], out int color))
            {
                tcr.StatusMessage = "claims:unknown_color";
                return tcr;
            }

            string cityName = Filter.filterName(args.RawArgs[0]);

            if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
            {
                tcr.StatusMessage = "claims:invalid_city_name";
                return tcr;
            }
            claims.dataStorage.GetCityByName(cityName, out City city);
            if (city == null)
            {
                return tcr;
            }
            city.trySetPlotColor(tcr, color, args.RawArgs[1]);
            return tcr;
        }
        public static TextCommandResult processClaimsColor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 3)
            {
                tcr.StatusMessage = "claims:need_name_and_color";
                return tcr;
            }

            ColorHandling.tryFindColor(args.RawArgs[2], out int color);

            if (args.RawArgs[0].Equals("city"))
            {
                string cityName = Filter.filterName(args.RawArgs[1]);

                if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
                {
                    tcr.StatusMessage = "claims:invalid_city_name";
                    return tcr;
                }
                claims.dataStorage.GetCityByName(cityName, out City city);
                if (city == null)
                {
                    return tcr;
                }
                city.trySetPlotColor(tcr, color, args.RawArgs[2]);
                return tcr;
            }        
            return tcr;
        }
       
      
       
       
        public static TextCommandResult plotType(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlot(PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z), out Plot plotHere);
            if (plotHere == null)
            {
                return tcr;
            }
            if (PlotInfo.nameToPlotType.ContainsKey((string)args.LastArg))
            {
                plotHere.setNewType(tcr, (string)args.LastArg, player, true);
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:no_such_plot_type";
                return tcr;
            }
        }
       
       
       
        
        
        
      
        
        public static TextCommandResult cGlobalBank(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs[0].Equals("info", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (args.RawArgs[0].Equals("deposit", StringComparison.OrdinalIgnoreCase))
            {
                if(args.RawArgs.Length == 0)
                {
                    return tcr;
                }
                int toAdd = 0;
                try
                {
                    toAdd = int.Parse(args.RawArgs[1]);
                }catch(FormatException e)
                {
                    return tcr;
                }
                if(toAdd < 0)
                {
                    return tcr;
                }

                return tcr;
            }
            else if (args.RawArgs[0].Equals("withdraw", StringComparison.OrdinalIgnoreCase))
            {
                if (args.RawArgs.Length == 0)
                {
                    return tcr;
                }
                int toTake = 0;
                try
                {
                    toTake = int.Parse(args.RawArgs[1]);
                }
                catch (FormatException e)
                {
                    return tcr;
                }
                if (toTake < 0)
                {
                    return tcr;
                }

                return tcr;
            }
            return tcr;
        }

        
       
        public static TextCommandResult chestsInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            //MessageHandler.sendMsgToPlayer(player, claimsEconomyInterface.getAccountInfoAdmin());
            return tcr;
        }
       */
        /*==============================================================================================*/
        /*=====================================CONFLICTS================================================*/
        /*==============================================================================================*/
        public static TextCommandResult StartWarTime(TextCommandCallingArgs args)
        {
            string firstName = Filter.filterName(args.Parsers[0].GetValue().ToString());
            string secondName = Filter.filterName(args.Parsers[1].GetValue().ToString());

            IConflictParty firstParty = null;
            if (claims.dataStorage.GetAllianceByName(firstName, out Alliance firstAlliance))
            {
                firstParty = firstAlliance;
            }
            else if (claims.dataStorage.GetCityByName(firstName, out City firstCity))
            {
                firstParty = firstCity;
            }
            else
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance_or_city", firstName));
            }

            IConflictParty secondParty = null;
            if (claims.dataStorage.GetAllianceByName(secondName, out Alliance secondAlliance))
            {
                secondParty = secondAlliance;
            }
            else if (claims.dataStorage.GetCityByName(secondName, out City secondCity))
            {
                secondParty = secondCity;
            }
            else
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance_or_city", secondName));
            }

            if (!ConflictHandler.TryGetConflictWithSides(firstParty, secondParty, out var conflict))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            }

            if (!claims.dataStorage.WarsTimes.ContainsKey(conflict.Guid) && !ModConfigReady.startWarCallbacks.TryGetValue(conflict.Guid, out var _))
            {
                long savedLong = claims.sapi.Event.RegisterCallback((float dt) =>
                {
                    if (!claims.dataStorage.WarsTimes.ContainsKey(conflict.Guid))
                    {
                        claims.dataStorage.WarsTimes.Add(conflict.Guid, new WarTime(conflict.Guid, conflict.NextBattleDateStart, conflict.NextBattleDateEnd));
                        conflict.ActiveWarTime = true;
                        if (ModConfigReady.startWarCallbacks.TryGetValue(conflict.Guid, out var savedCallback))
                        {
                            ModConfigReady.startWarCallbacks.Remove(conflict.Guid);
                        }
                        RightsHandler.ClearPlayerCachesAndUpdatePlotSavedRightsForClients(conflict);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First, new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_START);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second, new Dictionary<string, object> { { "value", conflict.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WAR_TIME_MARK_START);
                        MessageHandler.SendMsgInAlliance(conflict.First, Lang.Get("claims:battle_started_with", conflict.Second.GetPartName()));
                        MessageHandler.SendMsgInAlliance(conflict.Second, Lang.Get("claims:battle_started_with", conflict.First.GetPartName()));
                        MessageHandler.SendDiscoveryToAlliance(conflict.First, "ingamediscovery-battle-start", Lang.Get("claims:ingamediscovery-battle-start", conflict.Second.GetPartName()), new object[] { });
                        MessageHandler.SendDiscoveryToAlliance(conflict.Second, "ingamediscovery-battle-start", Lang.Get("claims:ingamediscovery-battle-start", conflict.First.GetPartName()), new object[] { });
                        ModConfigReady.CheckWarToEnd();
                    }
                }, 2 * 1000);
                ModConfigReady.startWarCallbacks[conflict.Guid] = savedLong;
            }
            return TextCommandResult.Success("claims:war_started");
        }

        public static TextCommandResult SetBattleDate(TextCommandCallingArgs args)
        {
            string firstName = Filter.filterName(args.Parsers[0].GetValue().ToString());
            string secondName = Filter.filterName(args.Parsers[1].GetValue().ToString());

            IConflictParty firstParty = null;
            if (claims.dataStorage.GetAllianceByName(firstName, out Alliance firstAlliance))
            {
                firstParty = firstAlliance;
            }
            else if (claims.dataStorage.GetCityByName(firstName, out City firstCity))
            {
                firstParty = firstCity;
            }
            else
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance_or_city", firstName));
            }

            IConflictParty secondParty = null;
            if (claims.dataStorage.GetAllianceByName(secondName, out Alliance secondAlliance))
            {
                secondParty = secondAlliance;
            }
            else if (claims.dataStorage.GetCityByName(secondName, out City secondCity))
            {
                secondParty = secondCity;
            }
            else
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance_or_city", secondName));
            }

            if (!ConflictHandler.TryGetConflictWithSides(firstParty, secondParty, out var conflict))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_conflict_found"));
            }

            int minutesUntilStart = 2;
            object rawMinutes = args.Parsers[2].GetValue();
            if (rawMinutes != null && rawMinutes is int parsedMinutes && parsedMinutes > 0)
            {
                minutesUntilStart = parsedMinutes;
            }

            int battleDurationMinutes = 30;
            object rawDuration = args.Parsers[3].GetValue();
            if (rawDuration != null && rawDuration is int parsedDuration && parsedDuration > 0)
            {
                battleDurationMinutes = parsedDuration;
            }

            conflict.NextBattleDateStart = DateTime.Now.AddMinutes(minutesUntilStart);
            conflict.NextBattleDateEnd = conflict.NextBattleDateStart.AddMinutes(battleDurationMinutes);
            conflict.saveToDatabase();

            ModConfigReady.CheckForWarToStart();

            return TextCommandResult.Success(string.Format("Battle date set: start {0}, end {1}",
                conflict.NextBattleDateStart.ToString("yyyy-MM-dd HH:mm:ss"),
                conflict.NextBattleDateEnd.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        public static TextCommandResult EndWar(TextCommandCallingArgs args)
        {
            string firstName = Filter.filterName(args.Parsers[0].GetValue().ToString());
            string secondName = Filter.filterName(args.Parsers[1].GetValue().ToString());

            IConflictParty firstParty = null;
            if (claims.dataStorage.GetAllianceByName(firstName, out Alliance firstAlliance))
                firstParty = firstAlliance;
            else if (claims.dataStorage.GetCityByName(firstName, out City firstCity))
                firstParty = firstCity;
            else
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance_or_city", firstName));

            IConflictParty secondParty = null;
            if (claims.dataStorage.GetAllianceByName(secondName, out Alliance secondAlliance))
                secondParty = secondAlliance;
            else if (claims.dataStorage.GetCityByName(secondName, out City secondCity))
                secondParty = secondCity;
            else
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance_or_city", secondName));

            if (!ConflictHandler.TryGetConflictWithSides(firstParty, secondParty, out var conflict))
                return TextCommandResult.Error(Lang.Get("claims:no_conflict_found"));

            PartDemolition.DemolishConflict(conflict, EnumConflictEndReason.Peace);
            return TextCommandResult.Success("Conflict between " + firstName + " and " + secondName + " has been force-ended.");
        }

    }

}
