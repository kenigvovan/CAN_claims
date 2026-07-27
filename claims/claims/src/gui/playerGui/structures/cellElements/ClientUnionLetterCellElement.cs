using claims.src.part.structure.conflict;
using claims.src.part.structure.union;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientUnionLetterCellElement
    {
        public string From { get; set; }
        public string FromGuid { get; set; }
        public string To { get; set; }
        public string ToGuid { get; set; }
        public long TimeStampExpire { get; set; }
        public string Guid { get; set; }
        /// <summary>Form a union or dissolve it - accepting means opposite things, so the GUI must say which.</summary>
        public UnionLetterPurpose Purpose { get; set; } = UnionLetterPurpose.Form;
        public ClientUnionLetterCellElement(string from, string fromGuid, string to, string toGuid, long timeStampExpire, string guid,
            UnionLetterPurpose purpose = UnionLetterPurpose.Form)
        {
            From = from;
            To = to;
            TimeStampExpire = timeStampExpire;
            Guid = guid;
            FromGuid = fromGuid;
            ToGuid = toGuid;
            Purpose = purpose;
        }
    }
}
