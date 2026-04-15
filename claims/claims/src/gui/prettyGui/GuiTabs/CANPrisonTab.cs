using System.Numerics;
using claims.src.auxialiry;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPrisonTab : CANGuiTab
    {
        public CANPrisonTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            if (clientInfo.CityInfo == null)
            {
                return;
            }
            ImGui.Text(Lang.Get("claims:gui-criminals", clientInfo.CityInfo.Criminals.Count));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.Criminals, ','));
            }

            var perms = clientInfo.PlayerPermissions;
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_CRIMINAL))
            {
                if (ImGui.ImageButton("addcriminal", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.ADD_CRIMINAL_NEED_NAME;
                }
                ImGui.SameLine();
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_CRIMINAL))
            {
                if (ImGui.ImageButton("removecriminal", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_CRIMINAL;
                }
            }

            ImGui.Text(Lang.Get("claims:gui-prison-cells-title"));

            ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
            int i = 0;
            foreach (var prisonCell in claims.clientDataStorage.clientPlayerInfo.CityInfo.PrisonCells)
            {
                ImGui.PushID(i);

                Vector2 start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;

                ImGui.BeginGroup();

                var text = Lang.Get("claims:gui-prison-cell-coords",
                                    (prisonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                                    (prisonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                                    (prisonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString());
                ImGui.Text(text);
                if (ImGui.ImageButton("removecell", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_PRISON_REMOVE_CELL_CONFIRM;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedPos = prisonCell.SpawnPosition;
                }

                string AllPlayers = "";
                if (prisonCell.Players.Count > 0)
                {
                    foreach (var it in prisonCell.Players)
                    {
                        AllPlayers += it;
                    }
                }

                ImGui.Text(AllPlayers);

                ImGui.EndGroup();

                Vector2 end = ImGui.GetItemRectMax();
                var draw = ImGui.GetWindowDrawList();

                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
            }
            ImGui.EndChild();           
        }
    }
}
