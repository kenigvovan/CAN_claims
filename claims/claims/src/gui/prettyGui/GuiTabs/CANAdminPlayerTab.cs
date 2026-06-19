using claims.src;
using claims.src.network.packets;
using claims.src.part.structure;
using ImGuiNET;
using System;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANAdminPlayerTab : CANGuiTab
    {
        private static readonly string[] PlotTypeNames = Enum.GetNames(typeof(PlotType));

        private string _diagPlayer    = "";
        private int    _plotFeeInput  = 0;
        private int    _plotFsInput   = 0;
        private string _lastPlotName  = "\x01";

        public CANAdminPlayerTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            AdminHeader(
                Lang.Get("claims:gui-admin-player-plot-title"),
                Lang.Get("claims:gui-admin-player-plot-subtitle")
            );

            DrawPlayerDiag();

            ImGui.Spacing();
            ImGui.Separator();

            DrawPlotAtPosition();
        }

        private void DrawPlayerDiag()
        {
            SectionTitle(Lang.Get("claims:gui-admin-player-diag"));
            Hint(Lang.Get("claims:gui-admin-player-diag-hint"));
            ImGui.Spacing();

            Label(Lang.Get("claims:gui-admin-player-name"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(180);
            ImGui.InputText("##diagplayer", ref _diagPlayer, 128);
            ImGui.SameLine();

            bool hasName = _diagPlayer.Length > 0;
            if (!hasName) ImGui.BeginDisabled();
            if (ImGui.Button(Lang.Get("claims:gui-admin-diag") + "##player") && hasName)
                Send("/cadmin diag " + _diagPlayer);
            if (!hasName) ImGui.EndDisabled();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-diag-tooltip"));
        }

        private void DrawPlotAtPosition()
        {
            SectionTitle(Lang.Get("claims:gui-admin-plot-at-position"));
            Hint(Lang.Get("claims:gui-admin-plot-at-position-hint"));
            ImGui.Spacing();

            if (ImGui.Button(Lang.Get("claims:gui-admin-refresh-plot")))
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST });
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-refresh-plot-tooltip"));

            var plot = claims.clientDataStorage?.clientPlayerInfo?.CurrentPlotInfo;

            if (plot == null)
            {
                ImGui.Spacing();
                Hint(Lang.Get("claims:gui-admin-no-plot-data"));
                return;
            }

            // Sync fee/price inputs when a different plot is loaded
            if (plot.PlotName != _lastPlotName)
            {
                _lastPlotName = plot.PlotName;
                _plotFeeInput = (int)plot.CustomTax;
                _plotFsInput  = plot.Price >= 0 ? (int)plot.Price : 0;
            }

            ImGui.Spacing();

            // Plot identity
            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
            ImGui.Text(plot.PlotName ?? Lang.Get("claims:gui-admin-unnamed"));
            ImGui.PopStyleColor();
            ImGui.SameLine();
            Hint(Lang.Get("claims:gui-admin-in-city", plot.OwnerName?.Length > 0 ? plot.OwnerName : Lang.Get("claims:gui-admin-none")));

            ImGui.Separator();
            ImGui.Spacing();

            // Flags
            Label(Lang.Get("claims:gui-admin-flags"));
            ImGui.SameLine();
            DrawPlotFlag(Lang.Get("claims:gui-admin-flag-pvp"),   "pvp",   plot.PermsHandler.pvpFlag,   v => plot.PermsHandler.pvpFlag   = v, Lang.Get("claims:gui-admin-plot-pvp-tooltip"));
            ImGui.SameLine();
            DrawPlotFlag(Lang.Get("claims:gui-admin-flag-fire"),  "fire",  plot.PermsHandler.fireFlag,  v => plot.PermsHandler.fireFlag  = v, Lang.Get("claims:gui-admin-plot-fire-tooltip"));
            ImGui.SameLine();
            DrawPlotFlag(Lang.Get("claims:gui-admin-flag-blast"), "blast", plot.PermsHandler.blastFlag, v => plot.PermsHandler.blastFlag = v, Lang.Get("claims:gui-admin-plot-blast-tooltip"));

            ImGui.Spacing();

            // Type
            Label(Lang.Get("claims:gui-admin-type"));
            ImGui.SameLine();
            string currentTypeName = plot.PlotType.ToString().ToLowerInvariant();
            ImGui.SetNextItemWidth(160);
            if (ImGui.BeginCombo("##plottype", currentTypeName))
            {
                foreach (var name in PlotTypeNames)
                {
                    string lower = name.ToLowerInvariant();
                    bool sel = lower == currentTypeName;
                    if (ImGui.Selectable(lower, sel))
                    {
                        plot.PlotType = (PlotType)Enum.Parse(typeof(PlotType), name);
                        Send("/cadmin plot type " + lower);
                    }
                    if (sel) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-plot-type-tooltip"));

            ImGui.Spacing();

            // Tax fee
            Label(Lang.Get("claims:gui-admin-tax-fee"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##plotfee", ref _plotFeeInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-tax-fee-tooltip"));
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-admin-apply") + "##plotfee"))
                Send("/cadmin plot fee " + _plotFeeInput);

            ImGui.Spacing();

            // For-sale price
            Label(Lang.Get("claims:gui-admin-fs-price"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##plotfs", ref _plotFsInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-fs-price-tooltip"));
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("claims:gui-admin-apply") + "##plotfs"))
                Send("/cadmin plot fs " + _plotFsInput);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Permissions table (read-only, from already-loaded PermsHandler)
            Label(Lang.Get("claims:gui-admin-permissions"));
            HelpMarker(Lang.Get("claims:gui-admin-permissions-help"));
            ImGui.Spacing();
            var ph = plot.PermsHandler;
            DrawPermRow(Lang.Get("claims:gui-admin-group-citizen"),  "citizen",  ph.CitizenPerms);
            DrawPermRow(Lang.Get("claims:gui-admin-group-stranger"), "stranger", ph.StrangerPerms);
            DrawPermRow(Lang.Get("claims:gui-admin-group-ally"),     "ally",     ph.AlliancePerms);
            DrawPermRow(Lang.Get("claims:gui-admin-group-friend"),   "friend",   ph.ComradePerms);
        }

        // Command tokens (stay English — used verbatim in /cadmin commands).
        private static readonly string[] PermTokens = { "use", "build", "attack" };
        private static readonly string[] PermLangKeys = { "claims:gui-admin-perm-use", "claims:gui-admin-perm-build", "claims:gui-admin-perm-attack" };

        private void DrawPermRow(string group, string groupCmd, bool[] perms)
        {
            Label(group + ":");
            ImGui.SameLine(80);
            for (int i = 0; i < perms.Length && i < PermTokens.Length; i++)
            {
                bool val = perms[i];
                if (ImGui.Checkbox(Lang.Get(PermLangKeys[i]) + "##perm_" + groupCmd + "_" + i, ref val))
                {
                    perms[i] = val;
                    Send("/cadmin plot set permissions " + groupCmd + " " + PermTokens[i] + (val ? " on" : " off"));
                }
                if (i < perms.Length - 1) ImGui.SameLine();
            }
        }

        private void DrawPlotFlag(string label, string cmd, bool current, Action<bool> onChanged, string tooltip = null)
        {
            bool val = current;
            if (ImGui.Checkbox(label + "##pf_" + cmd, ref val))
            {
                onChanged(val);
                Send("/cadmin plot set " + cmd + (val ? " on" : " off"));
            }
            if (tooltip != null && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void Send(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
