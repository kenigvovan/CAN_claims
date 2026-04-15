using System.Linq;
using System.Numerics;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANKickFromPlotsGroupConfirmTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandCallOnClick;
        private string ButtonString;
        public CANKickFromPlotsGroupConfirmTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandCallOnClick, string buttonString)
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
            PlotsGroupCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(capi.ModLoader.GetModSystem<claimsGui>().textInput), null);
            ImGui.Text(Lang.Get(TitleString, capi.ModLoader.GetModSystem<claimsGui>().textInput2, cell.Name));

            if (ImGui.Button(Lang.Get(ButtonString, capi.ModLoader.GetModSystem<claimsGui>().textInput, capi.ModLoader.GetModSystem<claimsGui>().textInput2)))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                string memberToKick = capi.ModLoader.GetModSystem<claimsGui>().textInput2;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                   string.Format("/c plotsgroup kick {0} {1}", cell.Name, memberToKick), EnumChatType.Macro, "");
                // Optimistic local update
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_PLOTSGROUP_KICK_PLAYER))
                {
                    cell.PlayersNames.Remove(memberToKick);
                }
                capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NONE;
                capi.ModLoader.GetModSystem<claimsGui>().textInput2 = "";
            }
            ImGui.End();
            }
        
    }
}
