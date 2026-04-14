using claims.src.part.structure.conflict;

namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientConflictLetterCellElement
    {
        public string From { get; set; }
        public string FromGuid { get; set; }
        public WarTargetType FromType { get; set; }
        public string To { get; set; }
        public string ToGuid { get; set; }
        public WarTargetType ToType { get; set; }
        public LetterPurpose Purpose { get; set; }
        public long TimeStampExpire { get; set; }
        public string Guid { get; set; }
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
    }
}
