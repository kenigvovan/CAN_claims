using claims.src.part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace claims.src.part.structure.conflict
{
    public class ConflictLetter
    {
        public IConflictParty From { get; set; }
        public IConflictParty To { get; set; }
        public LetterPurpose Purpose { get; set; }
        public long TimeStampExpire { get; set; }
        public Thread OnAccept { get; }
        public Thread OnDeny { get; }
        public string Guid { get; }
        public ConflictLetter(IConflictParty from, IConflictParty to, LetterPurpose purpose, long timeStampExpire, Thread onAccept, Thread OnDeny, string guid)
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
            int hash = 13;
            hash = (hash * 7) + this.From.GetHashCode();
            hash = (hash * 7) + this.To.GetHashCode();
            hash = (hash * 7) + this.Purpose.GetHashCode();
            return hash;
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
