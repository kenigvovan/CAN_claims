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
using System;
using System.Collections.Generic;
using System.Threading;
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
                new Thread(new ThreadStart(() =>
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
                })), player.PlayerUID));
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
                RightsHandler.reapplyRights(playerInCity);
            }
            RightsHandler.RemoveCityHostilesInAlliance(city, alliance);
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
                new Thread(new ThreadStart(() =>
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
                })),
                new Thread(new ThreadStart(() =>
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:disagrre_with_invitation_to_alliance", playerInfo.GetPartName(), city.GetPartName()));
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                        new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.TO_ALLIANCE_INVITE_REMOVE);
                }))
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
                    return TextCommandResult.Success(Lang.Get("claims:wait_before") + TimeFunctions.getDateFromEpochSeconds(stamp));
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
            //BOTH SIDES HAVE TO AGREE ON CONFLICT START
            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                string newConflictGuid = ConflictLetter.GetUnusedGuid().ToString();
                if (ConflictHandler.addConflictLetter(new ConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT, timestamp,
                        () =>
                        {
                            if (playerInfo == null || !playerInfo.HasAlliance())
                            {
                                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:sanity_test_for_new_alliance"));
                                return;
                            }

                            if(!ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT, out var letter))
                            {
                                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_letter_found"));
                                return;
                            }

                            Conflict newConflict = new Conflict("", newConflictGuid);

                            foreach (var city in ourAlliance.GetCities())
                                city.AddLogEntry(EnumCityLogEvent.ConflictDeclared, targetParty.GetPartName());
                            foreach (var city in targetParty.GetCities())
                                city.AddLogEntry(EnumCityLogEvent.ConflictDeclared, ourAlliance.GetPartName());

                            RightsHandler.SetPartiesHostile(ourAlliance, targetParty, newConflict);
                            if (targetParty is Alliance targetAllianceForAlly)
                            {
                                RightsHandler.AllianceAllySetHostileOnNewConflictStarted(ourAlliance, targetAllianceForAlly, newConflict);
                            }
                            claims.dataStorage.TryAddConflict(newConflict);
                            ConflictHandler.removeConflictLetter(letter);

                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance,
                                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);

                            newConflict.First = ourAlliance;
                            newConflict.Second = targetParty;
                            newConflict.StartedBy = ourAlliance;
                            newConflict.State = ConflictState.CREATED;
                            newConflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
                            newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
                            ourAlliance.FireConflictDeclared(targetParty);
                            var conflictCellElement = ClientConflictCellElement.FromConflict(newConflict);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance,
                                new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);

                            targetParty.saveToDatabase();
                            ourAlliance.saveToDatabase();
                            newConflict.saveToDatabase(false);
                            ConflictHandler.removeConflictLetter(letter);
                            foreach (var c in targetParty.GetCities())
                                MessageHandler.sendMsgInCity(c, Lang.Get("claims:conflict_created_with", ourAlliance.getPartNameReplaceUnder()));
                        },
                        () =>
                        {
                            if (ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT, out var denyLetter))
                            {
                                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance,
                                    new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                    new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            }
                            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:conflict_denied"));
                            ConflictHandler.removeConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT);
                        }
                        , newConflictGuid.ToString())))
                {
                    ConflictHandler.TryGetConflictLetter(newConflictGuid.ToString(), out ConflictLetter conflictLetter);
                    var letterCellElement = new ClientConflictLetterCellElement(conflictLetter.From.GetPartName(), conflictLetter.From.Guid.ToString(),
                            WarTargetTypeHelper.FromConflictParty(conflictLetter.From),
                            conflictLetter.To.GetPartName(), conflictLetter.To.Guid.ToString(),
                            WarTargetTypeHelper.FromConflictParty(conflictLetter.To),
                            conflictLetter.Purpose, conflictLetter.TimeStampExpire, conflictLetter.Guid);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
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
                newConflict.First = ourAlliance;
                newConflict.StartedBy = ourAlliance;
                newConflict.Second = targetParty;
                newConflict.State = ConflictState.CREATED;
                newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
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
        public static TextCommandResult AcceptStartConflict(TextCommandCallingArgs args)
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

            if (ConflictHandler.conflictAlreadyExist(ourAlliance, targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));
            }

            if(!ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            }

            letter.OnAccept?.Invoke();
            return TextCommandResult.Success();
        }
        public static TextCommandResult DenyStartConflict(TextCommandCallingArgs args)
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

            if (ConflictHandler.conflictAlreadyExist(ourAlliance, targetParty))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));
            }

            if (!ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.START_CONFLICT, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            }
            letter.OnDeny?.Invoke();
            return TextCommandResult.Success();
        }
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

            //BOTH SIDES HAVE TO AGREE ON CONFLICT START
            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                if (ConflictHandler.addConflictLetter(new ConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT, timestamp,
                        () =>
                        {
                            if (playerInfo == null || !playerInfo.HasAlliance())
                            {
                                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:sanity_test_for_new_alliance"));
                                return;
                            }

                            if(!ConflictHandler.TryGetConflictWithSides(ourAlliance, targetParty, out Conflict conflict))
                            {
                                foreach (var c in targetParty.GetCities())
                                    MessageHandler.sendMsgInCity(c, Lang.Get("claims:conflict_not_found"));
                                return;
                            }
                            if (ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT, out var acceptLetter))
                            {
                                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance,
                                    new Dictionary<string, object> { { "value", (acceptLetter.Guid, acceptLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                    new Dictionary<string, object> { { "value", (acceptLetter.Guid, acceptLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                                ConflictHandler.removeConflictLetter(acceptLetter);
                            }
                            PartDemolition.DemolishConflict(conflict);
                            foreach (var c in targetParty.GetCities())
                                MessageHandler.sendMsgInCity(c, Lang.Get("claims:conflict_stopped_with", ourAlliance.getPartNameReplaceUnder()));
                        },
                        () =>
                        {
                            if (ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT, out var denyLetter))
                            {
                                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance,
                                    new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                    new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            }
                            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:conflict_stop_denied"));
                            ConflictHandler.removeConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT);
                        },
                        conflict.Guid
                        )))
                {
                    ConflictHandler.TryGetConflictLetter(conflict.Guid, out ConflictLetter conflictLetter);
                    var letterCellElement = new ClientConflictLetterCellElement(conflictLetter.From.GetPartName(), conflictLetter.From.Guid.ToString(),
                            WarTargetTypeHelper.FromConflictParty(conflictLetter.From),
                            conflictLetter.To.GetPartName(), conflictLetter.To.Guid.ToString(),
                            WarTargetTypeHelper.FromConflictParty(conflictLetter.To),
                            conflictLetter.Purpose, conflictLetter.TimeStampExpire, conflictLetter.Guid);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
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

                PartDemolition.DemolishConflict(conflict);
                return TextCommandResult.Success(Lang.Get("claims:conflict_stopped_with", targetParty.GetPartName()));
            }
        }
        public static TextCommandResult AcceptStopConflict(TextCommandCallingArgs args)
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

            if (!ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            }

            letter.OnAccept?.Invoke();
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance, new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);

            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", letter.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance, new Dictionary<string, object> { { "value", letter.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE);
            return TextCommandResult.Success();
        }
        public static TextCommandResult DenyStopConflict(TextCommandCallingArgs args)
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

            if (!ConflictHandler.TryGetConflictLetter(ourAlliance, targetParty, LetterPurpose.END_CONFLICT, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            }

            letter.OnDeny?.Invoke();
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourAlliance, new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            return TextCommandResult.Success();
        }
        public static TextCommandResult DeclareUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!claims.dataStorage.GetAllianceByName(name, out Alliance targetAlliance))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance"));
            }

            Alliance ourAlliance = playerInfo.Alliance;

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

            long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
            string newConflictGuid = UnionLetter.GetUnusedGuid().ToString();
            if (UnionHander.addUnionLetter(new UnionLetter(ourAlliance, targetAlliance, timestamp,
                    () =>
                    {
                        if (playerInfo == null || !playerInfo.HasAlliance())
                        {
                            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:sanity_test_for_new_alliance"));
                            return;
                        }

                        if (!UnionHander.TryGetUnionLetter(ourAlliance, targetAlliance, out var letter))
                        {
                            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_letter_found"));
                            return;
                        }
                        PartInits.InitNewUnion(ourAlliance, targetAlliance);

                        UsefullPacketsSend.AddToQueueAllianceInfoUpdate(ourAlliance.Guid,
                            new Dictionary<string, object> { { "value", targetAlliance.GetPartName()  } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_ADDED);
                        UsefullPacketsSend.AddToQueueAllianceInfoUpdate(targetAlliance.Guid,
                            new Dictionary<string, object> { { "value", ourAlliance.GetPartName()  } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_ADDED);

                        UsefullPacketsSend.AddToQueueAllianceInfoUpdate(targetAlliance.Guid, new Dictionary<string, object> { { "value", newConflictGuid } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE);
                        UsefullPacketsSend.AddToQueueAllianceInfoUpdate(ourAlliance.Guid, new Dictionary<string, object> { { "value", newConflictGuid } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE);
                    },
                    () =>
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:union_denied"));
                        UnionHander.removeUnionLetter(ourAlliance, targetAlliance);
                    }
                    , newConflictGuid.ToString())))
            {
                UnionHander.TryGetUnionLetter(newConflictGuid.ToString(), out UnionLetter conflictLetter);
                UsefullPacketsSend.AddToQueueAllianceInfoUpdate(targetAlliance.Guid, new Dictionary<string, object> { { "value",
                            new ClientUnionLetterCellElement(conflictLetter.From.GetPartName(), conflictLetter.From.Guid.ToString(),
                            conflictLetter.To.GetPartName(), conflictLetter.To.Guid.ToString(),
                            conflictLetter.TimeStampExpire, conflictLetter.Guid) } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ADD);
                UsefullPacketsSend.AddToQueueAllianceInfoUpdate(ourAlliance.Guid, new Dictionary<string, object> { { "value", new ClientUnionLetterCellElement(
                            conflictLetter.From.GetPartName(), conflictLetter.From.Guid.ToString(),
                            conflictLetter.To.GetPartName(), conflictLetter.To.Guid.ToString(),
                            conflictLetter.TimeStampExpire, conflictLetter.Guid) } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ADD);

                MessageHandler.SendMsgInAlliance(targetAlliance, Lang.Get("claims:alliance_has_sent_union_letter", ourAlliance.getPartNameReplaceUnder()));
                return TextCommandResult.Success(Lang.Get("claims:union_letter_sent"));
            }
            else
            {
                return TextCommandResult.Success(Lang.Get("claims:union_letter_is_duplicate"));
            }
        }
        public static TextCommandResult RevokeUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!claims.dataStorage.GetAllianceByName(name, out Alliance targetAlliance))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance"));
            }

            Alliance ourAlliance = playerInfo.Alliance;

            if(ourAlliance.ComradAlliancies == null || !ourAlliance.ComradAlliancies.Contains(targetAlliance))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_union_found"));
            }

            PartDemolition.DemolishUnion(ourAlliance, targetAlliance);

            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(targetAlliance.Guid, new Dictionary<string, object> { { "value", ourAlliance.GetPartName()} }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_REMOVED);
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(ourAlliance.Guid, new Dictionary<string, object> { { "value", targetAlliance.GetPartName() } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_REMOVED);
            return TextCommandResult.Success(Lang.Get("claims:union_declaration_removed", targetAlliance.getPartNameReplaceUnder()));
        }
        public static TextCommandResult UnsendInviteUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!claims.dataStorage.GetAllianceByName(name, out Alliance targetAlliance))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance"));
            }

            Alliance ourAlliance = playerInfo.Alliance;

            if(!UnionHander.TryGetUnionLetter(ourAlliance, targetAlliance, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_union_letter_found"));
            }
            letter.OnDeny?.Invoke();
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(targetAlliance.Guid, new Dictionary<string, object> { { "value", letter.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(ourAlliance.Guid, new Dictionary<string, object> { { "value", letter.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE);
            return TextCommandResult.Success(Lang.Get("claims:union_declaration_removed", targetAlliance.getPartNameReplaceUnder()));
        }
        public static TextCommandResult AcceptInviteUnion(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;

            if (!playerInfo.HasAlliance())
            {
                return TextCommandResult.Success(Lang.Get("claims:no_alliance"));
            }
            string name = Filter.filterName((string)args.Parsers[0].GetValue());
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));
            }
            if (!claims.dataStorage.GetAllianceByName(name, out Alliance targetAlliance))
            {
                return TextCommandResult.Error(Lang.Get("claims:no_such_alliance"));
            }

            Alliance ourAlliance = playerInfo.Alliance;

            if (!UnionHander.TryGetUnionLetter(ourAlliance, targetAlliance, out var letter))
            {
                return TextCommandResult.Success(Lang.Get("claims:no_union_letter_found"));
            }
            letter.OnAccept?.Invoke();
            UnionHander.removeUnionLetter(letter);
            return TextCommandResult.Success(Lang.Get("claims:union_letter_accepted", targetAlliance.getPartNameReplaceUnder()));
        }
    }
}
