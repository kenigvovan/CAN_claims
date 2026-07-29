using System;
using System.Collections.Generic;
using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// The city's plots as a grid. Long stretches of unclaimed land are collapsed into a single
    /// marker column so a city spread over hundreds of blocks still fits on screen.
    ///
    /// The whole grid is drawn once into a texture; only the label under the cursor is regenerated,
    /// and only when the cursor moves to another plot.
    /// </summary>
    public class GuiElementCityMap : GuiElement
    {
        /// <summary>Gaps wider than this collapse to one marker column.</summary>
        private const int MaxGapBeforeCollapse = 5;

        public const double CellSize = 14;
        public const double CellGap = 1;
        private const double Step = CellSize + CellGap;

        private static readonly Dictionary<PlotType, double[]> PlotColors = new Dictionary<PlotType, double[]>
        {
            { PlotType.DEFAULT,        new[] { 0.35, 0.60, 0.35, 1.0 } },
            { PlotType.MAIN_CITY_PLOT, new[] { 1.00, 0.85, 0.20, 1.0 } },
            { PlotType.TAVERN,         new[] { 0.80, 0.50, 0.20, 1.0 } },
            { PlotType.PRISON,         new[] { 0.55, 0.55, 0.55, 1.0 } },
            { PlotType.EMBASSY,        new[] { 0.30, 0.60, 0.90, 1.0 } },
            { PlotType.SUMMON,         new[] { 0.70, 0.30, 0.90, 1.0 } },
            { PlotType.FARM,           new[] { 0.60, 0.85, 0.30, 1.0 } },
            { PlotType.CAMP,           new[] { 0.75, 0.55, 0.35, 1.0 } },
            { PlotType.TOURNAMENT,     new[] { 0.90, 0.30, 0.30, 1.0 } },
            { PlotType.TEMPLE,         new[] { 0.90, 0.85, 0.60, 1.0 } },
        };

        private static readonly double[] EmptyColor = { 0.25, 0.25, 0.25, 0.4 };
        private static readonly double[] GapColor = { 0.12, 0.12, 0.25, 0.7 };

        /// <summary>An axis entry: either a real plot coordinate, or a collapsed gap.</summary>
        private struct AxisEntry
        {
            public int Coord;
            public bool IsGap;
        }

        private readonly List<AxisEntry> columns = new List<AxisEntry>();
        private readonly List<AxisEntry> rows = new List<AxisEntry>();
        private readonly Dictionary<long, PlotType> plotAt = new Dictionary<long, PlotType>();

        private LoadedTexture mapTexture;
        private LoadedTexture hoverTexture;
        private string hoverText = "";

        public double MapWidth => columns.Count * Step;
        public double MapHeight => rows.Count * Step;

        /// <summary>Plot types present, with how many plots each has. Drives the legend.</summary>
        public Dictionary<PlotType, int> TypeCounts { get; } = new Dictionary<PlotType, int>();

        public static IReadOnlyDictionary<PlotType, double[]> Colors => PlotColors;
        public static double[] GapLegendColor => GapColor;

        public GuiElementCityMap(ICoreClientAPI capi, List<CityPlotMiniInfo> plots, ElementBounds bounds)
            : base(capi, bounds)
        {
            mapTexture = new LoadedTexture(capi);
            hoverTexture = new LoadedTexture(capi);

            var xs = new SortedSet<int>();
            var zs = new SortedSet<int>();
            foreach (var plot in plots)
            {
                plotAt[Key(plot.X, plot.Z)] = plot.Type;
                xs.Add(plot.X);
                zs.Add(plot.Z);

                TypeCounts.TryGetValue(plot.Type, out int count);
                TypeCounts[plot.Type] = count + 1;
            }

            BuildAxis(xs, columns);
            BuildAxis(zs, rows);
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        /// <summary>
        /// Turns the set of used coordinates into a drawing axis: short gaps are filled in so the
        /// layout stays true to scale, long ones become a single marker.
        /// </summary>
        private static void BuildAxis(SortedSet<int> values, List<AxisEntry> axis)
        {
            int prev = int.MinValue;
            foreach (int value in values)
            {
                if (prev != int.MinValue)
                {
                    if (value - prev > MaxGapBeforeCollapse)
                    {
                        axis.Add(new AxisEntry { Coord = -1, IsGap = true });
                    }
                    else
                    {
                        for (int i = prev + 1; i < value; i++) axis.Add(new AxisEntry { Coord = i, IsGap = false });
                    }
                }
                axis.Add(new AxisEntry { Coord = value, IsGap = false });
                prev = value;
            }
        }

        public override void ComposeElements(Context unusedCtx, ImageSurface unusedSurface)
        {
            Bounds.CalcWorldBounds();

            int width = (int)Math.Max(1, GuiElement.scaled(MapWidth));
            int height = (int)Math.Max(1, GuiElement.scaled(MapHeight));

            ImageSurface surface = new ImageSurface(Format.Argb32, width, height);
            Context ctx = new Context(surface);

            double cell = GuiElement.scaled(CellSize);
            double step = GuiElement.scaled(Step);

            for (int col = 0; col < columns.Count; col++)
            {
                for (int row = 0; row < rows.Count; row++)
                {
                    double x = col * step;
                    double y = row * step;

                    if (columns[col].IsGap || rows[row].IsGap)
                    {
                        Fill(ctx, x, y, cell, GapColor);
                    }
                    else if (plotAt.TryGetValue(Key(columns[col].Coord, rows[row].Coord), out PlotType type))
                    {
                        double[] color = PlotColors.TryGetValue(type, out var known) ? known : new double[] { 0.5, 0.5, 0.5, 1.0 };
                        Fill(ctx, x, y, cell, color);
                    }
                    else
                    {
                        // Unclaimed but inside the city's span: outline only.
                        ctx.SetSourceRGBA(EmptyColor[0], EmptyColor[1], EmptyColor[2], EmptyColor[3]);
                        ctx.LineWidth = 1;
                        ctx.Rectangle(x + 0.5, y + 0.5, cell - 1, cell - 1);
                        ctx.Stroke();
                    }
                }
            }

            generateTexture(surface, ref mapTexture);
            ctx.Dispose();
            surface.Dispose();
        }

        private static void Fill(Context ctx, double x, double y, double size, double[] color)
        {
            ctx.SetSourceRGBA(color[0], color[1], color[2], color[3]);
            ctx.Rectangle(x, y, size, size);
            ctx.Fill();
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.Render2DTexturePremultipliedAlpha(mapTexture.TextureId,
                (int)Bounds.renderX, (int)Bounds.renderY, mapTexture.Width, mapTexture.Height);

            UpdateHover();

            if (hoverTexture.TextureId != 0)
            {
                // Drawn just under the cursor, clear of the grid itself.
                api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId,
                    api.Input.MouseX + 12, api.Input.MouseY + 12, hoverTexture.Width, hoverTexture.Height);
            }
        }

        /// <summary>
        /// Works out which plot the cursor is over and regenerates the label only when that changes.
        /// </summary>
        private void UpdateHover()
        {
            string text = "";

            double relX = api.Input.MouseX - Bounds.renderX;
            double relY = api.Input.MouseY - Bounds.renderY;
            double step = GuiElement.scaled(Step);

            if (relX >= 0 && relY >= 0 && relX < GuiElement.scaled(MapWidth) && relY < GuiElement.scaled(MapHeight))
            {
                int col = Math.Min((int)(relX / step), columns.Count - 1);
                int row = Math.Min((int)(relY / step), rows.Count - 1);

                if (col >= 0 && row >= 0 && !columns[col].IsGap && !rows[row].IsGap
                    && plotAt.TryGetValue(Key(columns[col].Coord, rows[row].Coord), out PlotType type))
                {
                    text = string.Format("{0} ({1}, {2})", TypeName(type), columns[col].Coord, rows[row].Coord);
                }
            }

            if (text == hoverText) return;
            hoverText = text;

            if (text.Length == 0)
            {
                hoverTexture.Dispose();
                hoverTexture = new LoadedTexture(api);
                return;
            }

            CairoFont font = CairoFont.WhiteSmallText();
            TextExtents extents = font.GetTextExtents(text);
            int width = (int)extents.Width + 12;
            int height = (int)font.GetFontExtents().Height + 8;

            ImageSurface surface = new ImageSurface(Format.Argb32, width, height);
            Context ctx = new Context(surface);
            ctx.SetSourceRGBA(0, 0, 0, 0.75);
            ctx.Rectangle(0, 0, width, height);
            ctx.Fill();
            font.SetupContext(ctx);
            ctx.MoveTo(6, height - 6);
            ctx.ShowText(text);

            generateTexture(surface, ref hoverTexture);
            ctx.Dispose();
            surface.Dispose();
        }

        public static string TypeName(PlotType type)
            => Lang.Get("claims:gui-plot-type-" + type.ToString().ToLowerInvariant());

        public override void Dispose()
        {
            base.Dispose();
            mapTexture?.Dispose();
            hoverTexture?.Dispose();
        }
    }
}
