using claims.src.delayed;
using claims.src.part;
using System;

namespace claims.src.part.structure.conflict
{
    public class ConflictLetter : IExpirable
    {
        public IConflictParty From { get; set; }
        public IConflictParty To { get; set; }
        public LetterPurpose Purpose { get; set; }
        public long TimeStampExpire { get; set; }
        public Action OnAccept { get; }
        public Action OnDeny { get; }
        public string Guid { get; }
        public Action OnExpire => OnDeny;
        public ConflictLetter(IConflictParty from, IConflictParty to, LetterPurpose purpose, long timeStampExpire, Action onAccept, Action OnDeny, string guid)
        {
            From = from;
            To = to;
            Purpose = purpose;
            TimeStampExpire = timeStampExpire;
            this.OnAccept = onAccept;
            this.OnDeny = OnDeny;
            Guid = guid;
        }
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;
            if (!(obj is ConflictLetter))
                return false;

            return ((
                (this.From == ((ConflictLetter)obj).From && this.To == ((ConflictLetter)obj).To)
                ||
                (this.From == ((ConflictLetter)obj).To && this.To == ((ConflictLetter)obj).From)
                )
                &&
                (this.Purpose == ((ConflictLetter)obj).Purpose));
        }
        public override int GetHashCode()
        {
            // XOR of From/To so that A→B and B→A produce the same hash (matches symmetric Equals)
            return this.From.GetHashCode() ^ this.To.GetHashCode() ^ this.Purpose.GetHashCode();
        }
        public static Guid GetUnusedGuid()
        {
            Guid newGuid;
            while (true)
            {
                newGuid = System.Guid.NewGuid();
                if (ConflictHandler.GuidIsFree(newGuid))
                    break;
            }
            return newGuid;
        }
    }
}
