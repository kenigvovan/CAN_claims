using System;
using System.Collections.Generic;
using System.Linq;

namespace claims.src.subplots.geometry
{
    /// <summary>
    /// Immutable set of non-overlapping, non-empty boxes that make up one sub-plot. Taken over from
    /// canclaimsextended's PlotShape; named apart because a Plot here is a whole chunk.
    /// </summary>
    public sealed class BoxShape
    {
        private readonly BlockBox[] boxes;

        public IReadOnlyList<BlockBox> Boxes => boxes;
        public BlockBox Bounds { get; }
        public long Volume { get; }

        public BoxShape(BlockBox box) : this(new[] { box })
        {
        }

        public BoxShape(IEnumerable<BlockBox> boxes)
        {
            this.boxes = boxes.ToArray();
            if (this.boxes.Length == 0)
            {
                throw new ArgumentException("A shape needs at least one box");
            }
            BlockBox bounds = this.boxes[0];
            long volume = 0;
            for (int i = 0; i < this.boxes.Length; i++)
            {
                BlockBox b = this.boxes[i];
                if (b.IsEmpty)
                {
                    throw new ArgumentException($"Empty box in a shape: {b}");
                }
                for (int j = 0; j < i; j++)
                {
                    if (b.Intersects(this.boxes[j]))
                    {
                        throw new ArgumentException($"Boxes of one shape overlap: {this.boxes[j]} and {b}");
                    }
                }
                bounds = bounds.Union(b);
                volume += b.Volume;
            }
            Bounds = bounds;
            Volume = volume;
        }

        public bool Contains(int x, int y, int z)
        {
            if (!Bounds.Contains(x, y, z))
            {
                return false;
            }
            foreach (BlockBox b in boxes)
            {
                if (b.Contains(x, y, z))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(double x, double y, double z)
        {
            return Contains((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));
        }

        public bool Intersects(BlockBox box)
        {
            if (!Bounds.Intersects(box))
            {
                return false;
            }
            foreach (BlockBox b in boxes)
            {
                if (b.Intersects(box))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Intersects(BoxShape other)
        {
            if (!Bounds.Intersects(other.Bounds))
            {
                return false;
            }
            foreach (BlockBox b in other.boxes)
            {
                if (Intersects(b))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The shapes don't intersect and at least one pair of their boxes shares a face.</summary>
        public bool Touches(BoxShape other)
        {
            if (!Bounds.Expand(1).Intersects(other.Bounds) || Intersects(other))
            {
                return false;
            }
            foreach (BlockBox a in boxes)
            {
                foreach (BlockBox b in other.boxes)
                {
                    if (a.Touches(b))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Every box can be reached from every other one through shared faces.</summary>
        public bool IsConnected()
        {
            if (boxes.Length == 1)
            {
                return true;
            }
            return Connectivity.CountPieces(boxes, b => boxes.Where(b.Touches)) == 1;
        }

        public BoxShape WithBox(BlockBox box)
        {
            return new BoxShape(boxes.Append(box));
        }

        /// <summary>
        /// The parts of this shape inside one box - a chunk, for what the client is sent about it.
        /// Empty when the shape does not reach into it.
        /// </summary>
        public List<BlockBox> ClipTo(BlockBox area)
        {
            var parts = new List<BlockBox>();
            foreach (BlockBox b in boxes)
            {
                if (b.TryIntersect(area, out BlockBox part)) parts.Add(part);
            }
            return parts;
        }

        /// <summary>This shape with everything inside <paramref name="area"/> taken out; null when nothing is left.</summary>
        public BoxShape Without(BlockBox area)
        {
            if (!Intersects(area)) return this;
            var rest = new List<BlockBox>();
            foreach (BlockBox b in boxes) rest.AddRange(b.Subtract(area));
            return rest.Count == 0 ? null : new BoxShape(rest);
        }

        /// <summary>
        /// Joins boxes that together make one box: same extent on two axes, touching on the third.
        /// Undoes the cut along chunk lines the client gets its pieces with, so a flat across two
        /// chunks is drawn as one box rather than two with a wall between.
        /// </summary>
        public static List<BlockBox> MergeAligned(IEnumerable<BlockBox> pieces)
        {
            var list = pieces.ToList();
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < list.Count && !merged; i++)
                {
                    for (int j = i + 1; j < list.Count && !merged; j++)
                    {
                        if (TryJoin(list[i], list[j], out BlockBox joined))
                        {
                            list[i] = joined;
                            list.RemoveAt(j);
                            merged = true;
                        }
                    }
                }
            }
            return list;
        }

        private static bool TryJoin(BlockBox a, BlockBox b, out BlockBox joined)
        {
            joined = default;
            bool sameY = a.MinY == b.MinY && a.MaxY == b.MaxY;
            bool sameX = a.MinX == b.MinX && a.MaxX == b.MaxX;
            bool sameZ = a.MinZ == b.MinZ && a.MaxZ == b.MaxZ;
            bool touchX = a.MaxX == b.MinX || b.MaxX == a.MinX;
            bool touchZ = a.MaxZ == b.MinZ || b.MaxZ == a.MinZ;
            bool touchY = a.MaxY == b.MinY || b.MaxY == a.MinY;
            if ((sameY && sameZ && touchX) || (sameY && sameX && touchZ) || (sameX && sameZ && touchY))
            {
                joined = a.Union(b);
                return true;
            }
            return false;
        }

        public override string ToString() => string.Join(" + ", boxes);
    }
}
