using Vintagestory.API.Common;

namespace claims.src.part
{
    public enum EnumRespawnPreference
    {
        /// <summary>Closest point of any kind - the behaviour before the setting existed.</summary>
        NEAREST = 0,
        /// <summary>Prefer the city's temple respawn points.</summary>
        HOME = 1,
        /// <summary>Prefer a war camp of an active battle.</summary>
        CAMP = 2
    }

    /// <summary>
    /// Per player respawn preference. Kept in the player entity's WatchedAttributes: vanilla
    /// persists and syncs those for us, so no extra column in the mod database is needed.
    /// </summary>
    public static class RespawnPreference
    {
        private const string AttributeKey = "claimsrespawnpref";

        public static EnumRespawnPreference Read(IPlayer player)
        {
            var attributes = player?.Entity?.WatchedAttributes;
            if (attributes == null) return EnumRespawnPreference.NEAREST;

            int value = attributes.GetInt(AttributeKey, (int)EnumRespawnPreference.NEAREST);
            return System.Enum.IsDefined(typeof(EnumRespawnPreference), value)
                ? (EnumRespawnPreference)value
                : EnumRespawnPreference.NEAREST;
        }

        public static void Write(IPlayer player, EnumRespawnPreference preference)
        {
            var attributes = player?.Entity?.WatchedAttributes;
            if (attributes == null) return;

            attributes.SetInt(AttributeKey, (int)preference);
            attributes.MarkPathDirty(AttributeKey);
        }

        public static bool TryParse(string value, out EnumRespawnPreference preference)
        {
            switch (value?.ToLowerInvariant())
            {
                case "nearest": preference = EnumRespawnPreference.NEAREST; return true;
                case "home": preference = EnumRespawnPreference.HOME; return true;
                case "camp": preference = EnumRespawnPreference.CAMP; return true;
                default: preference = EnumRespawnPreference.NEAREST; return false;
            }
        }

        public static string LangKeyOf(EnumRespawnPreference preference)
        {
            switch (preference)
            {
                case EnumRespawnPreference.HOME: return "claims:respawn_pref_home";
                case EnumRespawnPreference.CAMP: return "claims:respawn_pref_camp";
                default: return "claims:respawn_pref_nearest";
            }
        }
    }
}
