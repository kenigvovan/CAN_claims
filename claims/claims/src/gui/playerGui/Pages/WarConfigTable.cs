using System;
using System.Collections.Generic;
using claims.src.part.structure.war;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public enum EnumCfgKind
    {
        Group, Flag, Int, Double, Text,
        /// <summary>One of a fixed set of words - picked in a dialog, not typed.</summary>
        Choice,
        /// <summary>Any number of a fixed set of words, sent as a comma separated list.</summary>
        MultiChoice
    }

    /// <summary>One entry of a <see cref="EnumCfgKind.Choice"/> row: the word sent, and its label.</summary>
    public sealed class CfgOption
    {
        public string Value;
        public string LabelLangKey;
        public CfgOption(string value, string labelLangKey) { Value = value; LabelLangKey = labelLangKey; }
    }

    /// <summary>
    /// One row of the war settings editor: a group heading, or a setting with the /cadmin setcfg
    /// key it writes and where its current value is read from.
    /// </summary>
    public sealed class WarCfgRow
    {
        public EnumCfgKind Kind;
        public string LabelLangKey;
        public string CfgKey;
        public Func<bool> GetFlag;
        public Func<int> GetInt;
        public Func<double> GetDouble;
        public Func<string> GetText;
        /// <summary>
        /// Shown in the list when the raw value is too long for the value column. The row still
        /// loads <see cref="GetText"/> into the edit field, so what is displayed may be shortened
        /// without making the edit field useless.
        /// </summary>
        public Func<string> GetDisplay;

        /// <summary>What a Choice/MultiChoice row offers. Null for every other kind.</summary>
        public CfgOption[] Options;

        /// <summary>
        /// Empty is a meaningful answer for a MultiChoice - "no restriction" - but the command word
        /// for it differs per setting ("none", "off"), so the row says which to send.
        /// </summary>
        public string EmptyValue = "none";

        public static WarCfgRow Group(string label) => new WarCfgRow { Kind = EnumCfgKind.Group, LabelLangKey = label };
        public static WarCfgRow Flag(string label, string key, Func<bool> get) => new WarCfgRow { Kind = EnumCfgKind.Flag, LabelLangKey = label, CfgKey = key, GetFlag = get };
        public static WarCfgRow Int(string label, string key, Func<int> get) => new WarCfgRow { Kind = EnumCfgKind.Int, LabelLangKey = label, CfgKey = key, GetInt = get };
        public static WarCfgRow Dbl(string label, string key, Func<double> get) => new WarCfgRow { Kind = EnumCfgKind.Double, LabelLangKey = label, CfgKey = key, GetDouble = get };
        /// <summary>A setting whose value is neither a switch nor a number - a day list, a zone id.</summary>
        public static WarCfgRow Text(string label, string key, Func<string> get, Func<string> display = null)
            => new WarCfgRow { Kind = EnumCfgKind.Text, LabelLangKey = label, CfgKey = key, GetText = get, GetDisplay = display };

        /// <summary>Pick one of <paramref name="options"/> in a dialog instead of typing the word.</summary>
        public static WarCfgRow Choice(string label, string key, Func<string> get, Func<string> display, params CfgOption[] options)
            => new WarCfgRow { Kind = EnumCfgKind.Choice, LabelLangKey = label, CfgKey = key, GetText = get, GetDisplay = display, Options = options };

        /// <summary>Pick any number of <paramref name="options"/>; sent as a comma separated list.</summary>
        public static WarCfgRow MultiChoice(string label, string key, Func<string> get, Func<string> display, params CfgOption[] options)
            => new WarCfgRow { Kind = EnumCfgKind.MultiChoice, LabelLangKey = label, CfgKey = key, GetText = get, GetDisplay = display, Options = options };
    }

    /// <summary>
    /// The war settings an admin may change at runtime, as data. Values are read straight from the
    /// live config, which the server echoes back after every /cadmin setcfg.
    /// </summary>
    public static class WarConfigTable
    {
        public static IReadOnlyList<WarCfgRow> Rows => rows;

        private static readonly WarCfgRow[] rows =
        {
            WarCfgRow.Group("claims:gui-admin-warcfg-flag"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-flag-interrupt", "flag_defender_interrupt", () => claims.config.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED),
            WarCfgRow.Int("claims:gui-admin-warcfg-flag-radius", "flag_defender_radius", () => claims.config.WAR_FLAG_DEFENDER_RADIUS),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-flag-regress", "flag_regress_mult", () => claims.config.WAR_FLAG_REGRESS_MULTIPLIER),
            WarCfgRow.Int("claims:gui-admin-warcfg-flag-seconds", "flag_capture_seconds", () => claims.config.FLAG_CAPTURE_DURATION_SECONDS),
            WarCfgRow.Int("claims:gui-admin-warcfg-flag-maxactive", "flag_max_active", () => claims.config.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE),
            WarCfgRow.Int("claims:gui-admin-warcfg-flag-reinforce", "flag_reinforcement", () => claims.config.FLAG_REINFORCEMENT_AMOUNT),
            WarCfgRow.Int("claims:gui-admin-warcfg-mindays", "min_days_between_battles", () => claims.config.MINIMUM_DAYS_BETWEEN_BATTLES),

            WarCfgRow.Group("claims:gui-admin-warcfg-score"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-score-enabled", "score_enabled", () => claims.config.WAR_SCORE_ENABLED),
            WarCfgRow.Int("claims:gui-admin-warcfg-score-towin", "score_to_win", () => claims.config.WAR_SCORE_TO_WIN),
            WarCfgRow.Int("claims:gui-admin-warcfg-score-capture", "score_per_capture", () => claims.config.WAR_SCORE_PER_PLOT_CAPTURE),
            WarCfgRow.Int("claims:gui-admin-warcfg-score-kill", "score_per_kill", () => claims.config.WAR_SCORE_PER_KILL),
            WarCfgRow.Int("claims:gui-admin-warcfg-score-hold", "score_per_hold", () => claims.config.WAR_SCORE_PER_HOLD_TICK),
            WarCfgRow.Int("claims:gui-admin-warcfg-score-holdsec", "score_hold_seconds", () => claims.config.WAR_SCORE_HOLD_TICK_SECONDS),

            WarCfgRow.Group("claims:gui-admin-warcfg-pillage"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-pillage-enabled", "pillage_enabled", () => claims.config.WAR_PILLAGE_ENABLED),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-pillage-percent", "pillage_percent", () => claims.config.WAR_PILLAGE_PERCENT),

            WarCfgRow.Group("claims:gui-admin-warcfg-camp"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-camp-enabled", "camp_enabled", () => claims.config.WAR_CAMP_ENABLED),
            WarCfgRow.Int("claims:gui-admin-warcfg-camp-max", "camp_max_per_conflict", () => claims.config.WAR_MAX_CAMPS_PER_CONFLICT),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-camp-cost", "camp_cost", () => claims.config.CAMP_PLOT_COST),
            WarCfgRow.Int("claims:gui-admin-warcfg-camp-mindist", "camp_min_distance", () => claims.config.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY),
            WarCfgRow.Int("claims:gui-admin-warcfg-camp-breaks", "camp_anchor_breaks", () => claims.config.WAR_CAMP_ANCHOR_BREAKS),

            WarCfgRow.Group("claims:gui-admin-warcfg-safezone"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-safezone-enabled", "safezone_enabled", () => claims.config.WAR_RESPAWN_SAFEZONE_ENABLED),
            WarCfgRow.Int("claims:gui-admin-warcfg-safezone-radius", "safezone_radius", () => claims.config.WAR_RESPAWN_SAFEZONE_RADIUS),
            WarCfgRow.Int("claims:gui-admin-warcfg-safezone-seconds", "safezone_seconds", () => claims.config.WAR_RESPAWN_SAFEZONE_SECONDS),

            // Siege ram: the server accepts these three through /cadmin setcfg, but no front-end ever
            // listed them.
            WarCfgRow.Group("claims:gui-admin-warcfg-siege"),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-siege-cost", "siege_cost", () => claims.config.WAR_SIEGE_RAM_COST),
            WarCfgRow.Int("claims:gui-admin-warcfg-siege-range", "siege_range", () => claims.config.WAR_SIEGE_RAM_RANGE),
            WarCfgRow.Int("claims:gui-admin-warcfg-siege-tick", "siege_tick_seconds", () => claims.config.WAR_SIEGE_RAM_TICK_SECONDS),

            WarCfgRow.Group("claims:gui-admin-warcfg-declare"),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-declare-cost", "war_declaration_cost", () => claims.config.WAR_DECLARATION_COST),
            WarCfgRow.Int("claims:gui-admin-warcfg-declare-cooldown", "war_redeclare_cooldown_days", () => claims.config.WAR_REDECLARE_COOLDOWN_DAYS),
            WarCfgRow.Flag("claims:gui-admin-warcfg-declare-cb", "war_require_casus_belli", () => claims.config.WAR_REQUIRE_CASUS_BELLI),
            WarCfgRow.Int("claims:gui-admin-warcfg-declare-cb-days", "war_casus_belli_grace_days", () => claims.config.WAR_CASUS_BELLI_GRACE_DAYS),

            WarCfgRow.Group("claims:gui-admin-warcfg-peace"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-peace-enabled", "war_peace_terms_enabled", () => claims.config.WAR_PEACE_TERMS_ENABLED),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-vassal-tribute", "war_vassal_tribute", () => claims.config.WAR_VASSAL_TRIBUTE),
            WarCfgRow.Int("claims:gui-admin-warcfg-vassal-days", "war_vassal_duration_days", () => claims.config.WAR_VASSAL_DURATION_DAYS),

            WarCfgRow.Group("claims:gui-admin-warcfg-nap"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-nap-enabled", "war_nap_enabled", () => claims.config.WAR_NAP_ENABLED),
            WarCfgRow.Int("claims:gui-admin-warcfg-nap-default-days", "war_nap_default_days", () => claims.config.WAR_NAP_DEFAULT_DAYS),
            WarCfgRow.Int("claims:gui-admin-warcfg-nap-max-days", "war_nap_max_days", () => claims.config.WAR_NAP_MAX_DAYS),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-nap-penalty", "war_nap_break_penalty", () => claims.config.WAR_NAP_BREAK_PENALTY),
            WarCfgRow.Int("claims:gui-admin-warcfg-battle-warn", "war_battle_warn_minutes", () => claims.config.WAR_BATTLE_WARN_MINUTES),

            WarCfgRow.Group("claims:gui-admin-warcfg-schedule"),
            WarCfgRow.MultiChoice("claims:gui-admin-warcfg-allowed-days", "war_allowed_battle_days",
                AllowedDaysValue, AllowedDaysDisplay, WeekdayOptions()),
            WarCfgRow.Text("claims:gui-admin-warcfg-timezone", "war_schedule_timezone", ScheduleTimezoneValue),

            WarCfgRow.Group("claims:gui-admin-warcfg-ultimatum"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-ultimatum-enabled", "war_ultimatum_enabled", () => claims.config.WAR_ULTIMATUM_ENABLED),
            WarCfgRow.Int("claims:gui-admin-warcfg-ultimatum-hours", "war_ultimatum_expire_hours", () => claims.config.WAR_ULTIMATUM_EXPIRE_HOURS),

            WarCfgRow.Group("claims:gui-admin-warcfg-bounty"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-bounty-enabled", "war_bounty_enabled", () => claims.config.WAR_BOUNTY_ENABLED),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-bounty-min", "war_bounty_min", () => claims.config.WAR_BOUNTY_MIN),
            WarCfgRow.Flag("claims:gui-admin-warcfg-plunder-enabled", "war_plunder_on_kill_enabled", () => claims.config.WAR_PLUNDER_ON_KILL_ENABLED),
            WarCfgRow.Dbl("claims:gui-admin-warcfg-plunder-percent", "war_plunder_on_kill_percent", () => claims.config.WAR_PLUNDER_ON_KILL_PERCENT),

            WarCfgRow.Group("claims:gui-admin-warcfg-report"),
            WarCfgRow.Flag("claims:gui-admin-warcfg-report-enabled", "war_report_enabled", () => claims.config.WAR_REPORT_ENABLED),
            WarCfgRow.Flag("claims:gui-admin-warcfg-hud-enabled", "war_hud_enabled", () => claims.config.WAR_HUD_ENABLED),

            WarCfgRow.Group("claims:gui-admin-warcfg-bankruptcy"),
            WarCfgRow.Choice("claims:gui-admin-warcfg-bankruptcy-mode", "city_bankruptcy_mode",
                () => claims.config?.CITY_BANKRUPTCY_MODE ?? "off",
                () => Lang.Get("claims:gui-warcfg-bankruptcy-" + (claims.config?.CITY_BANKRUPTCY_MODE ?? "off")),
                new CfgOption("off", "claims:gui-warcfg-bankruptcy-off"),
                new CfgOption("plots", "claims:gui-warcfg-bankruptcy-plots"),
                new CfgOption("whole_city", "claims:gui-warcfg-bankruptcy-whole_city")),
            WarCfgRow.Int("claims:gui-admin-warcfg-bankruptcy-grace", "city_bankruptcy_grace_days",
                () => claims.config.CITY_BANKRUPTCY_GRACE_DAYS),
        };

        /// <summary>The seven weekdays as options, labelled with the short day names already in lang.</summary>
        private static CfgOption[] WeekdayOptions()
        {
            var options = new CfgOption[7];
            for (int i = 0; i < 7; i++)
            {
                string day = ((DayOfWeek)i).ToString().ToLowerInvariant();
                options[i] = new CfgOption(day, "claims:gui_day_short_" + day);
            }
            return options;
        }

        /// <summary>The row a config key belongs to, for the dialog that edits it.</summary>
        public static bool TryGetRow(string cfgKey, out WarCfgRow row)
        {
            foreach (var it in rows)
            {
                if (it.CfgKey != null && it.CfgKey.Equals(cfgKey, StringComparison.OrdinalIgnoreCase))
                {
                    row = it;
                    return true;
                }
            }
            row = null;
            return false;
        }

        /// <summary>
        /// The allowed battle days, written the way setcfg takes them back - clicking the row loads
        /// this into the edit field, so it has to be valid input rather than prose.
        /// </summary>
        private static string AllowedDaysValue()
        {
            var days = claims.config?.WAR_ALLOWED_BATTLE_DAYS;
            if (days == null || days.Count == 0) return "none";

            var parts = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                var day = (DayOfWeek)i;
                if (days.Contains(day)) parts.Add(day.ToString().ToLowerInvariant());
            }
            return string.Join(",", parts);
        }

        /// <summary>Short day names, which is all the value column has room for.</summary>
        private static string AllowedDaysDisplay()
        {
            if (!WarScheduleHelper.HasDayRestriction)
            {
                return Lang.Get("claims:gui-admin-warcfg-days-any");
            }
            return WarScheduleHelper.AllowedDaysText();
        }

        private static string ScheduleTimezoneValue()
        {
            string zone = claims.config?.WAR_SCHEDULE_TIMEZONE;
            return string.IsNullOrWhiteSpace(zone) ? "server" : zone;
        }
    }
}
