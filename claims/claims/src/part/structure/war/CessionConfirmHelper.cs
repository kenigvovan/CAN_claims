using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Member co-sign for plot cession. When an alliance's leader agrees to cede a plot that belongs to a
    /// NON-capital member city (via peace terms or a complied ultimatum), the owning mayor must confirm
    /// before the transfer executes. Capital (MainCity) plots, reparations (alliance treasury) and vassalage
    /// (a collective surrender) stay the leader's unilateral call.
    /// </summary>
    public static class CessionConfirmHelper
    {
        /// <summary>True if ceding this plot needs the owning member's confirmation; outputs that city.</summary>
        public static bool NeedsCoSign(IConflictParty loser, PlotPosition cededPlot, out City ownerCity)
        {
            ownerCity = null;
            if (cededPlot == null) return false;
            if (loser is not Alliance alliance) return false;             // an independent city's mayor already decided
            if (!claims.dataStorage.GetPlot(cededPlot, out Plot plot) || !plot.hasCity()) return false;
            ownerCity = plot.getCity();
            if (!alliance.GetCities().Contains(ownerCity)) return false;   // not the loser's plot (changed hands)
            return !ownerCity.Equals(alliance.MainCity);                   // capital plot = leader's own → no co-sign
        }

        /// <summary>
        /// Asks the owning mayor to confirm the cession via a CESSION_CONFIRM letter. onConfirmed runs on accept,
        /// onRejected on deny or expiry. If a confirm is already pending, the new attempt is rejected.
        /// </summary>
        public static void RequestConfirm(IConflictParty winner, City ownerCity, Action onConfirmed, Action onRejected)
        {
            string guid = ConflictLetter.GetUnusedGuid().ToString();
            long expire = TimeFunctions.getEpochSeconds() + claims.config.DELAY_FOR_CONFLICT_ACTIVATED;
            void cleanup()
            {
                ConflictHandler.removeConflictLetter(winner, ownerCity, LetterPurpose.CESSION_CONFIRM);
                var payload = new Dictionary<string, object> { { "value", (guid, LetterPurpose.CESSION_CONFIRM) } };
                UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(ownerCity, payload, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE);
            }
            if (!ConflictHandler.addConflictLetter(new ConflictLetter(winner, ownerCity, LetterPurpose.CESSION_CONFIRM, expire,
                () => { cleanup(); onConfirmed(); },
                () => { cleanup(); onRejected(); },
                guid)))
            {
                onRejected(); // a confirm is already pending — roll back this attempt
                return;
            }

            // The letter itself is mirrored to both sides by ConflictHandler.addConflictLetter
            MessageHandler.sendMsgInCity(ownerCity, Lang.Get("claims:cession_confirm_requested", winner.GetPartName()));
            MessageHandler.SendMsgInAlliance(winner, Lang.Get("claims:cession_confirm_pending", ownerCity.GetPartName()));
        }
    }
}
