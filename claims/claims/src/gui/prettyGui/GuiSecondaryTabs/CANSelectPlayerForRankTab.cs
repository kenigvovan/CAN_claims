using System.Linq;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANSelectPlayerForRankTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandToCallOnYes;
        private string YesButtonString;
        private string NoButtonString;
        public CANSelectPlayerForRankTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandToCallOnYes, string yesButtonString = "claims:gui-yes-string", string noButtonString = "claims:gui-no-string")
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            TitleString = titleString;
            CommandToCallOnYes = commandToCallOnYes;
            YesButtonString = yesButtonString;
            NoButtonString = noButtonString;
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

            ImGui.Text(Lang.Get(TitleString, capi.ModLoader.GetModSystem<claimsGui>().textInput));
            ImGui.Combo("Name", ref capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst, claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(), claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.Count);

            if (ImGui.Button(Lang.Get(YesButtonString)))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                capi.ModLoader.GetModSystem<claimsGui>().textInput2 = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames[capi.ModLoader.GetModSystem<claimsGui>().selectedComboFirst];
                string rankName = capi.ModLoader.GetModSystem<claimsGui>().textInput;
                string playerName = capi.ModLoader.GetModSystem<claimsGui>().textInput2;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, this.CommandToCallOnYes + " " +
                    rankName + " " + playerName, EnumChatType.Macro, "");
                // Optimistic local update
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    var rankCell = claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks.FirstOrDefault(rc => rc.Name == rankName);
                    if (rankCell != null)
                    {
                        rankCell.Citizens.Add(playerName);
                    }
                }
                capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get(NoButtonString)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NONE;
            }
            ImGui.End();
        }
    }
}
