using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.rights;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANRanksInfoTab : CANGuiTab
    {
        EnumPlayerPermissions[] availableToAdd;
        bool[] availableToAddSelected;

        // Granted-permissions list uses a soft green distinct from the parchment theme.
        static readonly Vector4 PermColor = new Vector4(0.7f, 0.9f, 0.7f, 1.0f);
        public CANRanksInfoTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
            {
                return;
            }
            CityRankCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks.FirstOrDefault(rc => rc.Name.Equals(GuiSys.textInput), null);
            if (cell == null)
            {
                return;
            }

            var gui = GuiSys;

            // --- Rank name header ---
            CenteredTitle(cell.Name, ColValue);

            ImGui.Separator();
            ImGui.Spacing();

            if (BackButton())
                GuiSys.selectedTab = EnumSelectedTab.RANKS;

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Members ---
            Label(Lang.Get("claims:gui-rank-members", cell.Citizens.Count));
            if (ImGui.IsItemHovered() && cell.Citizens.Count > 0)
            {
                ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(cell.Citizens, ','));
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Add permissions section ---
            bool canAddPerm = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK);
            bool canRemovePerm = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_PERMISSION_FROM_RANK);

            if (canAddPerm)
            {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text(Lang.Get("claims:gui-rankinfo-add-perms"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Spacing();

            EnumPlayerPermissions[] availableToAdd = claims.config.AVAILABLE_CITY_PERMISSIONS == null
                                    ? new EnumPlayerPermissions[] { }
                                    : claims.config.AVAILABLE_CITY_PERMISSIONS.Where(v => !cell.Permissions.Contains(v)).ToArray();
            var availableToAddStrings = availableToAdd.Select(s => s.ToString()).ToArray();
            gui.multiSelectItems = availableToAddStrings;
            if (gui.selectedItems.Length != availableToAddStrings.Length)
            {
                gui.selectedItems = new bool[availableToAddStrings.Length];
            }

            if (ImGui.BeginCombo("##addperms", PreviewText(gui.multiSelectItems, gui.selectedItems)))
            {
                for (int i = 0; i < gui.multiSelectItems.Length; i++)
                {
                    ImGui.Checkbox(gui.multiSelectItems[i], ref gui.selectedItems[i]);
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (GreenButton(Lang.Get("claims:gui-rankinfo-add-button")))
            {
                List<string> fullList = new List<string>();
                for (int i = 0; i < gui.multiSelectItems.Length; i++)
                {
                    if (gui.selectedItems[i])
                    {
                        fullList.Add(gui.multiSelectItems[i]);
                    }
                }
                string allPerms = string.Join(' ', fullList);
                SendCommand(string.Format("/c rank addperm {0} {1}", gui.textInput, allPerms));

                // Optimistic local update
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK))
                {
                    for (int i = 0; i < availableToAdd.Length; i++)
                    {
                        if (gui.selectedItems[i])
                        {
                            cell.Permissions.Add(availableToAdd[i]);
                        }
                    }
                }

                for (var i = 0; i < gui.selectedItems.Length; i++)
                {
                    gui.selectedItems[i] = false;
                }
            }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Remove permissions section ---
            if (canRemovePerm)
            {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text(Lang.Get("claims:gui-rankinfo-remove-perms"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Spacing();

            EnumPlayerPermissions[] availableToRemove = claims.config.AVAILABLE_CITY_PERMISSIONS == null
                                            ? new EnumPlayerPermissions[] { }
                                            : claims.config.AVAILABLE_CITY_PERMISSIONS.Where(v => cell.Permissions.Contains(v)).ToArray();

            var availableToRemoveStrings = availableToRemove.Select(s => s.ToString()).ToArray();
            gui.multiSelectItems2 = availableToRemoveStrings;
            if (gui.selectedItems2.Length != availableToRemoveStrings.Length)
            {
                gui.selectedItems2 = new bool[availableToRemoveStrings.Length];
            }

            if (ImGui.BeginCombo("##removeperms", PreviewText(gui.multiSelectItems2, gui.selectedItems2)))
            {
                for (int i = 0; i < gui.multiSelectItems2.Length; i++)
                {
                    ImGui.Checkbox(gui.multiSelectItems2[i], ref gui.selectedItems2[i]);
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (RedButton(Lang.Get("claims:gui-rankinfo-remove-button")))
            {
                List<string> fullList = new List<string>();
                for (int i = 0; i < gui.multiSelectItems2.Length; i++)
                {
                    if (gui.selectedItems2[i])
                    {
                        fullList.Add(gui.multiSelectItems2[i]);
                    }
                }
                string allPerms = string.Join(' ', fullList);
                SendCommand(string.Format("/c rank removeperm {0} {1}", gui.textInput, allPerms));

                // Optimistic local update
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_PERMISSION_FROM_RANK))
                {
                    for (int i = 0; i < availableToRemove.Length; i++)
                    {
                        if (gui.selectedItems2[i])
                        {
                            cell.Permissions.Remove(availableToRemove[i]);
                        }
                    }
                }

                for (var i = 0; i < gui.selectedItems2.Length; i++)
                {
                    gui.selectedItems2[i] = false;
                }
            }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Current permissions list ---
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text(Lang.Get("claims:gui-rankinfo-current-perms"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Spacing();

            ImGui.BeginChild("PermsScroll", new Vector2(0, 0), false);
            foreach (var permission in cell.Permissions.Select(v => v.ToString()).ToList())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, PermColor);
                ImGui.Text(permission);
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();
        }

        private string PreviewText(string[] items, bool[] selected)
        {
            var list = new List<string>();
            for (int i = 0; i < items.Length; i++)
                if (selected[i]) list.Add(items[i]);

            return list.Count > 0 ? string.Join(", ", list) : Lang.Get("claims:gui-rankinfo-none-selected");
        }
    }
}
