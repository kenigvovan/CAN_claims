using System.Collections.Generic;
using claims.src.gui.prettyGui.GuiTabs;
using Vintagestory.API.Client;

namespace claims.src.gui.prettyGui
{
    public class TabDrawHandler
    {
        private Dictionary<EnumSelectedTab, CANGuiTab> TabDictionary;
        public ICoreClientAPI capi;
        public IconHandler iconHandler;
        public TabDrawHandler(ICoreClientAPI capi, IconHandler iconHandler)
        {
            TabDictionary = new();
            this.capi = capi;
            this.TabDictionary.Add(EnumSelectedTab.CITY, new CANCityTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PLAYER, new CANPlayerTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PRICES, new CANPricesTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PLOT, new CANPlotTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PRISON, new CANPrisonTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.SUMMON, new CANSummonTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.RANKS, new CANRanksTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.RANKINFOPAGE, new CANRanksInfoTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PlotsGroup, new CANPlotsGroupTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PlotsGroupInfoPage, new CANPlotsGroupInfoTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.PLOTSGROUPRECEIVEDINVITES, new CANPlotsGroupInvitesTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.AllianceInfoPage, new CANAllianceTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.ConflictLettersPage, new CANConflictLettersTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.ConflictsPage, new CANConflictTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.ConflictInfoPage, new CANConflictInfoTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.CITIESLISTPAGE, new CANCityListTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.UnionLettersPage, new CANUnionLettersTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.AllianceListPage, new CANAllianceListTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.CityPlotsColorSelector, new CANCityPlotsColorTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.CityLog, new CANCityLogTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSelectedTab.CityMap, new CANCityMapTab(capi, iconHandler));
            this.iconHandler = iconHandler;
        }
        public void RegisterAdminTabs(ICoreClientAPI capi, IconHandler iconHandler)
        {
            if (!TabDictionary.ContainsKey(EnumSelectedTab.ADMIN_WORLD))
                TabDictionary.Add(EnumSelectedTab.ADMIN_WORLD,  new CANAdminWorldTab(capi, iconHandler));
            if (!TabDictionary.ContainsKey(EnumSelectedTab.ADMIN_CITIES))
                TabDictionary.Add(EnumSelectedTab.ADMIN_CITIES, new CANAdminCitiesTab(capi, iconHandler));
            if (!TabDictionary.ContainsKey(EnumSelectedTab.ADMIN_WAR))
                TabDictionary.Add(EnumSelectedTab.ADMIN_WAR,    new CANAdminWarTab(capi, iconHandler));
            if (!TabDictionary.ContainsKey(EnumSelectedTab.ADMIN_PLAYER))
                TabDictionary.Add(EnumSelectedTab.ADMIN_PLAYER, new CANAdminPlayerTab(capi, iconHandler));
        }

        public void DrawTab(EnumSelectedTab selectedTab)
        {
            if (this.TabDictionary.TryGetValue(selectedTab, out var guiTab))
            {
                guiTab.DrawTab();
            }
        }
    }
}
