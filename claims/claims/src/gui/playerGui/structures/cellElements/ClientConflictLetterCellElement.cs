using claims.src.part.structure.conflict;
using claims.src.part.structure.war;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientConflictLetterCellElement
    {
        /// <summary>Demands attached to a peace offer / ultimatum, so the recipient sees what they accept.</summary>
        public PeaceTermType TermType { get; set; } = PeaceTermType.None;
        public long TermAmount { get; set; }
        /// <summary>Cession only: absolute block coords of the demanded plot's center, so the
        /// recipient knows which plot is being taken instead of just "a plot".</summary>
        public bool HasCededPlot { get; set; }
        public int CededPlotBlockX { get; set; }
        public int CededPlotBlockZ { get; set; }
        public string From { get; set; }
        public string FromGuid { get; set; }
        public WarTargetType FromType { get; set; }
        public string To { get; set; }
        public string ToGuid { get; set; }
        public WarTargetType ToType { get; set; }
        public LetterPurpose Purpose { get; set; }
        public long TimeStampExpire { get; set; }
        public string Guid { get; set; }
        /// <summary>NON_AGGRESSION only: how long the offered pact would last, so the GUI can name the price of "yes".</summary>
        public int NapDays { get; set; }
        public ClientConflictLetterCellElement(string from, string fromGuid, WarTargetType fromType,
            string to, string toGuid, WarTargetType toType,
            LetterPurpose purpose, long timeStampExpire, string guid)
        {
            From = from;
            FromGuid = fromGuid;
            FromType = fromType;
            To = to;
            ToGuid = toGuid;
            ToType = toType;
            Purpose = purpose;
            TimeStampExpire = timeStampExpire;
            Guid = guid;
        }

        /// <summary>Copies the pact length over for display. Server side only.</summary>
        public ClientConflictLetterCellElement WithNapDays(int napDays)
        {
            NapDays = napDays;
            return this;
        }

        /// <summary>Copies the letter's demands over for display. Server side only.</summary>
        public ClientConflictLetterCellElement WithTerms(PeaceTerms terms)
        {
            if (terms == null) return this;

            TermType = terms.Type;
            TermAmount = terms.Amount;
            if (terms.CededPlot != null)
            {
                int plotSize = claims.config.PLOT_SIZE;
                HasCededPlot = true;
                CededPlotBlockX = terms.CededPlot.X * plotSize + plotSize / 2;
                CededPlotBlockZ = terms.CededPlot.Z * plotSize + plotSize / 2;
            }
            return this;
        }
    }
}
