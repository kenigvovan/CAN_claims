namespace claims.src.part.structure
{
    public enum PlotType
    {
        // Stored in the database as a plain int, so new members may only be appended - inserting
        // one in the middle would renumber every plot already saved.
        DEFAULT, TAVERN, TOURNAMENT, FARM, CAMP, SUMMON, TEMPLE, EMBASSY, MAIN_CITY_PLOT, PRISON, ORCHARD,
        VILLAGE_MAIN
    }
}
