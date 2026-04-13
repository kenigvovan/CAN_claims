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
            ImGui.Text(Lang.Get("claims:gui-friends", clientInfo.Friends.Count));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.Friends, ','));
            }

            ImGui.SameLine();
            if (ImGui.ImageButton("addfriend", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(15)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ADD_FRIEND_NEED_NAME;
            }

            ImGui.SameLine();

            if (ImGui.ImageButton("removefriend", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(15)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_FRIEND;
            }
            if (claims.clientDataStorage.clientPlayerInfo.PlayerNextPayments.Count > 0)
            {
                ImGui.Text(Lang.Get("claims:player-next-payment", claims.clientDataStorage.clientPlayerInfo.PlayerNextPayments.Values.Sum().ToString()));
            }


            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);

            if (ImGui.ImageButton("citylist", this.iconHandler.GetOrLoadIcon("village"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.CITIESLISTPAGE;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui_city_list_title"));
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("alliancelist", this.iconHandler.GetOrLoadIcon("vertical-banner"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.AllianceListPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui_alliance_list_title"));
            }
        }
    }
}
