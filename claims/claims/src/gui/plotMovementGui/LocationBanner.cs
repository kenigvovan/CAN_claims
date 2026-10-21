using System.Collections.Generic;
using Cairo;
using claims.src.auxialiry;
using claims.src.auxialiry.innerclaims;
using claims.src.claimsext.map;
using claims.src.clientMapHandling;
using claims.src.gui.playerGui.GuiElements;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.plotMovementGui
{
    /// <summary>
    /// Where the player is, on screen: the city's name as a title when they walk into a city (or
    /// "Wild lands" when they leave one), a short line above the hotbar for a move between plots of
    /// one city - or for a change of the plot they stand on. Worked out on the client from the plots
    /// the server already sends. Taken over from canclaimsextended.
    ///
    /// What the plot panel did not: a place counts only once the player has stood on it for a
    /// moment, so walking along a border does not flicker; a city's title is not repeated within
    /// half a minute; pvp is said only when it changes; and a plot that changes under the player's
    /// feet is shown at once instead of at the next border.
    /// </summary>
    public class LocationBanner : IRenderer
    {
        private const int CheckMs = 250;
        private const int StableMs = 500;
        private const int TitleMs = 3500;
        private const int LineMs = 2500;
        private const int FadeInMs = 300;
        private const int FadeOutMs = 800;
        private const int RepeatMs = 30000;
        // Edge of the city's arms above its name, and the space between them, before GUI scaling.
        private const int EmblemPx = 72;
        private const int EmblemGapPx = 12;

        private readonly ICoreClientAPI capi;
        private readonly ClaimsClientPrefs prefs;
        private long tickListener;

        // What is under the player's feet as last seen, and since when.
        private string candidate;
        private long candidateSince;
        // The place the player is counted in: the plot, and what the plot looked like.
        private bool placed;
        private string currentKey = "";
        private string currentCity = "";
        private bool currentPvp;

        // City name ("" for the wild) -> when its title was last shown.
        private readonly Dictionary<string, long> titleShownAt = new();

        private LoadedTexture title, subtitle, line, emblem;
        private long titleStart = -1, lineStart = -1;

        public double RenderOrder => 0.9;
        public int RenderRange => 0;

        public LocationBanner(ICoreClientAPI capi, ClaimsClientPrefs prefs)
        {
            this.capi = capi;
            this.prefs = prefs;
            tickListener = capi.Event.RegisterGameTickListener(_ => Check(), CheckMs);
        }

        public bool Toggle()
        {
            prefs.ShowBanner = !prefs.ShowBanner;
            prefs.Save(capi);
            if (!prefs.ShowBanner) titleStart = lineStart = -1;
            return prefs.ShowBanner;
        }

        private void Check()
        {
            var entity = capi.World?.Player?.Entity;
            if (entity == null || claims.clientDataStorage == null || PlotPosition.plotSize <= 0) return;

            var pos = new Vec2i((int)entity.Pos.X / PlotPosition.plotSize, (int)entity.Pos.Z / PlotPosition.plotSize);
            claims.clientDataStorage.getSavedPlot(pos, out SavedPlotInfo plot);
            // Stepping into a flat, or out of it, is a change of place as much as crossing a border.
            var subPlot = plot?.SubPlotAt((int)System.Math.Floor(entity.Pos.X), (int)System.Math.Floor(entity.Pos.Y),
                                          (int)System.Math.Floor(entity.Pos.Z));
            // The plot and everything shown about it: a change of either is something to say.
            string key = plot == null ? "" : pos.X + "," + pos.Y + "|" + Describe(plot) + "|" + DescribeSubPlot(subPlot);
            long now = capi.ElapsedMilliseconds;
            if (key != candidate)
            {
                candidate = key;
                candidateSince = now;
                return;
            }
            if (now - candidateSince < StableMs || (placed && key == currentKey)) return;

            string city = plot?.cityName ?? "";
            bool inCity = city.Length > 0;
            bool pvp = plot?.PvPIsOn ?? false;
            bool firstPlace = !placed;
            bool cityChanged = city != currentCity;
            bool pvpChanged = pvp != currentPvp;
            string leftCity = currentCity;

            placed = true;
            currentKey = key;
            currentCity = city;
            currentPvp = pvp;

            // The panel, when open, follows the same changes - it used to wait for the next border.
            claims.RefreshMovementPanel(plot);

            if (!prefs.ShowBanner) return;
            if (firstPlace)
            {
                // Logging in on free land says nothing.
                if (inCity && TakeTitle(city, now)) ShowCity(city, pvp);
            }
            else if (cityChanged)
            {
                if (!TakeTitle(city, now))
                {
                    // Walking along the border: the same title again so soon would be noise.
                    if (inCity) ShowPlaceLine(plot, subPlot, pvpChanged ? pvp : null);
                }
                else if (inCity) ShowCity(city, pvp);
                else ShowWilderness(leftCity);
            }
            else if (inCity)
            {
                ShowPlaceLine(plot, subPlot, pvpChanged ? pvp : null);
            }
        }

        private static string Describe(SavedPlotInfo plot) =>
            plot.cityName + "|" + plot.plotName + "|" + plot.groupName + "|" + plot.price + "|" + plot.PvPIsOn;

        private static string DescribeSubPlot(ClientInnerClaim subPlot) =>
            subPlot == null ? "" : subPlot.Name + "|" + subPlot.Owner + "|" + subPlot.Price;

        /// <summary>"Flat · owner Bob" or "Flat · for sale 100"; the city's own while nobody holds it.</summary>
        public static string SubPlotLine(ClientInnerClaim subPlot)
        {
            string text = Lang.Get("claims:banner-subplot", subPlot.Name);
            text += " · " + (subPlot.Owner.Length > 0
                ? Lang.Get("claims:banner-subplot-owner", subPlot.Owner)
                : Lang.Get("claims:banner-subplot-city"));
            string offer = subplots.SubPlotText.Offer(subPlot.Price, subPlot.Tax);
            if (offer != null) text += " · " + offer;
            return text;
        }

        /// <summary>False when this title (a city, or "" for the wild) was shown a moment ago.</summary>
        private bool TakeTitle(string city, long now)
        {
            if (titleShownAt.TryGetValue(city, out long at) && now - at < RepeatMs) return false;
            titleShownAt[city] = now;
            return true;
        }

        private void ShowCity(string city, bool pvp)
        {
            SetTitle(StringFunctions.replaceUnderscore(city), Lighten(claims.clientDataStorage.ClientGetCityColor(city)),
                Lang.Get(pvp ? "claims:banner-pvp-on" : "claims:banner-pvp-off"),
                pvp ? new[] { 0.95, 0.35, 0.30, 1.0 } : new[] { 0.60, 0.90, 0.60, 1.0 });
            SetEmblem(PlotsMapLayer.EmblemOfCity(city));
            lineStart = -1;
        }

        /// <summary>The city's coat of arms above its name; none for a city without one.</summary>
        private void SetEmblem(string code)
        {
            emblem?.Dispose();
            emblem = null;
            if (string.IsNullOrEmpty(code)) return;

            int size = (int)GuiElement.scaled(EmblemPx);
            using (var surface = new ImageSurface(Format.Argb32, size, size))
            {
                using (var ctx = new Context(surface))
                {
                    EmblemCache.Draw(capi, ctx, code, 0, 0, size, size);
                }
                emblem = new LoadedTexture(capi);
                capi.Gui.LoadOrUpdateCairoTexture(surface, true, ref emblem);
            }
        }

        private void ShowWilderness(string leftCity)
        {
            SetEmblem(null);
            SetTitle(Lang.Get("claims:movementgui-wild-lands"), new[] { 0.85, 0.85, 0.80, 1.0 },
                string.IsNullOrEmpty(leftCity) ? "" : Lang.Get("claims:banner-left-city", StringFunctions.replaceUnderscore(leftCity)),
                new[] { 0.75, 0.75, 0.75, 1.0 });
            lineStart = -1;
        }

        /// <summary>The plot's name or "city plot", its group and its price when for sale; pvp only when it changed.</summary>
        private void ShowPlaceLine(SavedPlotInfo plot, ClientInnerClaim subPlot, bool? pvp)
        {
            string text;
            if (subPlot != null)
            {
                // Inside a sub-plot its name is the place; the chunk's own sale is not what is on offer here.
                text = SubPlotLine(subPlot);
            }
            else
            {
                text = !string.IsNullOrEmpty(plot.plotName) ? plot.plotName : Lang.Get("claims:banner-city-plot");
                if (!string.IsNullOrEmpty(plot.groupName)) text += " · " + StringFunctions.replaceUnderscore(plot.groupName);
                if (plot.price > -1) text += " · " + Lang.Get("claims:movementgui-price", plot.price);
            }
            if (pvp.HasValue) text += " · " + Lang.Get(pvp.Value ? "claims:banner-pvp-on" : "claims:banner-pvp-off");

            line?.Dispose();
            line = capi.Gui.TextTexture.GenTextTexture(text, CairoFont.WhiteSmallishText().WithStroke(new[] { 0.0, 0.0, 0.0, 0.8 }, 2));
            lineStart = capi.ElapsedMilliseconds;
        }

        private void SetTitle(string text, double[] color, string sub, double[] subColor)
        {
            title?.Dispose();
            subtitle?.Dispose();
            var stroke = new[] { 0.0, 0.0, 0.0, 0.85 };
            title = capi.Gui.TextTexture.GenTextTexture(text, new CairoFont(40, GuiStyle.DecorativeFontName, color).WithStroke(stroke, 3));
            subtitle = string.IsNullOrEmpty(sub) ? null
                : capi.Gui.TextTexture.GenTextTexture(sub, new CairoFont(20, GuiStyle.StandardFontName, subColor).WithStroke(stroke, 2));
            titleStart = capi.ElapsedMilliseconds;
        }

        /// <summary>City colours are texture pixels, ABGR; dark ones are lifted so the name stays readable.</summary>
        public static double[] Lighten(int abgr)
        {
            double r = (abgr & 0xFF) / 255.0, g = ((abgr >> 8) & 0xFF) / 255.0, b = ((abgr >> 16) & 0xFF) / 255.0;
            const double lift = 0.35;
            return new[] { r + (1 - r) * lift, g + (1 - g) * lift, b + (1 - b) * lift, 1.0 };
        }

        /// <summary>0..1: fades in, holds, fades out; 0 once over.</summary>
        public static float Alpha(long elapsed, int duration)
        {
            if (elapsed < 0 || elapsed >= duration) return 0;
            if (elapsed < FadeInMs) return (float)elapsed / FadeInMs;
            long left = duration - elapsed;
            return left < FadeOutMs ? (float)left / FadeOutMs : 1f;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!prefs.ShowBanner || capi.HideGuis) return;
            IRenderAPI rapi = capi.Render;
            long now = capi.ElapsedMilliseconds;

            float a = titleStart < 0 ? 0 : Alpha(now - titleStart, TitleMs);
            if (a > 0 && title != null)
            {
                float y = rapi.FrameHeight * 0.16f;
                if (emblem != null)
                {
                    // Above the name, not beside it: the decorative font with its outline runs past
                    // the width its texture reports, and the arms placed by that width sat on the text.
                    float emblemTop = System.Math.Max(0f, y - emblem.Height - (float)GuiElement.scaled(EmblemGapPx));
                    Draw(emblem, rapi.FrameWidth / 2f, emblemTop, a);
                    y = emblemTop + emblem.Height + (float)GuiElement.scaled(EmblemGapPx);
                }
                Draw(title, rapi.FrameWidth / 2f, y, a);
                if (subtitle != null) Draw(subtitle, rapi.FrameWidth / 2f, y + title.Height, a);
            }
            else
            {
                titleStart = -1;
            }

            a = lineStart < 0 ? 0 : Alpha(now - lineStart, LineMs);
            if (a > 0 && line != null)
            {
                // Above the hotbar and the held item's name.
                Draw(line, rapi.FrameWidth / 2f, rapi.FrameHeight - (float)GuiElement.scaled(170), a);
            }
            else
            {
                lineStart = -1;
            }
        }

        private void Draw(LoadedTexture tex, float centerX, float topY, float alpha)
        {
            // Text textures are premultiplied, so every channel fades together.
            capi.Render.Render2DTexturePremultipliedAlpha(tex.TextureId, centerX - tex.Width / 2f, topY, tex.Width, tex.Height, 20f,
                new Vec4f(alpha, alpha, alpha, alpha));
        }

        public void Dispose()
        {
            if (tickListener != 0) capi.Event.UnregisterGameTickListener(tickListener);
            tickListener = 0;
            title?.Dispose();
            subtitle?.Dispose();
            line?.Dispose();
            emblem?.Dispose();
            title = subtitle = line = emblem = null;
        }
    }
}
