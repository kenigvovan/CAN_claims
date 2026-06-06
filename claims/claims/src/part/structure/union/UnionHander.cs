using claims.src.delayed;
using claims.src.part.structure;
using System;
using System.Collections.Generic;

namespace claims.src.part.structure.union
{
    public class UnionHander
    {
        static LetterRegistry<UnionLetter> registry = new();

        public static void clearAll() => registry.Clear();
        public static bool addUnionLetter(UnionLetter letter) => registry.Add(letter);
        public static bool removeUnionLetter(UnionLetter letter) => registry.Remove(letter);
        public static void updateUnionLetters() => registry.ExpireOverdue();
        public static bool GuidIsFree(Guid guid) => registry.GuidIsFree(guid.ToString());

        public static bool removeUnionLetter(Alliance from, Alliance to)
        {
            foreach (var it in registry.All)
            {
                if ((it.From.Equals(from) && it.To.Equals(to)) ||
                    (it.From.Equals(to) && it.To.Equals(from)))
                {
                    registry.Remove(it);
                    return true;
                }
            }
            return false;
        }

        public static bool unionAlreadyExist(Alliance firstSide, Alliance secondSide) =>
            firstSide.ComradAlliancies.Contains(secondSide);

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
