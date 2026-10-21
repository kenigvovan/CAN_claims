using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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
        private const string TempleAttributeKey = "claimsrespawntemple";

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

        /// <summary>
        /// The temple plot the player picked, if it still belongs to <paramref name="cityGuid"/>.
        /// The city guid is stored with the plot position: a player who moved to another city must
        /// not keep pointing at the old one's temple, and plot coordinates repeat between cities.
        /// </summary>
        public static bool TryReadTemple(IPlayer player, string cityGuid, out Vec2i plotPos)
        {
            plotPos = null;
            var attributes = player?.Entity?.WatchedAttributes;
            if (attributes == null || string.IsNullOrEmpty(cityGuid)) return false;

            string raw = attributes.GetString(TempleAttributeKey, null);
            if (string.IsNullOrEmpty(raw)) return false;

            string[] parts = raw.Split('|');
            if (parts.Length != 3 || parts[0] != cityGuid) return false;
            if (!int.TryParse(parts[1], out int x) || !int.TryParse(parts[2], out int z)) return false;

            plotPos = new Vec2i(x, z);
            return true;
        }

        public static void WriteTemple(IPlayer player, string cityGuid, Vec2i plotPos)
        {
            var attributes = player?.Entity?.WatchedAttributes;
            if (attributes == null || string.IsNullOrEmpty(cityGuid) || plotPos == null) return;

            attributes.SetString(TempleAttributeKey, cityGuid + "|" + plotPos.X + "|" + plotPos.Y);
            attributes.MarkPathDirty(TempleAttributeKey);
        }

        public static void ClearTemple(IPlayer player)
        {
            var attributes = player?.Entity?.WatchedAttributes;
            if (attributes == null) return;

            attributes.RemoveAttribute(TempleAttributeKey);
            attributes.MarkPathDirty(TempleAttributeKey);
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
