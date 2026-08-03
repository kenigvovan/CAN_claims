using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANRemoveFriendTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANRemoveFriendTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo == null) { ImGui.End(); return; }

            ImGui.Text(Lang.Get(TitleString));

            ImGui.Combo("Name", ref GuiSys.selectedComboFirst, claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(), claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count);

            if (ImGui.Button(Lang.Get(ButtonString)))
            {
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count > GuiSys.selectedComboFirst)
                {
                    string playerName = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames[GuiSys.selectedComboFirst];
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, this.CommandCallOnClick + playerName, EnumChatType.Macro, "");
                    // Optimistic local update
                    claims.clientDataStorage.clientPlayerInfo.Friends.Remove(playerName);
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                }         
            }
            ImGui.End();
        }
    }
}
