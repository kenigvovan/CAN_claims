using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.part.structure.war
{
    public enum PeaceTermType
    {
        None,         // "white peace": status quo, no conditions
        Reparations,  // loser pays the winner a lump sum
        Vassalage,    // loser's cities become vassals of the winner
        Cession       // loser cedes the plot the proposer stands on to the winner
    }

    public class PeaceTerms
    {
        public PeaceTermType Type { get; set; } = PeaceTermType.None;
        public long Amount { get; set; } = 0;
        // For Cession: the plot to be transferred (set by the command at offer time).
        public PlotPosition CededPlot { get; set; } = null;

        public PeaceTerms() { }
        public PeaceTerms(PeaceTermType type, long amount = 0) { Type = type; Amount = amount; }

        public static PeaceTerms Parse(string word, long amount)
        {
            PeaceTermType type = (word ?? "none").ToLowerInvariant() switch
            {
                "reparations" => PeaceTermType.Reparations,
                "vassalage" => PeaceTermType.Vassalage,
                "cession" => PeaceTermType.Cession,
                _ => PeaceTermType.None
            };
            return new PeaceTerms(type, amount);
        }
    }

    /// <summary>
    /// Applies negotiated peace terms when an END_CONFLICT offer is accepted. The proposer is the
    /// beneficiary (winner) and the target is the loser (pays reparations / becomes a vassal).
    /// Must be called BEFORE PartDemolition.DemolishConflict (which clears the sides).
    /// </summary>
    public static class PeaceTermsHelper
    {
        public static void Apply(IConflictParty proposer, IConflictParty target, PeaceTerms terms)
        {
            if (!claims.config.WAR_PEACE_TERMS_ENABLED) return;
            Enforce(proposer, target, terms);
        }

        /// <summary>
        /// Executes a transfer (reparations / vassalage / cession) with the proposer as the beneficiary,
        /// WITHOUT the peace-terms config gate. Used both by peace offers (via Apply) and by ultimatums,
        /// which have their own enable flag.
        /// </summary>
        public static void Enforce(IConflictParty proposer, IConflictParty target, PeaceTerms terms)
        {
            if (terms == null || proposer == null || target == null) return;
            switch (terms.Type)
            {
                case PeaceTermType.Reparations:
                    ApplyReparations(proposer, target, terms.Amount);
                    break;
                case PeaceTermType.Vassalage:
                    ApplyVassalage(proposer, target);
                    break;
                case PeaceTermType.Cession:
                    ApplyCession(proposer, target, terms.CededPlot);
                    break;
            }
        }

        /// <summary>
        /// Applies the terms and then runs <paramref name="finalize"/>. A Cession of a non-capital member plot is
        /// deferred to the owning mayor's co-sign: on confirm the plot transfers and finalize runs; on refusal or
        /// expiry <paramref name="onReject"/> runs and nothing transfers. All other terms apply immediately.
        /// </summary>
        public static void ApplyWithConfirm(IConflictParty proposer, IConflictParty target, PeaceTerms terms, Action finalize, Action onReject)
        {
            if (terms != null && terms.Type == PeaceTermType.Cession
                && CessionConfirmHelper.NeedsCoSign(target, terms.CededPlot, out City ownerCity))
            {
                PlotPosition ceded = terms.CededPlot;
                CessionConfirmHelper.RequestConfirm(proposer, ownerCity,
                    () => { ApplyCession(proposer, target, ceded); finalize(); },
                    onReject);
                return;
            }
            Enforce(proposer, target, terms);
            finalize();
        }

        private static void ApplyCession(IConflictParty winner, IConflictParty loser, PlotPosition cededPlot)
        {
            if (cededPlot == null) return;
            City winnerCity = winner is Alliance a ? a.MainCity : winner as City;
            if (winnerCity == null) return;
            if (!claims.dataStorage.GetPlot(cededPlot, out Plot plot) || !plot.hasCity()) return;
            City loserCity = plot.getCity();
            if (!loser.GetCities().Contains(loserCity)) return;   // plot changed hands since the offer
            if (loserCity.getCityPlots().Count <= 1) return;      // never strand the loser's last plot

            PlotTransferHelper.Transfer(plot, loserCity, winnerCity, markCaptured: true);
            loserCity.FirePlotsMapChanged(EnumPlotsMapChangeReason.PlotLostToEnemy);
            winnerCity.FirePlotsMapChanged(EnumPlotsMapChangeReason.PlotCapturedByUs);
            MessageHandler.SendMsgInAlliance(loser, Lang.Get("claims:peace_ceded_plot", winner.GetPartName()));
            MessageHandler.SendMsgInAlliance(winner, Lang.Get("claims:peace_gained_plot", loser.GetPartName()));
        }

        private static void ApplyReparations(IConflictParty winner, IConflictParty loser, long amount)
        {
            if (amount <= 0) return;
            decimal bal = claims.economyProvider.GetBalance(loser.MoneyAccountName);
            long amt = (long)Math.Min(amount, (long)bal);
            if (amt <= 0) return;
            if (claims.economyProvider.Transfer(loser.MoneyAccountName, winner.MoneyAccountName, (decimal)amt) != MoneyOperationResult.Success)
                return;
            MessageHandler.SendMsgInAlliance(loser, Lang.Get("claims:peace_reparations_paid", amt, winner.GetPartName()));
            MessageHandler.SendMsgInAlliance(winner, Lang.Get("claims:peace_reparations_received", amt, loser.GetPartName()));
        }

        private static void ApplyVassalage(IConflictParty winner, IConflictParty loser)
        {
            City overlord = winner is Alliance a ? a.MainCity : winner as City;
            if (overlord == null) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (City loserCity in loser.GetCities())
            {
                if (loserCity.Equals(overlord)) continue;
                // A city can be subdued twice over. Only its own pointer was being overwritten, so
                // the previous overlord kept it on the list it hands out in /city vassals - and,
                // worse, freed it from its new overlord when that old one was demolished.
                if (loserCity.IsVassal() && loserCity.OverlordGuid != overlord.Guid)
                {
                    ReleaseVassal(loserCity);
                }
                loserCity.OverlordGuid = overlord.Guid;
                loserCity.VassalSince = now;
                if (!overlord.VassalCities.Contains(loserCity)) overlord.VassalCities.Add(loserCity);
                loserCity.saveToDatabase();
            }
            overlord.saveToDatabase();
            MessageHandler.SendMsgInAlliance(loser, Lang.Get("claims:peace_became_vassal", overlord.GetPartName()));
            MessageHandler.SendMsgInAlliance(winner, Lang.Get("claims:peace_gained_vassal", loser.GetPartName()));
        }

        /// <summary>Breaks vassalage (auto-release or rebellion), clearing both sides of the link.</summary>
        public static void ReleaseVassal(City vassal)
        {
            if (vassal == null || !vassal.IsVassal()) return;
            City overlord = vassal.GetOverlord();
            vassal.OverlordGuid = "";
            vassal.VassalSince = 0;
            vassal.saveToDatabase();
            if (overlord != null)
            {
                overlord.VassalCities.Remove(vassal);
                overlord.saveToDatabase();
            }
        }
    }
}
