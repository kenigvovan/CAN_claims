using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANNeedNameTab: CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANNeedNameTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            TitleString = titleString;
            CommandCallOnClick = commandCallOnClick;
            ButtonString = buttonString;
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );

            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            ImGui.Text(Lang.Get(TitleString));
            ImGui.InputText("", ref GuiSys.textInput, 256);
            
            if(ImGui.Button((Lang.Get(ButtonString))))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                string inputValue = GuiSys.textInput;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, CommandCallOnClick + inputValue, EnumChatType.Macro, "");
                // Optimistic local updates
                var perms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;
                if (CommandCallOnClick.StartsWith("/city criminal add ")
                    && perms.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_CRIMINAL)
                    && !claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.Contains(inputValue))
                {
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.Add(inputValue);
                }
                else if (CommandCallOnClick.StartsWith("/city set name ")
                    && (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_NAME)
                        || perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL)))
                {
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.Name = inputValue;
                }
                else if (CommandCallOnClick.StartsWith("/plot set name ")
                    && (perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_NAME)
                        || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS)))
                {
                    claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PlotName = inputValue;
                }
                GuiSys.textInput = "";
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            ImGui.End();
        }
    }
}
