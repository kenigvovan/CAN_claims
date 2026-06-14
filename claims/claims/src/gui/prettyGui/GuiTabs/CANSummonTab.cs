using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANSummonTab : CANGuiTab
    {
        public CANSummonTab(ICoreClientAPI capi, IconHandler iconHandler)
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

            // Title
            CenteredTitle(Lang.Get("claims:gui-summon-points-title"), ColSection, 1.2f);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-summon-description"));

            ImGui.Separator();
            ImGui.Spacing();

            int i = 0;
            ImGui.BeginChild("SummonPointsScroll", new Vector2(0, 0), false);
            foreach (var summonCell in clientInfo.CityInfo.SummonCells)
            {
                ImGui.PushID(i);
                ImGui.BeginGroup();

                // Summon point name (gold)
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(summonCell.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // Coordinates (gray)
                Label(Lang.Get("claims:gui-prison-cell-coords",
                    (summonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                    (summonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                    (summonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString()));

                if (IconButton("usesummon", "dodging", 16, Lang.Get("claims:gui-summon-use-tooltip")))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c summon use " + summonCell.Name, EnumChatType.Macro, "");
                }

                if (clientInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_SUMMON))
                {
                    ImGui.SameLine();
                    if (IconButton("renamesummon", "highlighter", 16, Lang.Get("claims:gui-summon-rename-tooltip")))
                    {
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_SUMMON_NEED_NAME;
                        GuiSys.selectedPos = summonCell.SpawnPosition;
                    }
                }

                ImGui.EndGroup();
                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 4));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 4));
                i++;
            }
            ImGui.EndChild();
        }
    }
}
