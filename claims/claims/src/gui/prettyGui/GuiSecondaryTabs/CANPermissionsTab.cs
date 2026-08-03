using System.Numerics;
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
            PermsHandler permsHandler
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
                SendCommand($"/plot set p friend {commandSuffix} {(friend ? "on" : "off")}");
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.COMRADE, permType, friend);
            }
            ImGui.SameLine();

            if (ImGui.Checkbox(citizenLabel, ref citizen))
            {
                SendCommand($"/plot set p citizen {commandSuffix} {(citizen ? "on" : "off")}");
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.CITIZEN, permType, citizen);
            }
            ImGui.SameLine();

            if (ImGui.Checkbox(strangerLabel, ref stranger))
            {
                SendCommand($"/plot set p stranger {commandSuffix} {(stranger ? "on" : "off")}");
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.STRANGER, permType, stranger);
            }

            ImGui.SameLine();

            if (ImGui.Checkbox(allyLable, ref alliance))
            {
                SendCommand($"/plot set p ally {commandSuffix} {(alliance ? "on" : "off")}");
                if (canSetPerms) permsHandler.setPerm(perms.PermGroup.ALLY, permType, alliance);
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
            var permsHandler = claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler;
            var playerPerms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;
            bool canPvp = playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_PVP)
                || playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);
            bool canFire = playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FIRE)
                || playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);
            bool canBlast = playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_BLAST)
                || playerPerms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS);

            bool pvp = permsHandler.pvpFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-pvp") + "##plot", ref pvp))
            {
                SendCommand("/plot set pvp " + (pvp ? "on" : "off"));
                if (canPvp) permsHandler.pvpFlag = pvp;
            }

            // ===== FIRE =====
            bool fire = permsHandler.fireFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-fire") + "##plot", ref fire))
            {
                SendCommand("/plot set fire " + (fire ? "on" : "off"));
                if (canFire) permsHandler.fireFlag = fire;
            }

            bool blast = !permsHandler.blastFlag;
            if (ImGui.Checkbox(Lang.Get("claims:gui-admin-flag-blast") + "##plot", ref blast))
            {
                SendCommand("/plot set blast " + (!blast ? "on" : "off"));
                if (canBlast) permsHandler.blastFlag = !blast;
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
                permsHandler
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
                permsHandler
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
                permsHandler
            );

            ImGui.End();
        }
    }
}
