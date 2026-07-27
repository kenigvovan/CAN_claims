using claims.src.delayed;
using claims.src.part.structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace claims.src.part.structure.union
{
    public class UnionHander
    {
        static LetterRegistry<UnionLetter> registry = new();

        public static void clearAll() => registry.Clear();
        /// <summary>
        /// Adds a letter. Persisted ones survive a server restart; pass persist:false when the letter
        /// is being restored from the database (it is already stored there).
        /// </summary>
        public static bool addUnionLetter(UnionLetter letter, bool persist = true)
        {
            if (letter == null || !registry.Add(letter)) return false;
            if (persist)
            {
                claims.getModInstance()?.getDatabaseHandler()?.saveUnionLetter(letter, update: false);
                UnionLetterFactory.MirrorToClients(letter);
            }
            return true;
        }
        public static bool removeUnionLetter(UnionLetter letter)
        {
            if (!registry.Remove(letter)) return false;
            claims.getModInstance()?.getDatabaseHandler()?.deleteUnionLetterByGuid(letter.Guid);
            return true;
        }
        public static void updateUnionLetters()
        {
            // ExpireOverdue drops the letter from the registry itself, so the row has to be deleted
            // here - the OnExpire handler would no longer find it to clean up.
            long now = auxialiry.TimeFunctions.getEpochSeconds();
            foreach (var it in registry.Snapshot())
            {
                if (it.TimeStampExpire < now)
                    claims.getModInstance()?.getDatabaseHandler()?.deleteUnionLetterByGuid(it.Guid);
            }
            registry.ExpireOverdue();
        }
        public static bool GuidIsFree(Guid guid) => registry.GuidIsFree(guid.ToString());

        public static bool removeUnionLetter(Alliance from, Alliance to)
        {
            foreach (var it in registry.All.ToArray())
            {
                if ((it.From.Equals(from) && it.To.Equals(to)) ||
                    (it.From.Equals(to) && it.To.Equals(from)))
                {
                    // Goes through the overload that also drops the stored row.
                    return removeUnionLetter(it);
                }
            }
            return false;
        }

        public static bool unionAlreadyExist(Alliance firstSide, Alliance secondSide) =>
            firstSide.ComradAlliancies.Contains(secondSide);

        /// <summary>
        /// Party-aware union check for the war gates. Unions only exist between alliances, so a party
        /// that fights as a lone city can never be allied with anyone.
        /// </summary>
        public static bool PartiesAreAllied(conflict.IConflictParty first, conflict.IConflictParty second) =>
            first is Alliance firstAlliance && second is Alliance secondAlliance
            && unionAlreadyExist(firstAlliance, secondAlliance);

        public static List<UnionLetter> GetAllLettersForAlliance(Alliance alliance)
        {
            List<UnionLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.From.Equals(alliance) || it.To.Equals(alliance))
                    result.Add(it);
            }
            return result;
        }

        public static List<UnionLetter> getSentLettersForAlliance(Alliance alliance)
        {
            List<UnionLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.From.Equals(alliance))
                    result.Add(it);
            }
            return result;
        }

        public static List<UnionLetter> getReceivedLettersForAlliance(Alliance alliance)
        {
            List<UnionLetter> result = new();
            foreach (var it in registry.All)
            {
                if (it.To.Equals(alliance))
                    result.Add(it);
            }
            return result;
        }

        public static bool TryGetUnionLetter(Alliance first, Alliance second, out UnionLetter letter)
        {
            foreach (var it in registry.All)
            {
                if ((it.From.Equals(first) && it.To.Equals(second)) ||
                    (it.From.Equals(second) && it.To.Equals(first)))
                {
                    letter = it;
                    return true;
                }
            }
            letter = null;
            return false;
        }

        public static bool TryGetUnionLetter(string guid, out UnionLetter letter) =>
            registry.TryGetByGuid(guid, out letter);
    }
}
