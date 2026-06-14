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
    public class CANAdminWorldTab : CANGuiTab
    {
        public CANAdminWorldTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            AdminHeader(
                Lang.Get("claims:gui-admin-world-title"),
                Lang.Get("claims:gui-admin-world-subtitle")
            );

            var w = GuiSys.AdminWorldState;

            if (w == null)
            {
                Hint(Lang.Get("claims:gui-admin-world-loading"));
                return;
            }

            // PVP
            DrawFlagSection(
                Lang.Get("claims:gui-admin-pvp"),
                Lang.Get("claims:gui-admin-everywhere") + "##pvp", w.PvpEverywhere,
                Lang.Get("claims:gui-admin-pvp-ew-tooltip"),
                v => { w.PvpEverywhere = v; Send("pvpew", v); },
                Lang.Get("claims:gui-admin-forbidden") + "##pvp", w.PvpForbidden,
                Lang.Get("claims:gui-admin-pvp-fb-tooltip"),
                v => { w.PvpForbidden = v; Send("pvpfb", v); }
            );

            ImGui.Spacing();

            // Fire
            DrawFlagSection(
                Lang.Get("claims:gui-admin-fire-spread"),
                Lang.Get("claims:gui-admin-everywhere") + "##fire", w.FireEverywhere,
                Lang.Get("claims:gui-admin-fire-ew-tooltip"),
                v => { w.FireEverywhere = v; Send("fireew", v); },
                Lang.Get("claims:gui-admin-forbidden") + "##fire", w.FireForbidden,
                Lang.Get("claims:gui-admin-fire-fb-tooltip"),
                v => { w.FireForbidden = v; Send("firefb", v); }
            );

            ImGui.Spacing();

            // Blast
            DrawFlagSection(
                Lang.Get("claims:gui-admin-explosions"),
                Lang.Get("claims:gui-admin-everywhere") + "##blast", w.BlastEverywhere,
                Lang.Get("claims:gui-admin-blast-ew-tooltip"),
                v => { w.BlastEverywhere = v; Send("blastew", v); },
                Lang.Get("claims:gui-admin-forbidden") + "##blast", w.BlastForbidden,
                Lang.Get("claims:gui-admin-blast-fb-tooltip"),
                v => { w.BlastForbidden = v; Send("blastfb", v); }
            );

            ImGui.Spacing();
            ImGui.Separator();

            SectionTitle(Lang.Get("claims:gui-admin-diagnostics"));
            Hint(Lang.Get("claims:gui-admin-diagnostics-hint"));
            ImGui.Spacing();

            if (ImGui.Button(Lang.Get("claims:gui-admin-force-nday")))
            {
                SendCmd("/cadmin nday");
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-nday-world-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-force-nhour")))
            {
                SendCmd("/cadmin nhour");
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-nhour-world-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-force-backup")))
            {
                SendCmd("/cadmin backup");
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-backup-world-tooltip"));
        }

        private void DrawFlagSection(
            string sectionName,
            string labelA, bool valA, string tipA, System.Action<bool> onA,
            string labelB, bool valB, string tipB, System.Action<bool> onB)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(sectionName);
            ImGui.PopStyleColor();
            ImGui.Spacing();

            // Everywhere — disabled when Forbidden is active
            if (valB) ImGui.BeginDisabled();
            bool a = valA;
            if (ImGui.Checkbox(labelA, ref a)) onA(a);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(valB ? Lang.Get("claims:gui-admin-disabled-forbidden") : tipA);
            if (valB) ImGui.EndDisabled();

            ImGui.SameLine(200);

            // Forbidden — disabled when Everywhere is active
            if (valA) ImGui.BeginDisabled();
            bool b = valB;
            if (ImGui.Checkbox(labelB, ref b)) onB(b);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(valA ? Lang.Get("claims:gui-admin-disabled-everywhere") : tipB);
            if (valA) ImGui.EndDisabled();
        }

        private void Send(string key, bool on) =>
            SendCmd("/cadmin world set " + key + (on ? " on" : " off"));

        private void SendCmd(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
