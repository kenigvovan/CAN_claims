using System.Linq;
using System.Numerics;
using claims.src.part.structure.plots;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANChangePlotTypeTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANChangePlotTypeTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
            
            ImGui.Text(Lang.Get(TitleString));

            ImGui.Combo("Name", ref GuiSys.selectedComboFirst, PlotInfo.plotAccessableForPlayersWithCode.Keys.ToList().ToArray(), PlotInfo.plotAccessableForPlayersWithCode.Keys.ToList().Count);

            if (ImGui.Button(Lang.Get(ButtonString)))
            {
                if (PlotInfo.plotAccessableForPlayersWithCode.Values.Count > GuiSys.selectedComboFirst)
                {
                    string playerName = PlotInfo.plotAccessableForPlayersWithCode.Keys.ToList()[GuiSys.selectedComboFirst];
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, this.CommandCallOnClick + playerName, EnumChatType.Macro, "");
                    // Optimistic local update
                    if ((claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_TYPE)
                        || claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS))
                        && PlotInfo.nameToPlotType.TryGetValue(playerName, out var plotType)
                    && claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo != null)
                    {
                        claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PlotType = plotType;
                    }
                    GuiSys.textInput = "";
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                }         
            }
            ImGui.End();
        }
    }
}
