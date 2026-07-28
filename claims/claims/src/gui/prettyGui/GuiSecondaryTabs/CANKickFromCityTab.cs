using System.Linq;
using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANKickFromCityTab : CANGuiSecondaryTab
    {
        public CANKickFromCityTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo == null) { ImGui.End(); return; }

            ImGui.Text(Lang.Get("claims:gui-kick-select-player"));

            ImGui.Combo("Name", ref GuiSys.selectedComboFirst, claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(), claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count);

            if (ImGui.Button(Lang.Get("claims:gui-kick-button")))
            {
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count > GuiSys.selectedComboFirst)
                {
                    string playerName = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames[GuiSys.selectedComboFirst];
                    SendCommand("/city kick " + playerName);
                    // Optimistic local update
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_KICK))
                    {
                        claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Remove(playerName);
                        GuiSys.selectedComboFirst = 0;
                    }
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                }         
            }
            ImGui.End();
        }
    }
}
