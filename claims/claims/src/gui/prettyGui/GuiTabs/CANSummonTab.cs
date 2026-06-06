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
            string titleText = Lang.Get("claims:gui-summon-points-title");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(titleText).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(titleText);

            if (clientInfo.CityInfo == null)
            {
                return;
            }
            int i = 0;
            ImGui.BeginChild("SummonPointsScroll", new Vector2(0, 300), true);
            foreach (var summonCell in claims.clientDataStorage.clientPlayerInfo.CityInfo.SummonCells)
            {
                ImGui.PushID(i);

                Vector2 start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;

                ImGui.BeginGroup();
                ImGui.Text(summonCell.Name);
                string text = Lang.Get("claims:gui-prison-cell-coords",
                                     (summonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                                     (summonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                                     (summonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString());
                ImGui.Text(text);
               
                if (ImGui.ImageButton("usesummon", this.iconHandler.GetOrLoadIcon("dodging"), new Vector2(16)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c summon use " + summonCell.Name, EnumChatType.Macro, "");
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("removecell", this.iconHandler.GetOrLoadIcon("highlighter"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_SUMMON_NEED_NAME;
                    GuiSys.selectedPos = summonCell.SpawnPosition;
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
