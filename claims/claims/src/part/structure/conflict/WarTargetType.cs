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

        /// <summary>Localized "City" / "Alliance" label used wherever a war target is listed.</summary>
        public static string LangLabel(WarTargetType type)
        {
            return Vintagestory.API.Config.Lang.Get(type == WarTargetType.Alliance
                ? "claims:conflict_target_alliance" : "claims:conflict_target_city");
        }
    }
}
