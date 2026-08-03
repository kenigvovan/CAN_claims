namespace claims.src.part.structure
{
    /// <summary>
    /// Who, besides the owner, may use a boat tagged with a medallion.
    ///
    /// Lives on the boat, in WatchedAttributes: it persists with the entity, syncs to nearby clients,
    /// and can only be changed by walking up to that boat.
    /// </summary>
    public enum BoatShareMode
    {
        /// <summary>Vanilla behaviour: the owner alone.</summary>
        PERSONAL = 0,

        /// <summary>Everyone in the owner's city.</summary>
        CITY = 1,

        /// <summary>Everyone in the owner's city and its alliance.</summary>
        ALLIANCE = 2
    }

    public static class BoatShareModeHelper
    {
        /// <summary>Where the mode lives on the boat's WatchedAttributes.</summary>
        public const string ATTRIBUTE = "claimsShareMode";

        /// <summary>
        /// Mode a boat carries. Absent means CITY, so a boat nobody configured is shared.
        /// </summary>
        public static BoatShareMode Of(Vintagestory.API.Common.Entities.Entity entity)
        {
            if (entity == null) return BoatShareMode.CITY;
            return (BoatShareMode)entity.WatchedAttributes.GetInt(ATTRIBUTE, (int)BoatShareMode.CITY);
        }

        public static void Set(Vintagestory.API.Common.Entities.Entity entity, BoatShareMode mode)
        {
            entity.WatchedAttributes.SetInt(ATTRIBUTE, (int)mode);
            entity.WatchedAttributes.MarkPathDirty(ATTRIBUTE);
        }

        public static bool TryParse(string raw, out BoatShareMode mode)
        {
            switch (raw?.ToLowerInvariant())
            {
                case "personal": case "private": mode = BoatShareMode.PERSONAL; return true;
                case "city": case "town": mode = BoatShareMode.CITY; return true;
                case "alliance": mode = BoatShareMode.ALLIANCE; return true;
                default: mode = BoatShareMode.PERSONAL; return false;
            }
        }

        /// <summary>The command word for a mode - what /city boat share takes back.</summary>
        public static string CodeOf(BoatShareMode mode)
        {
            switch (mode)
            {
                case BoatShareMode.CITY: return "city";
                case BoatShareMode.ALLIANCE: return "alliance";
                default: return "personal";
            }
        }

        public static string LangKeyOf(BoatShareMode mode)
        {
            switch (mode)
            {
                case BoatShareMode.CITY: return "claims:boat-share-mode-city";
                case BoatShareMode.ALLIANCE: return "claims:boat-share-mode-alliance";
                default: return "claims:boat-share-mode-personal";
            }
        }

        /// <summary>
        /// The mode as applied, narrowed by config: a mode the host forbids degrades to the next
        /// one down rather than doing nothing.
        /// </summary>
        public static BoatShareMode Effective(BoatShareMode stored)
        {
            if (claims.config?.BOAT_SHARE_WITH_CITY != true) return BoatShareMode.PERSONAL;
            if (stored == BoatShareMode.ALLIANCE && claims.config.BOAT_SHARE_WITH_ALLIANCE != true)
            {
                return BoatShareMode.CITY;
            }
            return stored;
        }
    }
}
