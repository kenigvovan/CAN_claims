using claims.src.auxialiry;
using ImGuiNET;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPlayerTab: CANGuiTab
    {
        public CANPlayerTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;

            // --- City status ---
            if (clientInfo.CityInfo != null && clientInfo.CityInfo.Name != "")
            {
                // City name (gold, centered)
                CenteredTitle(clientInfo.CityInfo.Name, ColValue, 1.2f);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Lang.Get("claims:gui-citizen-info-tooltip"));

                // Prefix + titles (gray, centered)
                if (clientInfo.CityInfo.CityTitles != null && clientInfo.CityInfo.CityTitles.Count > 0)
                {
                    CenteredTitle(string.Join(", ", clientInfo.CityInfo.CityTitles), ColLabel, 1.0f);
                }
                else if (clientInfo.CityInfo.MayorName == capi.World.Player.PlayerName)
                {
                    CenteredTitle(Lang.Get("claims:gui-city-tab-mayor"), ColLabel, 1.0f);
                }

                // Prefix / AfterName if set
                if (!string.IsNullOrEmpty(clientInfo.CityInfo.Prefix) || !string.IsNullOrEmpty(clientInfo.CityInfo.AfterName))
                {
                    string displayName = $"{clientInfo.CityInfo.Prefix} {capi.World.Player.PlayerName} {clientInfo.CityInfo.AfterName}".Trim();
                    CenteredTitle(displayName, ColLabel, 1.0f);
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }

            // --- Friends ---
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(Lang.Get("claims:gui-friends", clientInfo.Friends.Count));
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered() && clientInfo.Friends.Count > 0)
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.Friends, ','));

            ImGui.SameLine();
            if (GreenIconButton("addfriend", "expander", 15, Lang.Get("claims:gui-player-add-friend-tooltip")))
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ADD_FRIEND_NEED_NAME;

            ImGui.SameLine();
            if (RedIconButton("removefriend", "contract", 15, Lang.Get("claims:gui-player-remove-friend-tooltip")))
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_FRIEND;

            // --- Economy ---
            if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY" && clientInfo.PlayerBalance > 0)
            {
                ImGui.Spacing();
                Label(Lang.Get("claims:gui-player-balance", clientInfo.PlayerBalance.ToString("F0")));
            }

            if (clientInfo.PlayerNextPayments.Count > 0)
            {
                Label(Lang.Get("claims:player-next-payment", clientInfo.PlayerNextPayments.Values.Sum().ToString()));
            }

            // --- Bottom navigation ---
            AlignBottom();

            if (IconButton("citylist", "village", 60, Lang.Get("claims:gui_city_list_title")))
                GuiSys.selectedTab = EnumSelectedTab.CITIESLISTPAGE;

            ImGui.SameLine();
            if (IconButton("alliancelist", "vertical-banner", 60, Lang.Get("claims:gui_alliance_list_title")))
                GuiSys.selectedTab = EnumSelectedTab.AllianceListPage;
        }
    }
}
