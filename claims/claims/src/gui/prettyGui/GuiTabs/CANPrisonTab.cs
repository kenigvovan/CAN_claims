using System.Numerics;
using claims.src.auxialiry;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPrisonTab : CANGuiTab
    {
        // Criminals/occupants use a warning red distinct from the parchment theme.
        static readonly Vector4 CriminalColor = new Vector4(1.0f, 0.45f, 0.35f, 1.0f);

        public CANPrisonTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            if (clientInfo.CityInfo == null)
            {
                return;
            }

            // Title
            CenteredTitle(Lang.Get("claims:gui-prison-tooltip"), ColSection, 1.2f);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-prison-description"));

            ImGui.Separator();
            ImGui.Spacing();

            // Criminals count (red-ish)
            var perms = clientInfo.PlayerPermissions;
            ImGui.PushStyleColor(ImGuiCol.Text, CriminalColor);
            ImGui.Text(Lang.Get("claims:gui-criminals", clientInfo.CityInfo.Criminals.Count));
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered() && clientInfo.CityInfo.Criminals.Count > 0)
            {
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.Criminals, ','));
            }

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_CRIMINAL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_CRIMINAL_ALL))
            {
                ImGui.SameLine();
                if (GreenIconButton("addcriminal", "expander", 16, Lang.Get("claims:gui-prison-add-criminal-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ADD_CRIMINAL_NEED_NAME;
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_CRIMINAL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_CRIMINAL_ALL))
            {
                ImGui.SameLine();
                if (RedIconButton("removecriminal", "contract", 16, Lang.Get("claims:gui-prison-remove-criminal-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_CRIMINAL;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Prison cells section header
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(Lang.Get("claims:gui-prison-cells-title"));
            ImGui.PopStyleColor();

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ADD_CELL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ALL))
            {
                ImGui.SameLine();
                if (GreenIconButton("addprisoncell", "expander", 16, Lang.Get("claims:gui-prison-add-cell-tooltip")))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c prison addcell", EnumChatType.Macro, "");
                }
            }

            ImGui.Spacing();

            ImGui.BeginChild("InvitesScroll", new Vector2(0, 0), false);
            int i = 0;
            foreach (var prisonCell in clientInfo.CityInfo.PrisonCells)
            {
                ImGui.PushID(i);
                ImGui.BeginGroup();

                var coords = Lang.Get("claims:gui-prison-cell-coords",
                                    (prisonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                                    (prisonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                                    (prisonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString());

                Label(coords);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_REMOVE_CELL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ALL))
                {
                    ImGui.SameLine();
                    if (RedIconButton("removecell", "contract", 16, Lang.Get("claims:gui-prison-remove-cell-tooltip")))
                    {
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PRISON_REMOVE_CELL_CONFIRM;
                        GuiSys.selectedPos = prisonCell.SpawnPosition;
                    }
                }

                if (prisonCell.Players.Count > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, CriminalColor);
                    ImGui.Text(string.Join(", ", prisonCell.Players));
                    ImGui.PopStyleColor();
                }
                else
                {
                    Label(Lang.Get("claims:gui-prison-cell-empty"));
                }

                ImGui.EndGroup();
                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 4));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 4));
                i++;
            }
            ImGui.EndChild();
        }
    }
}
