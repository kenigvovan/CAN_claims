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
                Lang.Get("claims:gui-admin-cities-title"),
                Lang.Get("claims:gui-admin-cities-subtitle")
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
            Label(Lang.Get("claims:gui-admin-new-city"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-60);
            ImGui.InputText("##createcity", ref _createCityInput, 64);
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-admin-create")) && _createCityInput.Length > 0)
            {
                Send("/cadmin city new " + _createCityInput);
                _createCityInput = "";
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-create-tooltip"));

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Refresh ---
            if (ImGui.Button(Lang.Get("claims:gui-admin-refresh") + "##citylist"))
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-refresh-cities-tooltip"));

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Filter + list ---
            Label(Lang.Get("claims:gui-admin-filter"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##adminfilter", ref _filter, 128);
            ImGui.Spacing();

            var cities = claims.clientDataStorage?.clientPlayerInfo?.AllCitiesList;
            if (cities == null)
            {
                Hint(Lang.Get("claims:gui-admin-no-city-data"));
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
                if (selected) ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
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
                Hint(Lang.Get("claims:gui-admin-no-cities-match"));

            ImGui.EndChild();
        }

        private void DrawCityActions(float width)
        {
            ImGui.BeginChild("AdminCityActions", new Vector2(width, 0), true);

            if (_selectedCityName == null)
            {
                ImGui.Spacing();
                ImGui.Spacing();
                Hint(Lang.Get("claims:gui-admin-select-city-hint"));
                ImGui.EndChild();
                return;
            }

            // City name header
            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text(_selectedCityName);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            GuiSys.AdminCityFlags.TryGetValue(_selectedCityName, out AdminCityFlagsItem flags);
            if (flags != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
                string stats = Lang.Get("claims:gui-admin-city-stats", flags.CitizenCount, flags.PlotCount);
                if (flags.HasBalance) stats += Lang.Get("claims:gui-admin-city-stats-balance", flags.Balance);
                ImGui.Text(stats);
                ImGui.PopStyleColor();
            }
            ImGui.Separator();

            // --- Rename ---
            SectionTitle(Lang.Get("claims:gui-admin-rename-section"));
            Label(Lang.Get("claims:gui-admin-new-name"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(160);
            ImGui.InputText("##rename", ref _renameInput, 128);
            ImGui.SameLine();
            bool canRename = _renameInput.Length > 0;
            if (!canRename) ImGui.BeginDisabled();
            if (ImGui.Button(Lang.Get("claims:gui-admin-rename") + "##city") && canRename)
            {
                Send("/cadmin city set name " + _selectedCityName + " " + _renameInput);
                _selectedCityName = _renameInput;
                _renameInput = "";
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
            }
            if (!canRename) ImGui.EndDisabled();

            ImGui.Separator();

            // --- Player operations ---
            SectionTitle(Lang.Get("claims:gui-admin-player-ops"));
            Hint(Lang.Get("claims:gui-admin-player-ops-hint"));
            ImGui.Spacing();
            Label(Lang.Get("claims:gui-admin-player"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(180);
            ImGui.InputText("##adminplayer", ref _playerInput, 128);
            ImGui.Spacing();

            bool hasPlayer = _playerInput.Length > 0;
            if (!hasPlayer) ImGui.BeginDisabled();

            if (ImGui.Button(Lang.Get("claims:gui-admin-set-mayor")))
                Send("/cadmin city set mayor " + _selectedCityName + " " + _playerInput);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-set-mayor-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-add-to-city")))
                Send("/cadmin city add " + _selectedCityName + " " + _playerInput);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-add-to-city-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-kick-from-city")))
                Send("/cadmin city kick " + _selectedCityName + " " + _playerInput);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-kick-from-city-tooltip"));

            if (!hasPlayer) ImGui.EndDisabled();

            ImGui.Separator();

            // --- Flags ---
            SectionTitle(Lang.Get("claims:gui-admin-city-flags"));
            Hint(Lang.Get("claims:gui-admin-city-flags-hint"));
            ImGui.Spacing();

            if (flags == null)
            {
                Hint(Lang.Get("claims:gui-admin-flag-loading"));
            }
            else
            {
                DrawFlagCheckbox(Lang.Get("claims:gui-admin-flag-open"),      "open",      flags.Open,      v => flags.Open      = v, Lang.Get("claims:gui-admin-flag-open-tooltip"));
                ImGui.SameLine(80);
                DrawFlagCheckbox(Lang.Get("claims:gui-admin-flag-pvp"),       "pvp",       flags.Pvp,       v => flags.Pvp       = v, Lang.Get("claims:gui-admin-flag-pvp-tooltip"));
                ImGui.SameLine(160);
                DrawFlagCheckbox(Lang.Get("claims:gui-admin-flag-fire"),      "fire",      flags.Fire,      v => flags.Fire      = v, Lang.Get("claims:gui-admin-flag-fire-tooltip"));
                ImGui.SameLine(240);
                DrawFlagCheckbox(Lang.Get("claims:gui-admin-flag-blast"),     "blast",     flags.Blast,     v => flags.Blast     = v, Lang.Get("claims:gui-admin-flag-blast-tooltip"));
                ImGui.SameLine(320);
                DrawFlagCheckbox(Lang.Get("claims:gui-admin-flag-technical"), "technical", flags.Technical, v => flags.Technical = v, Lang.Get("claims:gui-admin-flag-technical-tooltip"));
            }

            ImGui.Separator();

            // --- Numbers ---
            SectionTitle(Lang.Get("claims:gui-admin-capacity-economy"));
            Label(Lang.Get("claims:gui-admin-bonus-claims"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##bonusclaims", ref _bonusClaimsInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-bonus-claims-tooltip"));
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-admin-apply") + "##bonusclaims"))
                Send("/cadmin city set bonusclaims " + _selectedCityName + " " + _bonusClaimsInput);

            ImGui.Spacing();

            Label(Lang.Get("claims:gui-admin-join-fee"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##cityfee", ref _cityFeeInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-join-fee-tooltip"));
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-admin-apply") + "##cityfee"))
                Send("/cadmin city set fee " + _selectedCityName + " " + _cityFeeInput);

            ImGui.Separator();

            // --- Plot claiming ---
            SectionTitle(Lang.Get("claims:gui-admin-plot-claiming"));
            Hint(Lang.Get("claims:gui-admin-plot-claiming-hint"));
            ImGui.Spacing();

            if (ImGui.Button(Lang.Get("claims:gui-admin-claim") + "##city"))
                Send("/cadmin city claim " + _selectedCityName);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-claim-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-unclaim") + "##city"))
                Send("/cadmin city unclaim " + _selectedCityName);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-unclaim-tooltip"));

            ImGui.Spacing();
            Label(Lang.Get("claims:gui-admin-radius"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60);
            ImGui.InputInt("##radius", ref _radiusInput, 0, 0);
            ImGui.SameLine();
            bool canClaim = _radiusInput > 0;
            if (!canClaim) ImGui.BeginDisabled();
            if (ImGui.Button(Lang.Get("claims:gui-admin-radius-claim") + "##city") && canClaim)
                Send("/cadmin city radiusclaim " + _selectedCityName + " " + _radiusInput);
            if (!canClaim) ImGui.EndDisabled();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-radius-claim-tooltip"));

            ImGui.Separator();

            // --- Delete ---
            SectionTitle(Lang.Get("claims:gui-admin-danger-zone"));
            if (!_confirmDelete)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        ColRedBtn);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedBtnH);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ColRedBtnA);
                if (ImGui.Button(Lang.Get("claims:gui-admin-delete-city"))) _confirmDelete = true;
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-delete-city-tooltip"));
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                ImGui.TextWrapped(Lang.Get("claims:gui-admin-delete-city-warn"));
                ImGui.PopStyleColor();
                ImGui.Spacing();

                ImGui.PushStyleColor(ImGuiCol.Button,        ColRedBtn);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedBtnH);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ColRedBtnA);
                if (ImGui.Button(Lang.Get("claims:gui-admin-confirm-delete")))
                {
                    Send("/cadmin city delete " + _selectedCityName);
                    _selectedCityName = null;
                    _confirmDelete    = false;
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                if (ImGui.Button(Lang.Get("claims:gui-admin-cancel"))) _confirmDelete = false;
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
