using System;
using System.Collections.Generic;
using System.Linq;

namespace claims.src.part.structure
{
    /// <summary>
    /// Coat of arms of a city or an alliance, stored as a single string on the owning part.
    ///
    /// Layers are separated by ';', bottom one first: "color_red;cross_white;lion_left_yellow". A
    /// layer is the texture file name under assets/claims/textures/emblem/pattern, so every drawer
    /// resolves it the same way without a lookup table.
    ///
    /// Layers are whitelisted: the string comes from the client and ends up in an asset path, so an
    /// unknown pattern or colour is dropped rather than resolved.
    ///
    /// Named ...Handler because City/Alliance carry an "Emblem" property that would shadow the type.
    /// </summary>
    public static class EmblemHandler
    {
        public const char LAYER_DELIMITER = ';';
        public const char COLOR_DELIMITER = '_';

        /// <summary>Path inside the claims domain. Without the domain prefix: callers pass it as
        /// AssetLocation("claims", path), where a colon would become part of the file path.</summary>
        public const string TEXTURE_PATH_PREFIX = "textures/emblem/pattern/";

        /// <summary>Patterns offered by default, when EMBLEM_AVAILABLE_PATTERNS is empty.</summary>
        public static readonly IReadOnlyList<string> DEFAULT_PATTERNS = new List<string>
        {
            // backgrounds and divisions
            "color", "half_horizontal", "half_vertical", "diagonal_left", "diagonal_right",
            "gradient", "checker4", "small_stripes",
            // ordinaries
            "border", "curly_border", "cross", "straight_cross", "falx_left", "falx_right",
            "stripe_center", "stripe_middle", "triangle_bottom", "triangle_top", "rhombus",
            "circle", "circle_large",
            // charges
            "lion_left", "lion_right", "elk", "skull", "heart", "crescent_up",
            "fleurdelis", "flower", "club", "spade", "globe"
        };

        /// <summary>Colors every shipped pattern exists in. Same "empty config means all of these" rule as the patterns.</summary>
        public static readonly IReadOnlyList<string> DEFAULT_COLORS = new List<string>
        {
            "black", "blue", "brown", "gray", "green", "orange", "pink", "purple", "red", "white", "yellow"
        };

        /// <summary>Patterns a player may pick. A non-empty config entry replaces the built-in list
        /// rather than narrowing it - the mod ships more patterns than it offers by default.</summary>
        public static IReadOnlyList<string> AvailablePatterns()
        {
            var configured = claims.config?.EMBLEM_AVAILABLE_PATTERNS;
            if (configured == null || configured.Count == 0) return DEFAULT_PATTERNS;
            return configured.ToList();
        }

        /// <summary>Colors a player may currently pick. Same replace-not-narrow rule as the patterns.</summary>
        public static IReadOnlyList<string> AvailableColors()
        {
            var configured = claims.config?.EMBLEM_AVAILABLE_COLORS;
            if (configured == null || configured.Count == 0) return DEFAULT_COLORS;
            return configured.ToList();
        }

        public static int MaxLayers()
        {
            int max = claims.config?.EMBLEM_MAX_LAYERS ?? 6;
            return max < 1 ? 1 : max;
        }

        /// <summary>
        /// The pattern that fills the whole cloth. It has to be the bottom layer and cannot be any
        /// other: without it the uncovered part of the banner stays transparent, above it would
        /// paint over everything below.
        /// </summary>
        public const string BACKGROUND_PATTERN = "color";

        /// <summary>
        /// Patterns that may go at <paramref name="layerIndex"/> - the background alone at the
        /// bottom, everything else above it.
        /// </summary>
        public static IReadOnlyList<string> AvailablePatternsForLayer(int layerIndex)
        {
            var all = AvailablePatterns();
            if (layerIndex <= 0)
            {
                return all.Contains(BACKGROUND_PATTERN)
                    ? new List<string> { BACKGROUND_PATTERN }
                    : new List<string>();
            }
            return all.Where(p => p != BACKGROUND_PATTERN).ToList();
        }

        public static bool IsBackgroundLayer(string layer)
            => TrySplitLayer(layer, out string pattern, out _) && pattern == BACKGROUND_PATTERN;

        /// <summary>Splits a layer into its pattern and color halves. The color is what follows the LAST
        /// underscore, since patterns themselves contain underscores ("lion_left_red").</summary>
        public static bool TrySplitLayer(string layer, out string pattern, out string color)
        {
            pattern = null;
            color = null;
            if (string.IsNullOrEmpty(layer)) return false;
            int idx = layer.LastIndexOf(COLOR_DELIMITER);
            if (idx <= 0 || idx == layer.Length - 1) return false;
            pattern = layer.Substring(0, idx);
            color = layer.Substring(idx + 1);
            return true;
        }

        public static string MakeLayer(string pattern, string color) => pattern + COLOR_DELIMITER + color;

        public static bool IsValidLayer(string layer)
        {
            if (!TrySplitLayer(layer, out string pattern, out string color)) return false;
            return AvailablePatterns().Contains(pattern) && AvailableColors().Contains(color);
        }

        /// <summary>Asset location of a layer's texture. Only call with a layer that passed <see cref="IsValidLayer"/>.</summary>
        public static string TexturePath(string layer) => TEXTURE_PATH_PREFIX + layer + ".png";

        /// <summary>
        /// Layers of a stored or received emblem, dropping unknown ones and cutting to the limit.
        /// Never null, so "no emblem" and "unreadable emblem" need no separate handling.
        /// </summary>
        public static List<string> Parse(string raw)
        {
            Split(raw, false, out List<string> layers, out _);
            return layers;
        }

        /// <summary>Like <see cref="Parse"/>, but for typed or received input: reports the first bad
        /// layer instead of dropping it.</summary>
        public static bool TryParse(string raw, out List<string> layers, out string error)
            => Split(raw, true, out layers, out error);

        /// <summary>
        /// The one place an emblem string is read, under the same rules either way. Strict reports
        /// the first problem and yields nothing; lenient keeps what it can, so a stored emblem still
        /// draws after the whitelist changed under it.
        /// </summary>
        private static bool Split(string raw, bool strict, out List<string> layers, out string error)
        {
            layers = new List<string>();
            error = null;
            if (string.IsNullOrEmpty(raw)) return true;

            foreach (string part in raw.Split(LAYER_DELIMITER))
            {
                string layer = part.Trim();
                if (layer.Length == 0) continue;

                if (!IsValidLayer(layer))
                {
                    if (!strict) continue;
                    return Fail("unknown emblem layer '" + layer + "'", out layers, out error);
                }

                bool isBackground = IsBackgroundLayer(layer);
                bool wantsBackground = layers.Count == 0;
                if (isBackground != wantsBackground)
                {
                    // Lenient keeps a background-less stack as it is; it gets fixed up when stored.
                    if (!strict) { layers.Add(layer); }
                    else if (wantsBackground)
                    {
                        return Fail("the first layer must be a solid fill, e.g. '"
                            + MakeLayer(BACKGROUND_PATTERN, "red") + "'", out layers, out error);
                    }
                    else
                    {
                        return Fail("a solid fill ('" + layer + "') can only be the first layer - it "
                            + "would cover everything under it", out layers, out error);
                    }
                }
                else
                {
                    layers.Add(layer);
                }

                if (!strict && layers.Count >= MaxLayers()) break;
            }

            if (strict && layers.Count > MaxLayers())
            {
                return Fail("too many emblem layers: " + layers.Count + ", maximum is " + MaxLayers(),
                    out layers, out error);
            }
            return true;
        }

        private static bool Fail(string message, out List<string> layers, out string error)
        {
            layers = new List<string>();
            error = message;
            return false;
        }

        /// <summary>
        /// Forces a layer list to obey the background rule: a stack not starting with a solid fill
        /// is discarded, solid fills above the bottom are removed.
        ///
        /// Needed for emblems stored before the rule - the editor can only take layers off the top,
        /// so they would otherwise be uneditable.
        /// </summary>
        public static List<string> Sanitize(List<string> layers)
        {
            if (layers == null || layers.Count == 0) return new List<string>();
            if (!IsBackgroundLayer(layers[0])) return new List<string>();

            var result = new List<string> { layers[0] };
            result.AddRange(layers.Skip(1).Where(layer => !IsBackgroundLayer(layer)));
            return result;
        }

        public static string Join(IEnumerable<string> layers) =>
            layers == null ? "" : string.Join(LAYER_DELIMITER.ToString(), layers);

        /// <summary>Drops unknown layers, enforces the limit and the background rule, giving back a string safe to store.</summary>
        public static string Normalize(string raw) => Join(Sanitize(Parse(raw)));

        public static bool IsEmpty(string raw) => Parse(raw).Count == 0;
    }
}
