using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANAdminWarTab : CANGuiTab
    {
        private static readonly Vector4 WarBtn  = new Vector4(0.60f, 0.22f, 0.05f, 1.0f);
        private static readonly Vector4 WarBtnH = new Vector4(0.80f, 0.32f, 0.08f, 1.0f);
        private static readonly Vector4 WarBtnA = new Vector4(0.42f, 0.14f, 0.02f, 1.0f);

        private string _firstParty      = "";
        private string _secondParty     = "";
        private int    _minutesUntilStart = 2;
        private int    _battleDuration   = 30;

        public CANAdminWarTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            AdminHeader(
                "[ADMIN] War & Conflict",
                "Force-start wars and override scheduled battle dates."
            );

            DrawPartiesSection();

            ImGui.Spacing();
            ImGui.Separator();

            DrawBattleDateSection();

            ImGui.Spacing();
            ImGui.Separator();

            DrawActiveConflicts();

            ImGui.Spacing();
            ImGui.Separator();

            DrawUtility();
        }

        private void DrawPartiesSection()
        {
            SectionTitle("Parties");
            Hint("Enter the exact name of a city or alliance for each side.\nNames are case-sensitive.");
            ImGui.Spacing();

            Label("First party: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(220);
            ImGui.InputText("##firstparty", ref _firstParty, 128);

            Label("Second party:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(220);
            ImGui.InputText("##secondparty", ref _secondParty, 128);

            ImGui.Spacing();

            bool valid = _firstParty.Length > 0 && _secondParty.Length > 0;
            if (!valid) ImGui.BeginDisabled();

            ImGui.PushStyleColor(ImGuiCol.Button,        WarBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, WarBtnH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  WarBtnA);
            if (ImGui.Button("  Force Start War  ") && valid)
                Send("/cadmin startwar " + _firstParty + " " + _secondParty);
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Immediately creates a war declaration between the two parties,\nskipping the proposal phase.");

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button,        ColRedBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedBtnH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ColRedBtnA);
            if (ImGui.Button("  Force End War  ") && valid)
                Send("/cadmin endwar " + _firstParty + " " + _secondParty);
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Immediately ends the conflict between the two parties with a forced peace.");

            if (!valid) ImGui.EndDisabled();
        }

        private void DrawBattleDateSection()
        {
            SectionTitle("Override Battle Date");
            Hint("Reschedules the next battle for an existing conflict between the two parties above.");
            ImGui.Spacing();

            Label("Start in (min): ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##minstart", ref _minutesUntilStart, 0, 0);
            if (_minutesUntilStart < 1) _minutesUntilStart = 1;

            Label("Duration (min): ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##battledur", ref _battleDuration, 0, 0);
            if (_battleDuration < 1) _battleDuration = 1;

            ImGui.Spacing();

            bool valid = _firstParty.Length > 0 && _secondParty.Length > 0;
            if (!valid) ImGui.BeginDisabled();
            if (ImGui.Button("Set Battle Date") && valid)
                Send("/cadmin setbattledate " + _firstParty + " " + _secondParty
                    + " " + _minutesUntilStart + " " + _battleDuration);
            if (!valid) ImGui.EndDisabled();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Overrides the scheduled battle time for the conflict between the two parties.");
        }

        private void DrawActiveConflicts()
        {
            SectionTitle("Your City's Active Conflicts");

            var cityInfo = claims.clientDataStorage?.clientPlayerInfo?.CityInfo;
            if (cityInfo?.ClientConflictCellElements == null || cityInfo.ClientConflictCellElements.Count == 0)
            {
                Hint("No active conflicts found for your city.");
                return;
            }

            Hint("Click 'Use' to auto-fill the party names above.");
            ImGui.Spacing();

            foreach (var conflict in cityInfo.ClientConflictCellElements)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.Text(conflict.FirstPartyName + "  vs  " + conflict.SecondPartyName);
                ImGui.PopStyleColor();
                ImGui.SameLine();
                if (ImGui.SmallButton("Use##" + conflict.Guid))
                {
                    _firstParty  = conflict.FirstPartyName;
                    _secondParty = conflict.SecondPartyName;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Copy these party names into the fields above.");
            }
        }

        private void DrawUtility()
        {
            SectionTitle("Utility");
            Hint("These commands execute immediately and affect the entire server.");
            ImGui.Spacing();

            if (ImGui.Button("Force Backup"))
                Send("/cadmin backup");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Immediately save a backup copy of the claims database.");
            ImGui.SameLine();

            if (ImGui.Button("Force NDay"))
                Send("/cadmin nday");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Trigger next-day events right now (daily taxes, etc.).");
            ImGui.SameLine();

            if (ImGui.Button("Force NHour"))
                Send("/cadmin nhour");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Trigger next-hour events right now.");
        }

        private void Send(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
