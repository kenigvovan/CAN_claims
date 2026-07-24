using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// War score accumulation and the "threshold = victory" win condition. Points are earned for
    /// plot captures, kills and holding an active capture; when a side reaches
    /// <c>config.WAR_SCORE_TO_WIN</c> the conflict ends with that side declared winner — WITHOUT
    /// demolishing the losing city (unlike capturing the enemy's last plot).
    /// </summary>
    public static class WarScoreHelper
    {
        /// <summary>
        /// Adds <paramref name="points"/> to <paramref name="scoringParty"/>'s side of the conflict,
        /// persists, optionally announces, and checks the victory threshold.
        /// </summary>
        public static void AddScore(Conflict conflict, IConflictParty scoringParty, int points, string reasonKey, bool announce = true)
        {
            if (conflict == null || scoringParty == null) return;
            if (!claims.config.WAR_SCORE_ENABLED) return;
            if (points == 0) return;

            bool isFirst = conflict.First != null && conflict.First.Equals(scoringParty);
            bool isSecond = conflict.Second != null && conflict.Second.Equals(scoringParty);
            if (!isFirst && !isSecond) return;

            if (isFirst) conflict.FirstScore += points;
            else conflict.SecondScore += points;
            conflict.saveToDatabase();

            PushScore(conflict);
            if (announce) AnnounceScore(conflict);

            CheckVictory(conflict);
        }

        // Syncs the updated score to both sides' clients (for the war HUD / conflict list).
        private static void PushScore(Conflict conflict)
        {
            if (conflict.First == null || conflict.Second == null) return;
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First,
                new Dictionary<string, object> { { "value", ClientConflictCellElement.FromConflict(conflict) } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_SCORE_UPDATED);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second,
                new Dictionary<string, object> { { "value", ClientConflictCellElement.FromConflict(conflict) } }, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_SCORE_UPDATED);
        }

        private static void AnnounceScore(Conflict conflict)
        {
            if (conflict.First == null || conflict.Second == null) return;
            string msg = Lang.Get("claims:war_score_update",
                conflict.First.GetPartName(), conflict.FirstScore,
                conflict.Second.GetPartName(), conflict.SecondScore,
                claims.config.WAR_SCORE_TO_WIN);
            MessageHandler.SendMsgInAlliance(conflict.First, msg);
            MessageHandler.SendMsgInAlliance(conflict.Second, msg);
        }

        private static bool IsFirst(Conflict conflict, IConflictParty party) => conflict.First != null && conflict.First.Equals(party);
        private static bool IsSecond(Conflict conflict, IConflictParty party) => conflict.Second != null && conflict.Second.Equals(party);

        /// <summary>After-action stat: a plot captured by <paramref name="party"/>.</summary>
        public static void RecordPlotCapture(Conflict conflict, IConflictParty party)
        {
            if (conflict == null || party == null) return;
            if (IsFirst(conflict, party)) conflict.FirstPlotsCaptured++;
            else if (IsSecond(conflict, party)) conflict.SecondPlotsCaptured++;
            else return;
            conflict.saveToDatabase();
        }

        /// <summary>After-action stat: a kill by <paramref name="party"/>.</summary>
        public static void RecordKill(Conflict conflict, IConflictParty party)
        {
            if (conflict == null || party == null) return;
            if (IsFirst(conflict, party)) conflict.FirstKills++;
            else if (IsSecond(conflict, party)) conflict.SecondKills++;
            else return;
            conflict.saveToDatabase();
        }

        /// <summary>After-action stat: coins pillaged by <paramref name="party"/>.</summary>
        public static void RecordPillaged(Conflict conflict, IConflictParty party, long amount)
        {
            if (conflict == null || party == null || amount <= 0) return;
            if (IsFirst(conflict, party)) conflict.FirstPillaged += amount;
            else if (IsSecond(conflict, party)) conflict.SecondPillaged += amount;
            else return;
            conflict.saveToDatabase();
        }

        /// <summary>Ends the conflict with a winner if either side reached the score threshold.</summary>
        public static void CheckVictory(Conflict conflict)
        {
            if (conflict == null || !claims.config.WAR_SCORE_ENABLED) return;
            int toWin = claims.config.WAR_SCORE_TO_WIN;
            if (toWin <= 0) return;
            if (conflict.State == ConflictState.FIRST_WON || conflict.State == ConflictState.SECOND_WON) return;

            if (conflict.FirstScore >= toWin) EndWithWinner(conflict, conflict.First);
            else if (conflict.SecondScore >= toWin) EndWithWinner(conflict, conflict.Second);
        }

        public static void EndWithWinner(Conflict conflict, IConflictParty winner)
        {
            if (conflict == null || winner == null) return;
            IConflictParty loser = winner.Equals(conflict.First) ? conflict.Second : conflict.First;

            conflict.State = winner.Equals(conflict.First) ? ConflictState.FIRST_WON : ConflictState.SECOND_WON;
            conflict.saveToDatabase();

            if (winner != null)
                MessageHandler.SendMsgInAlliance(winner, Lang.Get("claims:war_score_victory", winner.GetPartName(), loser?.GetPartName()));
            if (loser != null)
                MessageHandler.SendMsgInAlliance(loser, Lang.Get("claims:war_score_defeat", winner.GetPartName(), loser.GetPartName()));

            // End the war with a winner but WITHOUT demolishing the losing city.
            PartDemolition.DemolishConflict(conflict, EnumConflictEndReason.ScoreVictory);
        }
    }
}
