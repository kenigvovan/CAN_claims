using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.beb;
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
using claims.src.part.structure.war;
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

            if (ourCity.IsVassal() && ourCity.OverlordGuid == targetParty.Guid)
                return TextCommandResult.Success(Lang.Get("claims:cannot_war_overlord"));
            // Check for a duplicate declaration letter BEFORE the gates: the gates withdraw the
            // declaration cost, and a late "duplicate" rejection would eat the money.
            if (claims.config.NEED_AGREE_FOR_CONFLICT
                && ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out _))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
            if (!WarDeclarationHelper.TryPassDeclarationGates(ourCity, targetParty, ourCity.MoneyAccountName, out string declErr))
                return TextCommandResult.Success(Lang.Get(declErr));

            if (claims.config.NEED_AGREE_FOR_CONFLICT)
            {
                long timestamp = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
                string newConflictGuid = ConflictLetter.GetUnusedGuid().ToString();
                if (ConflictHandler.addConflictLetter(new ConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, timestamp,
                    () =>
                    {
                        if (!ConflictHandler.TryGetConflictLetter(ourCity, targetParty, LetterPurpose.START_CONFLICT, out var letter)) return;
                        Conflict newConflict = new Conflict("", newConflictGuid);
                        newConflict.First = ourCity;
                        newConflict.Second = targetParty;
                        newConflict.StartedBy = ourCity;
                        newConflict.State = ConflictState.CREATED;
                        newConflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
                        newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
                        RightsHandler.SetPartiesHostile(ourCity, targetParty, newConflict);
                        if (targetParty is Alliance targetAllianceForAlly)
                            RightsHandler.AllianceAllySetHostileOnNewConflictStarted(targetAllianceForAlly, ourCity, newConflict);
                        claims.dataStorage.TryAddConflict(newConflict);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourCity,
                            new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                        UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty,
                            new Dictionary<string, object> { { "value", (letter.Guid, letter.Purpose) } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
                        ConflictHandler.removeConflictLetter(letter);
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
                newConflict.First = ourCity;
                newConflict.StartedBy = ourCity;
                newConflict.Second = targetParty;
                newConflict.State = ConflictState.CREATED;
                newConflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
                newConflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
                RightsHandler.SetPartiesHostile(ourCity, targetParty, newConflict);
                if (targetParty is Alliance targetAllianceForAlly)
                    RightsHandler.AllianceAllySetHostileOnNewConflictStarted(targetAllianceForAlly, ourCity, newConflict);
                claims.dataStorage.TryAddConflict(newConflict);
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
            return TextCommandResult.Success();
        }

        // Offers peace with terms (white / reparations <amount> / vassalage). Proposer = winner.
        public static TextCommandResult OfferPeaceCityConflict(TextCommandCallingArgs args)
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

            PeaceTerms terms = PeaceTerms.Parse((string)args.Parsers[1].GetValue(),
                args.Parsers.Count > 2 && args.Parsers[2].GetValue() != null ? System.Convert.ToInt64(args.Parsers[2].GetValue()) : 0);
            if (terms.Type != PeaceTermType.None && !claims.config.WAR_PEACE_TERMS_ENABLED)
                return TextCommandResult.Success(Lang.Get("claims:peace_terms_disabled"));
            if (terms.Type == PeaceTermType.Cession)
            {
                // Cede the enemy plot the proposer is standing on.
                PlotPosition cpos = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
                if (!claims.dataStorage.GetPlot(cpos, out Plot cplot) || !cplot.hasCity() || !targetParty.GetCities().Contains(cplot.getCity()))
                    return TextCommandResult.Success(Lang.Get("claims:cession_stand_on_enemy_plot"));
                terms.CededPlot = cpos;
            }

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
                    PeaceTermsHelper.ApplyWithConfirm(ourCity, targetParty, terms,
                        () =>
                        {
                            PartDemolition.DemolishConflict(c);
                            foreach (var city in targetParty.GetCities())
                                MessageHandler.sendMsgInCity(city, Lang.Get("claims:conflict_stopped_with", ourCity.getPartNameReplaceUnder()));
                        },
                        () =>
                        {
                            // The owning member refused the plot cession — peace is not concluded, the war continues.
                            MessageHandler.sendMsgInCity(ourCity, Lang.Get("claims:peace_cession_refused", targetParty.GetPartName()));
                            MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:peace_cession_refused_them", ourCity.getPartNameReplaceUnder()));
                        });
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

        // Places a bounty on a player's head (escrowed from the poster).
        public static TextCommandResult BountyPlace(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var posterInfo, out var callerErr)) return callerErr;
            string targetName = (string)args.Parsers[0].GetValue();
            long amount = System.Convert.ToInt64(args.Parsers[1].GetValue());
            if (!claims.dataStorage.getPlayerByName(targetName, out PlayerInfo targetInfo))
                return TextCommandResult.Success(Lang.Get("claims:player_not_found"));
            string err = BountyHelper.Place(targetInfo, posterInfo, amount, out long total);
            if (err != null) return TextCommandResult.Success(Lang.Get(err));
            return TextCommandResult.Success(Lang.Get("claims:bounty_placed", amount, targetInfo.GetPartName(), total));
        }

        // Cancels the caller's own bounty on a player and refunds it.
        public static TextCommandResult BountyCancel(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var posterInfo, out var callerErr)) return callerErr;
            string targetName = (string)args.Parsers[0].GetValue();
            if (!claims.dataStorage.getPlayerByName(targetName, out PlayerInfo targetInfo))
                return TextCommandResult.Success(Lang.Get("claims:player_not_found"));
            string err = BountyHelper.Cancel(targetInfo, posterInfo, out long refunded);
            if (err != null) return TextCommandResult.Success(Lang.Get(err));
            return TextCommandResult.Success(Lang.Get("claims:bounty_cancelled", refunded, targetInfo.GetPartName()));
        }

        // Shows the total bounty on a player's head.
        public static TextCommandResult BountyList(TextCommandCallingArgs args)
        {
            string targetName = (string)args.Parsers[0].GetValue();
            if (!claims.dataStorage.getPlayerByName(targetName, out PlayerInfo targetInfo))
                return TextCommandResult.Success(Lang.Get("claims:player_not_found"));
            return TextCommandResult.Success(Lang.Get("claims:bounty_on_head", targetInfo.GetPartName(), targetInfo.GetBountyTotal()));
        }

        // Shows this city's overlord (if a vassal) and its own vassals.
        public static TextCommandResult VassalsInfo(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Success(Lang.Get("claims:no_city"));
            City city = playerInfo.City;
            StringBuilder sb = new StringBuilder();
            if (city.IsVassal())
            {
                City overlord = city.GetOverlord();
                sb.AppendLine(Lang.Get("claims:vassal_status_overlord", overlord?.GetPartName() ?? "?"));
            }
            if (city.VassalCities.Count > 0)
                sb.AppendLine(Lang.Get("claims:vassal_status_vassals", string.Join(", ", city.VassalCities.Select(c => c.GetPartName()))));
            if (sb.Length == 0) sb.Append(Lang.Get("claims:vassal_status_none"));
            return TextCommandResult.Success(sb.ToString());
        }

        // Breaks free from an overlord; the overlord gets a casus belli to re-subjugate.
        public static TextCommandResult Rebel(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));
            City city = playerInfo.City;
            if (!city.IsVassal())
                return TextCommandResult.Success(Lang.Get("claims:not_a_vassal"));
            City overlord = city.GetOverlord();
            PeaceTermsHelper.ReleaseVassal(city);
            if (overlord != null)
            {
                WarDeclarationHelper.RecordGrievance(overlord, city.HasAlliance() ? city.Alliance.Guid : city.Guid);
                MessageHandler.sendMsgInCity(overlord, Lang.Get("claims:vassal_rebelled", city.getPartNameReplaceUnder()));
            }
            return TextCommandResult.Success(Lang.Get("claims:you_rebelled"));
        }

        // Lists the parties this city currently has a casus belli against (recent grievances).
        public static TextCommandResult CasusBelliInfo(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity()) return TextCommandResult.Success(Lang.Get("claims:no_city"));
            City city = playerInfo.City;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long grace = (long)claims.config.WAR_CASUS_BELLI_GRACE_DAYS * 86400;
            StringBuilder sb = new StringBuilder();
            foreach (var kv in city.Grievances)
            {
                if (now - kv.Value > grace) continue;
                string name = claims.dataStorage.getCityByGUID(kv.Key, out City c) ? c.GetPartName()
                    : (claims.dataStorage.GetAllianceByGUID(kv.Key, out Alliance a) ? a.GetPartName() : kv.Key);
                sb.AppendLine(Lang.Get("claims:cb_against", name));
            }
            if (sb.Length == 0) sb.Append(Lang.Get("claims:cb_none"));
            return TextCommandResult.Success(sb.ToString());
        }

        // Offers a non-aggression pact (timed mutual no-war) via a letter. Party-level: caller = alliance (leader) or city (mayor).
        public static TextCommandResult NapOffer(TextCommandCallingArgs args)
        {
            if (!claims.config.WAR_NAP_ENABLED) return TextCommandResult.Success(Lang.Get("claims:nap_disabled"));
            if (!TryResolveMyParty(args, true, out _, out _, out var ourParty, out var err)) return err;

            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (targetParty.Equals(ourParty)) return TextCommandResult.Success(Lang.Get("claims:same_alliance"));
            if (ConflictHandler.conflictAlreadyExist(ourParty, targetParty)) return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));
            if (NonAggressionHelper.HasActivePact(ourParty, targetParty)) return TextCommandResult.Success(Lang.Get("claims:nap_already_active"));
            if (ConflictHandler.TryGetConflictLetter(ourParty, targetParty, LetterPurpose.NON_AGGRESSION, out _))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));

            int days = args.Parsers.Count > 1 && args.Parsers[1].GetValue() != null ? System.Convert.ToInt32(args.Parsers[1].GetValue()) : claims.config.WAR_NAP_DEFAULT_DAYS;
            if (days < 1) days = 1;
            if (days > claims.config.WAR_NAP_MAX_DAYS) days = claims.config.WAR_NAP_MAX_DAYS;
            int daysCaptured = days;

            long letterExpire = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
            string napLetterGuid = ConflictLetter.GetUnusedGuid().ToString();
            if (ConflictHandler.addConflictLetter(new ConflictLetter(ourParty, targetParty, LetterPurpose.NON_AGGRESSION, letterExpire,
                () =>
                {
                    ConflictHandler.removeConflictLetter(ourParty, targetParty, LetterPurpose.NON_AGGRESSION);
                    SyncLetterRemove(ourParty, targetParty, napLetterGuid, LetterPurpose.NON_AGGRESSION);
                    if (ConflictHandler.conflictAlreadyExist(ourParty, targetParty)) return; // war started meanwhile
                    long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)daysCaptured * 86400;
                    NonAggressionHelper.RecordPact(ourParty, targetParty, expiry);
                    MessageHandler.SendMsgInAlliance(ourParty, Lang.Get("claims:nap_signed", targetParty.GetPartName(), daysCaptured));
                    MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:nap_signed", ourParty.GetPartName(), daysCaptured));
                },
                () =>
                {
                    ConflictHandler.removeConflictLetter(ourParty, targetParty, LetterPurpose.NON_AGGRESSION);
                    SyncLetterRemove(ourParty, targetParty, napLetterGuid, LetterPurpose.NON_AGGRESSION);
                    MessageHandler.SendMsgInAlliance(ourParty, Lang.Get("claims:nap_declined"));
                },
                napLetterGuid)))
            {
                // Mirror the letter to both parties' clients so it shows in the conflict-letters GUI.
                var letterCellElement = new ClientConflictLetterCellElement(ourParty.GetPartName(), ourParty.Guid.ToString(),
                        WarTargetTypeHelper.FromConflictParty(ourParty),
                        targetParty.GetPartName(), targetParty.Guid.ToString(),
                        WarTargetTypeHelper.FromConflictParty(targetParty),
                        LetterPurpose.NON_AGGRESSION, letterExpire, napLetterGuid);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:nap_offered", ourParty.GetPartName(), daysCaptured));
                return TextCommandResult.Success(Lang.Get("claims:nap_offer_sent"));
            }
            return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
        }

        // Removes a letter from both parties' client GUIs (matches the ALLIANCE_LETTER_REMOVE format).
        private static void SyncLetterRemove(IConflictParty a, IConflictParty b, string letterGuid, LetterPurpose purpose)
        {
            var payload = new Dictionary<string, object> { { "value", (letterGuid, purpose) } };
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(a, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(b, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
        }

        public static TextCommandResult NapAccept(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, true, out _, out _, out var ourParty, out var err)) return err;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(ourParty, targetParty, LetterPurpose.NON_AGGRESSION, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnAccept?.Invoke();
            return TextCommandResult.Success();
        }

        public static TextCommandResult NapDeny(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, true, out _, out _, out var ourParty, out var err)) return err;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(ourParty, targetParty, LetterPurpose.NON_AGGRESSION, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnDeny?.Invoke();
            return TextCommandResult.Success();
        }

        public static TextCommandResult NapList(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, false, out _, out _, out var ourParty, out var err)) return err;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Aggregate across the party's member cities, deduped by partner guid (each entry is replicated on every city).
            var merged = new Dictionary<string, long>();
            foreach (City pc in ourParty.GetCities())
                foreach (var kv in pc.NonAggressionPacts)
                    if (kv.Value > now && (!merged.TryGetValue(kv.Key, out long e) || kv.Value > e))
                        merged[kv.Key] = kv.Value;
            StringBuilder sb = new StringBuilder();
            foreach (var kv in merged)
            {
                string name = claims.dataStorage.getCityByGUID(kv.Key, out City c) ? c.GetPartName()
                    : (claims.dataStorage.GetAllianceByGUID(kv.Key, out Alliance a) ? a.GetPartName() : kv.Key);
                sb.AppendLine(Lang.Get("claims:nap_entry", name, (int)((kv.Value - now) / 86400)));
            }
            if (sb.Length == 0) sb.Append(Lang.Get("claims:nap_none"));
            return TextCommandResult.Success(sb.ToString());
        }

        // Breaks a pact early: pay a penalty and hand the partner a casus belli (betrayal).
        public static TextCommandResult NapBreak(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, true, out _, out _, out var ourParty, out var err)) return err;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!NonAggressionHelper.HasActivePact(ourParty, targetParty))
                return TextCommandResult.Success(Lang.Get("claims:nap_none_with"));

            double penalty = claims.config.WAR_NAP_BREAK_PENALTY;
            if (penalty > 0)
            {
                if (claims.economyProvider.GetBalance(ourParty.MoneyAccountName) < (decimal)penalty)
                    return TextCommandResult.Success(Lang.Get("claims:not_enough_money"));
                if (claims.economyProvider.Withdraw(ourParty.MoneyAccountName, (decimal)penalty) != MoneyOperationResult.Success)
                    return TextCommandResult.Success(Lang.Get("claims:economy_money_transaction_error"));
            }
            NonAggressionHelper.RemovePact(ourParty, targetParty);
            foreach (City tc in targetParty.GetCities())
                WarDeclarationHelper.RecordGrievance(tc, ourParty.Guid);
            MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:nap_betrayed", ourParty.GetPartName()));
            return TextCommandResult.Success(Lang.Get("claims:nap_broken", targetParty.GetPartName()));
        }

        // Issues an ultimatum: a peacetime demand (pay money / cede a plot). Complying transfers it;
        // refusing or letting it expire grants the demander a free, justified war (see WarDeclarationHelper).
        public static TextCommandResult UltimatumOffer(TextCommandCallingArgs args)
        {
            if (!claims.config.WAR_ULTIMATUM_ENABLED) return TextCommandResult.Success(Lang.Get("claims:ultimatum_disabled"));
            if (!TryResolveMyParty(args, true, out var player, out _, out var ourParty, out var err)) return err;

            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            string name = Filter.filterName(parsedName);
            if (!TryResolveWarTarget(name, targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (targetParty.Equals(ourParty)) return TextCommandResult.Success(Lang.Get("claims:same_alliance"));
            if (ConflictHandler.conflictAlreadyExist(ourParty, targetParty)) return TextCommandResult.Success(Lang.Get("claims:conflict_already_exists"));
            if (NonAggressionHelper.HasActivePact(ourParty, targetParty)) return TextCommandResult.Success(Lang.Get("claims:ultimatum_nap_active"));
            if (ConflictHandler.TryGetConflictLetter(ourParty, targetParty, LetterPurpose.ULTIMATUM, out _))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));

            // Build the demand as a PeaceTerms payload (reused verbatim on comply).
            string type = ((string)args.Parsers[1].GetValue() ?? "").ToLowerInvariant();
            PeaceTerms terms;
            string demandLabel;
            if (type == "money")
            {
                long amount = args.Parsers.Count > 2 && args.Parsers[2].GetValue() != null ? System.Convert.ToInt64(args.Parsers[2].GetValue()) : 0;
                if (amount <= 0) return TextCommandResult.Success(Lang.Get("claims:ultimatum_needs_amount"));
                terms = new PeaceTerms(PeaceTermType.Reparations, amount);
                demandLabel = Lang.Get("claims:ultimatum_demand_money", amount);
            }
            else if (type == "plot")
            {
                PlotPosition cpos = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
                if (!claims.dataStorage.GetPlot(cpos, out Plot cplot) || !cplot.hasCity() || !targetParty.GetCities().Contains(cplot.getCity()))
                    return TextCommandResult.Success(Lang.Get("claims:cession_stand_on_enemy_plot"));
                terms = new PeaceTerms(PeaceTermType.Cession) { CededPlot = cpos };
                demandLabel = Lang.Get("claims:ultimatum_demand_plot");
            }
            else return TextCommandResult.Success(Lang.Get("claims:ultimatum_bad_type"));

            long letterExpire = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
            string ultLetterGuid = ConflictLetter.GetUnusedGuid().ToString();
            if (ConflictHandler.addConflictLetter(new ConflictLetter(ourParty, targetParty, LetterPurpose.ULTIMATUM, letterExpire,
                () =>
                {
                    // Complied: transfer the demand (a member-plot cession may need the owning mayor's co-sign).
                    ConflictHandler.removeConflictLetter(ourParty, targetParty, LetterPurpose.ULTIMATUM);
                    SyncLetterRemove(ourParty, targetParty, ultLetterGuid, LetterPurpose.ULTIMATUM);
                    PeaceTermsHelper.ApplyWithConfirm(ourParty, targetParty, terms,
                        () =>
                        {
                            MessageHandler.SendMsgInAlliance(ourParty, Lang.Get("claims:ultimatum_complied_by", targetParty.GetPartName()));
                            MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:ultimatum_you_complied", ourParty.GetPartName()));
                        },
                        () =>
                        {
                            // The owning member refused to cede — the ultimatum is unsatisfied, so the demander gets a free war.
                            WarDeclarationHelper.RecordWarJustification(ourParty, targetParty);
                            MessageHandler.SendMsgInAlliance(ourParty, Lang.Get("claims:ultimatum_refused_by", targetParty.GetPartName()));
                            MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:ultimatum_you_refused", ourParty.GetPartName()));
                        });
                },
                () =>
                {
                    // Refused or expired: the demander gets a free, justified war against the target.
                    ConflictHandler.removeConflictLetter(ourParty, targetParty, LetterPurpose.ULTIMATUM);
                    SyncLetterRemove(ourParty, targetParty, ultLetterGuid, LetterPurpose.ULTIMATUM);
                    WarDeclarationHelper.RecordWarJustification(ourParty, targetParty);
                    MessageHandler.SendMsgInAlliance(ourParty, Lang.Get("claims:ultimatum_refused_by", targetParty.GetPartName()));
                    MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:ultimatum_you_refused", ourParty.GetPartName()));
                },
                ultLetterGuid)))
            {
                var letterCellElement = new ClientConflictLetterCellElement(ourParty.GetPartName(), ourParty.Guid.ToString(),
                        WarTargetTypeHelper.FromConflictParty(ourParty),
                        targetParty.GetPartName(), targetParty.Guid.ToString(),
                        WarTargetTypeHelper.FromConflictParty(targetParty),
                        LetterPurpose.ULTIMATUM, letterExpire, ultLetterGuid);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ourParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(targetParty, new Dictionary<string, object> { { "value", letterCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
                MessageHandler.SendMsgInAlliance(targetParty, Lang.Get("claims:ultimatum_received", ourParty.GetPartName(), demandLabel));
                return TextCommandResult.Success(Lang.Get("claims:ultimatum_sent", demandLabel, targetParty.GetPartName()));
            }
            return TextCommandResult.Success(Lang.Get("claims:conflict_letter_is_duplicate"));
        }

        public static TextCommandResult UltimatumAccept(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, true, out _, out _, out var ourParty, out var err)) return err;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(ourParty, targetParty, LetterPurpose.ULTIMATUM, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnAccept?.Invoke();
            return TextCommandResult.Success();
        }

        public static TextCommandResult UltimatumDeny(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, true, out _, out _, out var ourParty, out var err)) return err;
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty targetParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(ourParty, targetParty, LetterPurpose.ULTIMATUM, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnDeny?.Invoke();
            return TextCommandResult.Success();
        }

        // Lists the free-war justifications this party holds from refused/expired ultimatums.
        public static TextCommandResult UltimatumList(TextCommandCallingArgs args)
        {
            if (!TryResolveMyParty(args, false, out _, out _, out var ourParty, out var err)) return err;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Aggregate across the party's member cities, deduped by target guid (each entry is replicated on every city).
            var merged = new Dictionary<string, long>();
            foreach (City pc in ourParty.GetCities())
                foreach (var kv in pc.WarJustifications)
                    if (kv.Value > now && (!merged.TryGetValue(kv.Key, out long e) || kv.Value > e))
                        merged[kv.Key] = kv.Value;
            StringBuilder sb = new StringBuilder();
            foreach (var kv in merged)
            {
                string jname = claims.dataStorage.getCityByGUID(kv.Key, out City c) ? c.GetPartName()
                    : (claims.dataStorage.GetAllianceByGUID(kv.Key, out Alliance a) ? a.GetPartName() : kv.Key);
                sb.AppendLine(Lang.Get("claims:ultimatum_justification_entry", jname, (int)((kv.Value - now) / 86400)));
            }
            if (sb.Length == 0) sb.Append(Lang.Get("claims:ultimatum_no_justifications"));
            return TextCommandResult.Success(sb.ToString());
        }

        // The owning mayor confirms/refuses an alliance leader's decision to cede this city's plot (peace/ultimatum co-sign).
        public static TextCommandResult CessionAccept(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty winnerParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(playerInfo.City, winnerParty, LetterPurpose.CESSION_CONFIRM, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnAccept?.Invoke();
            return TextCommandResult.Success();
        }

        public static TextCommandResult CessionDeny(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));
            var (parsedName, targetType) = ParseWarTargetInput((string)args.Parsers[0].GetValue());
            if (!TryResolveWarTarget(Filter.filterName(parsedName), targetType, out IConflictParty winnerParty, out string errorMsg))
                return TextCommandResult.Success(errorMsg);
            if (!ConflictHandler.TryGetConflictLetter(playerInfo.City, winnerParty, LetterPurpose.CESSION_CONFIRM, out var letter))
                return TextCommandResult.Success(Lang.Get("claims:conflict_letter_doesnt_exist"));
            letter.OnDeny?.Invoke();
            return TextCommandResult.Success();
        }

        // Claims the plot the mayor stands on as a war camp for the current active battle window.
        public static TextCommandResult ClaimCamp(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!claims.config.WAR_CAMP_ENABLED)
                return TextCommandResult.Success(Lang.Get("claims:camp_disabled"));
            if (!playerInfo.hasCity() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));

            City city = playerInfo.City;
            IConflictParty party = playerInfo.HasAlliance() ? (IConflictParty)playerInfo.Alliance : city;
            Conflict activeConflict = null;
            foreach (var c in party.RunningConflicts)
            {
                if (c.ActiveWarTime) { activeConflict = c; break; }
            }
            if (activeConflict == null)
                return TextCommandResult.Success(Lang.Get("claims:no_active_war"));

            PlotPosition pos = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            if (claims.dataStorage.GetPlot(pos, out _))
                return TextCommandResult.Success(Lang.Get("claims:plot_must_be_free"));

            int campsForConflict = city.campPlots.Count(p => p.PlotDesc is PlotDescCamp pd && pd.ConflictGuid == activeConflict.Guid);
            if (campsForConflict >= claims.config.WAR_MAX_CAMPS_PER_CONFLICT)
                return TextCommandResult.Success(Lang.Get("claims:max_camps_reached"));

            // Anchor = an obsidian anchor block placed at the plot center on the surface;
            // breaking it destroys the whole camp.
            int plotSize = claims.config.PLOT_SIZE;
            int ax = pos.getPos().X * plotSize + plotSize / 2;
            int az = pos.getPos().Y * plotSize + plotSize / 2;
            int ay = claims.sapi.World.BlockAccessor.GetTerrainMapheightAt(new BlockPos(ax, 0, az)) + 1;
            Vec3i anchor = new Vec3i(ax, ay, az);
            Plot camp = new Plot(pos);
            camp.Type = PlotType.CAMP;
            camp.PlotDesc = new PlotDescCamp(activeConflict.Guid, anchor) { BreaksLeft = claims.config.WAR_CAMP_ANCHOR_BREAKS };
            camp.setCity(city);

            // Camps have their own (usually smaller) distance rule and respect forbidden areas.
            // Checked BEFORE withdrawing the cost.
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(camp, claims.config.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY))
                return TextCommandResult.Success(Lang.Get("claims:too_close_to_another_city"));
            if (!claims.dataStorage.CheckClaimLimiters(playerInfo, pos))
                return TextCommandResult.Success(Lang.Get("claims:too_close_to_forbidden_area"));

            if (claims.economyProvider.GetBalance(city.MoneyAccountName) < (decimal)claims.config.CAMP_PLOT_COST)
                return TextCommandResult.Success(Lang.Get("claims:not_enough_money"));
            if (claims.economyProvider.Withdraw(city.MoneyAccountName, (decimal)claims.config.CAMP_PLOT_COST) != MoneyOperationResult.Success)
                return TextCommandResult.Success(Lang.Get("claims:economy_money_transaction_error"));

            // Place the physical anchor block (after payment succeeded).
            Block anchorBlock = claims.sapi.World.GetBlock(new AssetLocation("claims", "campanchor"));
            if (anchorBlock != null)
            {
                BlockPos apos = new BlockPos(anchor.X, anchor.Y, anchor.Z);
                claims.sapi.World.BlockAccessor.SetBlock(anchorBlock.Id, apos);
                if (claims.sapi.World.BlockAccessor.GetBlockEntity(apos) is BlockEntityCampAnchor anchorBe)
                {
                    anchorBe.BreaksLeft = claims.config.WAR_CAMP_ANCHOR_BREAKS;
                    anchorBe.MarkDirty(true);
                }
            }

            camp.getPermsHandler().setPerm(city.getPermsHandler());
            camp.TimeStampClaimed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            claims.dataStorage.addClaimedPlot(pos, camp);
            city.getCityPlots().Add(camp);
            city.campPlots.Add(camp);
            city.saveToDatabase();
            camp.saveToDatabase();
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(camp.getPos());
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            city.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);
            camp.CheckBorderPlotValue();
            return TextCommandResult.Success(Lang.Get("claims:camp_claimed"));
        }

        // Removes the war camp the mayor stands on.
        public static TextCommandResult UnclaimCamp(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity() || !playerInfo.City.isMayor(playerInfo))
                return TextCommandResult.Success(Lang.Get("claims:you_are_not_mayor"));

            PlotPosition pos = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            if (!claims.dataStorage.GetPlot(pos, out Plot plot) || plot.Type != PlotType.CAMP
                || !plot.hasCity() || !plot.getCity().Equals(playerInfo.City))
                return TextCommandResult.Success(Lang.Get("claims:camp_not_here"));

            PartDemolition.DemolishCamp(plot);
            return TextCommandResult.Success(Lang.Get("claims:camp_removed"));
        }
    }
}
