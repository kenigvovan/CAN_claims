using ImGuiNET;
using System.Collections.Generic;
using System.Globalization;
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

        // Local mirror of the editable war config values. Loaded from claims.config on demand
        // (numeric inputs must not be re-read every frame or they'd fight the user's typing);
        // the "Reload" button re-syncs from the server-authoritative config.
        private readonly Dictionary<string, int> _cfgInts = new Dictionary<string, int>();
        private readonly Dictionary<string, double> _cfgDoubles = new Dictionary<string, double>();
        private bool _cfgLoaded;

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

            ImGui.Spacing();
            ImGui.Separator();

            DrawWarSettings();
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
                ImGui.Text(conflict.FirstPartyName + " " + Lang.Get("claims:gui-admin-vs") + " " + conflict.SecondPartyName
                    + "   [" + conflict.FirstScore + ":" + conflict.SecondScore + "]");
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

        private void DrawWarSettings()
        {
            if (!_cfgLoaded) LoadCfgMirror();

            SectionTitle(Lang.Get("claims:gui-admin-warcfg-title"));
            Hint(Lang.Get("claims:gui-admin-warcfg-hint"));
            if (ImGui.Button(Lang.Get("claims:gui-admin-warcfg-reload"))) LoadCfgMirror();
            ImGui.Spacing();

            CfgGroup(Lang.Get("claims:gui-admin-warcfg-flag"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-flag-interrupt"), "flag_defender_interrupt", claims.config.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED);
            IntCfg(Lang.Get("claims:gui-admin-warcfg-flag-radius"), "flag_defender_radius");
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-flag-regress"), "flag_regress_mult");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-flag-seconds"), "flag_capture_seconds");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-flag-maxactive"), "flag_max_active");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-flag-reinforce"), "flag_reinforcement");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-mindays"), "min_days_between_battles");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-score"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-score-enabled"), "score_enabled", claims.config.WAR_SCORE_ENABLED);
            IntCfg(Lang.Get("claims:gui-admin-warcfg-score-towin"), "score_to_win");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-score-capture"), "score_per_capture");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-score-kill"), "score_per_kill");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-score-hold"), "score_per_hold");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-score-holdsec"), "score_hold_seconds");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-pillage"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-pillage-enabled"), "pillage_enabled", claims.config.WAR_PILLAGE_ENABLED);
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-pillage-percent"), "pillage_percent");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-camp"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-camp-enabled"), "camp_enabled", claims.config.WAR_CAMP_ENABLED);
            IntCfg(Lang.Get("claims:gui-admin-warcfg-camp-max"), "camp_max_per_conflict");
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-camp-cost"), "camp_cost");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-camp-mindist"), "camp_min_distance");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-camp-breaks"), "camp_anchor_breaks");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-safezone"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-safezone-enabled"), "safezone_enabled", claims.config.WAR_RESPAWN_SAFEZONE_ENABLED);
            IntCfg(Lang.Get("claims:gui-admin-warcfg-safezone-radius"), "safezone_radius");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-safezone-seconds"), "safezone_seconds");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-declare"));
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-declare-cost"), "war_declaration_cost");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-declare-cooldown"), "war_redeclare_cooldown_days");
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-declare-cb"), "war_require_casus_belli", claims.config.WAR_REQUIRE_CASUS_BELLI);
            IntCfg(Lang.Get("claims:gui-admin-warcfg-declare-cb-days"), "war_casus_belli_grace_days");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-peace"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-peace-enabled"), "war_peace_terms_enabled", claims.config.WAR_PEACE_TERMS_ENABLED);
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-vassal-tribute"), "war_vassal_tribute");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-vassal-days"), "war_vassal_duration_days");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-nap"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-nap-enabled"), "war_nap_enabled", claims.config.WAR_NAP_ENABLED);
            IntCfg(Lang.Get("claims:gui-admin-warcfg-nap-default-days"), "war_nap_default_days");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-nap-max-days"), "war_nap_max_days");
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-nap-penalty"), "war_nap_break_penalty");
            IntCfg(Lang.Get("claims:gui-admin-warcfg-battle-warn"), "war_battle_warn_minutes");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-ultimatum"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-ultimatum-enabled"), "war_ultimatum_enabled", claims.config.WAR_ULTIMATUM_ENABLED);

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-bounty"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-bounty-enabled"), "war_bounty_enabled", claims.config.WAR_BOUNTY_ENABLED);
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-bounty-min"), "war_bounty_min");
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-plunder-enabled"), "war_plunder_on_kill_enabled", claims.config.WAR_PLUNDER_ON_KILL_ENABLED);
            DoubleCfg(Lang.Get("claims:gui-admin-warcfg-plunder-percent"), "war_plunder_on_kill_percent");

            ImGui.Spacing();
            CfgGroup(Lang.Get("claims:gui-admin-warcfg-report"));
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-report-enabled"), "war_report_enabled", claims.config.WAR_REPORT_ENABLED);
            BoolCfg(Lang.Get("claims:gui-admin-warcfg-hud-enabled"), "war_hud_enabled", claims.config.WAR_HUD_ENABLED);
        }

        private void CfgGroup(string title)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(title);
            ImGui.PopStyleColor();
        }

        private void LoadCfgMirror()
        {
            _cfgInts["flag_defender_radius"]     = claims.config.WAR_FLAG_DEFENDER_RADIUS;
            _cfgInts["flag_capture_seconds"]     = claims.config.FLAG_CAPTURE_DURATION_SECONDS;
            _cfgInts["flag_max_active"]          = claims.config.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE;
            _cfgInts["flag_reinforcement"]       = claims.config.FLAG_REINFORCEMENT_AMOUNT;
            _cfgInts["min_days_between_battles"] = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
            _cfgInts["score_to_win"]             = claims.config.WAR_SCORE_TO_WIN;
            _cfgInts["score_per_capture"]        = claims.config.WAR_SCORE_PER_PLOT_CAPTURE;
            _cfgInts["score_per_kill"]           = claims.config.WAR_SCORE_PER_KILL;
            _cfgInts["score_per_hold"]           = claims.config.WAR_SCORE_PER_HOLD_TICK;
            _cfgInts["score_hold_seconds"]       = claims.config.WAR_SCORE_HOLD_TICK_SECONDS;
            _cfgInts["camp_max_per_conflict"]    = claims.config.WAR_MAX_CAMPS_PER_CONFLICT;
            _cfgInts["camp_min_distance"]        = claims.config.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY;
            _cfgInts["camp_anchor_breaks"]       = claims.config.WAR_CAMP_ANCHOR_BREAKS;
            _cfgInts["safezone_radius"]          = claims.config.WAR_RESPAWN_SAFEZONE_RADIUS;
            _cfgInts["safezone_seconds"]         = claims.config.WAR_RESPAWN_SAFEZONE_SECONDS;
            _cfgInts["siege_tick_seconds"]       = claims.config.WAR_SIEGE_RAM_TICK_SECONDS;
            _cfgInts["siege_range"]              = claims.config.WAR_SIEGE_RAM_RANGE;

            _cfgInts["war_redeclare_cooldown_days"] = claims.config.WAR_REDECLARE_COOLDOWN_DAYS;
            _cfgInts["war_casus_belli_grace_days"]  = claims.config.WAR_CASUS_BELLI_GRACE_DAYS;
            _cfgInts["war_vassal_duration_days"]    = claims.config.WAR_VASSAL_DURATION_DAYS;
            _cfgInts["war_nap_default_days"]        = claims.config.WAR_NAP_DEFAULT_DAYS;
            _cfgInts["war_nap_max_days"]            = claims.config.WAR_NAP_MAX_DAYS;
            _cfgInts["war_battle_warn_minutes"]     = claims.config.WAR_BATTLE_WARN_MINUTES;

            _cfgDoubles["flag_regress_mult"] = claims.config.WAR_FLAG_REGRESS_MULTIPLIER;
            _cfgDoubles["pillage_percent"]   = claims.config.WAR_PILLAGE_PERCENT;
            _cfgDoubles["camp_cost"]         = claims.config.CAMP_PLOT_COST;
            _cfgDoubles["siege_cost"]        = claims.config.WAR_SIEGE_RAM_COST;
            _cfgDoubles["war_declaration_cost"]        = claims.config.WAR_DECLARATION_COST;
            _cfgDoubles["war_vassal_tribute"]          = claims.config.WAR_VASSAL_TRIBUTE;
            _cfgDoubles["war_bounty_min"]              = claims.config.WAR_BOUNTY_MIN;
            _cfgDoubles["war_nap_break_penalty"]       = claims.config.WAR_NAP_BREAK_PENALTY;
            _cfgDoubles["war_plunder_on_kill_percent"] = claims.config.WAR_PLUNDER_ON_KILL_PERCENT;
            _cfgLoaded = true;
        }

        private void BoolCfg(string label, string key, bool current)
        {
            bool b = current;
            if (ImGui.Checkbox(label + "##" + key, ref b))
                Send("/cadmin setcfg " + key + " " + (b ? "on" : "off"));
        }

        private void IntCfg(string label, string key)
        {
            int v = _cfgInts.TryGetValue(key, out var iv) ? iv : 0;
            Label(label);
            ImGui.SameLine(260);
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt("##" + key, ref v, 1, 10)) _cfgInts[key] = v;
            // Send only once editing is committed (enter / focus loss), not on every keystroke.
            if (ImGui.IsItemDeactivatedAfterEdit())
                Send("/cadmin setcfg " + key + " " + _cfgInts[key].ToString(CultureInfo.InvariantCulture));
        }

        private void DoubleCfg(string label, string key)
        {
            double v = _cfgDoubles.TryGetValue(key, out var dv) ? dv : 0;
            Label(label);
            ImGui.SameLine(260);
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputDouble("##" + key, ref v)) _cfgDoubles[key] = v;
            if (ImGui.IsItemDeactivatedAfterEdit())
                Send("/cadmin setcfg " + key + " " + _cfgDoubles[key].ToString(CultureInfo.InvariantCulture));
        }

        private void Send(string cmd) =>
            ((claims.capi.World as ClientMain).eventManager)
                .TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, cmd, EnumChatType.Macro, "");
    }
}
