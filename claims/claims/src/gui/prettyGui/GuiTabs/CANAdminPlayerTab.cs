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
                "[ADMIN] Player & Plot",
                "Diagnose players and directly edit plots at your current position."
            );

            DrawPlayerDiag();

            ImGui.Spacing();
            ImGui.Separator();

            DrawPlotAtPosition();
        }

        private void DrawPlayerDiag()
        {
            SectionTitle("Player Diagnostic");
            Hint("Shows the player's city membership, permissions, mayor status and role in chat.");
            ImGui.Spacing();

            Label("Player name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(180);
            ImGui.InputText("##diagplayer", ref _diagPlayer, 128);
            ImGui.SameLine();

            bool hasName = _diagPlayer.Length > 0;
            if (!hasName) ImGui.BeginDisabled();
            if (ImGui.Button("Diag##player") && hasName)
                Send("/cadmin diag " + _diagPlayer);
            if (!hasName) ImGui.EndDisabled();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Run diagnostic for this player. Results appear in chat.");
        }

        private void DrawPlotAtPosition()
        {
            SectionTitle("Plot at Your Position");
            Hint("Stand on a plot tile, then press Refresh to load its current state.");
            ImGui.Spacing();

            if (ImGui.Button("Refresh Plot Info"))
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST });
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Requests the server to send data for the plot you are standing on.");

            var plot = claims.clientDataStorage?.clientPlayerInfo?.CurrentPlotInfo;

            if (plot == null)
            {
                ImGui.Spacing();
                Hint("No plot data loaded — stand on a claimed plot and press Refresh.");
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
            ImGui.Text(plot.PlotName ?? "(unnamed)");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            Hint("in city: " + (plot.OwnerName?.Length > 0 ? plot.OwnerName : "(none)"));

            ImGui.Separator();
            ImGui.Spacing();

            // Flags
            Label("Flags:");
            ImGui.SameLine();
            DrawPlotFlag("PVP",   "pvp",   plot.PermsHandler.pvpFlag,   v => plot.PermsHandler.pvpFlag   = v, "Allow players to attack each other on this plot.");
            ImGui.SameLine();
            DrawPlotFlag("Fire",  "fire",  plot.PermsHandler.fireFlag,  v => plot.PermsHandler.fireFlag  = v, "Allow fire to spread on this plot.");
            ImGui.SameLine();
            DrawPlotFlag("Blast", "blast", plot.PermsHandler.blastFlag, v => plot.PermsHandler.blastFlag = v, "Allow explosives to be used on this plot.");

            ImGui.Spacing();

            // Type
            Label("Type:  ");
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
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Changes the functional type of this plot (affects special mechanics).");

            ImGui.Spacing();

            // Tax fee
            Label("Tax fee:       ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##plotfee", ref _plotFeeInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Custom tax fee charged to the plot owner each period. 0 = use city default.");
            ImGui.SameLine();
            if (ImGui.Button("Apply##plotfee"))
                Send("/cadmin plot fee " + _plotFeeInput);

            ImGui.Spacing();

            // For-sale price
            Label("For-sale price:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##plotfs", ref _plotFsInput, 0, 0);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Put this plot up for sale at the given price. 0 = not for sale.");
            ImGui.SameLine();
            if (ImGui.Button("Apply##plotfs"))
                Send("/cadmin plot fs " + _plotFsInput);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Permissions table (read-only, from already-loaded PermsHandler)
            Label("Permissions:");
            HelpMarker("use / build / attack for each group. + = allowed, - = denied.");
            ImGui.Spacing();
            var ph = plot.PermsHandler;
            DrawPermRow("Citizen", "citizen", ph.CitizenPerms);
            DrawPermRow("Stranger", "stranger", ph.StrangerPerms);
            DrawPermRow("Ally",    "ally",    ph.AlliancePerms);
            DrawPermRow("Friend",  "friend",  ph.ComradePerms);
        }

        private static readonly string[] PermLabels = { "use", "build", "attack" };

        private void DrawPermRow(string group, string groupCmd, bool[] perms)
        {
            Label(group + ":");
            ImGui.SameLine(80);
            for (int i = 0; i < perms.Length && i < PermLabels.Length; i++)
            {
                bool val = perms[i];
                if (ImGui.Checkbox(PermLabels[i] + "##perm_" + groupCmd + "_" + i, ref val))
                {
                    perms[i] = val;
                    Send("/cadmin plot set permissions " + groupCmd + " " + PermLabels[i] + (val ? " on" : " off"));
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
