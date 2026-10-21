using System.Collections.Generic;
using System.Globalization;
using System.Text;
using claims.src.part;
using claims.src.perms;
using claims.src.subplots.geometry;
using Vintagestory.API.MathTools;

namespace claims.src.subplots
{
    /// <summary>
    /// A piece of a city cut out in boxes - a flat on one floor, a cellar, a shop in a shared house -
    /// with an owner, a price and rules of its own. It may reach over several chunks, all of the same
    /// city and none bought by a citizen (see <see cref="SubPlotRules"/>). Inside it its rules come
    /// first; outside it, above and below included, the chunk's.
    /// </summary>
    public class SubPlot : Part
    {
        public City City { get; set; }
        /// <summary>Null while the city still holds it.</summary>
        public PlayerInfo Owner { get; set; }
        public BoxShape Shape { get; set; }
        /// <summary>For sale at this price; -1 when not.</summary>
        public double Price { get; set; } = -1;
        /// <summary>What each group may do inside: citizen, stranger, ally, friend.</summary>
        public PermsHandler PermsHandler { get; set; } = new PermsHandler();
        /// <summary>Who the owner shares it with; they may do everything the owner may.</summary>
        public HashSet<PlayerInfo> Members { get; } = new HashSet<PlayerInfo>();
        public long TimeStampClaimed { get; set; }
        /// <summary>What the owner paid, the base of the refund when they give it back.</summary>
        public long LastPaidPrice { get; set; }
        /// <summary>
        /// The plots group it belongs to, or null: the group's members get the group's rights inside,
        /// and pay its fee as for any of its land.
        /// </summary>
        public CityPlotsGroup Group { get; set; }
        /// <summary>What the owner pays the city each day, on top of the city fee; 0 for none.</summary>
        public double Tax { get; set; }
        /// <summary>An announced raise of <see cref="Tax"/>, -1 when none; see SubPlotTaxHelper.</summary>
        public double PendingTax { get; set; } = -1;
        /// <summary>When the announced raise lands, unix seconds.</summary>
        public long PendingTaxAt { get; set; }
        /// <summary>Whether the owner agreed to the announced raise.</summary>
        public bool PendingTaxAccepted { get; set; }
        public bool HasPendingTax => PendingTax >= 0;

        public void ClearPendingTax()
        {
            PendingTax = -1;
            PendingTaxAt = 0;
            PendingTaxAccepted = false;
        }

        public SubPlot(string name, string guid) : base(name, guid)
        {
        }

        public bool HasOwner => Owner != null;
        public bool IsForSale => Price >= 0;

        /// <summary>The chunks the shape reaches into, as plot positions.</summary>
        public HashSet<Vec2i> Chunks(int plotSize) => ChunksOf(Shape, plotSize);

        public static HashSet<Vec2i> ChunksOf(BoxShape shape, int plotSize)
        {
            var chunks = new HashSet<Vec2i>();
            if (shape == null || plotSize <= 0) return chunks;
            foreach (BlockBox box in shape.Boxes)
            {
                // The last block inside is Max - 1. Plain division, as PlotPosition.fromXZ does it:
                // a chunk must come out the same here as everywhere else the mod looks one up.
                int x0 = box.MinX / plotSize, x1 = (box.MaxX - 1) / plotSize;
                int z0 = box.MinZ / plotSize, z1 = (box.MaxZ - 1) / plotSize;
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        chunks.Add(new Vec2i(x, z));
                    }
                }
            }
            return chunks;
        }

        /// <summary>The server's world height; 256 without a running world.</summary>
        public static int WorldHeight
        {
            get
            {
                int height = claims.sapi?.WorldManager?.MapSizeY ?? 0;
                return height > 0 ? height : 256;
            }
        }

        /// <summary>The column of one chunk, the whole world height, as a box to clip shapes against.</summary>
        public static BlockBox ChunkColumn(Vec2i chunk, int plotSize, int worldHeight)
        {
            return new BlockBox(chunk.X * plotSize, 0, chunk.Y * plotSize,
                                chunk.X * plotSize + plotSize, worldHeight, chunk.Y * plotSize + plotSize);
        }

        /// <summary>"minx,miny,minz,maxx,maxy,maxz" per box, joined by ';' - how the boxes are stored.</summary>
        public static string FormatBoxes(BoxShape shape)
        {
            var sb = new StringBuilder();
            if (shape == null) return "";
            foreach (BlockBox b in shape.Boxes)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(string.Join(",", new[] { b.MinX, b.MinY, b.MinZ, b.MaxX, b.MaxY, b.MaxZ }));
            }
            return sb.ToString();
        }

        /// <summary>Back from <see cref="FormatBoxes"/>; null when the text is not a valid shape.</summary>
        public static BoxShape ParseBoxes(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var boxes = new List<BlockBox>();
            foreach (string part in text.Split(';'))
            {
                string[] n = part.Split(',');
                if (n.Length != 6) return null;
                var v = new int[6];
                for (int i = 0; i < 6; i++)
                {
                    if (!int.TryParse(n[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out v[i])) return null;
                }
                if (v[3] < v[0] || v[4] < v[1] || v[5] < v[2]) return null;
                boxes.Add(new BlockBox(v[0], v[1], v[2], v[3], v[4], v[5]));
            }
            try
            {
                return new BoxShape(boxes);
            }
            catch (System.ArgumentException)
            {
                // Overlapping or empty boxes: a row edited by hand.
                return null;
            }
        }

        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().saveSubPlot(this, update);
        }
    }
}
