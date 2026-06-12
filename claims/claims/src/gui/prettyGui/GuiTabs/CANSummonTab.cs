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

            Vector4 titleColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);

            // Title
            ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
            ImGui.SetWindowFontScale(1.2f);
            string titleText = Lang.Get("claims:gui-summon-points-title");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(titleText).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(titleText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
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
                ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                ImGui.SetWindowFontScale(1.1f);
                ImGui.Text(summonCell.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.PopStyleColor();

                // Coordinates (gray)
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-prison-cell-coords",
                    (summonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                    (summonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                    (summonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString()));
                ImGui.PopStyleColor();

                if (ImGui.ImageButton("usesummon", this.iconHandler.GetOrLoadIcon("dodging"), new Vector2(16)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c summon use " + summonCell.Name, EnumChatType.Macro, "");
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-summon-use-tooltip"));

                if (clientInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_SUMMON))
                {
                    ImGui.SameLine();
                    if (ImGui.ImageButton("renamesummon", this.iconHandler.GetOrLoadIcon("highlighter"), new Vector2(16)))
                    {
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_SUMMON_NEED_NAME;
                        GuiSys.selectedPos = summonCell.SpawnPosition;
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-summon-rename-tooltip"));
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
