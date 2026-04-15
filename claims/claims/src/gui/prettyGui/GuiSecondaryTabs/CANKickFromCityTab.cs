using System.Drawing;
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
                new Vector2(capi.ModLoader.GetModSystem<claimsGui>().mainWindowPos.X + capi.ModLoader.GetModSystem<claimsGui>().mainWindowSize.X, capi.ModLoader.GetModSystem<claimsGui>().mainWindowPos.Y)
            );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowOpen, flags1);
            
            ImGui.Text("Select player's name:");

            ImGui.Combo("Name", ref capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst, claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(), claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count);

            if (ImGui.Button("Kick"))
            {
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count > capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst)
                {
                    string playerName = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames[capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst];
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city kick " + playerName, EnumChatType.Macro, "");
                    // Optimistic local update
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_KICK))
                    {
                        claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Remove(playerName);
                        capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst = 0;
                    }
                    capi.ModLoader.GetModSystem<claimsGui>().textInput = "";
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                }         
            }
            ImGui.End();
        }
    }
}
