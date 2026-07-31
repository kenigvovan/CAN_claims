using System;
using System.Collections.Generic;
using System.Globalization;

namespace claims.src.config
{
    /// <summary>
    /// Whitelist + validation for the war-related config fields that admins can edit in-game
    /// via <c>/cadmin setcfg &lt;key&gt; &lt;value&gt;</c> and the admin War tab. This is the single
    /// source of truth for editable keys, their type and their allowed range — the command parses
    /// and assigns through here, so range checks live in one place (the mod has no generic config
    /// validation otherwise).
    /// </summary>
    public static class WarConfigEditor
    {
        private abstract class Spec
        {
            public abstract bool TrySet(string raw, out string error);
        }

        private sealed class IntSpec : Spec
        {
            private readonly int min, max;
            private readonly Action<int> set;
            public IntSpec(int min, int max, Action<int> set) { this.min = min; this.max = max; this.set = set; }
            public override bool TrySet(string raw, out string error)
            {
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                {
                    error = "expected an integer, got '" + raw + "'";
                    return false;
                }
                if (v < min || v > max)
                {
                    error = "value " + v + " is out of range [" + min + ".." + max + "]";
                    return false;
                }
                set(v);
                error = null;
                return true;
            }
        }

        private sealed class DoubleSpec : Spec
        {
            private readonly double min, max;
            private readonly Action<double> set;
            public DoubleSpec(double min, double max, Action<double> set) { this.min = min; this.max = max; this.set = set; }
            public override bool TrySet(string raw, out string error)
            {
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    error = "expected a number, got '" + raw + "'";
                    return false;
                }
                if (v < min || v > max)
                {
                    error = "value " + v + " is out of range [" + min + ".." + max + "]";
                    return false;
                }
                set(v);
                error = null;
                return true;
            }
        }

        private sealed class BoolSpec : Spec
        {
            private readonly Action<bool> set;
            public BoolSpec(Action<bool> set) { this.set = set; }
            public override bool TrySet(string raw, out string error)
            {
                switch (raw.ToLowerInvariant())
                {
                    case "on": case "true": case "1": case "yes": set(true); error = null; return true;
                    case "off": case "false": case "0": case "no": set(false); error = null; return true;
                    default:
                        error = "expected on/off, got '" + raw + "'";
                        return false;
                }
            }
        }

        private static Config C => claims.config;

        private static readonly Dictionary<string, Spec> fields = new Dictionary<string, Spec>(StringComparer.OrdinalIgnoreCase)
        {
            // flag capture
            { "flag_defender_interrupt", new BoolSpec(v => C.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED = v) },
            { "flag_defender_radius", new IntSpec(1, 64, v => C.WAR_FLAG_DEFENDER_RADIUS = v) },
            { "flag_regress_mult", new DoubleSpec(0, 5, v => C.WAR_FLAG_REGRESS_MULTIPLIER = v) },
            { "flag_capture_seconds", new IntSpec(1, 3600, v => C.FLAG_CAPTURE_DURATION_SECONDS = v) },
            { "flag_max_active", new IntSpec(1, 50, v => C.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE = v) },
            { "flag_reinforcement", new IntSpec(0, 1000, v => C.FLAG_REINFORCEMENT_AMOUNT = v) },
            { "min_days_between_battles", new IntSpec(0, 365, v => C.MINIMUM_DAYS_BETWEEN_BATTLES = v) },

            // war score
            { "score_enabled", new BoolSpec(v => C.WAR_SCORE_ENABLED = v) },
            { "score_to_win", new IntSpec(1, 1000000, v => C.WAR_SCORE_TO_WIN = v) },
            { "score_per_capture", new IntSpec(0, 1000000, v => C.WAR_SCORE_PER_PLOT_CAPTURE = v) },
            { "score_per_kill", new IntSpec(0, 1000000, v => C.WAR_SCORE_PER_KILL = v) },
            { "score_per_hold", new IntSpec(0, 1000000, v => C.WAR_SCORE_PER_HOLD_TICK = v) },
            { "score_hold_seconds", new IntSpec(5, 3600, v => C.WAR_SCORE_HOLD_TICK_SECONDS = v) },

            // pillage
            { "pillage_enabled", new BoolSpec(v => C.WAR_PILLAGE_ENABLED = v) },
            { "pillage_percent", new DoubleSpec(0, 100, v => C.WAR_PILLAGE_PERCENT = v) },

            // camp
            { "camp_enabled", new BoolSpec(v => C.WAR_CAMP_ENABLED = v) },
            { "camp_max_per_conflict", new IntSpec(0, 50, v => C.WAR_MAX_CAMPS_PER_CONFLICT = v) },
            { "camp_cost", new DoubleSpec(0, 1000000, v => C.CAMP_PLOT_COST = v) },
            { "camp_min_distance", new IntSpec(0, 100, v => C.WAR_CAMP_MIN_DISTANCE_FROM_OTHER_CITY = v) },
            { "camp_anchor_breaks", new IntSpec(1, 1000, v => C.WAR_CAMP_ANCHOR_BREAKS = v) },

            // respawn safe zone
            { "safezone_enabled", new BoolSpec(v => C.WAR_RESPAWN_SAFEZONE_ENABLED = v) },
            { "safezone_radius", new IntSpec(0, 128, v => C.WAR_RESPAWN_SAFEZONE_RADIUS = v) },
            { "safezone_seconds", new IntSpec(0, 600, v => C.WAR_RESPAWN_SAFEZONE_SECONDS = v) },

            // siege ram
            { "siege_enabled", new BoolSpec(v => C.WAR_SIEGE_ENABLED = v) },
            { "siege_tick_seconds", new IntSpec(1, 600, v => C.WAR_SIEGE_RAM_TICK_SECONDS = v) },
            { "siege_range", new IntSpec(1, 64, v => C.WAR_SIEGE_RAM_RANGE = v) },
            { "siege_cost", new DoubleSpec(0, 1000000, v => C.WAR_SIEGE_RAM_COST = v) },

            // declaration cost / cooldown / casus belli
            { "war_declaration_cost", new DoubleSpec(0, 1000000, v => C.WAR_DECLARATION_COST = v) },
            { "war_redeclare_cooldown_days", new IntSpec(0, 365, v => C.WAR_REDECLARE_COOLDOWN_DAYS = v) },
            { "war_require_casus_belli", new BoolSpec(v => C.WAR_REQUIRE_CASUS_BELLI = v) },
            { "war_casus_belli_grace_days", new IntSpec(0, 365, v => C.WAR_CASUS_BELLI_GRACE_DAYS = v) },

            // peace terms / vassalage
            { "war_peace_terms_enabled", new BoolSpec(v => C.WAR_PEACE_TERMS_ENABLED = v) },
            { "war_vassal_tribute", new DoubleSpec(0, 1000000, v => C.WAR_VASSAL_TRIBUTE = v) },
            { "war_vassal_duration_days", new IntSpec(0, 3650, v => C.WAR_VASSAL_DURATION_DAYS = v) },

            // non-aggression pacts
            { "war_nap_enabled", new BoolSpec(v => C.WAR_NAP_ENABLED = v) },
            { "war_nap_default_days", new IntSpec(1, 3650, v => C.WAR_NAP_DEFAULT_DAYS = v) },
            { "war_nap_max_days", new IntSpec(1, 3650, v => C.WAR_NAP_MAX_DAYS = v) },
            { "war_nap_break_penalty", new DoubleSpec(0, 1000000, v => C.WAR_NAP_BREAK_PENALTY = v) },

            // battle warning
            { "war_battle_warn_minutes", new IntSpec(0, 1440, v => C.WAR_BATTLE_WARN_MINUTES = v) },

            // ultimatums
            { "war_ultimatum_enabled", new BoolSpec(v => C.WAR_ULTIMATUM_ENABLED = v) },
            { "war_ultimatum_expire_hours", new IntSpec(1, 8760, v => C.WAR_ULTIMATUM_EXPIRE_HOURS = v) },

            // bounties / plunder on kill
            { "war_bounty_enabled", new BoolSpec(v => C.WAR_BOUNTY_ENABLED = v) },
            { "war_bounty_min", new DoubleSpec(0, 1000000, v => C.WAR_BOUNTY_MIN = v) },
            { "war_plunder_on_kill_enabled", new BoolSpec(v => C.WAR_PLUNDER_ON_KILL_ENABLED = v) },
            { "war_plunder_on_kill_percent", new DoubleSpec(0, 100, v => C.WAR_PLUNDER_ON_KILL_PERCENT = v) },

            // after-action report / HUD
            { "war_report_enabled", new BoolSpec(v => C.WAR_REPORT_ENABLED = v) },
            { "war_hud_enabled", new BoolSpec(v => C.WAR_HUD_ENABLED = v) },

            // boats shared with the owner's city (not war-related, but this is the one
            // runtime-editable config surface the mod has)
            { "boat_share_with_city", new BoolSpec(v => C.BOAT_SHARE_WITH_CITY = v) },
            { "boat_share_with_alliance", new BoolSpec(v => C.BOAT_SHARE_WITH_ALLIANCE = v) },
        };

        /// <summary>Known editable keys (for help/error messages).</summary>
        public static IEnumerable<string> Keys => fields.Keys;

        /// <summary>
        /// Parses <paramref name="rawValue"/> for <paramref name="key"/> and assigns it to
        /// <see cref="claims.config"/> if it is a known key and within range.
        /// Returns false with a human-readable <paramref name="error"/> otherwise.
        /// Caller is responsible for persisting (StoreModConfig) and re-broadcasting to clients.
        /// </summary>
        public static bool TrySet(string key, string rawValue, out string error)
        {
            if (key == null || !fields.TryGetValue(key, out var spec))
            {
                error = "unknown war config key '" + key + "'";
                return false;
            }
            return spec.TrySet(rawValue ?? "", out error);
        }
    }
}
