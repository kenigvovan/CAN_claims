using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPlotsGroupTab : CANGuiTab
    {
        public CANPlotsGroupTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
            {
                return;
            }
           
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            string titleText = Lang.Get("claims:gui-plots-group-title");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(titleText).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(titleText);

            var perms = clientInfo.PlayerPermissions;
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_CREATE))
            {
                if (ImGui.ImageButton("addnewplotsgroup", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = "";
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-add-new-plotsgroup"));
                }
                ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_REMOVE))
            {
                if (ImGui.ImageButton("removeplotsgroup", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = "";
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_REMOVE_SELECT;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-remove-plotsgroup"));
                }
                ImGui.SameLine();
            }
            if (ImGui.ImageButton("showreceivedplotsgroupinvites", this.iconHandler.GetOrLoadIcon("circle"), new Vector2(16)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.PLOTSGROUPRECEIVEDINVITES;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-show-received-invites"));
            }

            ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
            int i = 0;
            foreach (var plotsGroup in claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells)
            {
                ImGui.PushID(i);

                Vector2 start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;

                ImGui.BeginGroup();

                ImGui.Text($"City: {plotsGroup.CityName}");
                ImGui.Text($"Name: {plotsGroup.Name}");

                if (ImGui.Button("Info##" + i))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.PlotsGroupInfoPage;
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = plotsGroup.Guid;
                }

                ImGui.EndGroup();

                Vector2 end = ImGui.GetItemRectMax();
                var draw = ImGui.GetWindowDrawList();

                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
                i++;
            }
            ImGui.EndChild();
        }
    }
}
