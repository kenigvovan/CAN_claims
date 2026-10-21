using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace claims.src.gui.plotMovementGui
{
    /// <summary>
    /// Plot borders as a grid of lines, taken over from canclaimsextended - in place of the
    /// world-high translucent box the server's /plot borders highlights. Drawn on the client from the
    /// plots it already has: the edges of every city's land around the player, where it meets another
    /// city or the wild, and the four sides of the plot underfoot even inside a city. A band of the
    /// world around the player's height, not the whole column, so it does not wall off the view.
    /// </summary>
    public class PlotBorderRenderer : IRenderer
    {
        private const int RadiusPlots = 3;
        private const int Down = 4;
        private const int Up = 12;
        private const int Step = 4;
        private const float LineWidth = 2f;
        // Lines sit this far inside their plot, so two cities meeting on one edge draw side by side.
        private const float Inset = 0.05f;
        // As vanilla wireframes: the shader draws such vertices a little closer, over the blocks.
        private const int WireframeFlags = 256;
        // Rebuilt at least this often, so a plot claimed or sold nearby shows without walking off.
        private const long RefreshMs = 2000;

        private readonly ICoreClientAPI capi;
        private readonly ClaimsClientPrefs prefs;
        private readonly Matrixf mvMat = new Matrixf();
        private MeshRef mesh;
        private int builtX = int.MinValue, builtZ, builtY;
        private long builtAt;
        // Mesh coordinates are relative to this block, so floats stay small far from the origin.
        private int originX, originY, originZ;

        public double RenderOrder => 0.5;
        public int RenderRange => (RadiusPlots + 1) * Math.Max(PlotPosition.plotSize, 1);

        public PlotBorderRenderer(ICoreClientAPI capi, ClaimsClientPrefs prefs)
        {
            this.capi = capi;
            this.prefs = prefs;
        }

        public bool Toggle()
        {
            prefs.ShowPlotBorders = !prefs.ShowPlotBorders;
            prefs.Save(capi);
            builtX = int.MinValue;
            return prefs.ShowPlotBorders;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            var entity = capi.World?.Player?.Entity;
            int size = PlotPosition.plotSize;
            if (!prefs.ShowPlotBorders || entity == null || size <= 0 || claims.clientDataStorage == null) return;

            // Plot keys are made by integer division, as everywhere else the client looks them up.
            int cx = (int)entity.Pos.X / size;
            int cz = (int)entity.Pos.Z / size;
            int baseY = (int)Math.Floor(entity.Pos.Y / Step) * Step;
            long now = capi.ElapsedMilliseconds;
            if (cx != builtX || cz != builtZ || baseY != builtY || now - builtAt > RefreshMs)
            {
                Rebuild(cx, cz, baseY, size, now);
            }
            if (mesh == null) return;

            IShaderProgram program = capi.Shader.GetProgram((int)EnumShaderProgram.Wireframe);
            program.Use();
            capi.Render.LineWidth = LineWidth;
            capi.Render.GLEnableDepthTest();
            capi.Render.GLDepthMask(false);
            capi.Render.GlToggleBlend(true, EnumBlendMode.Standard);
            Vec3d cam = entity.CameraPos;
            mvMat.Identity().Set(capi.Render.CameraMatrixOrigin).Translate(originX - cam.X, originY - cam.Y, originZ - cam.Z);
            program.Uniform("origin", 0f, 0f, 0f);
            program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
            program.UniformMatrix("modelViewMatrix", mvMat.Values);
            // The vertex colours tell own city, other cities and the wild apart.
            program.Uniform("colorIn", ColorUtil.WhiteArgbVec);
            capi.Render.RenderMesh(mesh);
            program.Stop();
            capi.Render.LineWidth = 1.6f;
            capi.Render.GLDepthMask(true);
        }

        private void Rebuild(int cx, int cz, int baseY, int size, long now)
        {
            builtX = cx;
            builtZ = cz;
            builtY = baseY;
            builtAt = now;
            originX = cx * size;
            originZ = cz * size;
            originY = Math.Max(0, baseY - Down);
            int height = Math.Min(capi.World.BlockAccessor.MapSizeY, baseY + Up) - originY;

            var xyz = new List<float>();
            var colors = new List<int>();
            if (height > 0)
            {
                var subPlotPieces = new Dictionary<string, (List<subplots.geometry.BlockBox> Boxes, bool MayBuild)>();
                for (int dx = -RadiusPlots; dx <= RadiusPlots; dx++)
                {
                    for (int dz = -RadiusPlots; dz <= RadiusPlots; dz++)
                    {
                        AddPlotEdges(cx + dx, cz + dz, size, height, dx == 0 && dz == 0, xyz, colors);
                        CollectSubPlotPieces(cx + dx, cz + dz, subPlotPieces);
                    }
                }
                AddSubPlotBoxes(subPlotPieces, xyz, colors);
            }

            mesh?.Dispose();
            mesh = null;
            int count = colors.Count;
            if (count == 0) return;
            var data = new MeshData(count, count, false, false, true, true);
            data.SetMode(EnumDrawMode.Lines);
            for (int i = 0; i < count; i++)
            {
                data.AddVertexSkipTex(xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2], colors[i]);
                data.AddIndex(i);
            }
            for (int i = 0; i < count; i++) data.Flags[i] = WireframeFlags;
            mesh = capi.Render.UploadMesh(data);
        }

        /// <summary>
        /// The sides of one plot that face another city or the wild; for the plot underfoot all four,
        /// wild ground included - seeing where a chunk ends is what a player wants before claiming it.
        /// </summary>
        private void AddPlotEdges(int x, int z, int size, int height, bool underfoot,
                                  List<float> xyz, List<int> colors)
        {
            string city = CityAt(x, z);
            if (city.Length == 0 && !underfoot) return;
            int color = Color(city);

            float x0 = x * size - originX, z0 = z * size - originZ;
            float x1 = x0 + size, z1 = z0 + size;
            if (underfoot || CityAt(x + 1, z) != city) AddSide(x1 - Inset, z0, x1 - Inset, z1, height, color, xyz, colors);
            if (underfoot || CityAt(x - 1, z) != city) AddSide(x0 + Inset, z0, x0 + Inset, z1, height, color, xyz, colors);
            if (underfoot || CityAt(x, z + 1) != city) AddSide(x0, z1 - Inset, x1, z1 - Inset, height, color, xyz, colors);
            if (underfoot || CityAt(x, z - 1) != city) AddSide(x0, z0 + Inset, x1, z0 + Inset, height, color, xyz, colors);
        }

        // Sub-plot outlines: green where this player may build, amber where not.
        private static readonly int SubPlotMayBuild = ColorUtil.ToRgba(230, 90, 230, 110);
        private static readonly int SubPlotShut = ColorUtil.ToRgba(230, 250, 170, 40);

        /// <summary>The sub-plot pieces in one plot, gathered by sub-plot so its pieces can be joined again.</summary>
        private static void CollectSubPlotPieces(int x, int z,
            Dictionary<string, (List<subplots.geometry.BlockBox> Boxes, bool MayBuild)> into)
        {
            if (!claims.clientDataStorage.getSavedPlot(new Vec2i(x, z), out SavedPlotInfo plot) || plot == null) return;
            foreach (var piece in plot.SubPlotPieces())
            {
                // Names are unique within a city only.
                string key = plot.cityName + "|" + piece.Name;
                if (!into.TryGetValue(key, out var entry))
                {
                    entry = (new List<subplots.geometry.BlockBox>(), piece.May(1));
                    into[key] = entry;
                }
                entry.Boxes.Add(new subplots.geometry.BlockBox(piece.MinX, piece.MinY, piece.MinZ, piece.MaxX, piece.MaxY, piece.MaxZ));
            }
        }

        /// <summary>
        /// The twelve edges of each sub-plot box, whole height, not only the band around the player: a
        /// flat is small, and its floor and ceiling are the point. The pieces cut along chunk lines are
        /// joined first, so a flat over two chunks shows no wall where the chunks meet.
        /// </summary>
        private void AddSubPlotBoxes(Dictionary<string, (List<subplots.geometry.BlockBox> Boxes, bool MayBuild)> pieces,
                                     List<float> xyz, List<int> colors)
        {
            foreach (var (boxes, mayBuild) in pieces.Values)
            foreach (var piece in subplots.geometry.BoxShape.MergeAligned(boxes))
            {
                int color = mayBuild ? SubPlotMayBuild : SubPlotShut;
                float x0 = piece.MinX - originX + Inset, x1 = piece.MaxX - originX - Inset;
                float y0 = piece.MinY - originY + Inset, y1 = piece.MaxY - originY - Inset;
                float z0 = piece.MinZ - originZ + Inset, z1 = piece.MaxZ - originZ - Inset;
                // Floor and ceiling rings, then the four uprights.
                foreach (float y in new[] { y0, y1 })
                {
                    AddLine(x0, y, z0, x1, y, z0, color, xyz, colors);
                    AddLine(x1, y, z0, x1, y, z1, color, xyz, colors);
                    AddLine(x1, y, z1, x0, y, z1, color, xyz, colors);
                    AddLine(x0, y, z1, x0, y, z0, color, xyz, colors);
                }
                AddLine(x0, y0, z0, x0, y1, z0, color, xyz, colors);
                AddLine(x1, y0, z0, x1, y1, z0, color, xyz, colors);
                AddLine(x1, y0, z1, x1, y1, z1, color, xyz, colors);
                AddLine(x0, y0, z1, x0, y1, z1, color, xyz, colors);
            }
        }

        private static string CityAt(int x, int z)
        {
            return claims.clientDataStorage.getSavedPlot(new Vec2i(x, z), out SavedPlotInfo plot) && plot != null
                ? plot.cityName ?? ""
                : "";
        }

        /// <summary>
        /// A city is drawn in its own map colour; the wild uses the colour of the old highlight
        /// (a, r, g, b in the config). Both made opaque enough for lines.
        /// </summary>
        private static int Color(string city)
        {
            if (city.Length > 0 && claims.clientDataStorage != null)
            {
                // City colours are stored byte-reversed for the map texture (red in the low byte);
                // the line mesh takes ColorUtil.ToRgba order, red in bits 16-23.
                int rgb = ColorUtil.ReverseColorBytes(claims.clientDataStorage.ClientGetCityColor(city));
                return (rgb & 0x00FFFFFF) | (220 << 24);
            }
            int[] rgba = claims.config?.PLOT_BORDERS_COLOR_WILD_PLOT;
            if (rgba == null || rgba.Length < 4) return ColorUtil.ToRgba(220, 255, 255, 255);
            return ColorUtil.ToRgba(220, rgba[1], rgba[2], rgba[3]);
        }

        /// <summary>A grid on one side: across every Step blocks of height, upright every Step blocks along.</summary>
        private static void AddSide(float ax, float az, float bx, float bz, int height, int color, List<float> xyz, List<int> colors)
        {
            for (int y = 0; y <= height; y += Step)
            {
                AddLine(ax, y, az, bx, y, bz, color, xyz, colors);
            }
            float length = Math.Max(Math.Abs(bx - ax), Math.Abs(bz - az));
            for (float t = 0; t <= length + 0.01f; t += Step)
            {
                float f = length > 0 ? t / length : 0;
                float px = ax + (bx - ax) * f, pz = az + (bz - az) * f;
                AddLine(px, 0, pz, px, height, pz, color, xyz, colors);
            }
        }

        private static void AddLine(float ax, float ay, float az, float bx, float by, float bz, int color, List<float> xyz, List<int> colors)
        {
            xyz.Add(ax); xyz.Add(ay); xyz.Add(az);
            xyz.Add(bx); xyz.Add(by); xyz.Add(bz);
            colors.Add(color);
            colors.Add(color);
        }

        public void Dispose()
        {
            mesh?.Dispose();
            mesh = null;
        }
    }
}
