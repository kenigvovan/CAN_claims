using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANNeedDoubleInputTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANNeedDoubleInputTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
            ImGui.InputInt("", ref GuiSys.doubleInput);
            
            if(ImGui.Button((Lang.Get(ButtonString))))
            {
                SendCommand(CommandCallOnClick + GuiSys.doubleInput.ToString());
                // Optimistic local update for plot tax
                if (CommandCallOnClick.StartsWith("/plot set fee ")
                    && (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FEE)
                        || claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS)))
                {
                    claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.CustomTax = GuiSys.doubleInput;
                }
                GuiSys.textInput = "";
            }
            ImGui.End();
        }
    }
}
