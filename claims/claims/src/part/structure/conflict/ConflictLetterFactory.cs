using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part.structure.war;
using claims.src.rights;
using Vintagestory.API.Config;

namespace claims.src.part.structure.conflict
{
    /// <summary>
    /// Single place that builds conflict letters together with their accept/deny behaviour.
    /// Commands and the database loader both go through here, so a letter restored after a server
    /// restart behaves exactly like the one that was originally sent (the behaviour used to live in
    /// inline closures inside the commands, which is why letters could not be persisted at all).
    /// </summary>
    public static class ConflictLetterFactory
    {
        public static ConflictLetter Build(IConflictParty from, IConflictParty to, LetterPurpose purpose,
            long expire, string guid, PeaceTerms terms = null, int napDays = 0)
        {
            switch (purpose)
            {
                case LetterPurpose.START_CONFLICT: return BuildStartConflict(from, to, expire, guid);
                case LetterPurpose.END_CONFLICT: return BuildEndConflict(from, to, expire, guid, terms);
                case LetterPurpose.NON_AGGRESSION: return BuildNonAggression(from, to, expire, guid, napDays);
                case LetterPurpose.ULTIMATUM: return BuildUltimatum(from, to, expire, guid, terms);
                default: return null;
            }
        }

        /// <summary>Whether a letter of this purpose can be rebuilt from the database.</summary>
        public static bool IsPersistable(LetterPurpose purpose)
        {
            // CESSION_CONFIRM is a sub-step inside accepting a peace deal: its callbacks continue an
            // in-flight negotiation that no longer exists after a restart.
            return purpose != LetterPurpose.CESSION_CONFIRM;
        }

        private static ConflictLetter BuildStartConflict(IConflictParty from, IConflictParty to, long expire, string conflictGuid)
        {
            ConflictLetter letter = null;
            letter = new ConflictLetter(from, to, LetterPurpose.START_CONFLICT, expire,
                () =>
                {
                    if (!ConflictHandler.TryGetConflictLetter(from, to, LetterPurpose.START_CONFLICT, out var acceptLetter)) return;
                    // A union may have been signed while the declaration was pending; going through
                    // would leave both sides hostile AND allied at once.
                    if (union.UnionHander.PartiesAreAllied(from, to))
                    {
                        RemoveAndSync(from, to, LetterPurpose.START_CONFLICT, conflictGuid);
                        MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:war_blocked_by_union"));
                        MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:war_blocked_by_union"));
                        return;
                    }

                    Conflict newConflict = new Conflict("", conflictGuid)
                    {
                        First = from,
                        Second = to,
                        StartedBy = from,
                        State = ConflictState.CREATED,
                        TimeStampStarted = TimeFunctions.getEpochSeconds(),
                        MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES
                    };

                    foreach (var city in from.GetCities())
                        city.AddLogEntry(EnumCityLogEvent.ConflictDeclared, to.GetPartName());
                    foreach (var city in to.GetCities())
                        city.AddLogEntry(EnumCityLogEvent.ConflictDeclared, from.GetPartName());

                    RightsHandler.SetPartiesHostile(from, to, newConflict);
                    // Pull in the allies of BOTH sides. The old code was inconsistent here: an
                    // alliance declaring war dragged in its own allies, a city declaring war dragged
                    // in the defender's allies - depending on which command was used.
                    RightsHandler.AllianceAllySetHostileOnNewConflictStarted(from, to, newConflict);
                    RightsHandler.AllianceAllySetHostileOnNewConflictStarted(to, from, newConflict);

                    claims.dataStorage.TryAddConflict(newConflict);
                    ConflictHandler.removeConflictLetter(acceptLetter);
                    SyncLetterRemove(from, to, acceptLetter.Guid, acceptLetter.Purpose);

                    if (from is Alliance fromAlliance) fromAlliance.FireConflictDeclared(to);

                    var conflictCellElement = ClientConflictCellElement.FromConflict(newConflict);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(from,
                        new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
                    UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(to,
                        new Dictionary<string, object> { { "value", conflictCellElement } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);

                    from.saveToDatabase();
                    to.saveToDatabase();
                    newConflict.saveToDatabase(false);

                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:conflict_created_with", to.GetPartName()));
                    MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:conflict_created_with", from.GetPartName()));
                },
                () =>
                {
                    RemoveAndSync(from, to, LetterPurpose.START_CONFLICT, conflictGuid);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:conflict_denied"));
                },
                conflictGuid);
            return letter;
        }

        private static ConflictLetter BuildEndConflict(IConflictParty from, IConflictParty to, long expire, string conflictGuid, PeaceTerms terms)
        {
            PeaceTerms peaceTerms = terms ?? new PeaceTerms();
            ConflictLetter letter = new ConflictLetter(from, to, LetterPurpose.END_CONFLICT, expire,
                () =>
                {
                    if (!ConflictHandler.TryGetConflictWithSides(from, to, out Conflict conflict)) return;
                    RemoveAndSync(from, to, LetterPurpose.END_CONFLICT, conflictGuid);

                    PeaceTermsHelper.ApplyWithConfirm(from, to, peaceTerms,
                        () =>
                        {
                            PartDemolition.DemolishConflict(conflict);
                            MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:conflict_stopped_with", to.GetPartName()));
                            MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:conflict_stopped_with", from.GetPartName()));
                        },
                        () =>
                        {
                            // The owning member refused the plot cession - the war continues.
                            MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:peace_cession_refused", to.GetPartName()));
                            MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:peace_cession_refused_them", from.GetPartName()));
                        });
                },
                () =>
                {
                    RemoveAndSync(from, to, LetterPurpose.END_CONFLICT, conflictGuid);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:conflict_stop_denied"));
                },
                conflictGuid);
            letter.Terms = peaceTerms;
            return letter;
        }

        private static ConflictLetter BuildNonAggression(IConflictParty from, IConflictParty to, long expire, string guid, int napDays)
        {
            int days = napDays > 0 ? napDays : claims.config.WAR_NAP_DEFAULT_DAYS;
            ConflictLetter letter = new ConflictLetter(from, to, LetterPurpose.NON_AGGRESSION, expire,
                () =>
                {
                    RemoveAndSync(from, to, LetterPurpose.NON_AGGRESSION, guid);
                    if (ConflictHandler.conflictAlreadyExist(from, to)) return; // war started meanwhile
                    long pactExpiry = TimeFunctions.getEpochSeconds() + (long)days * 86400;
                    NonAggressionHelper.RecordPact(from, to, pactExpiry);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:nap_signed", to.GetPartName(), days));
                    MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:nap_signed", from.GetPartName(), days));
                },
                () =>
                {
                    RemoveAndSync(from, to, LetterPurpose.NON_AGGRESSION, guid);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:nap_declined"));
                },
                guid);
            letter.NapDays = days;
            return letter;
        }

        private static ConflictLetter BuildUltimatum(IConflictParty from, IConflictParty to, long expire, string guid, PeaceTerms terms)
        {
            PeaceTerms demand = terms ?? new PeaceTerms();
            ConflictLetter letter = new ConflictLetter(from, to, LetterPurpose.ULTIMATUM, expire,
                () =>
                {
                    RemoveAndSync(from, to, LetterPurpose.ULTIMATUM, guid);
                    PeaceTermsHelper.ApplyWithConfirm(from, to, demand,
                        () =>
                        {
                            MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:ultimatum_complied_by", to.GetPartName()));
                            MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:ultimatum_you_complied", from.GetPartName()));
                        },
                        () =>
                        {
                            // Refusing to hand over the plot leaves the ultimatum unsatisfied.
                            WarDeclarationHelper.RecordWarJustification(from, to);
                            MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:ultimatum_refused_by", to.GetPartName()));
                            MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:ultimatum_you_refused", from.GetPartName()));
                        });
                },
                () =>
                {
                    // Refused or expired: the demander gets a free, justified war against the target.
                    RemoveAndSync(from, to, LetterPurpose.ULTIMATUM, guid);
                    WarDeclarationHelper.RecordWarJustification(from, to);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:ultimatum_refused_by", to.GetPartName()));
                    MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:ultimatum_you_refused", from.GetPartName()));
                },
                guid);
            letter.Terms = demand;
            return letter;
        }

        /// <summary>Pushes a newly added letter to both sides' conflict-letters GUI.</summary>
        public static void MirrorToClients(ConflictLetter letter)
        {
            if (letter?.From == null || letter.To == null) return;

            var cell = new ClientConflictLetterCellElement(
                letter.From.GetPartName(), letter.From.Guid.ToString(), WarTargetTypeHelper.FromConflictParty(letter.From),
                letter.To.GetPartName(), letter.To.Guid.ToString(), WarTargetTypeHelper.FromConflictParty(letter.To),
                letter.Purpose, letter.TimeStampExpire, letter.Guid).WithTerms(letter.Terms).WithNapDays(letter.NapDays);

            var payload = new Dictionary<string, object> { { "value", cell } };
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(letter.From, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(letter.To, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_ADD);
        }

        /// <summary>
        /// Drops the letter and tells both sides' GUIs about it. The client sync must NOT depend on
        /// the letter still being in the registry: on expiry ExpiringList removes it before invoking
        /// OnExpire, and the stale entry would then stay in the conflict-letters window forever.
        /// </summary>
        private static void RemoveAndSync(IConflictParty from, IConflictParty to, LetterPurpose purpose, string guid)
        {
            if (ConflictHandler.TryGetConflictLetter(from, to, purpose, out var letter))
            {
                ConflictHandler.removeConflictLetter(letter);
            }
            else
            {
                // Already gone from the registry (expiry path) - the row still has to go.
                claims.getModInstance()?.getDatabaseHandler()?.deleteConflictLetterByGuid(guid);
            }
            SyncLetterRemove(from, to, guid, purpose);
        }

        private static void SyncLetterRemove(IConflictParty from, IConflictParty to, string guid, LetterPurpose purpose)
        {
            var payload = new Dictionary<string, object> { { "value", (guid, purpose) } };
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(from, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(to, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
        }
    }
}
