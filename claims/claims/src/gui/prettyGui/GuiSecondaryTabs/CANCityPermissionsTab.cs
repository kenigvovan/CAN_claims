using claims.src.perms;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public class CANCityPermissionsTab : CANGuiSecondaryTab
    {
        private string TitleString;
        private string CommandToCallOnYes;
        private string YesButtonString;
        private string NoButtonString;
        public CANCityPermissionsTab(ICoreClientAPI capi, IconHandler iconHandler, string titleString, string commandToCallOnYes, string yesButtonString = "claims:gui-yes-string", string noButtonString = "claims:gui-no-string")
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
            bool friend = permsHandler.getPerm(perms.PermGroup.COMRADE, permType);
            bool citizen = permsHandler.getPerm(perms.PermGroup.CITIZEN, permType);
            bool stranger = permsHandler.getPerm(perms.PermGroup.STRANGER, permType);
            bool alliance = permsHandler.getPerm(perms.PermGroup.ALLY, permType);

            if (ImGui.Checkbox(friendLabel, ref friend))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/city set p friend {commandSuffix} {(friend ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.setPerm(perms.PermGroup.COMRADE, permType, friend);
            }
            ImGui.SameLine();

            if (ImGui.Checkbox(citizenLabel, ref citizen))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/city set p citizen {commandSuffix} {(citizen ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.setPerm(perms.PermGroup.CITIZEN, permType, citizen);
            }
            ImGui.SameLine();

            if (ImGui.Checkbox(strangerLabel, ref stranger))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/city set p stranger {commandSuffix} {(stranger ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.setPerm(perms.PermGroup.STRANGER, permType, stranger);
            }

            ImGui.SameLine();

            if (ImGui.Checkbox(allyLable, ref alliance))
            {
                cem.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    $"/city set p ally {commandSuffix} {(alliance ? "on" : "off")}",
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.setPerm(perms.PermGroup.ALLY, permType, alliance);
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
            var permsHandler = claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler;
            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            bool pvp = permsHandler.pvpFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-pvp") + "##city", ref pvp))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    "/city set pvp " + (pvp ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.pvpFlag = pvp;
            }

            // ===== FIRE =====
            bool fire = permsHandler.fireFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-fire") + "##city", ref fire))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    "/city set fire " + (fire ? "on" : "off"),
                    EnumChatType.Macro,
                    ""
                );
                permsHandler.fireFlag = fire;
            }

            bool blast = !permsHandler.blastFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-blast") + "##city", ref blast))
            {
                clientEventManager.TriggerNewClientChatLine(
                    GlobalConstants.CurrentChatGroup,
                    "/city set blast " + (!blast ? "on" : "off"),
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
                Lang.Get("claims:gui-admin-group-ally")    + "##build",
                perms.type.PermType.BUILD_AND_DESTROY_PERM,
                "build",
                permsHandler,
                clientEventManager
            );

            ImGui.Separator();

            ImGui.Text(Lang.Get("claims:gui-use-title"));

            DrawPermTriple(
                Lang.Get("claims:gui-admin-group-friend")  + "##use",
                Lang.Get("claims:gui-admin-group-citizen") + "##use",
                Lang.Get("claims:gui-admin-group-stranger")+ "##use",
                Lang.Get("claims:gui-admin-group-ally")    + "##use",
                perms.type.PermType.USE_PERM,
                "use",
                permsHandler,
                clientEventManager
            );

            ImGui.Separator();
            ImGui.Text(Lang.Get("claims:gui-attack-animals-title"));

            DrawPermTriple(
                Lang.Get("claims:gui-admin-group-friend")  + "##attack",
                Lang.Get("claims:gui-admin-group-citizen") + "##attack",
                Lang.Get("claims:gui-admin-group-stranger")+ "##attack",
                Lang.Get("claims:gui-admin-group-ally")    + "##attack",
                perms.type.PermType.ATTACK_ANIMALS_PERM,
                "attack",
                permsHandler,
                clientEventManager
            );

            ImGui.End();
        }
    }
}
