using System.Linq;
using System.Numerics;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.perms;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANPlotsGroupPermissionsTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandToCallOnYes;
        private string YesButtonString;
        private string NoButtonString;
        public CANPlotsGroupPermissionsTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandToCallOnYes, string yesButtonString = "claims:gui-yes-string", string noButtonString = "claims:gui-no-string")
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
            TitleString = titleString;
            CommandToCallOnYes = commandToCallOnYes;
            YesButtonString = yesButtonString;
            NoButtonString = noButtonString;
        }
        void DrawPermTriple(
            string friendLabel,
            string citizenLabel,
            string strangerLabel,
            perms.type.PermType permType,
            string commandSuffix,
            PermsHandler permsHandler,
            ClientEventManager cem,
            PlotsGroupCellElement pgce
        )
        {
            bool citizen = permsHandler.getPerm(perms.PermGroup.CITIZEN, permType);

            if (ImGui.Checkbox(citizenLabel, ref citizen))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/c plotsgroup set p {pgce.Name} citizen {commandSuffix} {(citizen ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.setPerm(perms.PermGroup.CITIZEN, permType, citizen);
            }
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(GuiSys.mainWindowPos.X + GuiSys.mainWindowSize.X, GuiSys.mainWindowPos.Y)
            );


            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref GuiSys.secondaryWindowOpen, flags1);
            
            ImGui.Text(Lang.Get(TitleString, GuiSys.textInput2, GuiSys.textInput));
            var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(GuiSys.textInput), null);
            var permsHandler = cell.PermsHandler;
            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            bool pvp = permsHandler.pvpFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-pvp") + "##pg", ref pvp))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/c plotsgroup set pvp {cell.Name} " + (pvp ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.pvpFlag = pvp;
            }

            // ===== FIRE =====
            bool fire = permsHandler.fireFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-fire") + "##pg", ref fire))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/c plotsgroup set fire {cell.Name} " + (fire ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.fireFlag = fire;
            }

            bool blast = !permsHandler.blastFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-blast") + "##pg", ref blast))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/c plotsgroup set blast {cell.Name} " + (!blast ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.blastFlag = !blast;
            }

            ImGui.Separator();

            ImGui.Text(Lang.Get("claims:gui-build-title"));
            DrawPermTriple(
                Lang.Get("claims:gui-admin-group-friend")  + "##build",
                Lang.Get("claims:gui-admin-group-citizen") + "##build",
                Lang.Get("claims:gui-admin-group-stranger")+ "##build",
                perms.type.PermType.BUILD_AND_DESTROY_PERM,
                "build",
                permsHandler,
                clientEventManager,
                cell
            );

            ImGui.Separator();

            ImGui.Text(Lang.Get("claims:gui-use-title"));

            DrawPermTriple(
                Lang.Get("claims:gui-admin-group-friend")  + "##use",
                Lang.Get("claims:gui-admin-group-citizen") + "##use",
                Lang.Get("claims:gui-admin-group-stranger")+ "##use",
                perms.type.PermType.USE_PERM,
                "use",
                permsHandler,
                clientEventManager,
                cell
            );

            ImGui.Separator();
            ImGui.Text(Lang.Get("claims:gui-attack-animals-title"));

            DrawPermTriple(
                Lang.Get("claims:gui-admin-group-friend")  + "##attack",
                Lang.Get("claims:gui-admin-group-citizen") + "##attack",
                Lang.Get("claims:gui-admin-group-stranger")+ "##attack",
                perms.type.PermType.ATTACK_ANIMALS_PERM,
                "attack",
                permsHandler,
                clientEventManager,
                cell
            );

            ImGui.End();
        }
    }
}
