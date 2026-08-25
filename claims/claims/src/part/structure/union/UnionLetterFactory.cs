using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.union
{
    /// <summary>
    /// Single place that builds union letters together with their accept/deny behaviour, so a letter
    /// restored from the database after a restart behaves exactly like the one originally sent.
    /// Mirrors ConflictLetterFactory; the behaviour used to live in closures inside the commands,
    /// which is why union letters could not be persisted at all.
    /// </summary>
    public static class UnionLetterFactory
    {
        public static UnionLetter Build(Alliance from, Alliance to, UnionLetterPurpose purpose, long expire, string guid)
        {
            return purpose == UnionLetterPurpose.Dissolve
                ? BuildDissolve(from, to, expire, guid)
                : BuildForm(from, to, expire, guid);
        }

        private static UnionLetter BuildForm(Alliance from, Alliance to, long expire, string guid)
        {
            return new UnionLetter(from, to, expire,
                () =>
                {
                    RemoveAndSync(from, to, guid);
                    if (UnionHander.unionAlreadyExist(from, to)) return;

                    // Re-check the gates: a war may have started and the post-betrayal cooldown may
                    // still be running while the letter was pending.
                    if (from.IsNeutral || to.IsNeutral)
                    {
                        MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:target_alliance_is_neutral"));
                        MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:target_alliance_is_neutral"));
                        return;
                    }
                    if (ConflictHandler.conflictAlreadyExist(from, to))
                    {
                        MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:union_blocked_by_war"));
                        MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:union_blocked_by_war"));
                        return;
                    }
                    long cooldown = UnionBreakHelper.ReformCooldownLeft(from, to);
                    if (cooldown > 0)
                    {
                        string left = StringFunctions.FormatDuration(cooldown);
                        MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:union_reform_on_cooldown", left));
                        MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:union_reform_on_cooldown", left));
                        return;
                    }

                    PartInits.InitNewUnion(from, to);
                    foreach (City city in from.Cities) city.AddLogEntry(EnumCityLogEvent.UnionFormed, to.GetPartName());
                    foreach (City city in to.Cities) city.AddLogEntry(EnumCityLogEvent.UnionFormed, from.GetPartName());

                    UsefullPacketsSend.AddToQueueAllianceInfoUpdate(from.Guid,
                        new Dictionary<string, object> { { "value", to.GetPartName() } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_ADDED);
                    UsefullPacketsSend.AddToQueueAllianceInfoUpdate(to.Guid,
                        new Dictionary<string, object> { { "value", from.GetPartName() } }, EnumPlayerRelatedInfo.ALLIANCE_ALLY_ADDED);

                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:union_letter_accepted", to.getPartNameReplaceUnder()));
                    MessageHandler.SendMsgInAlliance(to, Lang.Get("claims:union_letter_accepted", from.getPartNameReplaceUnder()));
                },
                () =>
                {
                    RemoveAndSync(from, to, guid);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:union_denied"));
                },
                guid)
            { Purpose = UnionLetterPurpose.Form };
        }

        private static UnionLetter BuildDissolve(Alliance from, Alliance to, long expire, string guid)
        {
            return new UnionLetter(from, to, expire,
                () =>
                {
                    RemoveAndSync(from, to, guid);
                    if (!UnionHander.unionAlreadyExist(from, to)) return; // already gone
                    UnionBreakHelper.DissolveByAgreement(from, to);
                },
                () =>
                {
                    RemoveAndSync(from, to, guid);
                    MessageHandler.SendMsgInAlliance(from, Lang.Get("claims:union_dissolve_denied", to.getPartNameReplaceUnder()));
                },
                guid)
            { Purpose = UnionLetterPurpose.Dissolve };
        }

        /// <summary>Pushes a newly added letter to both alliances' union-letters GUI.</summary>
        public static void MirrorToClients(UnionLetter letter)
        {
            if (letter?.From == null || letter.To == null) return;

            var cell = new ClientUnionLetterCellElement(letter.From.GetPartName(), letter.From.Guid,
                letter.To.GetPartName(), letter.To.Guid, letter.TimeStampExpire, letter.Guid, letter.Purpose);
            var payload = new Dictionary<string, object> { { "value", cell } };
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(letter.From.Guid, payload, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ADD);
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(letter.To.Guid, payload, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_ADD);
        }

        /// <summary>
        /// Drops the letter and tells both GUIs. Must not depend on the letter still being in the
        /// registry: on expiry ExpiringList removes it before invoking OnExpire, and a stale row would
        /// otherwise stay in the union-letters window forever.
        /// </summary>
        private static void RemoveAndSync(Alliance from, Alliance to, string guid)
        {
            if (UnionHander.TryGetUnionLetter(guid, out UnionLetter letter)) UnionHander.removeUnionLetter(letter);
            else claims.getModInstance()?.getDatabaseHandler()?.deleteUnionLetterByGuid(guid);

            var payload = new Dictionary<string, object> { { "value", guid } };
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(from.Guid, payload, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE);
            UsefullPacketsSend.AddToQueueAllianceInfoUpdate(to.Guid, payload, EnumPlayerRelatedInfo.ALLIANCE_UNION_LETTER_REMOVE);
        }
    }
}
