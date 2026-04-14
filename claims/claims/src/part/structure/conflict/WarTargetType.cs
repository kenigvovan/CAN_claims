using claims.src.part.structure;

namespace claims.src.part.structure.conflict
{
    public enum WarTargetType
    {
        City,
        Alliance
    }

    public static class WarTargetTypeHelper
    {
        public static WarTargetType FromConflictParty(IConflictParty party)
        {
            return party is Alliance ? WarTargetType.Alliance : WarTargetType.City;
        }
    }
}
