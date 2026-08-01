namespace claims.src.citylog
{
    public enum EnumCityLogEvent
    {
        CityCreated,
        CitizenJoined,
        CitizenLeft,
        CitizenKicked,
        MayorChanged,
        ConflictDeclared,
        FlagCaptured,
        AllianceJoined,
        AllianceLeft,
        TreasuryPillaged,
        WarEnded,
        UnionFormed,
        UnionBreakAnnounced,
        UnionBroken,
        // Append only: entries are persisted by their numeric value.
        VillageFounded,
        VillageUpgraded,
        PlotSoldToCity,
        PlotBoughtFromCity,
    }
}
