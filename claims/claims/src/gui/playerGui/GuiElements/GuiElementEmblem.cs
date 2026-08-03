using System.Collections.Generic;
using Cairo;
using claims.src.part.structure;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// Draws a coat of arms, scaled to the element's bounds.
    ///
    /// Composes into the parent surface and holds no GL texture, so it works both as a composer
    /// element and as a child of a list cell.
    /// </summary>
    public class GuiElementEmblem : GuiElement
    {
        private readonly List<string> layers;

        /// <summary>Whether to fill the bounds with a dark plate when the part has no emblem yet.</summary>
        private readonly bool drawPlaceholder;

        /// <summary>
        /// Origin of the target surface when it is not the window: a list cell composes into a
        /// surface of its own size, so window coordinates would land outside it. Null for a plain
        /// composer element, whose surface shares the window's origin.
        /// </summary>
        private readonly ElementBounds surfaceOrigin;

        public GuiElementEmblem(ICoreClientAPI capi, ElementBounds bounds, string emblem,
                                bool drawPlaceholder = true, ElementBounds surfaceOrigin = null)
            : base(capi, bounds)
        {
            // Unknown layers are dropped here rather than resolved, so a bad string from the network
            // can never turn into an asset path.
            this.layers = EmblemHandler.Parse(emblem);
            this.drawPlaceholder = drawPlaceholder;
            this.surfaceOrigin = surfaceOrigin;
        }

        private double DrawX => Bounds.drawX - (surfaceOrigin?.drawX ?? 0);
        private double DrawY => Bounds.drawY - (surfaceOrigin?.drawY ?? 0);

        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();

            if (layers.Count == 0)
            {
                if (drawPlaceholder) DrawPlaceholder(ctx);
                return;
            }

            // Through the cache: a window recomposes on every click, decoding layer PNGs each time
            // would stutter.
            EmblemCache.Draw(api as ICoreClientAPI, ctx, string.Join(EmblemHandler.LAYER_DELIMITER.ToString(), layers),
                DrawX, DrawY, Bounds.OuterWidth, Bounds.OuterHeight);
        }


        /// <summary>
        /// What a part without arms looks like: a blank plate with a hairline border, so a row keeps
        /// its shape and reads as "no arms yet" rather than as a rendering failure.
        /// </summary>
        private void DrawPlaceholder(Context ctx)
        {
            ctx.Save();

            RoundRectangle(ctx, DrawX, DrawY, Bounds.OuterWidth, Bounds.OuterHeight, 2.0);
            ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.25);
            ctx.FillPreserve();

            ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.12);
            ctx.LineWidth = 1.0;
            ctx.Stroke();

            ctx.Restore();
        }
    }

    public static class GuiElementEmblemHelper
    {
        /// <summary>Adds a coat of arms to the composer. Key it per page like any other element.</summary>
        public static GuiComposer AddEmblem(this GuiComposer composer, string emblem, ElementBounds bounds,
                                            string key = null, bool drawPlaceholder = true)
        {
            if (!composer.Composed)
            {
                composer.AddStaticElement(new GuiElementEmblem(composer.Api, bounds, emblem, drawPlaceholder), key);
            }
            return composer;
        }
    }
}
