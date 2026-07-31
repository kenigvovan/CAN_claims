namespace claims.src.gui.playerGui
{
    /// <summary>Which page the main dialog is currently showing.</summary>
    public enum EnumSelectedTab
    {
        City, Player, Prices, Plot, Prison, Summon, PlotsGroup, PlotsGroupReceivedInvites, Ranks, RankInfoPage, CityPlotsColorSelector, PlotsGroupInfoPage,
        AllianceInfoPage, CitiesListPage, ConflictLettersPage, ConflictsPage, ConflictInfoPage,
        UnionLettersPage, CityLog, AllianceListPage,
        CityMap,
        EmblemEditor,
        AdminWorld, AdminCities, AdminWar, AdminPlayer
    }

    /// <summary>Ordering of the world-wide alliance list.</summary>
    public enum EnumAllianceSort
    {
        Default, Oldest, Cities, Name
    }

    /// <summary>How the world city list is ordered.</summary>
    public enum EnumCitySort
    {
        Default, Oldest, Population, Plots
    }

    /// <summary>Which of the two war range lists the conflict info page is showing.</summary>
    public enum EnumSelectedWarRangesTab
    {
        APPROVED, SUGGESTIONS
    }
}
