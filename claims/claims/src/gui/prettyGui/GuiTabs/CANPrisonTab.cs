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

            Vector4 titleColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 criminalColor = new Vector4(1.0f, 0.45f, 0.35f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);

            // Title
            ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
            ImGui.SetWindowFontScale(1.2f);
            string titleText = Lang.Get("claims:gui-prison-tooltip");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(titleText).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(titleText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-prison-description"));

            ImGui.Separator();
            ImGui.Spacing();

            // Criminals count (red-ish)
            var perms = clientInfo.PlayerPermissions;
            ImGui.PushStyleColor(ImGuiCol.Text, criminalColor);
            ImGui.Text(Lang.Get("claims:gui-criminals", clientInfo.CityInfo.Criminals.Count));
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered() && clientInfo.CityInfo.Criminals.Count > 0)
            {
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.Criminals, ','));
            }

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_CRIMINAL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_CRIMINAL_ALL))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("addcriminal", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ADD_CRIMINAL_NEED_NAME;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-prison-add-criminal-tooltip"));
            }
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_CRIMINAL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_CRIMINAL_ALL))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("removecriminal", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.REMOVE_CRIMINAL;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-prison-remove-criminal-tooltip"));
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Prison cells section header
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.Text(Lang.Get("claims:gui-prison-cells-title"));
            ImGui.PopStyleColor();

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ADD_CELL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ALL))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("addprisoncell", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c prison addcell", EnumChatType.Macro, "");
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-prison-add-cell-tooltip"));
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

                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(coords);
                ImGui.PopStyleColor();

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_REMOVE_CELL) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ALL))
                {
                    ImGui.SameLine();
                    if (ImGui.ImageButton("removecell", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                    {
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PRISON_REMOVE_CELL_CONFIRM;
                        GuiSys.selectedPos = prisonCell.SpawnPosition;
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-prison-remove-cell-tooltip"));
                }

                if (prisonCell.Players.Count > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, criminalColor);
                    ImGui.Text(string.Join(", ", prisonCell.Players));
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-prison-cell-empty"));
                    ImGui.PopStyleColor();
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
