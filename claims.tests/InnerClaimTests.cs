using claims.src.auxialiry;
using Vintagestory.API.MathTools;

namespace claims.tests
{
    public class InnerClaimTests
    {
        private static InnerClaim Box(int x1, int y1, int z1, int x2, int y2, int z2)
            => new InnerClaim(new Vec3i(x1, y1, z1), new Vec3i(x2, y2, z2));

        // =====================================================================
        // Intersects
        // =====================================================================

        [Fact]
        public void Intersects_ClearlyOverlapping_ReturnsTrue()
        {
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(3, 3, 3, 8, 8, 8);
            Assert.True(a.Intersects(b));
            Assert.True(b.Intersects(a)); // symmetric
        }

        [Fact]
        public void Intersects_NotOverlapping_X_ReturnsFalse()
        {
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(6, 0, 0, 10, 5, 5);
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_NotOverlapping_Y_ReturnsFalse()
        {
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(0, 6, 0, 5, 10, 5);
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_NotOverlapping_Z_ReturnsFalse()
        {
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(0, 0, 6, 5, 5, 10);
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_TouchingEdge_X_ReturnsTrue()
        {
            // MaxX of a == MinX of b — touching counts as intersecting
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(5, 0, 0, 10, 5, 5);
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_TouchingEdge_Z_ReturnsTrue()
        {
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(0, 0, 5, 5, 5, 10);
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_Contained_ReturnsTrue()
        {
            var outer = Box(0, 0, 0, 10, 10, 10);
            var inner = Box(2, 2, 2, 4, 4, 4);
            Assert.True(outer.Intersects(inner));
            Assert.True(inner.Intersects(outer));
        }

        [Fact]
        public void Intersects_Identical_ReturnsTrue()
        {
            var a = Box(0, 0, 0, 5, 5, 5);
            var b = Box(0, 0, 0, 5, 5, 5);
            Assert.True(a.Intersects(b));
        }

        // =====================================================================
        // Contains(Vec3d) — exclusive upper bound
        // =====================================================================

        [Fact]
        public void Contains_Vec3d_Inside_ReturnsTrue()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.True(c.Contains(new Vec3d(5, 5, 5)));
        }

        [Fact]
        public void Contains_Vec3d_Outside_ReturnsFalse()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.False(c.Contains(new Vec3d(11, 5, 5)));
        }

        [Fact]
        public void Contains_Vec3d_AtMinBoundary_ReturnsTrue()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.True(c.Contains(new Vec3d(0, 0, 0)));
        }

        [Fact]
        public void Contains_Vec3d_AtMaxBoundary_ReturnsFalse()
        {
            // Vec3d version uses exclusive upper bound (< MaxX)
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.False(c.Contains(new Vec3d(10, 5, 5)));
        }

        // =====================================================================
        // Contains(int, int, int) — exclusive upper bound
        // =====================================================================

        [Fact]
        public void Contains_Int_Inside_ReturnsTrue()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.True(c.Contains(5, 5, 5));
        }

        [Fact]
        public void Contains_Int_AtMax_ReturnsFalse()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.False(c.Contains(10, 5, 5));
        }

        // =====================================================================
        // Contains(BlockPos) — Bug: X/Y use <= (inclusive), Z uses < (exclusive)
        // =====================================================================

        [Fact]
        public void Contains_BlockPos_Inside_ReturnsTrue()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.True(c.Contains(new BlockPos(5, 5, 5)));
        }

        [Fact]
        public void Contains_BlockPos_AtMaxX_ReturnsTrue()
        {
            // BlockPos uses <= for X (inclusive), unlike Vec3d/int which use <
            var c = Box(0, 0, 0, 10, 10, 10);
            Assert.True(c.Contains(new BlockPos(10, 5, 5)));
        }

        /// <summary>
        /// Bug: Contains(BlockPos) is inconsistent — X and Y use inclusive upper bound (<=)
        /// but Z uses exclusive (<). Same block at MaxZ is excluded while MaxX is included.
        /// Fix: make all axes consistent (all inclusive or all exclusive).
        /// </summary>
        [Fact]
        public void Contains_BlockPos_AtMaxZ_ShouldMatchMaxX_Inconsistency()
        {
            var c = Box(0, 0, 0, 10, 10, 10);
            bool atMaxX = c.Contains(new BlockPos(10, 5, 5)); // true (inclusive)
            bool atMaxZ = c.Contains(new BlockPos(5, 5, 10)); // false (exclusive) — inconsistent!

            Assert.NotEqual(atMaxX, atMaxZ); // documents the inconsistency
        }

        // =====================================================================
        // Min/Max properties with reversed pos1/pos2
        // =====================================================================

        [Fact]
        public void MinMax_ReversedCoords_NormalizedCorrectly()
        {
            // pos2 < pos1, should still work via Math.Min/Max
            var c = Box(10, 10, 10, 0, 0, 0);
            Assert.Equal(0, c.MinX);
            Assert.Equal(10, c.MaxX);
        }

        // =====================================================================
        // ToString serialization
        // =====================================================================

        [Fact]
        public void ToString_FormatsCorrectly()
        {
            var c = Box(1, 2, 3, 4, 5, 6);
            c.permissionsFlags[0] = true;
            c.permissionsFlags[1] = false;
            c.permissionsFlags[2] = true;

            string s = c.ToString();

            Assert.StartsWith("1,2,3:4,5,6:", s);
            Assert.Contains("1,0,1", s);
        }

        [Fact]
        public void ToString_WithMembers_IncludesUids()
        {
            var c = Box(0, 0, 0, 5, 5, 5);
            c.membersUids.Add("uid-1");
            c.membersUids.Add("uid-2");

            string s = c.ToString();

            Assert.Contains("uid-1", s);
            Assert.Contains("uid-2", s);
        }
    }
}
