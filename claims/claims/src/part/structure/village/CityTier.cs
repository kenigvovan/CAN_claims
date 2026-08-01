namespace claims.src.part.structure
{
    /// <summary>
    /// Settlement tier. A village is the same <see cref="City"/> object with a cut-down feature set:
    /// no treasury, alliances, wars, prisons, summons or plot groups, and a much smaller plot limit.
    /// CITY is 0 on purpose - old rows (column DEFAULT 0) and any packet that never carried the
    /// field fall back to the pre-village behaviour.
    /// </summary>
    public enum CityTier
    {
        CITY = 0,
        VILLAGE = 1
    }
}
