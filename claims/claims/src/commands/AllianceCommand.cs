using claims.src.agreement;
using claims.src.economy;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.delayed.cooldowns;
using claims.src.delayed.invitations;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.union;
using claims.src.part.structure.war;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class AllianceCommand : BaseCommand
    {
        // ================= TEST HOOKS =================

        internal static IMoneyProvider Economy => claims.economyProvider;
        internal static IConfig Config = claims.config;

        internal static Func<long> Now = () => TimeFunctions.getEpochSeconds();

        internal static System.Func<string, string> Loc = k => Lang.Get(k);
        public static TextCommandResult CreateAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_city"));
            }
            City city = playerInfo.City;
            if (city.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:already_has_alliance"));
            }
            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (claims.dataStorage.GetAllianceByName(name, out Alliance targetAlliance))
            {
                return TextCommandResult.Error(Lang.Get("claims:already_exists_alliance"));
            }
            if (!city.isMayor(playerInfo))
            {
                return TextCommandResult.Error(Lang.Get("claims:should_be_mayor"));
            }
            if ((claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.NEW_ALLIANCE_COST))
            {
                return TextCommandResult.Error(Lang.Get("claims:not_enough_money"));
            }
            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                () =>
                {
                if (playerInfo.hasCity() && !playerInfo.City.HasAlliance())
                {
                    if ((claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.NEW_ALLIANCE_COST))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_enough_money"));
                        return;
                    }
                        claims.economyProvider.Withdraw(city.MoneyAccountName, (decimal)claims.config.NEW_ALLIANCE_COST);
                        PartInits.InitNewAlliance(playerInfo, name);
                        MessageHandler.sendGlobalMsg(Lang.Get("claims:new_alliance_created", playerInfo.GetPartName(), name));
                    }
                    else
                    {
                        MessageHandler.sendGlobalMsg(Lang.Get("claims:alliance_wont_be_created"));
                        return;
                    }
                }, player.PlayerUID));
            return TextCommandResult.Success(Lang.Get("claims:help_agreement_new_alliance", claims.config.AGREEMENT_COMMAND));
        }
        public static TextCommandResult DeleteAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }
            Alliance alliance = playerInfo.City.Alliance;
            if (!alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Error(Lang.Get("claims:alliance_not_a_leader"));
            }

            foreach (var it in alliance.RunningConflicts)
            {
                if (it.ActiveWarTime)
                {
                    return TextCommandResult.Success(Lang.Get("claims:cannot_delete_alliance_during_battle"));
                }
            }

            PartDemolition.DemolishAlliance(alliance);
            MessageHandler.sendGlobalMsg(Lang.Get("claims:alliance_was_deleted", alliance.GetPartName()));
            return TextCommandResult.Success();
        }
        public static TextCommandResult LeaveAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }
            Alliance alliance = playerInfo.City.Alliance;
            if (alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Error(Lang.Get("claims:must_not_be_leader"));
            }
            City city = playerInfo.City;
            if (!city.isMayor(playerInfo))
            {
                return TextCommandResult.Error(Lang.Get("claims:must_be_a_city_mayor"));
            }

            foreach(var it in alliance.RunningConflicts)
            {
                if(it.ActiveWarTime)
                {
                    return TextCommandResult.Success(Lang.Get("claims:cannot_leave_during_battle"));
                }
            }
            

            alliance.Cities.Remove(city);
            city.AddLogEntry(EnumCityLogEvent.AllianceLeft, alliance.GetPartName());
            alliance.FireCityLeft(city, EnumCityLeaveReason.Left);
            //delete alliance titles
            foreach (var it in city.getCityCitizens())
            {
                it.ClearAllAllianceTitles();
                RightsHandler.reapplyRights(it);
            }
            //delete hostile cities
            foreach (var it in alliance.HostileParties)
            {
                foreach (var hosCity in it.GetCities())
                {
                    hosCity.HostileCities.Remove(city);
                    hosCity.saveToDatabase();
                }
            }
            //delete comrade cities
            foreach (var it in alliance.ComradAlliancies)
            {
                foreach (var comCity in it.Cities)
                {
                    comCity.ComradeCities.Remove(city);
                    comCity.saveToDatabase();
                }

            }
            MessageHandler.SendMsgInAlliance(alliance, Lang.Get("claims:city_left_alliance", city.getPartNameReplaceUnder()));
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:our_city_left_alliance"));
            city.ComradeCities.Clear();
            city.HostileCities.Clear();
            RightsHandler.RemoveCityHostilesInAlliance(city, alliance);
            city.Alliance = null;
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(alliance.Guid, new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_LOG);
            city.saveToDatabase();
            alliance.saveToDatabase();
            return TextCommandResult.Success();
        }
        public static TextCommandResult KickFromAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }           
            if (!playerInfo.City.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Error(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }

            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_city_name"));
            }
            if(!claims.dataStorage.GetCityByName(name, out City city))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_city"));
            }

            Alliance alliance = playerInfo.City.Alliance;
            if (city.Equals(alliance.MainCity))
            {
                return TextCommandResult.Error(Lang.Get("claims:not_main_city_to_kick"));
            }
            if (!alliance.Cities.Contains(city))
            {
                return TextCommandResult.Error(Lang.Get("claims:city_not_in_alliance"));
            }

            foreach (var it in alliance.RunningConflicts)
            {
                if (it.ActiveWarTime)
                {
                    return TextCommandResult.Success(Lang.Get("claims:cannot_kick_from_alliance_during_battle"));
                }
            }

            alliance.Cities.Remove(city);
            city.AddLogEntry(EnumCityLogEvent.AllianceLeft, alliance.GetPartName());
            alliance.FireCityLeft(city, EnumCityLeaveReason.Kicked);
            foreach (PlayerInfo playerInCity in city.getPlayerInfos())
            {
                playerInCity.ClearAllAllianceTitles();
                RightsHandler.reapplyRights(playerInCity);
            }
            foreach (var comradeAlliance in alliance.ComradAlliancies)
            {
                foreach (var comCity in comradeAlliance.Cities)
                {
                    comCity.ComradeCities.Remove(city);
                    comCity.saveToDatabase();
                }
            }
            city.ComradeCities.Clear();
            RightsHandler.RemoveCityHostilesInAlliance(city, alliance);
            city.HostileCities.Clear();
            city.Alliance = null;
            alliance.saveToDatabase();
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(alliance.Guid, new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.OWN_ALLIANCE_REMOVE);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_LOG);
            return TextCommandResult.Success();
        }
        public static void processAllianceUninvite(IServerPlayer player, CmdArgs args, TextCommandResult res)
        {

        }
        public static TextCommandResult InviteToAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Error(Lang.Get("claims:no_alliance"));
            }
            if (!playerInfo.City.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Error(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }

            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_city_name"));
            }
            if(!claims.dataStorage.GetCityByName(name, out City city))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_city"));
            }
            Alliance alliance = playerInfo.City.Alliance;
            if (alliance.Cities.Contains(city))
            {
                return TextCommandResult.Success(Lang.Get("claims:already_our_city"));
            }
            if (city.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:has_other_alliance"));
            }
            long timeStamp = TimeFunctions.getEpochSeconds() + claims.config.HOUR_TIMEOUT_INVITATION_TO_ALLIANCE * 60;
            if (InvitationHandler.addNewInvite(new Invitation(alliance, city, timeStamp,
                () =>
                {
                    alliance.Cities.Add(city);
                    RightsHandler.AddCityHostilesInAlliance(city, alliance);
                    foreach (PlayerInfo playerInCity in city.getPlayerInfos())
                    {
                        RightsHandler.reapplyRights(playerInCity);
                    }
                    MessageHandler.SendMsgInAlliance(alliance, Lang.Get("claims:city_joined_alliance", city.GetPartName()));
                    city.Alliance = alliance;
                    city.AddLogEntry(EnumCityLogEvent.AllianceJoined, alliance.GetPartName());
                    alliance.FireCityJoined(city);
                    city.saveToDatabase();
                    alliance.saveToDatabase();
                    UsefullPacketsSend.AddToQueueAllianceInfoUpdate(alliance.Guid, new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                        new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.TO_ALLIANCE_INVITE_REMOVE);
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_LOG);
                },
                () =>
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:disagrre_with_invitation_to_alliance", playerInfo.GetPartName(), city.GetPartName()));
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                        new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.TO_ALLIANCE_INVITE_REMOVE);
                }
                )))
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                    new Dictionary<string, object> { { "value", new ClientToAllianceInvitationCellElement(alliance.GetPartName(), alliance.Guid, timeStamp) } },
                    EnumPlayerRelatedInfo.TO_ALLIANCE_INVITE_ADD);
                MessageHandler.sendMsgInCity(city, Lang.Get("claims:your_city_was_invited_to_alliance", alliance.GetPartName()));
                return TextCommandResult.Success(Lang.Get("claims:invitation_to_alliance_was_sent", city.GetPartName()));
            }
            else
            {
                return TextCommandResult.Success(Lang.Get("claims:city_already_has_invitation"));
            }

        }
        public static TextCommandResult SetNameAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            if (!playerInfo.City.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }

            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success(Lang.Get("claims:invalid_alliance_name"));
            }
            if (claims.dataStorage.GetAllianceByName(name, out Alliance alliance))
            {
                return TextCommandResult.Success(Lang.Get("claims:name_is_taken"));
            }
            alliance = playerInfo.Alliance;

            if (claims.economyProvider.GetBalance(alliance.MoneyAccountName) < (decimal)claims.config.ALLIANCE_RENAME_COST)
            {
                return TextCommandResult.Success(Lang.Get("claims:not_enough_money"));
            }
            else
            {
                long stamp = CooldownHandler.hasCooldown(alliance, CooldownType.RENAMING);
                if (stamp != 0)
                {
                    return TextCommandResult.Success(Lang.Get("claims:wait_before", TimeFunctions.getDateFromEpochSeconds(stamp)));
                }
                else
                {
                    CooldownHandler.addCooldown(alliance, new CooldownInfo(TimeFunctions.getEpochSeconds() + claims.config.SECONDS_ALLIANCE_RENAME_COOLDOWN, CooldownType.RENAMING));
                    claims.economyProvider.Withdraw(alliance.MoneyAccountName, (decimal)claims.config.ALLIANCE_RENAME_COST);
                    alliance.SetPartName(name);
                    alliance.saveToDatabase();
                    UsefullPacketsSend.AddToQueueAllianceInfoUpdate(alliance.Guid, new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_NAME);
                    return TextCommandResult.Success();
                }
            }
        }
        public static TextCommandResult SetFeeAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            if (!playerInfo.City.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }

            int fee = (int)args.Parsers[0].GetValue();

            if (fee < 0 || fee > claims.config.ALLIANCE_MAX_FEE)
            {
                return TextCommandResult.Success(Lang.Get("claims:alliance_fee_too_high", claims.config.ALLIANCE_MAX_FEE));
            }
            playerInfo.Alliance.AllianceFee = fee;
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(playerInfo.Alliance.Guid, EnumPlayerRelatedInfo.CITY_DAY_PAYMENT);
            playerInfo.Alliance.saveToDatabase();
            return TextCommandResult.Success(Lang.Get("claims:alliane_fee_set_to", fee));
        }
        public static TextCommandResult SetCapitalAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            if (!playerInfo.City.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }

            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success(Lang.Get("claims:invalid_city_name"));
            }
            if (!claims.dataStorage.GetCityByName(name, out City city))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_such_city"));
            }
            Alliance alliance = playerInfo.Alliance;
            if (!alliance.Cities.Contains(city))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_such_city"));
            }
            if (!city.HasMayor())
                return TextCommandResult.Success(Lang.Get("claims:city_has_no_mayor"));
            alliance.MainCity = city;
            alliance.Leader = city.getMayor();
            alliance.saveToDatabase();
            return TextCommandResult.Success(Lang.Get("claims:capital_changed_to", city.GetPartName(), alliance.GetPartName()));
        }
        public static TextCommandResult SetPrefixAlliance(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            if (!playerInfo.City.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }
            
            if (args.Parsers.Count == 0)
            {
                playerInfo.Alliance.Prefix = "";
                playerInfo.Alliance.saveToDatabase();
                return TextCommandResult.Success();
            }

            string prefix = Filter.filterName((string)args.Parsers[0].GetValue());
            if (prefix.Length == 0 || !Filter.checkForBlockedNames(prefix))
            {
                return TextCommandResult.Success(Lang.Get("claims:invalid_name"));
            }
            playerInfo.Alliance.Prefix = prefix;
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(playerInfo.Alliance.Guid, new Dictionary<string, object> { { "value", playerInfo.Alliance.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);
            playerInfo.Alliance.saveToDatabase();
            return TextCommandResult.Success();
        }
        public static TextCommandResult PrintInviteList(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            if (args.Parsers.Count == 0)
            {
                MessageHandler.sendMsgToPlayer(player, playerInfo.Alliance.GetSentInvitations().Count > 0
                                                            ? StringFunctions.getNthPageOf(playerInfo.Alliance.GetSentInvitations(), 1)
                                                            : Lang.Get("claims:no_invitations"));
                return TextCommandResult.Success();
            }

            int page = (int)args.Parsers[0].GetValue();
            MessageHandler.sendMsgToPlayer(player, playerInfo.Alliance.GetSentInvitations().Count > 0
                                                        ? StringFunctions.getNthPageOf(playerInfo.Alliance.GetSentInvitations(), page)
                                                        : Lang.Get("claims:no_invitations"));
            return TextCommandResult.Success();
        }
        public static TextCommandResult DeclareConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            var (parsedName, targetType) = CityCommand.ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!CityCommand.TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string resolveError))
            {
                return TextCommandResult.Error(resolveError);
            }

            Alliance ourAlliance = playerInfo.Alliance;
            if (!ourAlliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }
            if(ourAlliance.Equals(targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:same_alliance"));
            }
            if (ourAlliance.Neutral)
            {
                return TextCommandResult.Success(Lang.Get("claims:our_alliance_is_neutral"));
            }
            if (ConflictHandler.conflictAlreadyExist(ourAlliance, targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));
            }
            if (targetParty.Neutral)
            {
                return TextCommandResult.Success(Lang.Get("claims:target_alliance_is_neutral"));
            }
            // Warring an ally would put both sides in HostileCities AND ComradeCities at once.
            // Breaking the union stays an explicit act, so refuse instead of dissolving it silently.
            if (UnionHander.PartiesAreAllied(ourAlliance, targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:war_blocked_by_union"));
            }
            // Check for a duplicate declaration letter BEFORE the gates: the gates withdraw the
            // declaration cost, and a late "duplicate" rejection would eat the money.
            if (claims.config.NEED_AGREE_FOR_CONFLICT
                && ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT, out _))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
            }
            if (!WarDeclarationHelper.TryPassDeclarationGates(ourAlliance, targetParty, ourAlliance.MoneyAccountName, out string declErr))
            {
                return TextCommandResult.Success(Lang.Get(declErr));
            }
            //BOTH SIDES HAVE TO AGREE ON CONFLICT START
            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                string newConflictGuid = ConflictLetter.GetUnusedGuid().ToString();
                if (ConflictHandler.addConflictLetter(ConflictLetterFactory.Build(
                        ourAlliance, targetParty, LetterPurpose.START_CONFLICT, timestamp, newConflictGuid.ToString())))
                {
                    // The letter itself is mirrored to both sides by ConflictHandler.addConflictLetter
                    foreach (var c in targetParty.GetCities())
                        MessageHandler.sendMsgInCity(c, Lang.Get("claims:alliance_has_sent_conflict_letter", ourAlliance.getPartNameReplaceUnder()));
                    return TextCommandResult.Success(Lang.Get("claims:conflict_letter_sent"));
                }
                else
                {
                    return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
                }
            }

            //ONE SIDE CAN START CONFLICT
            else
            {
                if (playerInfo == null || !playerInfo.HasAlliance())
                {
                    return TextCommandResult.Success(Lang.Get("claims:sanity_test_for_new_alliance"));
                }

                Conflict newConflict = new Conflict("", Alliance.GetUnusedGuid());
                newConflict.First = ourAlliance;
                newConflict.StartedBy = ourAlliance;
                newConflict.Second = targetParty;
                newConflict.State = ConflictState.CREATED;
                newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;

                foreach (var city in ourAlliance.GetCities())
                    city.AddLogEntry(EnumCityLogEvent.ConflictDeclared, targetParty.GetPartName());
                foreach (var city in targetParty.GetCities())
                    city.AddLogEntry(EnumCityLogEvent.ConflictDeclared, ourAlliance.GetPartName());

                RightsHandler.SetPartiesHostile(ourAlliance, targetParty, newConflict);
                if (targetParty is Alliance targetAllianceForAlly2)
                {
                    RightsHandler.AllianceAllySetHostileOnNewConflictStarted(ourAlliance, targetAllianceForAlly2, newConflict);
                }
                claims.dataStorage.TryAddConflict(newConflict);
                ourAlliance.FireConflictDeclared(targetParty);
                targetParty.saveToDatabase();
                ourAlliance.saveToDatabase();
                newConflict.saveToDatabase(false);
                return TextCommandResult.Success(Lang.Get("claims:conflict_created_with", targetParty.GetPartName()));
            }
        }
        public static TextCommandResult RevokeConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            if (!playerInfo.Alliance.IsLeader(playerInfo))
            {
                return TextCommandResult.Success(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }
            var (parsedName, targetType) = CityCommand.ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!CityCommand.TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string resolveError))
            {
                return TextCommandResult.Error(resolveError);
            }

            Alliance ourAlliance = playerInfo.Alliance;

            if (ConflictHandler.conflictAlreadyExist(ourAlliance, targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_started"));
            }

            var conflictLettersList = ConflictHandler.getReceivedLettersForPartyWithPurpose(targetParty, LetterPurpose.START_CONFLICT);
            ConflictLetter foundLetter = null;
            foreach(var it in conflictLettersList)
            {
                if(it.From.Equals(ourAlliance))
                {
                    foundLetter = it;
                    break;
                }
            }

            if(foundLetter == null)
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            }
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", (foundLetter.Guid, foundLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance, new Dictionary<string, object> { { "value", (foundLetter.Guid, foundLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            ConflictHandler.removeConflictLetter(foundLetter);
            return TextCommandResult.Success(Lang.Get("claims:conflict_declaration_removed", targetParty.GetPartName()));
        }
        /// <summary>
        /// Shared preamble of the alliance-side "answer a conflict letter" commands: the caller must
        /// be in an alliance and the argument must name a resolvable war target. The tail (war-state
        /// check, letter lookup, handler) is shared with the city-side commands.
        /// </summary>
        private static TextCommandResult RespondAsAlliance(TextCommandCallingArgs args, LetterPurpose purpose,
            bool accept, CityCommand.EnumWarStateRequirement requirement)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }

            var (parsedName, targetType) = CityCommand.ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!CityCommand.TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string resolveError))
            {
                return TextCommandResult.Error(resolveError);
            }

            return CityCommand.RespondToConflictLetter(playerInfo.Alliance, targetParty, purpose, accept, requirement);
        }

        public static TextCommandResult AcceptStartConflict(TextCommandCallingArgs args)
            => RespondAsAlliance(args, LetterPurpose.START_CONFLICT, accept: true, CityCommand.EnumWarStateRequirement.NotAtWar);

        public static TextCommandResult DenyStartConflict(TextCommandCallingArgs args)
            => RespondAsAlliance(args, LetterPurpose.START_CONFLICT, accept: false, CityCommand.EnumWarStateRequirement.NotAtWar);
        public static TextCommandResult OfferStopConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            var (parsedName, targetType) = CityCommand.ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!CityCommand.TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string resolveError))
            {
                return TextCommandResult.Error(resolveError);
            }

            Alliance ourAlliance = playerInfo.Alliance;

            if (!ConflictHandler.conflictAlreadyExist(ourAlliance, targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            }

            if (!ConflictHandler.TryGetConflictWithSides(ourAlliance, targetParty, out Conflict conflict))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            }

            if (ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:end_conflict_letter_exist"));
            }

            // Optional peace terms (offerstop [term] [amount]): none / reparations / vassalage.
            PeaceTerms terms = PeaceTerms.Parse(
                args.Parsers.Count > 1 ? (string)args.Parsers[1].GetValue() : null,
                args.Parsers.Count > 2 && args.Parsers[2].GetValue() != null ? System.Convert.ToInt64(args.Parsers[2].GetValue()) : 0);
            if (terms.Type != PeaceTermType.None && !claims.config.WAR_PEACE_TERMS_ENABLED)
            {
                return TextCommandResult.Success(Lang.Get("claims:peace_terms_disabled"));
            }
            if (terms.Type == PeaceTermType.Cession)
            {
                PlotPosition cpos = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
                if (!claims.dataStorage.GetPlot(cpos, out Plot cplot) || !cplot.hasCity() || !targetParty.GetCities().Contains(cplot.getCity()))
                {
                    return TextCommandResult.Success(Lang.Get("claims:cession_stand_on_enemy_plot"));
                }
                terms.CededPlot = cpos;
            }

            //BOTH SIDES HAVE TO AGREE ON CONFLICT START
            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                if (ConflictHandler.addConflictLetter(ConflictLetterFactory.Build(
                        ourAlliance, targetParty, LetterPurpose.END_CONFLICT, timestamp, conflict.Guid, terms)))
                {
                    // The letter itself is mirrored to both sides by ConflictHandler.addConflictLetter
                    foreach (var c in targetParty.GetCities())
                        MessageHandler.sendMsgInCity(c, Lang.Get("claims:alliance_has_sent_conflict_letter", ourAlliance.getPartNameReplaceUnder()));
                    return TextCommandResult.Success(Lang.Get("claims:conflict_letter_sent"));
                }
                else
                {
                    return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
                }
            }

            //ONE SIDE CAN START CONFLICT
            else
            {
                if (playerInfo == null || !playerInfo.HasAlliance())
                {
                    return TextCommandResult.Success(Lang.Get("claims:sanity_test_for_new_alliance"));
                }

                // Imposing reparations/vassalage always requires the other side's consent —
                // in unilateral mode only a plain (no-terms) peace is allowed.
                if (terms.Type != PeaceTermType.None)
                {
                    return TextCommandResult.Success(Lang.Get("claims:peace_terms_require_agreement"));
                }
                PartDemolition.DemolishConflict(conflict);
                return TextCommandResult.Success(Lang.Get("claims:conflict_stopped_with", targetParty.GetPartName()));
            }
        }
        public static TextCommandResult AcceptStopConflict(TextCommandCallingArgs args)
            => RespondAsAlliance(args, LetterPurpose.END_CONFLICT, accept: true, CityCommand.EnumWarStateRequirement.AtWar);

        public static TextCommandResult DenyStopConflict(TextCommandCallingArgs args)
            => RespondAsAlliance(args, LetterPurpose.END_CONFLICT, accept: false, CityCommand.EnumWarStateRequirement.AtWar);
        /// <summary>
        /// Shared preamble of the union commands: the caller must be in an alliance and the first
        /// argument must name an existing one. Keeps the /alliance union subcommands from each
        /// carrying their own copy of the same four checks.
        /// </summary>
        private static bool TryResolveUnionSides(TextCommandCallingArgs args, out PlayerInfo playerInfo,
            out Alliance ourAlliance, out Alliance targetAlliance, out TextCommandResult err)
        {
            ourAlliance = null;
            targetAlliance = null;
            if (!TryResolveCaller(args, out _, out playerInfo, out err)) return false;

            if (!playerInfo.HasAlliance())
            {
                err = TextCommandResult.Success(Lang.Get("claims:no_alliance"));
                return false;
            }
            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                err = TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
                return false;
            }
            if (!claims.dataStorage.GetAllianceByName(name, out targetAlliance))
            {
                err = TextCommandResult.Error(Lang.Get("claims:no_such_alliance"));
                return false;
            }

            ourAlliance = playerInfo.Alliance;
            err = null;
            return true;
        }

        public static TextCommandResult DeclareUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveUnionSides(args, out _, out Alliance ourAlliance, out Alliance targetAlliance, out var err))
                return err;

            if(ourAlliance.Equals(targetAlliance))
            {
                return TextCommandResult.Error(Lang.Get("claims:same_alliance"));
            }
            if (ourAlliance.Neutral)
            {
                return TextCommandResult.Success(Lang.Get("claims:our_alliance_is_neutral"));
            }
            if (UnionHander.unionAlreadyExist(ourAlliance, targetAlliance))
            {
                return TextCommandResult.Success(Lang.Get("claims:union_already_exists"));
            }
            if (targetAlliance.Neutral)
            {
                return TextCommandResult.Success(Lang.Get("claims:target_alliance_is_neutral"));
            }
            // Allying the party you are at war with would leave both sides in HostileCities AND
            // ComradeCities at once - PvP would stay open while ally rights are granted. Make peace first.
            if (ConflictHandler.conflictAlreadyExist(ourAlliance, targetAlliance))
            {
                return TextCommandResult.Success(Lang.Get("claims:union_blocked_by_war"));
            }
            long reformLeft = UnionBreakHelper.ReformCooldownLeft(ourAlliance, targetAlliance);
            if (reformLeft > 0)
            {
                return TextCommandResult.Success(Lang.Get("claims:union_reform_on_cooldown", StringFunctions.FormatDuration(reformLeft)));
            }

            long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
            string letterGuid = UnionLetter.GetUnusedGuid().ToString();
            // The letter's behaviour lives in the factory so it can be rebuilt after a restart;
            // addUnionLetter persists it and mirrors it to both sides' GUI.
            if (!UnionHander.addUnionLetter(UnionLetterFactory.Build(ourAlliance, targetAlliance,
                    UnionLetterPurpose.Form, timestamp, letterGuid)))
            {
                return TextCommandResult.Success(Lang.Get("claims:union_letter_is_duplicate"));
            }

            MessageHandler.SendMsgInAlliance(targetAlliance, Lang.Get("claims:alliance_has_sent_union_letter", ourAlliance.getPartNameReplaceUnder()));
            return TextCommandResult.Success(Lang.Get("claims:union_letter_sent"));
        }
        public static TextCommandResult RevokeUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveUnionSides(args, out _, out Alliance ourAlliance, out Alliance targetAlliance, out var err))
                return err;

            if(ourAlliance.ComradAlliancies == null || !ourAlliance.ComradAlliancies.Contains(targetAlliance))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_union_found"));
            }

            // One-sided exit = denunciation: announced now, effective after a delay. The messages,
            // the log entries and the actual break all happen inside the helper.
            string errKey = UnionBreakHelper.TryAnnounceBreak(ourAlliance, targetAlliance);
            if (errKey != null) return TextCommandResult.Success(Lang.Get(errKey));

            long left = UnionBreakHelper.PendingBreakLeft(ourAlliance, targetAlliance);
            return TextCommandResult.Success(left > 0
                ? Lang.Get("claims:union_break_announced_by_us", targetAlliance.getPartNameReplaceUnder(), StringFunctions.FormatDuration(left))
                : Lang.Get("claims:union_broken", targetAlliance.getPartNameReplaceUnder()));
        }

        /// <summary>Calls off a denunciation before it takes effect (either side may do it).</summary>
        public static TextCommandResult CancelUnionBreak(TextCommandCallingArgs args)
        {
            if (!TryResolveUnionSides(args, out _, out Alliance ourAlliance, out Alliance targetAlliance, out var err))
                return err;

            string errKey = UnionBreakHelper.TryCancelBreak(ourAlliance, targetAlliance);
            if (errKey != null) return TextCommandResult.Success(Lang.Get(errKey));
            return TextCommandResult.Success(Lang.Get("claims:union_break_cancelled", targetAlliance.getPartNameReplaceUnder()));
        }

        /// <summary>Offers to dissolve the union by mutual consent: no delay and no cooldowns for either side.</summary>
        public static TextCommandResult DissolveUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveUnionSides(args, out _, out Alliance ourAlliance, out Alliance targetAlliance, out var err))
                return err;

            if (!UnionHander.unionAlreadyExist(ourAlliance, targetAlliance))
                return TextCommandResult.Success(Lang.Get("claims:no_union_found"));
            if (claims.config.UNION_BREAK_BLOCKED_IN_SHARED_WAR && UnionBreakHelper.SharesRunningWar(ourAlliance, targetAlliance))
                return TextCommandResult.Success(Lang.Get("claims:union_break_blocked_shared_war"));
            if (UnionHander.TryGetUnionLetter(ourAlliance, targetAlliance, out _))
                return TextCommandResult.Success(Lang.Get("claims:union_letter_is_duplicate"));

            long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
            string letterGuid = UnionLetter.GetUnusedGuid().ToString();
            if (!UnionHander.addUnionLetter(UnionLetterFactory.Build(ourAlliance, targetAlliance,
                    UnionLetterPurpose.Dissolve, timestamp, letterGuid)))
            {
                return TextCommandResult.Success(Lang.Get("claims:union_letter_is_duplicate"));
            }

            MessageHandler.SendMsgInAlliance(targetAlliance, Lang.Get("claims:union_dissolve_offered", ourAlliance.getPartNameReplaceUnder()));
            return TextCommandResult.Success(Lang.Get("claims:union_dissolve_sent", targetAlliance.getPartNameReplaceUnder()));
        }
        public static TextCommandResult UnsendInviteUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveUnionSides(args, out _, out Alliance ourAlliance, out Alliance targetAlliance, out var err))
                return err;

            if(!UnionHander.TryGetUnionLetter(ourAlliance, targetAlliance, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_union_letter_found"));
            }
            // The handler drops the letter and syncs both GUIs itself (UnionLetterFactory.RemoveAndSync).
            letter.OnDeny?.Invoke();
            return TextCommandResult.Success(Lang.Get("claims:union_declaration_removed", targetAlliance.getPartNameReplaceUnder()));
        }
        public static TextCommandResult AcceptInviteUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveUnionSides(args, out _, out Alliance ourAlliance, out Alliance targetAlliance, out var err))
                return err;

            if (!UnionHander.TryGetUnionLetter(ourAlliance, targetAlliance, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_union_letter_found"));
            }
            // The letter's own handler removes it (and its stored row); dropping it again here would
            // be a no-op at best and, for a Dissolve letter, would hide the message it just sent.
            letter.OnAccept?.Invoke();
            return TextCommandResult.Success(Lang.Get("claims:union_letter_accepted", targetAlliance.getPartNameReplaceUnder()));
        }
    }
}
