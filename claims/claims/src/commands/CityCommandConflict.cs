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
    public partial class CityCommand
    {
        /*==============================================================================================*/
        /* CITY WAR COMMANDS                                                                            */
        /*==============================================================================================*/

        public static TextCommandResult DeclareCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
                return TextCommandResult.Success(Lang.Get("claims:no_city"));
            if (playerInfo.HasAlliance())
                return TextCommandResult.Success(Lang.Get("claims:city_in_alliance_use_alliance_war"));
            if (!playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));

            City ourCity = playerInfo.City;
            if (ourCity.Neutral)
                return TextCommandResult.Success(Lang.Get("claims:our_alliance_is_neutral"));

            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
                return TextCommandResult.Error(Lang.Get("claims:invalid_alliance_name"));

            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (targetParty.Equals(ourCity))
                return TextCommandResult.Success(Lang.Get("claims:same_alliance"));
            if (targetParty.Neutral)
                return TextCommandResult.Success(Lang.Get("claims:target_alliance_is_neutral"));
            if (ConflictHandler.conflictAlreadyExist(ourCity, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));

            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                string newConflictGuid = ConflictLetter.GetUnusedGuid().ToString();
                if (ConflictHandler.addConflictLetter(new ConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, timestamp,
                    () =>
                    {
                        if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out var letter)) return;
                        Conflict newConflict = new Conflict("", newConflictGuid);
                        RightsHandler.SetPartiesHostile(ourCity, targetParty, newConflict);
                        claims.dataStorage.TryAddConflict(newConflict);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                            new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                            new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                        ConflictHandler.removeConflictLetter(letter);
                        newConflict.First = ourCity;
                        newConflict.Second = targetParty;
                        newConflict.StartedBy = ourCity;
                        newConflict.State = ConflictState.CREATED;
                        newConflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
                        newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
                        var conflictCellElement = ClientConflictCellElement.FromConflict(newConflict);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                            new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                            new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                        ourCity.saveToDatabase();
                        targetParty.saveToDatabase();
                        newConflict.saveToDatabase(false);
                        MessageHandler.sendMsgInCity(ourCity, Lang.Get("claims:conflict_created_with", targetParty.GetPartName()));
                        foreach (var c in targetParty.GetCities())
                            MessageHandler.sendMsgInCity(c, Lang.Get("claims:conflict_created_with", ourCity.getPartNameReplaceUnder()));
                    },
                    () =>
                    {
                        if (ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out var denyLetter))
                        {
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                                new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                        }
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:conflict_denied"));
                        ConflictHandler.removeConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT);
                    },
                    newConflictGuid)))
                {
                    ConflictHandler.TryGetConflictLetter(newConflictGuid, out ConflictLetter conflictLetter);
                    var letterCellElement = new ClientConflictLetterCellElement(conflictLetter.From.GetPartName(), conflictLetter.From.Guid.ToString(),
                            WarTargetTypeHelper.FromConflictParty(conflictLetter.From),
                            conflictLetter.To.GetPartName(), conflictLetter.To.Guid.ToString(),
                            WarTargetTypeHelper.FromConflictParty(conflictLetter.To),
                            conflictLetter.Purpose, conflictLetter.TimeStampExpire, conflictLetter.Guid);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                    foreach (var c in targetParty.GetCities())
                        MessageHandler.sendMsgInCity(c, Lang.Get("claims:alliance_has_sent_conflict_letter", ourCity.getPartNameReplaceUnder()));
                    return TextCommandResult.Success(Lang.Get("claims:conflict_letter_sent"));
                }
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
            }
            else
            {
                Conflict newConflict = new Conflict("", Alliance.GetUnusedGuid());
                RightsHandler.SetPartiesHostile(ourCity, targetParty, newConflict);
                claims.dataStorage.TryAddConflict(newConflict);
                newConflict.First = ourCity;
                newConflict.StartedBy = ourCity;
                newConflict.Second = targetParty;
                newConflict.State = ConflictState.CREATED;
                newConflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
                newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
                var conflictCellElement = ClientConflictCellElement.FromConflict(newConflict);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                    new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                    new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                ourCity.saveToDatabase();
                targetParty.saveToDatabase();
                newConflict.saveToDatabase(false);
                return TextCommandResult.Success(Lang.Get("claims:conflict_created_with", targetParty.GetPartName()));
            }
        }

        public static TextCommandResult RevokeCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || playerInfo.HasAlliance() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:no_city"));

            City ourCity = playerInfo.City;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (ConflictHandler.conflictAlreadyExist(ourCity, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_started"));
            if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            ConflictHandler.removeConflictLetter(letter);
            return TextCommandResult.Success(Lang.Get("claims:conflict_declaration_removed", targetParty.GetPartName()));
        }

        public static TextCommandResult AcceptStartCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || playerInfo.HasAlliance() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:no_city"));

            City ourCity = playerInfo.City;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (ConflictHandler.conflictAlreadyExist(ourCity, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));
            if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnAccept?.Invoke();
            return TextCommandResult.Success();
        }

        public static TextCommandResult DenyStartCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || playerInfo.HasAlliance() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:no_city"));

            City ourCity = playerInfo.City;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnDeny?.Invoke();
            return TextCommandResult.Success();
        }

        public static TextCommandResult OfferStopCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || playerInfo.HasAlliance() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:no_city"));

            City ourCity = playerInfo.City;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.conflictAlreadyExist(ourCity, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            if (!ConflictHandler.TryGetConflictWithSides(ourCity, targetParty, out Conflict conflict))
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            if (ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT, out _))
                return TextCommandResult.Success(Lang.Get("claims:end_conflict_letter_exist"));

            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                if (ConflictHandler.addConflictLetter(new ConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT, timestamp,
                    () =>
                    {
                        if (!ConflictHandler.TryGetConflictWithSides(ourCity, targetParty, out Conflict c)) return;
                        if (ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT, out var acceptLetter))
                        {
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                                new Dictionary<string, object> { { "value", (acceptLetter.Guid, acceptLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                new Dictionary<string, object> { { "value", (acceptLetter.Guid, acceptLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            ConflictHandler.removeConflictLetter(acceptLetter);
                        }
                        PartDemolition.DemolishConflict(c);
                        foreach (var city in targetParty.GetCities())
                            MessageHandler.sendMsgInCity(city, Lang.Get("claims:conflict_stopped_with", ourCity.getPartNameReplaceUnder()));
                    },
                    () =>
                    {
                        if (ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT, out var denyLetter))
                        {
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                                new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                                new Dictionary<string, object> { { "value", (denyLetter.Guid, denyLetter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                        }
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:conflict_stop_denied"));
                        ConflictHandler.removeConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT);
                    },
                    conflict.Guid)))
                {
                    return TextCommandResult.Success(Lang.Get("claims:conflict_letter_sent"));
                }
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
            }
            else
            {
                PartDemolition.DemolishConflict(conflict);
                return TextCommandResult.Success(Lang.Get("claims:conflict_stopped_with", targetParty.GetPartName()));
            }
        }

        public static TextCommandResult AcceptStopCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || playerInfo.HasAlliance() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:no_city"));

            City ourCity = playerInfo.City;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.conflictAlreadyExist(ourCity, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnAccept?.Invoke();
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                new Dictionary<string, object> { { "value", letter.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                new Dictionary<string, object> { { "value", letter.Guid } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_REMOVE);
            return TextCommandResult.Success();
        }

        public static TextCommandResult DenyStopCityConflict(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || playerInfo.HasAlliance() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:no_city"));

            City ourCity = playerInfo.City;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.conflictAlreadyExist(ourCity, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:no_conflict_found"));
            if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.END_CONFLICT, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnDeny?.Invoke();
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            return TextCommandResult.Success();
        }
    }
}
