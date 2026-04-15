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
            CityRankCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks.FirstOrDefault(rc => rc.Name.Equals(capi.ModLoader.GetModSystem<claimsGui>().textInput), null);
            if (cell == null)
            {
                return;
            }

            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 permColor = new Vector4(0.7f, 0.9f, 0.7f, 1.0f);

            var gui = capi.ModLoader.GetModSystem<claimsGui>();

            // --- Rank name header ---
            ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
            ImGui.SetWindowFontScale(1.3f);
            string text = cell.Name;
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            // --- Members ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-rank-members", cell.Citizens.Count));
            ImGui.PopStyleColor();
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
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text(Lang.Get("claims:gui-rankinfo-add-perms"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Spacing();

            EnumPlayerPermissions[] availableToAdd = claims.config.AVAILABLE_CITY_PERMISSIONS.Where(v => !cell.Permissions.Contains(v)).ToArray();
            var availableToAddStrings = availableToAdd.Select(s => s.ToString()).ToArray();
            gui.multiSelectItems = availableToAddStrings;
            if (gui.selectedItems.Count() != availableToAddStrings.Count())
            {
                gui.selectedItems = new bool[availableToAddStrings.Count()];
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
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
            if (ImGui.Button(Lang.Get("claims:gui-rankinfo-add-button")))
            {
                ClientEventManager clientEventManager = (capi.World as ClientMain).eventManager;
                List<string> fullList = new List<string>();
                for (int i = 0; i < gui.multiSelectItems.Length; i++)
                {
                    if (gui.selectedItems[i])
                    {
                        fullList.Add(gui.multiSelectItems[i]);
                    }
                }
                string allPerms = string.Join(' ', fullList);
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                    string.Format("/c rank addperm {0} {1}", gui.textInput, allPerms), EnumChatType.Macro, "");

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

                for (var i = 0; i < gui.selectedItems.Count(); i++)
                {
                    gui.selectedItems[i] = false;
                }
            }
            ImGui.PopStyleColor(3);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Remove permissions section ---
            if (canRemovePerm)
            {
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
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
            if (gui.selectedItems2.Count() != availableToRemoveStrings.Count())
            {
                gui.selectedItems2 = new bool[availableToRemoveStrings.Count()];
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
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
            if (ImGui.Button(Lang.Get("claims:gui-rankinfo-remove-button")))
            {
                ClientEventManager clientEventManager = (capi.World as ClientMain).eventManager;
                List<string> fullList = new List<string>();
                for (int i = 0; i < gui.multiSelectItems2.Length; i++)
                {
                    if (gui.selectedItems2[i])
                    {
                        fullList.Add(gui.multiSelectItems2[i]);
                    }
                }
                string allPerms = string.Join(' ', fullList);
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                    string.Format("/c rank removeperm {0} {1}", gui.textInput, allPerms), EnumChatType.Macro, "");

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

                for (var i = 0; i < gui.selectedItems2.Count(); i++)
                {
                    gui.selectedItems2[i] = false;
                }
            }
            ImGui.PopStyleColor(3);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Current permissions list ---
            ImGui.PushStyleColor(ImGuiCol.Text, sectionColor);
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text(Lang.Get("claims:gui-rankinfo-current-perms"));
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            ImGui.Spacing();

            ImGui.BeginChild("PermsScroll", new Vector2(0, 0), false);
            foreach (var permission in cell.Permissions.Select(v => v.ToString()).ToList())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, permColor);
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
