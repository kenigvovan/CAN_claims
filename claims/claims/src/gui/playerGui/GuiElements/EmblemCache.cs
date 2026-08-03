using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One composed Cairo surface per distinct coat of arms, and the drawing of it at any size.
    ///
    /// Everything that shows arms goes through here: GUI windows recompose on every click and the
    /// map's hover box on every mouse move, while emblems themselves change almost never.
    /// </summary>
    public static class EmblemCache
    {
        /// <summary>Edge the arms are composed at - larger than any box they are drawn into, so the
        /// small pixel-art layers are upscaled once here and only ever scaled down afterwards.</summary>
        private const int ComposeSize = 128;

        private static readonly Dictionary<string, ImageSurface> surfaces = new Dictionary<string, ImageSurface>();

        /// <summary>Draws the arms into the given box, composing them once on first use.</summary>
        public static void Draw(ICoreClientAPI capi, Context ctx, string emblem,
                                double x, double y, double width, double height)
        {
            ImageSurface img = Get(capi, emblem);
            if (img == null) return;

            var pattern = new SurfacePattern(img);
            // Good rather than Nearest: this is a downscale, and nearest sampling drops details.
            pattern.Filter = Filter.Good;

            ctx.Save();
            ctx.Translate(x, y);
            ctx.Scale(width / img.Width, height / img.Height);
            ctx.SetSource(pattern);
            ctx.Paint();
            ctx.Restore();

            pattern.Dispose();
        }

        private static ImageSurface Get(ICoreClientAPI capi, string emblem)
        {
            if (string.IsNullOrEmpty(emblem)) return null;
            if (surfaces.TryGetValue(emblem, out ImageSurface cached)) return cached;

            ImageSurface surface = EmblemImage.Compose(capi, emblem, ComposeSize);
            if (surface == null) return null;

            surfaces[emblem] = surface;
            return surface;
        }

        /// <summary>Drops everything, so the next draw rebuilds from the emblems now known.</summary>
        public static void Invalidate()
        {
            foreach (var surface in surfaces.Values) surface?.Dispose();
            surfaces.Clear();
        }
    }
}
