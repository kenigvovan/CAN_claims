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
                Lang.Get("claims:gui-admin-war-title"),
                Lang.Get("claims:gui-admin-war-subtitle")
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
            SectionTitle(Lang.Get("claims:gui-admin-parties"));
            Hint(Lang.Get("claims:gui-admin-parties-hint"));
            ImGui.Spacing();

            Label(Lang.Get("claims:gui-admin-first-party"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(220);
            ImGui.InputText("##firstparty", ref _firstParty, 128);

            Label(Lang.Get("claims:gui-admin-second-party"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(220);
            ImGui.InputText("##secondparty", ref _secondParty, 128);

            ImGui.Spacing();

            bool valid = _firstParty.Length > 0 && _secondParty.Length > 0;
            if (!valid) ImGui.BeginDisabled();

            ImGui.PushStyleColor(ImGuiCol.Button,        WarBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, WarBtnH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  WarBtnA);
            if (ImGui.Button(Lang.Get("claims:gui-admin-force-start-war")) && valid)
                Send("/cadmin startwar " + _firstParty + " " + _secondParty);
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-force-start-war-tooltip"));

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button,        ColRedBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedBtnH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ColRedBtnA);
            if (ImGui.Button(Lang.Get("claims:gui-admin-force-end-war")) && valid)
                Send("/cadmin endwar " + _firstParty + " " + _secondParty);
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-force-end-war-tooltip"));

            if (!valid) ImGui.EndDisabled();
        }

        private void DrawBattleDateSection()
        {
            SectionTitle(Lang.Get("claims:gui-admin-override-battle"));
            Hint(Lang.Get("claims:gui-admin-override-battle-hint"));
            ImGui.Spacing();

            Label(Lang.Get("claims:gui-admin-start-in-min"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##minstart", ref _minutesUntilStart, 0, 0);
            if (_minutesUntilStart < 1) _minutesUntilStart = 1;

            Label(Lang.Get("claims:gui-admin-duration-min"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            ImGui.InputInt("##battledur", ref _battleDuration, 0, 0);
            if (_battleDuration < 1) _battleDuration = 1;

            ImGui.Spacing();

            bool valid = _firstParty.Length > 0 && _secondParty.Length > 0;
            if (!valid) ImGui.BeginDisabled();
            if (ImGui.Button(Lang.Get("claims:gui-admin-set-battle-date")) && valid)
                Send("/cadmin setbattledate " + _firstParty + " " + _secondParty
                    + " " + _minutesUntilStart + " " + _battleDuration);
            if (!valid) ImGui.EndDisabled();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Lang.Get("claims:gui-admin-set-battle-date-tooltip"));
        }

        private void DrawActiveConflicts()
        {
            SectionTitle(Lang.Get("claims:gui-admin-active-conflicts"));

            var cityInfo = claims.clientDataStorage?.clientPlayerInfo?.CityInfo;
            if (cityInfo?.ClientConflictCellElements == null || cityInfo.ClientConflictCellElements.Count == 0)
            {
                Hint(Lang.Get("claims:gui-admin-no-active-conflicts"));
                return;
            }

            Hint(Lang.Get("claims:gui-admin-use-hint"));
            ImGui.Spacing();

            foreach (var conflict in cityInfo.ClientConflictCellElements)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                ImGui.Text(conflict.FirstPartyName + " " + Lang.Get("claims:gui-admin-vs") + " " + conflict.SecondPartyName);
                ImGui.PopStyleColor();
                ImGui.SameLine();
                if (ImGui.SmallButton(Lang.Get("claims:gui-admin-use") + "##" + conflict.Guid))
                {
                    _firstParty  = conflict.FirstPartyName;
                    _secondParty = conflict.SecondPartyName;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Lang.Get("claims:gui-admin-use-tooltip"));
            }
        }

        private void DrawUtility()
        {
            SectionTitle(Lang.Get("claims:gui-admin-utility"));
            Hint(Lang.Get("claims:gui-admin-utility-hint"));
            ImGui.Spacing();

            if (ImGui.Button(Lang.Get("claims:gui-admin-force-backup")))
                Send("/cadmin backup");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-force-backup-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-force-nday")))
                Send("/cadmin nday");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-force-nday-tooltip"));
            ImGui.SameLine();

            if (ImGui.Button(Lang.Get("claims:gui-admin-force-nhour")))
                Send("/cadmin nhour");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-admin-force-nhour-tooltip"));
        }

        private void Send(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
