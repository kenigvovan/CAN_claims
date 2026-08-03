using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.cityplotsgroups;
using claims.src.delayed.cooldowns;
using claims.src.delayed.invitations;
using claims.src.delayed.teleportation;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.rights;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public partial class CityCommand: BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CityHere(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                return TextCommandResult.Error("claims:no_city_here");
            }
            if (!plotHere.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }
            // Passing the caller matters for villages: their raid window is only shown to their own.
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo callerInfo);
            return TextCommandResult.Success(string.Join("", plotHere.getCity().getStatus(callerInfo)));
        }
        public static TextCommandResult ProcessListCities(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            return SuccessWithParams("claims:cities_list", new object[] { StringFunctions.makeFeasibleStringFromNames(StringFunctions.getNamesOfCities("", claims.dataStorage.getCitiesList()), ' ') });
        }
        public static TextCommandResult CityInfo(TextCommandCallingArgs args)
        {
            string cityName = Filter.filterName((string)args.LastArg);
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
            {
                return TextCommandResult.Error("claims:invalid_city_name");
            }
            claims.dataStorage.GetCityByName(cityName, out City city);

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);

            if (city == null)
            {
                return TextCommandResult.Success("claims:no_such_city");
            }
            else
            {
                return TextCommandResult.Success(string.Join("", city.getStatus(playerInfo)));
            }
        }
        public static TextCommandResult CreateNewCity(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            // Shared with villages, including the rule that the ruins of a fallen one block a city
            // too - otherwise the penalty is sidestepped by founding a city on the same spot.
            if (!SettlementFounding.TryValidate(playerInfo, currentPlotPosition,
                    claims.config.MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY, (string)args.LastArg,
                    out string newCityName, out TextCommandResult foundingError))
            {
                return foundingError;
            }
            if (claims.economyProvider.GetBalance(playerInfo.Guid) < (decimal)claims.config.NEW_CITY_COST)
            {
                return TextCommandResult.Error("claims:not_enough_for_new_city");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                () =>
                {
                    claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere);
                    if (playerInfo.hasCity() || plotHere != null)
                    {
                        return;
                    }
                    if (!claims.config.NEW_CITY_ONLY_BY_ITEM)
                    {
                        if (claims.economyProvider.Withdraw(playerInfo.Guid, (decimal)claims.config.NEW_CITY_COST) == MoneyOperationResult.Success)
                        {
                            PartInits.initNewCity(playerInfo, currentPlotPosition, newCityName);
                        }
                    }
                    else
                    {
                        PartInits.initNewCity(playerInfo, currentPlotPosition, newCityName);
                    }
                }, player.PlayerUID));

            if (player != null)
            {
                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.AGREE_NEEDED_ON_NEW_CITY_CREATION,
                    data = newCityName

                }, player as IServerPlayer);
            }

            return SuccessWithParams("claims:help_agreement_new_city", new object[] { claims.config.AGREEMENT_COMMAND });
        }
        public static TextCommandResult DeleteCity(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            if (!city.isMayor(playerInfo))
            {
                return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            }

            if(city.Alliance != null)
            {
                return TextCommandResult.Error("claims:leave_alliance_before_that");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                () =>
                {
                    MessageHandler.sendGlobalMsg(Lang.Get("claims:city_has_been_demolished", city.getPartNameReplaceUnder(), playerInfo.getPartNameReplaceUnder()));

                    PartDemolition.demolishCity(city, string.Format("Deleted by player {0}", player.PlayerName));
                }, player.PlayerUID));
            return SuccessWithParams("claims:help_agreement_delete_city", new object[] { claims.config.AGREEMENT_COMMAND });
        }
        /*==============================================================================================*/
        /*=====================================HELPERS==================================================*/
        /*==============================================================================================*/
        public static bool CheckForAtleastOneClaimedPlotOnBorderSameCity(Plot plot)
        {
            PlotPosition cl = plot.plotPosition.Clone();
            cl.getPos().Add(-1, 0);
            if (claims.dataStorage.GetPlot(cl, out Plot foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(2, 0);
            if (claims.dataStorage.GetPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(-1, 0);
            cl.getPos().Add(0, -1);
            if (claims.dataStorage.GetPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(0, 2);
            if (claims.dataStorage.GetPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool HelperFunctionRank(IServerPlayer player, string rankNamePassed, string playerName, out City city, out PlayerInfo targetPlayer, TextCommandResult tcr)
        {
            city = null;
            targetPlayer = null;
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player_info"));
                return false;
            }

            city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }
            // Ranks are a city feature; the rank commands do not check mayorship, so gate here.
            if (city.IsVillage())
            {
                tcr.StatusMessage = "claims:village_feature_locked";
                return false;
            }
            string targetPlayerName = Filter.filterName(playerName);
            if (targetPlayerName.Length == 0 || !Filter.checkForBlockedNames(targetPlayerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }

            if(!claims.dataStorage.getPlayerByName(targetPlayerName, out targetPlayer))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            if (!targetPlayer.hasCity() || !targetPlayer.City.Equals(city))
            {
                tcr.StatusMessage = "claims:player_should_be_in_same_city";
                return false;
            }

            string rankName = Filter.filterName(rankNamePassed);
            if (rankName.Length == 0 || !Filter.checkForBlockedNames(rankName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            if(!city.HasCityRank(rankName))
            {
                tcr.StatusMessage = "claims:no_such_city_rank";
                return false;
            }


            return true;
        }
        public static bool HelperFunctionPrison(IServerPlayer player, out City city, out Plot plotHere, TextCommandResult tcr)
        {
            city = null;
            plotHere = null;
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return false;
            }
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }
            city = playerInfo.City;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            claims.dataStorage.GetPlot(currentPlotPosition, out plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:not_claimed";
                return false;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return false;
            }
            if (!plotHere.getCity().Equals(city))
            {
                tcr.StatusMessage = "claims:not_same_city";
                return false;
            }
            if (!(plotHere.Type == PlotType.PRISON))
            {
                tcr.StatusMessage = "claims:not_prison_here";
                return false;
            }
            return true;
        }

        // Helper: resolve target party by name (city or alliance).
        // Returns false and sets errorMsg if not allowed.
        internal static (string name, WarTargetType? targetType) ParseWarTargetInput(string rawInput)
        {
            if (rawInput.StartsWith("city:"))
                return (rawInput.Substring("city:".Length), WarTargetType.City);
            if (rawInput.StartsWith("alliance:"))
                return (rawInput.Substring("alliance:".Length), WarTargetType.Alliance);
            return (rawInput, null);
        }

        internal static bool TryResolveWarTarget(string name, WarTargetType? targetType, out IConflictParty targetParty, out string errorMsg)
        {
            if (targetType == WarTargetType.City)
            {
                if (!claims.dataStorage.GetCityByName(name, out City prefixedCity))
                {
                    targetParty = null;
                    errorMsg = Lang.Get("claims:no_such_city_or_alliance");
                    return false;
                }
                if (prefixedCity.HasAlliance())
                {
                    targetParty = null;
                    errorMsg = Lang.Get("claims:city_in_alliance_attack_alliance", prefixedCity.Alliance.getPartNameReplaceUnder());
                    return false;
                }
                if (prefixedCity.IsVillage())
                {
                    targetParty = null;
                    errorMsg = Lang.Get("claims:village_cannot_be_attacked");
                    return false;
                }
                targetParty = prefixedCity;
                errorMsg = null;
                return true;
            }
            if (targetType == WarTargetType.Alliance)
            {
                if (!claims.dataStorage.GetAllianceByName(name, out Alliance prefixedAlliance))
                {
                    targetParty = null;
                    errorMsg = Lang.Get("claims:no_such_city_or_alliance");
                    return false;
                }
                targetParty = prefixedAlliance;
                errorMsg = null;
                return true;
            }

            // No explicit type: try city first, then alliance (backward compatibility)
            if (claims.dataStorage.GetCityByName(name, out City targetCity))
            {
                if (targetCity.HasAlliance())
                {
                    targetParty = null;
                    errorMsg = Lang.Get("claims:city_in_alliance_attack_alliance", targetCity.Alliance.getPartNameReplaceUnder());
                    return false;
                }
                if (targetCity.IsVillage())
                {
                    targetParty = null;
                    errorMsg = Lang.Get("claims:village_cannot_be_attacked");
                    return false;
                }
                targetParty = targetCity;
                errorMsg = null;
                return true;
            }
            if (claims.dataStorage.GetAllianceByName(name, out Alliance targetAlliance))
            {
                targetParty = targetAlliance;
                errorMsg = null;
                return true;
            }
            targetParty = null;
            errorMsg = Lang.Get("claims:no_such_city_or_alliance");
            return false;
        }

        // Resolves the caller's OWN conflict party (alliance if they're in one, else their city).
        // requireAuthority=true additionally requires alliance leadership / city mayorship.
        internal static bool TryResolveMyParty(TextCommandCallingArgs args, bool requireAuthority,
            out IServerPlayer player, out PlayerInfo playerInfo, out IConflictParty ourParty, out TextCommandResult err)
        {
            ourParty = null;
            if (!TryResolveCaller(args, out player, out playerInfo, out err)) return false;
            if (!playerInfo.hasCity())
            {
                err = TextCommandResult.Success(Lang.Get("claims:no_city"));
                return false;
            }
            // One gate for every war command: a village is never a party to a conflict.
            if (playerInfo.City.IsVillage())
            {
                err = TextCommandResult.Success(Lang.Get("claims:village_feature_locked"));
                return false;
            }
            if (playerInfo.HasAlliance())
            {
                if (requireAuthority && !playerInfo.Alliance.IsLeader(playerInfo))
                {
                    err = TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
                    return false;
                }
                ourParty = playerInfo.Alliance;
            }
            else
            {
                if (requireAuthority && !playerInfo.City.isMayor(playerInfo))
                {
                    err = TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));
                    return false;
                }
                ourParty = playerInfo.City;
            }
            err = null;
            return true;
        }
    }
}
