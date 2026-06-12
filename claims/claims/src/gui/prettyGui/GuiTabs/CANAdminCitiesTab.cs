using claims.src;
using claims.src.network.packets;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANAdminCitiesTab : CANGuiTab
    {
        private static readonly Vector4 NameColor  = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
        private static readonly Vector4 WarnColor  = new Vector4(1.0f, 0.35f, 0.35f, 1.0f);

        private string _filter          = "";
        private string _selectedCityName = null;
        private string _renameInput     = "";
        private string _playerInput     = "";
        private int    _bonusClaimsInput = 0;
        private int    _cityFeeInput    = 0;
        private int    _radiusInput     = 5;
        private string _createCityInput = "";
        private bool   _confirmDelete   = false;

        public CANAdminCitiesTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            AdminHeader(
                "[ADMIN] City Management",
                "Create, configure and manage all cities on the server."
            );

            float windowWidth = ImGui.GetWindowSize().X;
            float listWidth   = windowWidth * 0.32f;
            float detailWidth = windowWidth - listWidth - 20f;

            DrawCityList(listWidth);
            ImGui.SameLine();
            DrawCityActions(detailWidth);
        }

        private void DrawCityList(float width)
        {
            ImGui.BeginChild("AdminCityList", new Vector2(width, 0), true);

            // --- Create city ---
            Label("New city:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-60);
            ImGui.InputText("##createcity", ref _createCityInput, 64);
            ImGui.SameLine();
            if (ImGui.Button("Create") && _createCityInput.Length > 0)
            {
                Send("/cadmin city new " + _createCityInput);
                _createCityInput = "";
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Create a new city at your current standing position.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Refresh ---
            if (ImGui.Button("Refresh##citylist"))
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Re-fetch all city flags and stats from the server.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Filter + list ---
            Label("Filter:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##adminfilter", ref _filter, 128);
            ImGui.Spacing();

            var cities = claims.clientDataStorage?.clientPlayerInfo?.AllCitiesList;
            if (cities == null)
            {
                Hint("No city data received yet.");
                ImGui.EndChild();
                return;
            }

            string filterLower = _filter.ToLowerInvariant();
            int shown = 0;
            int idx = 0;
            foreach (var city in cities)
            {
                if (_filter.Length > 0 && !city.Name.ToLowerInvariant().Contains(filterLower))
                {
                    idx++;
                    continue;
                }
                shown++;
                ImGui.PushID(idx);
                bool selected = _selectedCityName == city.Name;
                if (selected) ImGui.PushStyleColor(ImGuiCol.Text, NameColor);
                if (ImGui.Selectable(city.Name, selected))
                {
                    _selectedCityName = city.Name;
                    _renameInput      = "";
                    _playerInput      = "";
                    _bonusClaimsInput = 0;
                    _cityFeeInput     = 0;
                    _confirmDelete    = false;
                }
                if (selected) ImGui.PopStyleColor();
                ImGui.PopID();
                idx++;
            }

            if (shown == 0)
                Hint("No cities match the filter.");

            ImGui.EndChild();
        }

        private void DrawCityActions(float width)
        {
            ImGui.BeginChild("AdminCityActions", new Vector2(width, 0), true);

            if (_selectedCityName == null)
            {
                ImGui.Spacing();
                ImGui.Spacing();
                Hint("← Select a city from the list to see management options.");
                ImGui.EndChild();
                return;
            }

            // City name header
            ImGui.PushStyleColor(ImGuiCol.Text, NameColor);
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text(_selectedCityName);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            GuiSys.AdminCityFlags.TryGetValue(_selectedCityName, out AdminCityFlagsItem flags);
            if (flags != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
                string stats = flags.CitizenCount + " citizens  |  " + flags.PlotCount + " plots";
                if (flags.HasBalance) stats += "  |  balance: " + flags.Balance;
                ImGui.Text(stats);
                ImGui.PopStyleColor();
            }
            ImGui.Separator();

            // --- Rename ---
            SectionTitle("Rename");
            Label("New name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(160);
            ImGui.InputText("##rename", ref _renameInput, 128);
            ImGui.SameLine();
            bool canRename = _renameInput.Length > 0;
            if (!canRename) ImGui.BeginDisabled();
            if (ImGui.Button("Rename##city") && canRename)
            {
                Send("/cadmin city set name " + _selectedCityName + " " + _renameInput);
                _selectedCityName = _renameInput;
                _renameInput = "";
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
            }
            if (!canRename) ImGui.EndDisabled();

            ImGui.Separator();

            // --- Player operations ---
            SectionTitle("Player Operations");
            Hint("Enter the exact player name, then use one of the actions below.");
            ImGui.Spacing();
            Label("Player:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(180);
            ImGui.InputText("##adminplayer", ref _playerInput, 128);
            ImGui.Spacing();

            bool hasPlayer = _playerInput.Length > 0;
            if (!hasPlayer) ImGui.BeginDisabled();

            if (ImGui.Button("Set Mayor"))
                Send("/cadmin city set mayor " + _selectedCityName + " " + _playerInput);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Transfers mayoralty to the specified player.");
            ImGui.SameLine();

            if (ImGui.Button("Add to City"))
                Send("/cadmin city add " + _selectedCityName + " " + _playerInput);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Force-adds a player to this city without an invitation.");
            ImGui.SameLine();

            if (ImGui.Button("Kick from City"))
                Send("/cadmin city kick " + _selectedCityName + " " + _playerInput);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Removes the player from this city.");

            if (!hasPlayer) ImGui.EndDisabled();

            ImGui.Separator();

            // --- Flags ---
            SectionTitle("City Flags");
            Hint("Default flags applied to all plots in this city unless a plot overrides them.");
            ImGui.Spacing();

            if (flags == null)
            {
                Hint("Flag data loading...");
            }
            else
            {
                DrawFlagCheckbox("Open",      "open",      flags.Open,      v => flags.Open      = v, "Anyone can join the city without an invitation.");
                ImGui.SameLine(80);
                DrawFlagCheckbox("PVP",       "pvp",       flags.Pvp,       v => flags.Pvp       = v, "Players can attack each other inside this city.");
                ImGui.SameLine(160);
                DrawFlagCheckbox("Fire",      "fire",      flags.Fire,      v => flags.Fire      = v, "Fire can spread within this city.");
                ImGui.SameLine(240);
                DrawFlagCheckbox("Blast",     "blast",     flags.Blast,     v => flags.Blast     = v, "Explosives can be used within this city.");
                ImGui.SameLine(320);
                DrawFlagCheckbox("Technical", "technical", flags.Technical, v => flags.Technical = v, "Marks this as a technical/system city (hidden from regular players).");
            }

            ImGui.Separator();

            // --- Numbers ---
            SectionTitle("Capacity & Economy");
            Label("Bonus claims:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##bonusclaims", ref _bonusClaimsInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Extra plot claims beyond the city's base allowance.");
            ImGui.SameLine();
            if (ImGui.Button("Apply##bonusclaims"))
                Send("/cadmin city set bonusclaims " + _selectedCityName + " " + _bonusClaimsInput);

            ImGui.Spacing();

            Label("Join fee:   ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##cityfee", ref _cityFeeInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Cost a player pays to join this city (in server currency).");
            ImGui.SameLine();
            if (ImGui.Button("Apply##cityfee"))
                Send("/cadmin city set fee " + _selectedCityName + " " + _cityFeeInput);

            ImGui.Separator();

            // --- Plot claiming ---
            SectionTitle("Plot Claiming");
            Hint("These commands operate on the plot tile at your current standing position.");
            ImGui.Spacing();

            if (ImGui.Button("Claim##city"))
                Send("/cadmin city claim " + _selectedCityName);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Claim the plot under your feet for this city.");
            ImGui.SameLine();

            if (ImGui.Button("Unclaim##city"))
                Send("/cadmin city unclaim " + _selectedCityName);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Release the plot under your feet from this city.");

            ImGui.Spacing();
            Label("Radius:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60);
            ImGui.InputInt("##radius", ref _radiusInput, 0, 0);
            ImGui.SameLine();
            bool canClaim = _radiusInput > 0;
            if (!canClaim) ImGui.BeginDisabled();
            if (ImGui.Button("Radius Claim##city") && canClaim)
                Send("/cadmin city radiusclaim " + _selectedCityName + " " + _radiusInput);
            if (!canClaim) ImGui.EndDisabled();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Claim all plots within the given radius around your position.");

            ImGui.Separator();

            // --- Delete ---
            SectionTitle("Danger Zone");
            if (!_confirmDelete)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        ColRedBtn);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedBtnH);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ColRedBtnA);
                if (ImGui.Button("Delete City")) _confirmDelete = true;
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Permanently delete this city along with all plots, citizens and conflicts.\nThis action cannot be undone.");
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, WarnColor);
                ImGui.TextWrapped("This will permanently delete ALL plots, citizens and conflict records for this city!");
                ImGui.PopStyleColor();
                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Button,        ColRedBtn);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedBtnH);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ColRedBtnA);
                if (ImGui.Button("CONFIRM DELETE"))
                {
                    Send("/cadmin city delete " + _selectedCityName);
                    _selectedCityName = null;
                    _confirmDelete    = false;
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) _confirmDelete = false;
            }

            ImGui.EndChild();
        }

        private void DrawFlagCheckbox(string label, string cmdKey, bool currentVal, System.Action<bool> onChanged, string tooltip = null)
        {
            bool val = currentVal;
            if (ImGui.Checkbox(label + "##flag_" + cmdKey, ref val))
            {
                Send("/cadmin city set " + cmdKey + " " + _selectedCityName + " " + (val ? "on" : "off"));
                onChanged(val);
            }
            if (tooltip != null && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void Send(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
