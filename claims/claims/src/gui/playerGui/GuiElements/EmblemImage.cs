using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cairo;
using claims.src.part.structure;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// Composes a coat of arms with Cairo, layer over layer, onto a surface of its own. Decodes a
    /// PNG per layer, so callers use <see cref="EmblemCache"/> rather than this directly.
    /// </summary>
    public static class EmblemImage
    {
        /// <summary>Paints the emblem at the given box. A layer that fails to load is skipped.</summary>
        private static void Draw(ICoreClientAPI capi, Context ctx, string emblem,
                                 double x, double y, double width, double height)
        {
            foreach (string layer in EmblemHandler.Parse(emblem))
            {
                DrawLayer(capi, ctx, layer, x, y, width, height);
            }
        }

        /// <summary>The emblem on its own surface, or null when it has no drawable layers.</summary>
        public static ImageSurface Compose(ICoreClientAPI capi, string emblem, int size)
        {
            List<string> layers = EmblemHandler.Parse(emblem);
            if (layers.Count == 0) return null;

            var surface = new ImageSurface(Format.Argb32, size, size);
            var ctx = new Context(surface);
            Draw(capi, ctx, emblem, 0, 0, size, size);
            ctx.Dispose();
            return surface;
        }

        private static void DrawLayer(ICoreClientAPI capi, Context ctx, string layer,
                                      double x, double y, double width, double height)
        {
            ImageSurface img;
            try
            {
                img = LoadPremultiplied(capi, new AssetLocation("claims", EmblemHandler.TexturePath(layer)));
            }
            catch
            {
                // Whitelisted pattern with no file behind it (partial asset copy, resource pack).
                capi.Logger.Warning("[claims] emblem layer texture '{0}' could not be loaded", layer);
                return;
            }

            // Nearest keeps the pixel-art outlines sharp; the source is built by hand because
            // SetSourceSurface gives no way to set the filter.
            SurfacePattern pattern = new SurfacePattern(img);
            pattern.Filter = Filter.Nearest;

            ctx.Save();
            ctx.Translate(x, y);
            ctx.Scale(width / img.Width, height / img.Height);
            ctx.SetSource(pattern);
            ctx.Paint();
            ctx.Restore();

            pattern.Dispose();
            img.Dispose();
        }

        /// <summary>
        /// Loads a layer texture into a Cairo surface, premultiplying its alpha on the way.
        ///
        /// GuiElement.getImageSurfaceFromAsset copies the decoded pixels verbatim, but the engine
        /// decodes PNGs unpremultiplied while Cairo's Argb32 expects premultiplied. Transparent
        /// pixels then bleed their colour into whatever is under them.
        /// </summary>
        private static ImageSurface LoadPremultiplied(ICoreClientAPI capi, AssetLocation location)
        {
            byte[] data = capi.Assets.Get(location.Clone().WithPathPrefixOnce("textures/")).Data;
            BitmapExternal bitmap = capi.Render.BitmapCreateFromPng(data);

            try
            {
                int[] pixels = bitmap.Pixels;
                for (int i = 0; i < pixels.Length; i++)
                {
                    uint pixel = (uint)pixels[i];
                    uint a = pixel >> 24;
                    if (a == 255) continue;
                    if (a == 0)
                    {
                        // Colour behind zero alpha is what bleeds - drop it.
                        pixels[i] = 0;
                        continue;
                    }

                    uint r = ((pixel >> 16) & 0xFF) * a / 255;
                    uint g = ((pixel >> 8) & 0xFF) * a / 255;
                    uint b = (pixel & 0xFF) * a / 255;
                    pixels[i] = (int)((a << 24) | (r << 16) | (g << 8) | b);
                }

                var surface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
                // Row by row against the surface stride - Cairo may pad rows.
                int stride = surface.Stride;
                for (int row = 0; row < bitmap.Height; row++)
                {
                    Marshal.Copy(pixels, row * bitmap.Width,
                        surface.DataPtr + row * stride, bitmap.Width);
                }
                surface.MarkDirty();
                return surface;
            }
            finally
            {
                bitmap.Dispose();
            }
        }
    }
}
