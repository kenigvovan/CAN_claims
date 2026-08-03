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

            // Hide host-disabled plot types (server still enforces in Plot.setNewType).
            var availableTypes = PlotInfo.plotAccessableForPlayersWithCode.Keys
                .Where(k => !claims.config.DISABLED_PLOT_TYPES.Contains(k))
                .ToList();

            ImGui.Combo("Name", ref GuiSys.selectedComboFirst, availableTypes.ToArray(), availableTypes.Count);

            if (ImGui.Button(Lang.Get(ButtonString)))
            {
                if (availableTypes.Count > GuiSys.selectedComboFirst)
                {
                    string playerName = availableTypes[GuiSys.selectedComboFirst];
                    SendCommand(this.CommandCallOnClick + playerName);
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
