using claims.src.auxialiry;
using claims.src.delayed;
using claims.src.part;
using claims.src.part.structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace claims.src.part.structure.conflict
{
    public class ConflictHandler
    {
        static LetterRegistry<ConflictLetter> registry = new();

        public static void clearAll() => registry.Clear();
        public static bool addConflictLetter(ConflictLetter letter) => registry.Add(letter);
        public static bool removeConflictLetter(ConflictLetter letter) => registry.Remove(letter);
        public static void updateConflictLetters() => registry.ExpireOverdue();
        public static bool GuidIsFree(Guid guid) => registry.GuidIsFree(guid.ToString());

        public static bool removeConflictLetter(IConflictParty from, IConflictParty to, LetterPurpose purpose)
        {
            foreach (var it in registry.All.ToArray())
            {
                if ((it.From.Equals(from) && it.To.Equals(to) && it.Purpose.Equals(purpose)) ||
                    (it.From.Equals(to) && it.To.Equals(from) && it.Purpose.Equals(purpose)))
                {
                    registry.Remove(it);
                    return true;
                }
            }
            return false;
        }

        public static bool conflictAlreadyExist(IConflictParty firstSide, IConflictParty secondSide)
        {
            foreach (Conflict conflict in claims.dataStorage.conflicts)
            {
                if ((conflict.First.Equals(firstSide) && conflict.Second.Equals(secondSide)) ||
                    ((conflict.First.Equals(secondSide) && conflict.Second.Equals(firstSide))))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetConflictWithSides(IConflictParty firstSide, IConflictParty secondSide, out Conflict conflict)
        {
            foreach (Conflict it in claims.dataStorage.conflicts)
            {
                if (it.First.Equals(firstSide) && it.Second.Equals(secondSide) ||
                    (it.First.Equals(secondSide) && it.Second.Equals(firstSide)))
                {
                    conflict = it;
                    return true;
                }
            }
            conflict = null;
            return false;
        }

        public static bool TryGetConflictByGuid(string Guid, out Conflict conflict)
        {
            foreach (Conflict it in claims.dataStorage.conflicts)
            {
                if (it.Guid.Equals(Guid))
                {
                    conflict = it;
                    return true;
                }
            }
            conflict = null;
            return false;
        }

        public static List<Conflict> GetAllConflictsForParty(IConflictParty party)
        {
            List<Conflict> result = new();
            foreach (var it in claims.dataStorage.conflicts)
            {
                if (it.First.Equals(party) || it.Second.Equals(party))
                    result.Add(it);
            }
            return result;
        }

        public static List<ConflictLetter> GetAllLettersForParty(IConflictParty party)
        {
            List<ConflictLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.From.Equals(party) || it.To.Equals(party))
                    result.Add(it);
            }
            return result;
        }

        public static List<ConflictLetter> getSentLettersForParty(IConflictParty party)
        {
            List<ConflictLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.From.Equals(party))
                    result.Add(it);
            }
            return result;
        }

        public static List<ConflictLetter> getReceivedLettersForParty(IConflictParty party)
        {
            List<ConflictLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.To.Equals(party))
                    result.Add(it);
            }
            return result;
        }

        public static List<ConflictLetter> getReceivedLettersForPartyWithPurpose(IConflictParty party, LetterPurpose purpose)
        {
            List<ConflictLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.To.Equals(party) && it.Purpose.Equals(purpose))
                    result.Add(it);
            }
            return result;
        }

        public static bool TryGetConflictLetter(IConflictParty first, IConflictParty second, LetterPurpose purpose, out ConflictLetter letter)
        {
            foreach (var it in registry.All)
            {
                if ((it.From.Equals(first) && it.To.Equals(second) && it.Purpose.Equals(purpose)) ||
                    (it.From.Equals(second) && it.To.Equals(first) && it.Purpose.Equals(purpose)))
                {
                    letter = it;
                    return true;
                }
            }
            letter = null;
            return false;
        }

        public static bool TryGetConflictLetter(string guid, out ConflictLetter letter) =>
            registry.TryGetByGuid(guid, out letter);
    }
}
