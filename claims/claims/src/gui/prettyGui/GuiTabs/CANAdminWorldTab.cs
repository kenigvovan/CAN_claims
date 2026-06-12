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
                "[ADMIN] World Settings",
                "Global rules that override all city and plot-level settings."
            );

            var w = GuiSys.AdminWorldState;

            if (w == null)
            {
                Hint("Loading world flags from server...");
                return;
            }

            // PVP
            DrawFlagSection(
                "PVP",
                "Everywhere##pvp", w.PvpEverywhere,
                "Forces PVP everywhere on the server.\nPlayers can attack each other in any area,\nregardless of plot or city settings.",
                v => { w.PvpEverywhere = v; Send("pvpew", v); },
                "Forbidden##pvp", w.PvpForbidden,
                "Blocks PVP everywhere on the server.\nOverrides any plot or city that has PVP enabled.",
                v => { w.PvpForbidden = v; Send("pvpfb", v); }
            );

            ImGui.Spacing();

            // Fire
            DrawFlagSection(
                "Fire Spread",
                "Everywhere##fire", w.FireEverywhere,
                "Fire spreads in all areas, including\nplots and cities where it is normally disabled.",
                v => { w.FireEverywhere = v; Send("fireew", v); },
                "Forbidden##fire", w.FireForbidden,
                "Prevents fire from spreading anywhere.\nOverrides city and plot fire settings.",
                v => { w.FireForbidden = v; Send("firefb", v); }
            );

            ImGui.Spacing();

            // Blast
            DrawFlagSection(
                "Explosions",
                "Everywhere##blast", w.BlastEverywhere,
                "Bombs and explosives can be used anywhere,\nincluding plots that normally forbid them.",
                v => { w.BlastEverywhere = v; Send("blastew", v); },
                "Forbidden##blast", w.BlastForbidden,
                "Prevents all bomb use on the entire server.\nNo explosive can be ignited regardless of plot settings.",
                v => { w.BlastForbidden = v; Send("blastfb", v); }
            );

            ImGui.Spacing();
            ImGui.Separator();

            SectionTitle("Diagnostics / Utility");
            Hint("These trigger server events immediately. Useful for testing or emergency resets.");
            ImGui.Spacing();

            if (ImGui.Button("Force NDay"))
            {
                SendCmd("/cadmin nday");
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Advance to the next in-game day immediately.");
            ImGui.SameLine();

            if (ImGui.Button("Force NHour"))
            {
                SendCmd("/cadmin nhour");
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Advance to the next in-game hour immediately.");
            ImGui.SameLine();

            if (ImGui.Button("Force Backup"))
            {
                SendCmd("/cadmin backup");
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Immediately write a backup of the claims database.");
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
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(valB ? "Disabled while Forbidden is active." : tipA);
            if (valB) ImGui.EndDisabled();

            ImGui.SameLine(200);

            // Forbidden — disabled when Everywhere is active
            if (valA) ImGui.BeginDisabled();
            bool b = valB;
            if (ImGui.Checkbox(labelB, ref b)) onB(b);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(valA ? "Disabled while Everywhere is active." : tipB);
            if (valA) ImGui.EndDisabled();
        }

        private void Send(string key, bool on) =>
            SendCmd("/cadmin world set " + key + (on ? " on" : " off"));

        private void SendCmd(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
