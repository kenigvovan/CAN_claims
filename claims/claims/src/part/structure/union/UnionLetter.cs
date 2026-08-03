using claims.src.delayed;
using System;

namespace claims.src.part.structure.union
{
    /// <summary>What the two sides are asked to agree on.</summary>
    public enum UnionLetterPurpose
    {
        /// <summary>Form a union.</summary>
        Form,
        /// <summary>Dissolve it by mutual consent - no denunciation delay, no post-break cooldowns.</summary>
        Dissolve
    }

    public class UnionLetter : IExpirable
    {
        public Alliance From { get; set; }
        public Alliance To { get; set; }
        public long TimeStampExpire { get; set; }
        public UnionLetterPurpose Purpose { get; set; } = UnionLetterPurpose.Form;
        public Action OnAccept { get; }
        public Action OnDeny { get; }
        public string Guid { get; }
        public Action OnExpire => OnDeny;
        public UnionLetter(Alliance from, Alliance to, long timeStampExpire, Action onAccept, Action OnDeny, string guid)
        {
            From = from;
            To = to;
            TimeStampExpire = timeStampExpire;
            this.OnAccept = onAccept;
            this.OnDeny = OnDeny;
            Guid = guid;
        }
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;
            if (!(obj is UnionLetter))
                return false;

            return ((
                (this.From == ((UnionLetter)obj).From && this.To == ((UnionLetter)obj).To)
                ||
                (this.From == ((UnionLetter)obj).To && this.To == ((UnionLetter)obj).From)
                ));
        }
        public override int GetHashCode()
        {
            return From.GetHashCode() ^ To.GetHashCode();
        }
        public static Guid GetUnusedGuid()
        {
            Guid newGuid;
            while (true)
            {
                newGuid = System.Guid.NewGuid();
                if (UnionHander.GuidIsFree(newGuid))
                    break;
            }
            return newGuid;
        }
    }
}
