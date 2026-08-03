using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// Shared body of every claims list cell: the composed texture, the hover highlights, the
    /// click zones down the right edge, and the disposal of all four GL textures.
    ///
    /// Each of the twelve cells used to carry its own copy of this - the same ComposeHover, the
    /// same genOnTexture, the same UpdateCellHeight, the same zone hit test - about 120 duplicated
    /// lines apiece. A subclass now only draws its own contents.
    /// </summary>
    public abstract class CANGuiElementCellBase : GuiElementTextBase, IGuiElementCell, IDisposable
    {
        /// <summary>Which of the columns down the right edge a highlight covers.</summary>
        protected enum HighlightZone
        {
            Left, Middle, Right
        }

        /// <summary>
        /// Width of one click column. Bound to the vanilla constant because that is what the hit
        /// tests already used; the cells' own copies of 40.0 only drove the highlight drawing, so
        /// a mismatch meant the lit column and the clickable column drifted apart.
        /// </summary>
        protected static double UnscaledRightBoxWidth => GuiElementMainMenuCell.unscaledRightBoxWidth;

        protected const double UnscaledSwitchPadding = 4.0;
        protected const double UnscaledSwitchSize = 25.0;

        protected readonly ICoreClientAPI capi;

        /// <summary>
        /// Sub-elements a cell builds in its constructor - buttons, labels, toggles. The base
        /// composes, renders, forwards mouse events to and disposes of all of them.
        /// </summary>
        protected readonly List<GuiElement> children = new List<GuiElement>();

        /// <summary>
        /// Rich text blocks. Kept apart from <see cref="children"/> because they are composed via
        /// Compose() and need BeforeCalcBounds() when the cell is measured.
        /// </summary>
        protected readonly List<GuiElementRichtext> richTexts = new List<GuiElementRichtext>();

        /// <summary>
        /// Hover labels for the cell's own buttons. The composer's AddHoverText cannot reach inside a
        /// cell, so the cell carries them itself. Kept out of <see cref="children"/> so they never
        /// count as a click target.
        /// </summary>
        private readonly List<GuiElementHoverText> hoverTexts = new List<GuiElementHoverText>();

        public bool On;

        public Action<int> OnMouseDownOnCellLeft;
        public Action<int> OnMouseDownOnCellMiddle;
        public Action<int> OnMouseDownOnCellRight;

        private LoadedTexture cellTexture;
        private int leftHighlightTextureId;
        private int middleHighlightTextureId;
        private int rightHighlightTextureId;
        protected int switchOnTextureId;

        ElementBounds IGuiElementCell.Bounds => Bounds;

        protected CANGuiElementCellBase(ICoreClientAPI capi, ElementBounds bounds)
            : base(capi, "", null, bounds)
        {
            this.capi = capi;
            this.Font = CairoFont.WhiteSmallishText();
            cellTexture = new LoadedTexture(capi);
        }

        // ---- what a subclass provides ----

        /// <summary>Draws the cell's own contents. The border and highlights are the base's job.</summary>
        protected abstract void ComposeContent(Context ctx, ImageSurface surface);

        /// <summary>Cells refuse to shrink below this. Was a bare literal in each copy.</summary>
        protected virtual double MinCellHeight => 73.0;

        /// <summary>How many clickable columns the right edge has: 1, 2 or 3.</summary>
        protected virtual int ClickZones => 3;

        /// <summary>
        /// Whether the cell lights up under the cursor and reports zone clicks. False for cells that
        /// are just a grid of their own toggles, like the war schedule - those would otherwise build
        /// three highlight textures they never draw.
        /// </summary>
        protected virtual bool UseHoverHighlights => true;

        protected virtual bool ShowModifyIcons => true;

        /// <summary>
        /// Names one of the clickable columns on hover. Cells whose actions live on zones rather than
        /// on buttons had no way to say what a column does; the icon drawn there was the only hint.
        /// Call from the constructor.
        /// </summary>
        protected void AddZoneTooltip(HighlightZone zone, string text)
        {
            double width = Bounds.fixedWidth;
            if (width <= 0) return;

            // Fixed coordinates, so the unscaled column width is the right one here - GuiElement
            // scales them again at CalcWorldBounds, matching what ZoneAt tests against.
            double w = UnscaledRightBoxWidth;
            double x;

            switch (zone)
            {
                case HighlightZone.Right:
                    x = width - w;
                    break;
                case HighlightZone.Middle:
                    x = width - w * 2;
                    break;
                default:
                    x = 0;
                    w = ClickZones >= 3 ? width - w * 2
                      : ClickZones == 2 ? width - w
                      : width;
                    break;
            }

            if (w <= 0) return;

            AddTooltip(ElementBounds.Fixed(x, 0, w, Bounds.fixedHeight).WithParent(Bounds), text);
        }

        // ---- the coat of arms column both world lists start with ----

        /// <summary>Geometry of the column. Shared so the city and alliance lists, one tab apart,
        /// line up.</summary>
        protected const double EmblemSize = 48;
        private const double EmblemX = 12;
        private const double EmblemGap = 14;

        /// <summary>Where text starts in a row that carries arms.</summary>
        protected const double EmblemTextX = EmblemX + EmblemSize + EmblemGap;

        /// <summary>
        /// Puts the arms in their column, centred against the row height. Rows without arms keep the
        /// column and get an empty plate, so the list stays aligned. Call from the constructor.
        /// </summary>
        protected void AddEmblemColumn(ICoreClientAPI capi, string emblem, double cellHeight)
        {
            var bounds = ElementBounds
                .Fixed(EmblemX, (cellHeight - EmblemSize) / 2, EmblemSize, EmblemSize)
                .WithParent(Bounds);

            // A cell composes into a surface of its own size, so the element draws in local
            // coordinates relative to Bounds. See GuiElementEmblem.surfaceOrigin.
            children.Add(new GuiElementEmblem(capi, bounds, emblem, surfaceOrigin: Bounds));
        }

        /// <summary>Names one of the cell's buttons on hover. Call from the constructor.</summary>
        protected void AddTooltip(ElementBounds bounds, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Sized to the text, like Widgets.Tooltip: a fixed width leaves a short label sitting in
            // a wide empty box.
            CairoFont font = CairoFont.WhiteSmallText();
            double width = font.GetTextExtents(text).Width + 16;
            if (width > 320) width = 320;

            hoverTexts.Add(new GuiElementHoverText(capi, text, font, (int)width, bounds.FlatCopy()));
        }

        // ---- text, in the same palette and sizes the page cards use ----

        /// <summary>Heading line of a cell, in the accent colour.</summary>
        protected void AddTitle(string text, ElementBounds bounds, int fontSize = 20)
        {
            AddText(text, bounds, Widgets.ClaimsColors.Value, fontSize);
        }

        /// <summary>A muted line under the heading - what the cell is, in one phrase.</summary>
        protected void AddSubtitle(string text, ElementBounds bounds, int fontSize = 15)
        {
            AddText(text, bounds, Widgets.ClaimsColors.Label, fontSize);
        }

        /// <summary>Grey caption on the left of the row, coloured value right after it.</summary>
        protected void AddLabelValue(string label, string value, ElementBounds row, double labelWidth,
                                     double[] valueColor = null, int fontSize = 15)
        {
            AddText(label, row.FlatCopy().WithFixedWidth(labelWidth), Widgets.ClaimsColors.Label, fontSize);

            ElementBounds valueBounds = row.FlatCopy().WithFixedWidth(row.fixedWidth - labelWidth);
            valueBounds.fixedX += labelWidth;
            AddText(value, valueBounds, valueColor ?? Widgets.ClaimsColors.Value, fontSize);
        }

        /// <summary>One line of rich text in the given colour.</summary>
        protected void AddText(string text, ElementBounds bounds, double[] color, int fontSize = 15)
        {
            if (string.IsNullOrEmpty(text)) return;

            CairoFont font = CairoFont.WhiteMediumText().WithFontSize(fontSize).WithColor(color);
            richTexts.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, text, font), bounds));
        }

        // ---- composed once, then cached in cellTexture ----

        private void Compose()
        {
            if (UseHoverHighlights)
            {
                ComposeHover(HighlightZone.Left, ref leftHighlightTextureId);
                ComposeHover(HighlightZone.Middle, ref middleHighlightTextureId);
                ComposeHover(HighlightZone.Right, ref rightHighlightTextureId);
            }
            GenOnTexture();

            ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            Context ctx = new Context(surface);
            Bounds.CalcWorldBounds();

            ComposeContent(ctx, surface);

            // Border, drawn like a button.
            EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)GuiElement.scaled(4.0), 0);

            foreach (var child in children) child.ComposeElements(ctx, surface);
            foreach (var rich in richTexts)
            {
                rich.BeforeCalcBounds();
                rich.Compose();
            }

            generateTexture(surface, ref cellTexture);
            ctx.Dispose();
            surface.Dispose();

            ComposeHoverTexts();
        }

        /// <summary>
        /// Hover labels are set up against a throwaway surface: they draw into textures of their own,
        /// and anything they did put on the cell surface would be baked in permanently.
        /// </summary>
        private void ComposeHoverTexts()
        {
            if (hoverTexts.Count == 0) return;

            ImageSurface scratch = new ImageSurface(Format.Argb32, 1, 1);
            Context scratchCtx = new Context(scratch);
            foreach (var hover in hoverTexts)
            {
                hover.Bounds.CalcWorldBounds();
                hover.ComposeElements(scratchCtx, scratch);
            }
            scratchCtx.Dispose();
            scratch.Dispose();
        }

        private void GenOnTexture()
        {
            double size = GuiElement.scaled(UnscaledSwitchSize - 2.0 * UnscaledSwitchPadding);
            ImageSurface surface = new ImageSurface(Format.Argb32, (int)size, (int)size);
            Context ctx = genContext(surface);
            GuiElement.RoundRectangle(ctx, 0.0, 0.0, size, size, 2.0);
            GuiElement.fillWithPattern(api, ctx, GuiElement.waterTextureName);
            generateTexture(surface, ref switchOnTextureId);
            ctx.Dispose();
            surface.Dispose();
        }

        private void ComposeHover(HighlightZone zone, ref int textureId)
        {
            ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context ctx = genContext(surface);
            double w = GuiElement.scaled(UnscaledRightBoxWidth);

            // Where the left zone ends is the same boundary ZoneAt tests against, so the lit area
            // always matches the clickable one. With a single zone the whole row lights up.
            double leftEdge = ClickZones >= 3 ? Bounds.InnerWidth - w * 2
                            : ClickZones == 2 ? Bounds.InnerWidth - w
                            : Bounds.OuterWidth;

            ctx.NewPath();
            switch (zone)
            {
                case HighlightZone.Left:
                    ctx.LineTo(0.0, 0.0);
                    ctx.LineTo(leftEdge, 0.0);
                    ctx.LineTo(leftEdge, Bounds.OuterHeight);
                    ctx.LineTo(0.0, Bounds.OuterHeight);
                    break;
                case HighlightZone.Middle:
                    ctx.LineTo(Bounds.InnerWidth - w * 2, 0.0);
                    ctx.LineTo(Bounds.InnerWidth - w, 0.0);
                    ctx.LineTo(Bounds.InnerWidth - w, Bounds.OuterHeight);
                    ctx.LineTo(Bounds.InnerWidth - w * 2, Bounds.OuterHeight);
                    break;
                default:
                    ctx.LineTo(Bounds.InnerWidth - w, 0.0);
                    ctx.LineTo(Bounds.OuterWidth, 0.0);
                    ctx.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
                    ctx.LineTo(Bounds.InnerWidth - w, Bounds.OuterHeight);
                    break;
            }
            ctx.ClosePath();

            ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
            ctx.Fill();
            generateTexture(surface, ref textureId);
            ctx.Dispose();
            surface.Dispose();
        }

        public virtual void UpdateCellHeight()
        {
            Bounds.CalcWorldBounds();
            foreach (var rich in richTexts) rich.BeforeCalcBounds();
            if (ShowModifyIcons && Bounds.fixedHeight < MinCellHeight)
            {
                Bounds.fixedHeight = MinCellHeight;
            }
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            base.RenderInteractiveElements(deltaTime);
            foreach (var child in children) child.RenderInteractiveElements(deltaTime);
        }

        // ---- interaction ----

        /// <summary>
        /// Which column the cursor is over. Shared by the highlight drawing and the click handling,
        /// so the two cannot disagree.
        /// </summary>
        private HighlightZone ZoneAt(Vec2d posInside)
        {
            double w = GuiElement.scaled(UnscaledRightBoxWidth);

            if (ClickZones >= 2 && posInside.X > Bounds.InnerWidth - w) return HighlightZone.Right;
            if (ClickZones >= 3 && posInside.X > Bounds.InnerWidth - w * 2) return HighlightZone.Middle;
            return HighlightZone.Left;
        }

        public virtual void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
        {
            if (cellTexture.TextureId == 0)
            {
                Compose();
            }

            api.Render.Render2DTexturePremultipliedAlpha(cellTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);

            foreach (var child in children) child.RenderInteractiveElements(deltaTime);
            foreach (var rich in richTexts) rich.RenderInteractiveElements(deltaTime);

            // After the contents so a label is never drawn under the button it belongs to.
            foreach (var hover in hoverTexts) hover.RenderInteractiveElements(deltaTime);

            if (!UseHoverHighlights) return;

            Vec2d posInside = Bounds.PositionInside(api.Input.MouseX, api.Input.MouseY);
            if (posInside == null) return;

            int highlight;
            switch (ZoneAt(posInside))
            {
                case HighlightZone.Right: highlight = rightHighlightTextureId; break;
                case HighlightZone.Middle: highlight = middleHighlightTextureId; break;
                default: highlight = leftHighlightTextureId; break;
            }
            api.Render.Render2DTexturePremultipliedAlpha(highlight, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
        }

        public virtual void OnMouseUpOnElement(MouseEvent args, int elementIndex)
        {
            Vec2d posInside = Bounds.PositionInside(api.Input.MouseX, api.Input.MouseY);
            if (posInside == null) return;

            int mouseX = api.Input.MouseX;
            int mouseY = api.Input.MouseY;

            // A click that landed on one of the cell's own buttons belongs to that button alone.
            // Otherwise the row action would fire on top of it and, where it rebuilds the window,
            // undo whatever the button just did.
            //
            // Only the button under the cursor is told about it: a cell's children are not part of
            // the composer's own hit testing, so handing the event to all of them fires every button
            // in the row at once.
            bool onChild = false;
            foreach (var child in children)
            {
                if (!child.Bounds.PointInside(mouseX, mouseY)) continue;
                onChild = true;
                child.OnMouseUpOnElement(api, args);
            }

            if (onChild)
            {
                args.Handled = true;
                return;
            }

            if (!UseHoverHighlights) return;

            api.Gui.PlaySound("menubutton_press");

            switch (ZoneAt(posInside))
            {
                case HighlightZone.Right: OnMouseDownOnCellRight?.Invoke(elementIndex); break;
                case HighlightZone.Middle: OnMouseDownOnCellMiddle?.Invoke(elementIndex); break;
                default: OnMouseDownOnCellLeft?.Invoke(elementIndex); break;
            }
            args.Handled = true;
        }

        public virtual void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
        {
            foreach (var child in children) child.OnMouseMove(api, args);
            foreach (var hover in hoverTexts) hover.OnMouseMove(api, args);
        }

        public virtual void OnMouseDownOnElement(MouseEvent args, int elementIndex)
        {
            foreach (var child in children)
            {
                if (child.Bounds.PointInside(args.X, args.Y)) child.OnMouseDownOnElement(api, args);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var child in children) child.Dispose();
            foreach (var rich in richTexts) rich.Dispose();
            foreach (var hover in hoverTexts) hover.Dispose();
            cellTexture?.Dispose();
            api.Render.GLDeleteTexture(leftHighlightTextureId);
            api.Render.GLDeleteTexture(middleHighlightTextureId);
            api.Render.GLDeleteTexture(rightHighlightTextureId);
            api.Render.GLDeleteTexture(switchOnTextureId);
        }
    }
}
