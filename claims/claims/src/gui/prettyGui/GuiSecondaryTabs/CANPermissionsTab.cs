using System.Numerics;
using System.Runtime.ConstrainedExecution;
using claims.src.perms;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANPermissionsTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandToCallOnYes;
        private string YesButtonString;
        private string NoButtonString;
        public CANPermissionsTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandToCallOnYes, string yesButtonString = "claims:gui-yes-string", string noButtonString = "claims:gui-no-string")
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
            string allyLable,
            perms.type.PermType permType,
            string commandSuffix,
            PermsHandler permsHandler,
            ClientEventManager cem
        )
        {
            bool canSetPerms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_PLOT_ACCESS_PERMISSIONS)
                || claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);

            bool friend = permsHandler.getPerm(perms.PermGroup.COMRADE, permType);
            bool citizen = permsHandler.getPerm(perms.PermGroup.CITIZEN, permType);
            bool stranger = permsHandler.getPerm(perms.PermGroup.STRANGER, permType);
            bool alliance = permsHandler.getPerm(perms.PermGroup.ALLY, permType);

            if (ImGui.Checkbox(friendLabel, ref friend))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/plot set p friend {commandSuffix} {(friend ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.COMRADE, permType, friend);
            }
            ImGui.SameLine();

            if (ImGui.Checkbox(citizenLabel, ref citizen))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/plot set p citizen {commandSuffix} {(citizen ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.CITIZEN, permType, citizen);
            }
            ImGui.SameLine();

            if (ImGui.Checkbox(strangerLabel, ref stranger))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/plot set p stranger {commandSuffix} {(stranger ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.STRANGER, permType, stranger);
            }

            ImGui.SameLine();

            if (ImGui.Checkbox(allyLable, ref alliance))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/plot set p ally {commandSuffix} {(alliance ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.ALLY, permType, alliance);
            }
        }
        public override void DrawTab()
        {
            ImGui.SetNextWindowPos(
                new Vector2(capi.ModLoader.GetModSystem<claimsGui>().mainWindowPos.X + capi.ModLoader.GetModSystem<claimsGui>().mainWindowSize.X, capi.ModLoader.GetModSystem<claimsGui>().mainWindowPos.Y)
            );


            ImGuiWindowFlags flags1 =
                 ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("ClaimsDetails", p_open: ref capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowOpen, flags1);
            
            ImGui.Text(Lang.Get(TitleString, capi.ModLoader.GetModSystem<claimsGui>().textInput2, capi.ModLoader.GetModSystem<claimsGui>().textInput));
            var permsHandler = claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler;
            var playerPerms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;
            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            bool canPvp = playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_PVP)
                || playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);
            bool canFire = playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FIRE)
                || playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);
            bool canBlast = playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_BLAST)
                || playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);

            bool pvp = permsHandler.pvpFlag;
            if (ImGui.Checkbox("PVP", ref pvp))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    "/plot set pvp " + (pvp ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                if (canPvp) permsHandler.pvpFlag = pvp;
            }

            // ===== FIRE =====
            bool fire = permsHandler.fireFlag;
            if (ImGui.Checkbox("Fire", ref fire))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    "/plot set fire " + (fire ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                if (canFire) permsHandler.fireFlag = fire;
            }

            bool blast = !permsHandler.blastFlag;
            if (ImGui.Checkbox("Blast", ref blast))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    "/plot set blast " + (!blast ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                if (canBlast) permsHandler.blastFlag = !blast;
            }

            ImGui.Separator();

            ImGui.Text(Lang.Get("claims:gui-build-title"));
            DrawPermTriple(
                "Friend##build",
                "Citizen##build",
                "Stranger##build",
                "Ally##build",
                perms.type.PermType.BUILD_AND_DESTROY_PERM,
            "build",
                permsHandler,
                clientEventManager
            );

            ImGui.Separator();

            ImGui.Text(Lang.Get("claims:gui-use-title"));

            DrawPermTriple(
                "Friend##use",
                "Citizen##use",
                "Stranger##use",
                "Ally##use",
                perms.type.PermType.USE_PERM,
                "use",
                permsHandler,
                                clientEventManager

            );

            ImGui.Separator();
            ImGui.Text(Lang.Get("claims:gui-attack-animals-title"));

            DrawPermTriple(
                "Friend##attack",
                "Citizen##attack",
                "Stranger##attack",
                "Ally##attack",
                perms.type.PermType.ATTACK_ANIMALS_PERM,
                "attack",
                permsHandler,
                clientEventManager

            );

            ImGui.End();
        }
    }
}
