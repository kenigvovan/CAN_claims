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
        CityBankrupt,
        CityAbsorbed,
        /// <summary>Removed by the day cycle for not paying. Args: player name, amount owed.</summary>
        CitizenEvictedUnpaid,
        /// <summary>Sub-plots. Args: name.</summary>
        SubPlotCreated,
        /// <summary>Removed by the city or gone with its ground. Args: name.</summary>
        SubPlotRemoved,
        /// <summary>Args: name, buyer, price.</summary>
        SubPlotSold,
        /// <summary>Handed to a citizen by the city. Args: name, player.</summary>
        SubPlotGiven,
        /// <summary>Back with the city: given back, taken back, the owner left or did not pay. Args: name, former owner.</summary>
        SubPlotReturned,
        /// <summary>A new daily tax in force. Args: name, tax.</summary>
        SubPlotTaxChanged,
        /// <summary>The owner did not accept a raise and lost it. Args: name, former owner, tax.</summary>
        SubPlotTaxRefused,
    }
}
