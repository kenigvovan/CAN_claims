using System;

namespace claims.src.part.structure.union
{
    public class UnionLetter
    {
        public Alliance From { get; set; }
        public Alliance To { get; set; }
        public long TimeStampExpire { get; set; }
        public Action OnAccept { get; }
        public Action OnDeny { get; }
        public string Guid { get; }
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
            int hash = 13;
            hash = (hash * 7) + this.From.GetHashCode();
            hash = (hash * 7) + this.To.GetHashCode();
            return hash;
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
