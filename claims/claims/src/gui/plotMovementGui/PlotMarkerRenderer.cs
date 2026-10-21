using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.auxialiry.innerclaims;
using claims.src.clientMapHandling;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.plotMovementGui
{
    /// <summary>
    /// Small labelled squares floating over the claimed plots around the player, taken over from
    /// canclaimsextended: one per flag that is on - pvp, fire, blasts, no mobs - and three for what
    /// this player may do there - use, build, attack - green when allowed, grey when not. Above them
    /// the price when the plot is for sale, and its name or group. Drawn like vanilla name tags: a
    /// world point projected to the screen, so they always face the camera.
    /// Off by default, switched with .claimsmarkers or the player page.
    /// </summary>
    public class PlotMarkerRenderer : IRenderer
    {
        private const int Range = 48;
        private const int MaxPlots = 12;
        private const int SquarePx = 26;
        private const int Gap = 3;

        private readonly ICoreClientAPI capi;
        private readonly ClaimsClientPrefs prefs;
        private readonly Dictionary<string, LoadedTexture> textures = new();

        public double RenderOrder => 0.5;
        public int RenderRange => Range;

        private static readonly double[] PvpColor = { 0.80, 0.15, 0.15, 0.9 };
        private static readonly double[] FireColor = { 0.95, 0.50, 0.10, 0.9 };
        private static readonly double[] BlastColor = { 0.90, 0.80, 0.10, 0.9 };
        private static readonly double[] NoMobsColor = { 0.55, 0.25, 0.75, 0.9 };
        private static readonly double[] Allowed = { 0.20, 0.65, 0.25, 0.9 };
        private static readonly double[] Denied = { 0.35, 0.35, 0.35, 0.8 };
        private static readonly double[] PriceColor = { 0.85, 0.65, 0.15, 0.95 };
        private static readonly double[] NameColor = { 0.20, 0.35, 0.55, 0.9 };

        public PlotMarkerRenderer(ICoreClientAPI capi, ClaimsClientPrefs prefs)
        {
            this.capi = capi;
            this.prefs = prefs;
        }

        public bool Toggle()
        {
            prefs.ShowPlotMarkers = !prefs.ShowPlotMarkers;
            prefs.Save(capi);
            return prefs.ShowPlotMarkers;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!prefs.ShowPlotMarkers || capi.HideGuis) return;
            var entity = capi.World?.Player?.Entity;
            int size = PlotPosition.plotSize;
            if (entity == null || claims.clientDataStorage == null || size <= 0) return;

            Vec3d eye = entity.Pos.XYZ.Add(entity.LocalEyePos);
            // Plot keys are made by integer division, as everywhere else the client looks them up.
            int cx = (int)eye.X / size, cz = (int)eye.Z / size;
            int reach = Range / size + 1;

            var near = new List<(SavedPlotInfo Plot, Vec3d Anchor, double Dist, bool Underfoot)>();
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dz = -reach; dz <= reach; dz++)
                {
                    var pos = new Vec2i(cx + dx, cz + dz);
                    if (!claims.clientDataStorage.getSavedPlot(pos, out SavedPlotInfo plot) || plot == null) continue;
                    var anchor = new Vec3d(pos.X * size + size / 2.0, eye.Y + 1, pos.Y * size + size / 2.0);
                    double dist = Math.Sqrt((anchor.X - eye.X) * (anchor.X - eye.X) + (anchor.Z - eye.Z) * (anchor.Z - eye.Z));
                    if (dist <= Range) near.Add((plot, anchor, dist, dx == 0 && dz == 0));
                }
            }
            near.Sort((a, b) => a.Dist.CompareTo(b.Dist));

            for (int i = 0; i < near.Count && i < MaxPlots; i++)
            {
                var (plot, anchor, _, underfoot) = near[i];
                if (!Project(anchor, out float x, out float y, out float scale)) continue;
                float gap = (float)GuiElement.scaled(Gap) * scale;
                // Everyone's rights only for the plot underfoot: twelve tables at once would be noise,
                // and the plot page has the full table for any plot.
                if (underfoot) DrawGroupRights(plot, x, y + gap, scale);
                float top = y - DrawRow(plot, x, y, scale) - gap;
                if (plot.price > -1)
                {
                    top -= DrawTag(Lang.Get("claims:movementgui-price", plot.price), "price-" + plot.price, PriceColor, x, top, scale) + gap;
                }
                string name = !string.IsNullOrEmpty(plot.groupName) ? StringFunctions.replaceUnderscore(plot.groupName)
                            : plot.plotName ?? "";
                if (name.Length > 0) DrawTag(name, "name-" + name, NameColor, x, top, scale);
            }

            DrawSubPlots(near, eye);
        }

        private const int MaxSubPlots = 8;
        private static readonly double[] SubPlotColor = { 0.45, 0.30, 0.15, 0.9 };

        /// <summary>
        /// One marker per sub-plot nearby, over the middle of its top: its name and owner, the price
        /// when for sale, and what this player may do inside. A sub-plot over several plots comes in
        /// several pieces; the one nearest the player carries the marker.
        /// </summary>
        private void DrawSubPlots(List<(SavedPlotInfo Plot, Vec3d Anchor, double Dist, bool Underfoot)> near, Vec3d eye)
        {
            var nearest = new Dictionary<string, (ClientInnerClaim Piece, Vec3d Anchor, double Dist)>();
            foreach (var entry in near)
            {
                foreach (ClientInnerClaim piece in entry.Plot.SubPlotPieces())
                {
                    var anchor = new Vec3d((piece.MinX + piece.MaxX) / 2.0, piece.MaxY + 0.5, (piece.MinZ + piece.MaxZ) / 2.0);
                    double dist = anchor.DistanceTo(eye);
                    if (dist > Range) continue;
                    string key = entry.Plot.cityName + "|" + piece.Name;
                    if (!nearest.TryGetValue(key, out var best) || dist < best.Dist) nearest[key] = (piece, anchor, dist);
                }
            }

            var shown = new List<(ClientInnerClaim Piece, Vec3d Anchor, double Dist)>(nearest.Values);
            shown.Sort((a, b) => a.Dist.CompareTo(b.Dist));
            for (int i = 0; i < shown.Count && i < MaxSubPlots; i++)
            {
                var (piece, anchor, _) = shown[i];
                if (!Project(anchor, out float x, out float y, out float scale)) continue;
                float gap = (float)GuiElement.scaled(Gap) * scale;
                var row = new List<LoadedTexture>
                {
                    Square("marker-use", "U", piece.May(0)),
                    Square("marker-build", "Bu", piece.May(1)),
                    Square("marker-attack", "A", piece.May(2)),
                };
                float top = y - DrawSquares(row, x, y, scale, 1f) - gap;
                string offer = subplots.SubPlotText.Offer(piece.Price, piece.Tax);
                if (offer != null)
                {
                    top -= DrawTag(offer, "offer-" + offer, PriceColor, x, top, scale) + gap;
                }
                string label = piece.Owner.Length > 0 ? piece.Name + " · " + piece.Owner : piece.Name;
                DrawTag(label, "subplot-" + label, SubPlotColor, x, top, scale);
            }
        }

        /// <summary>A world point on the screen; false when it is behind the camera.</summary>
        private bool Project(Vec3d point, out float x, out float y, out float scale)
        {
            IRenderAPI rapi = capi.Render;
            Vec3d screen = MatrixToolsd.Project(point, rapi.PerspectiveProjectionMat, rapi.PerspectiveViewMat, rapi.FrameWidth, rapi.FrameHeight);
            x = (float)screen.X;
            y = rapi.FrameHeight - (float)screen.Y;
            scale = GameMath.Clamp(8f / (float)Math.Max(1, screen.Z), 0.5f, 1f);
            return screen.Z >= 0;
        }

        /// <summary>The flag and right squares with their bottom at y; returns their height on screen.</summary>
        private float DrawRow(SavedPlotInfo plot, float centerX, float y, float scale)
        {
            var row = new List<LoadedTexture>();
            if (plot.PvPIsOn) row.Add(Square("marker-pvp", "PvP", PvpColor));
            if (plot.FireIsOn) row.Add(Square("marker-fire", "F", FireColor));
            if (plot.BlastIsOn) row.Add(Square("marker-blast", "B", BlastColor));
            if (plot.NoMobSpawn) row.Add(Square("marker-nomobs", "M", NoMobsColor));
            row.Add(Square("marker-use", "U", plot.useFlag));
            row.Add(Square("marker-build", "Bu", plot.buildFlag));
            row.Add(Square("marker-attack", "A", plot.attackAnimalsFlag));

            return DrawSquares(row, centerX, y, scale, 1f);
        }

        // Short labels of the groups, in SavedPlotInfo.RightsGroupOrder: friend, citizen, ally, stranger.
        private static readonly (string Key, string Fallback)[] GroupLabels =
        {
            ("marker-group-friend", "Fr"), ("marker-group-citizen", "Ci"), ("marker-group-ally", "Al"), ("marker-group-stranger", "St")
        };
        private static readonly double[] GroupLabelColor = { 0.15, 0.15, 0.20, 0.9 };

        /// <summary>
        /// What each group may do on this plot, one small line per group under the viewer's own row,
        /// starting at topY; returns the height taken.
        /// </summary>
        private float DrawGroupRights(SavedPlotInfo plot, float centerX, float topY, float scale)
        {
            const float small = 0.7f;
            float gap = (float)GuiElement.scaled(Gap) * scale * small;
            float y = topY;
            for (int g = 0; g < GroupLabels.Length; g++)
            {
                var row = new List<LoadedTexture>
                {
                    Square(GroupLabels[g].Key, GroupLabels[g].Fallback, GroupLabelColor),
                    Square("marker-use", "U", plot.GroupMay(g, 0)),
                    Square("marker-build", "Bu", plot.GroupMay(g, 1)),
                    Square("marker-attack", "A", plot.GroupMay(g, 2)),
                };
                float h = DrawSquares(row, centerX, y + (float)GuiElement.scaled(SquarePx) * scale * small, scale, small);
                y += h + gap;
            }
            return y - topY;
        }

        /// <summary>
        /// A centred row of squares with its bottom at y; returns its height on screen. A square is as
        /// wide as its label needs - "PvP" did not fit one of the letter's width.
        /// </summary>
        private float DrawSquares(List<LoadedTexture> row, float centerX, float y, float scale, float size)
        {
            // In screen pixels, so the squares follow the GUI scale like the textures do.
            float side = (float)GuiElement.scaled(SquarePx) * scale * size;
            float gap = (float)GuiElement.scaled(Gap) * scale * size;
            float total = -gap;
            foreach (LoadedTexture tex in row) total += Width(tex, side) + gap;
            float x = centerX - total / 2;
            foreach (LoadedTexture tex in row)
            {
                float w = Width(tex, side);
                capi.Render.Render2DTexture(tex.TextureId, x, y - side, w, side, 20f);
                x += w + gap;
            }
            return side;
        }

        /// <summary>A texture's width on screen at the given height, keeping its proportions.</summary>
        private static float Width(LoadedTexture tex, float height) =>
            tex.Height > 0 ? height * tex.Width / tex.Height : height;

        private LoadedTexture Square(string langKey, string fallback, bool allowed) =>
            Square(langKey + (allowed ? "-yes" : "-no"), langKey, fallback, allowed ? Allowed : Denied);

        private LoadedTexture Square(string langKey, string fallback, double[] color) =>
            Square(langKey, langKey, fallback, color);

        /// <summary>A square with a short label, made once and kept.</summary>
        private LoadedTexture Square(string cacheKey, string langKey, string fallback, double[] color)
        {
            if (textures.TryGetValue(cacheKey, out LoadedTexture tex)) return tex;
            string label = Lang.GetIfExists("claims:" + langKey) ?? fallback;
            var background = new TextBackground
            {
                FillColor = color,
                Radius = 3,
                BorderWidth = 1,
                BorderColor = new[] { 0.0, 0.0, 0.0, 0.8 }
            };
            int px = (int)GuiElement.scaled(SquarePx);
            CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
            // Wider than square when the label needs it: "PvP" ran over the edges of a square.
            // Extents come unscaled, as Card.Fit uses them; the texture is in screen pixels.
            int width = Math.Max(px, (int)Math.Ceiling(GuiElement.scaled(font.GetTextExtents(label).Width + 8)));
            tex = capi.Gui.TextTexture.GenTextTexture(label, font, width, px, background, EnumTextOrientation.Center);
            textures[cacheKey] = tex;
            return tex;
        }

        /// <summary>Text on a coloured plate, centred with its bottom at bottomY; returns its height on screen.</summary>
        private float DrawTag(string label, string cacheKey, double[] color, float centerX, float bottomY, float scale)
        {
            if (!textures.TryGetValue(cacheKey, out LoadedTexture tex))
            {
                var background = new TextBackground
                {
                    FillColor = color,
                    Padding = 3,
                    Radius = 3,
                    BorderWidth = 1,
                    BorderColor = new[] { 0.0, 0.0, 0.0, 0.8 }
                };
                tex = capi.Gui.TextTexture.GenTextTexture(label, CairoFont.WhiteSmallText(), background);
                textures[cacheKey] = tex;
            }
            float w = tex.Width * scale;
            float h = tex.Height * scale;
            capi.Render.Render2DTexture(tex.TextureId, centerX - w / 2, bottomY - h, w, h, 20f);
            return h;
        }

        public void Dispose()
        {
            foreach (LoadedTexture tex in textures.Values)
            {
                tex.Dispose();
            }
            textures.Clear();
        }
    }
}
