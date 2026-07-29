using System.Collections.Generic;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Maps a tab to the page that draws it. Pages are created once and reused, the way the ImGui
    /// side already does it. Add() is public so future pages - the admin tabs, the city log, the
    /// map - can register themselves without touching this constructor.
    /// </summary>
    public sealed class PageRegistry
    {
        private readonly Dictionary<EnumSelectedTab, CANGuiPage> pages = new Dictionary<EnumSelectedTab, CANGuiPage>();

        public PageRegistry()
        {
            Add(EnumSelectedTab.City, new CityPage());
            Add(EnumSelectedTab.Player, new PlayerPage());
            Add(EnumSelectedTab.Prices, new PricesPage());
            Add(EnumSelectedTab.Plot, new PlotPage());
            Add(EnumSelectedTab.Prison, new PrisonPage());
            Add(EnumSelectedTab.Summon, new SummonPage());
            Add(EnumSelectedTab.PlotsGroup, new PlotsGroupPage());
            Add(EnumSelectedTab.PlotsGroupInfoPage, new PlotsGroupInfoPage());
            Add(EnumSelectedTab.PlotsGroupReceivedInvites, new PlotsGroupInvitesPage());
            Add(EnumSelectedTab.Ranks, new RanksPage());
            Add(EnumSelectedTab.RankInfoPage, new RankInfoPage());
            Add(EnumSelectedTab.CityPlotsColorSelector, new PlotColorSelectorPage());
            Add(EnumSelectedTab.CitiesListPage, new CitiesListPage());
            Add(EnumSelectedTab.AllianceInfoPage, new AlliancePage());
            Add(EnumSelectedTab.ConflictLettersPage, new ConflictLettersPage());
            Add(EnumSelectedTab.ConflictsPage, new ConflictsPage());
            Add(EnumSelectedTab.ConflictInfoPage, new ConflictInfoPage());
            Add(EnumSelectedTab.UnionLettersPage, new UnionLettersPage());
            Add(EnumSelectedTab.CityLog, new CityLogPage());
            Add(EnumSelectedTab.AllianceListPage, new AllianceListPage());
            Add(EnumSelectedTab.CityMap, new CityMapPage());
        }

        /// <summary>
        /// Registered separately once the player's role is known: the role only arrives after the
        /// world has loaded, so it cannot be decided in the constructor.
        /// </summary>
        public void AddAdminPages()
        {
            if (pages.ContainsKey(EnumSelectedTab.AdminWorld)) return;

            Add(EnumSelectedTab.AdminWorld, new AdminWorldPage());
            Add(EnumSelectedTab.AdminCities, new AdminCitiesPage());
            Add(EnumSelectedTab.AdminWar, new AdminWarPage());
            Add(EnumSelectedTab.AdminPlayer, new AdminPlayerPage());
        }

        public void Add(EnumSelectedTab tab, CANGuiPage page) => pages[tab] = page;

        public bool TryGet(EnumSelectedTab tab, out CANGuiPage page) => pages.TryGetValue(tab, out page);
    }
}
