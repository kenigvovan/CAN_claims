using System.Linq;
using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANSelectPlotsGroupToRemoveTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANSelectPlotsGroupToRemoveTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
            var plotsGroupsArray = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.Select(gr => gr.Name).ToArray();
            ImGui.Combo("Name", ref GuiSys.selectedComboFirst, plotsGroupsArray, plotsGroupsArray.Length);

            if (ImGui.Button(Lang.Get(ButtonString)))
            {
                if (claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.Count > GuiSys.selectedComboFirst)
                {
                    string plotsGroupName = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells[GuiSys.selectedComboFirst].Name;
                    GuiSys.textInput = plotsGroupName;
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTSGROUP_REMOVE_CONFIRM;
                }         
            }
            ImGui.End();
        }
    }
}
