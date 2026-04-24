using System.Drawing;
using System.Linq;
using System.Numerics;
using claims.src.part.structure.plots;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANRemoveCriminalTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANRemoveCriminalTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            this.TitleString = titleString;
            this.CommandCallOnClick = commandCallOnClick;
            this.ButtonString = buttonString;
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
               new Vector2(capi.ModLoader.GetModSystem<claimsGui>().mainWindowPos.X + capi.ModLoader.GetModSystem<claimsGui>().mainWindowSize.X, capi.ModLoader.GetModSystem<claimsGui>().mainWindowPos.Y)
           );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowOpen, flags1);
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo == null) { ImGui.End(); return; }

            ImGui.Text(Lang.Get(TitleString));

            ImGui.Combo("Name", ref capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst, claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.ToArray(), claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.Count);

            if (ImGui.Button(Lang.Get(ButtonString)))
            {
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.Count > capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst)
                {
                    string playerName = claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals[capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst];
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, this.CommandCallOnClick + playerName, EnumChatType.Macro, "");
                    // Optimistic local update
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_CRIMINAL))
                    {
                        claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.Remove(playerName);
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
