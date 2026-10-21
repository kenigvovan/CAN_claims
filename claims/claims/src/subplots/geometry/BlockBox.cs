#nullable enable
using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace claims.src.subplots.geometry
{
    /// <summary>
    /// Axis-aligned box of blocks, half-open: a block (x, y, z) is inside when Min &lt;= x &lt; Max on
    /// every axis. Taken over from canclaimsextended.
    /// </summary>
    public readonly struct BlockBox : IEquatable<BlockBox>
    {
        public readonly int MinX, MinY, MinZ;
        public readonly int MaxX, MaxY, MaxZ;

        public BlockBox(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            if (maxX < minX || maxY < minY || maxZ < minZ)
            {
                throw new ArgumentException($"Box max is below min: ({minX},{minY},{minZ})-({maxX},{maxY},{maxZ})");
            }
            MinX = minX; MinY = minY; MinZ = minZ;
            MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        /// <summary>Box spanning two corner blocks, both included, in any order.</summary>
        public static BlockBox FromCorners(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            return new BlockBox(Math.Min(x1, x2), Math.Min(y1, y2), Math.Min(z1, z2),
                                Math.Max(x1, x2) + 1, Math.Max(y1, y2) + 1, Math.Max(z1, z2) + 1);
        }

        public static BlockBox FromCorners(BlockPos a, BlockPos b)
        {
            return FromCorners(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public int SizeX => MaxX - MinX;
        public int SizeY => MaxY - MinY;
        public int SizeZ => MaxZ - MinZ;
        public long Volume => (long)SizeX * SizeY * SizeZ;
        public bool IsEmpty => SizeX == 0 || SizeY == 0 || SizeZ == 0;

        public bool Contains(int x, int y, int z)
        {
            return x >= MinX && x < MaxX
                && y >= MinY && y < MaxY
                && z >= MinZ && z < MaxZ;
        }

        public bool Contains(BlockPos pos) => Contains(pos.X, pos.Y, pos.Z);

        /// <summary>Entity position: tested by the block it stands in.</summary>
        public bool Contains(double x, double y, double z)
        {
            return Contains((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));
        }

        public bool Contains(Vec3d pos) => Contains(pos.X, pos.Y, pos.Z);

        /// <summary>The other box lies fully inside this one.</summary>
        public bool Contains(BlockBox other)
        {
            return other.MinX >= MinX && other.MaxX <= MaxX
                && other.MinY >= MinY && other.MaxY <= MaxY
                && other.MinZ >= MinZ && other.MaxZ <= MaxZ;
        }

        /// <summary>The boxes share at least one block.</summary>
        public bool Intersects(BlockBox other)
        {
            return MinX < other.MaxX && other.MinX < MaxX
                && MinY < other.MaxY && other.MinY < MaxY
                && MinZ < other.MaxZ && other.MinZ < MaxZ;
        }

        public bool TryIntersect(BlockBox other, out BlockBox result)
        {
            if (!Intersects(other))
            {
                result = default;
                return false;
            }
            result = new BlockBox(Math.Max(MinX, other.MinX), Math.Max(MinY, other.MinY), Math.Max(MinZ, other.MinZ),
                                  Math.Min(MaxX, other.MaxX), Math.Min(MaxY, other.MaxY), Math.Min(MaxZ, other.MaxZ));
            return true;
        }

        /// <summary>
        /// The boxes don't intersect but share a face of non-zero area. Touching only by an edge or a corner doesn't count.
        /// </summary>
        public bool Touches(BlockBox other)
        {
            int overlapX = Overlap(MinX, MaxX, other.MinX, other.MaxX);
            int overlapY = Overlap(MinY, MaxY, other.MinY, other.MaxY);
            int overlapZ = Overlap(MinZ, MaxZ, other.MinZ, other.MaxZ);

            bool adjX = MaxX == other.MinX || other.MaxX == MinX;
            bool adjY = MaxY == other.MinY || other.MaxY == MinY;
            bool adjZ = MaxZ == other.MinZ || other.MaxZ == MinZ;

            return (adjX && overlapY > 0 && overlapZ > 0)
                || (adjY && overlapX > 0 && overlapZ > 0)
                || (adjZ && overlapX > 0 && overlapY > 0);
        }

        public BlockBox Expand(int n)
        {
            return new BlockBox(MinX - n, MinY - n, MinZ - n, MaxX + n, MaxY + n, MaxZ + n);
        }

        /// <summary>Shortest distance between the boxes in blocks; 0 when they touch or intersect.</summary>
        public double DistanceTo(BlockBox other)
        {
            long dx = Gap(MinX, MaxX, other.MinX, other.MaxX);
            long dy = Gap(MinY, MaxY, other.MinY, other.MaxY);
            long dz = Gap(MinZ, MaxZ, other.MinZ, other.MaxZ);
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// This box minus cut, as at most 6 non-overlapping boxes. Slabs are cut along X first, then Y, then Z.
        /// </summary>
        public List<BlockBox> Subtract(BlockBox cut)
        {
            var result = new List<BlockBox>(6);
            if (!TryIntersect(cut, out BlockBox c))
            {
                result.Add(this);
                return result;
            }
            if (MinX < c.MinX) result.Add(new BlockBox(MinX, MinY, MinZ, c.MinX, MaxY, MaxZ));
            if (c.MaxX < MaxX) result.Add(new BlockBox(c.MaxX, MinY, MinZ, MaxX, MaxY, MaxZ));
            if (MinY < c.MinY) result.Add(new BlockBox(c.MinX, MinY, MinZ, c.MaxX, c.MinY, MaxZ));
            if (c.MaxY < MaxY) result.Add(new BlockBox(c.MinX, c.MaxY, MinZ, c.MaxX, MaxY, MaxZ));
            if (MinZ < c.MinZ) result.Add(new BlockBox(c.MinX, c.MinY, MinZ, c.MaxX, c.MaxY, c.MinZ));
            if (c.MaxZ < MaxZ) result.Add(new BlockBox(c.MinX, c.MinY, c.MaxZ, c.MaxX, c.MaxY, MaxZ));
            return result;
        }

        /// <summary>Smallest box containing both.</summary>
        public BlockBox Union(BlockBox other)
        {
            return new BlockBox(Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY), Math.Min(MinZ, other.MinZ),
                                Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY), Math.Max(MaxZ, other.MaxZ));
        }

        private static int Overlap(int aMin, int aMax, int bMin, int bMax)
        {
            return Math.Min(aMax, bMax) - Math.Max(aMin, bMin);
        }

        private static long Gap(int aMin, int aMax, int bMin, int bMax)
        {
            if (bMin >= aMax) return (long)bMin - aMax;
            if (aMin >= bMax) return (long)aMin - bMax;
            return 0;
        }

        public bool Equals(BlockBox other)
        {
            return MinX == other.MinX && MinY == other.MinY && MinZ == other.MinZ
                && MaxX == other.MaxX && MaxY == other.MaxY && MaxZ == other.MaxZ;
        }

        public override bool Equals(object? obj) => obj is BlockBox other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);
        public static bool operator ==(BlockBox a, BlockBox b) => a.Equals(b);
        public static bool operator !=(BlockBox a, BlockBox b) => !a.Equals(b);
        public override string ToString() => $"[{MinX},{MinY},{MinZ} - {MaxX},{MaxY},{MaxZ})";
    }
}
