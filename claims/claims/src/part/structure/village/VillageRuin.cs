namespace claims.src.part.structure
{
    /// <summary>
    /// The site of a village that is gone. Stores when it went and how, not when the ban ends:
    /// the length is read from the current config, so changing it applies to existing ruins.
    /// </summary>
    public class VillageRuin
    {
        public long Since { get; set; }
        public bool Abandoned { get; set; }

        public VillageRuin(long since, bool abandoned)
        {
            Since = since;
            Abandoned = abandoned;
        }

        public long Until => Since + (long)Hours * 3600;

        private int Hours => Abandoned
            ? claims.config.VILLAGE_ABANDON_COOLDOWN_HOURS
            : claims.config.VILLAGE_SITE_COOLDOWN_HOURS;
    }
}
