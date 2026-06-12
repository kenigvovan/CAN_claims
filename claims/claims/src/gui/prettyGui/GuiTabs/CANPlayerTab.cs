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

            Vector4 titleColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 valueColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);

            // --- City status ---
            if (clientInfo.CityInfo != null && clientInfo.CityInfo.Name != "")
            {
                float windowWidth = ImGui.GetWindowSize().X;

                // City name (gold, centered)
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.2f);
                string cityName = clientInfo.CityInfo.Name;
                float cityNameWidth = ImGui.CalcTextSize(cityName).X;
                ImGui.SetCursorPosX((windowWidth - cityNameWidth) * 0.5f);
                ImGui.Text(cityName);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Lang.Get("claims:gui-citizen-info-tooltip"));

                // Prefix + titles (gray, centered)
                if (clientInfo.CityInfo.CityTitles != null && clientInfo.CityInfo.CityTitles.Count > 0)
                {
                    string titles = string.Join(", ", clientInfo.CityInfo.CityTitles);
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    float titlesWidth = ImGui.CalcTextSize(titles).X;
                    ImGui.SetCursorPosX((windowWidth - titlesWidth) * 0.5f);
                    ImGui.Text(titles);
                    ImGui.PopStyleColor();
                }
                else if (clientInfo.CityInfo.MayorName == capi.World.Player.PlayerName)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    string mayorLabel = Lang.Get("claims:gui-city-tab-mayor");
                    float mayorWidth = ImGui.CalcTextSize(mayorLabel).X;
                    ImGui.SetCursorPosX((windowWidth - mayorWidth) * 0.5f);
                    ImGui.Text(mayorLabel);
                    ImGui.PopStyleColor();
                }

                // Prefix / AfterName if set
                if (!string.IsNullOrEmpty(clientInfo.CityInfo.Prefix) || !string.IsNullOrEmpty(clientInfo.CityInfo.AfterName))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    string displayName = $"{clientInfo.CityInfo.Prefix} {capi.World.Player.PlayerName} {clientInfo.CityInfo.AfterName}".Trim();
                    float nameWidth = ImGui.CalcTextSize(displayName).X;
                    ImGui.SetCursorPosX((windowWidth - nameWidth) * 0.5f);
                    ImGui.Text(displayName);
                    ImGui.PopStyleColor();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }

            // --- Friends ---
            ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
            ImGui.Text(Lang.Get("claims:gui-friends", clientInfo.Friends.Count));
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered() && clientInfo.Friends.Count > 0)
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.Friends, ','));

            ImGui.SameLine();
            if (ImGui.ImageButton("addfriend", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(15)))
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ADD_FRIEND_NEED_NAME;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-player-add-friend-tooltip"));

            ImGui.SameLine();
            if (ImGui.ImageButton("removefriend", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(15)))
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_FRIEND;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-player-remove-friend-tooltip"));

            // --- Economy ---
            if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY" && clientInfo.PlayerBalance > 0)
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-player-balance", clientInfo.PlayerBalance.ToString("F0")));
                ImGui.PopStyleColor();
            }

            if (clientInfo.PlayerNextPayments.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:player-next-payment", clientInfo.PlayerNextPayments.Values.Sum().ToString()));
                ImGui.PopStyleColor();
            }

            // --- Bottom navigation ---
            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);

            if (ImGui.ImageButton("citylist", this.iconHandler.GetOrLoadIcon("village"), new Vector2(60)))
                GuiSys.selectedTab = EnumSelectedTab.CITIESLISTPAGE;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui_city_list_title"));

            ImGui.SameLine();
            if (ImGui.ImageButton("alliancelist", this.iconHandler.GetOrLoadIcon("vertical-banner"), new Vector2(60)))
                GuiSys.selectedTab = EnumSelectedTab.AllianceListPage;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui_alliance_list_title"));
        }
    }
}
