using claims.src;
using claims.src.auxialiry;
using Vintagestory.API.MathTools;

namespace claims.tests
{
    public class PlotPositionTests
    {
        public PlotPositionTests()
        {
            claims.src.claims.config = new Config();
        }

        // =====================================================================
        // fromXZ
        // =====================================================================

        [Fact]
        public void FromXZ_DividesbyPlotSize()
        {
            claims.src.claims.config.PLOT_SIZE = 16;
            var p = PlotPosition.fromXZ(32, 64);
            Assert.Equal(2, p.X);
            Assert.Equal(4, p.Z);
        }

        [Fact]
        public void FromXZ_Zero_ReturnsZero()
        {
            claims.src.claims.config.PLOT_SIZE = 16;
            var p = PlotPosition.fromXZ(0, 0);
            Assert.Equal(0, p.X);
            Assert.Equal(0, p.Z);
        }

        [Fact]
        public void FromXZ_IntegerDivision_Truncates()
        {
            claims.src.claims.config.PLOT_SIZE = 16;
            var p = PlotPosition.fromXZ(17, 31);
            Assert.Equal(1, p.X);
            Assert.Equal(1, p.Z);
        }

        // =====================================================================
        // fromBlockPos
        // =====================================================================

        [Fact]
        public void FromBlockPos_DividesByPlotSize()
        {
            claims.src.claims.config.PLOT_SIZE = 16;
            var p = PlotPosition.fromBlockPos(new BlockPos(48, 64, 96));
            Assert.Equal(3, p.X);
            Assert.Equal(6, p.Z);
        }

        // =====================================================================
        // Equals
        // =====================================================================

        [Fact]
        public void Equals_SamePosition_ReturnsTrue()
        {
            var a = new PlotPosition(3, 5);
            var b = new PlotPosition(3, 5);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentPosition_ReturnsFalse()
        {
            var a = new PlotPosition(3, 5);
            var b = new PlotPosition(3, 6);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            var a = new PlotPosition(1, 1);
            Assert.True(a.Equals(a));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var a = new PlotPosition(1, 1);
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void Equals_NonPlotPosition_ReturnsFalse()
        {
            var a = new PlotPosition(1, 1);
            Assert.False(a.Equals("not a position"));
        }

        /// <summary>
        /// PlotPosition.Equals has dead code: after `if (!(obj is PlotPosition)) return false;`
        /// there is a second identical `if (!(obj is PlotPosition))` block (lines 98-102)
        /// that can never be reached. The equality logic still works correctly via lines 104-106,
        /// but the dead block at 99-101 silently never runs.
        /// This test confirms correctness is preserved despite the dead code.
        /// </summary>
        [Fact]
        public void Equals_DeadCodeDoesNotAffectResult()
        {
            var a = new PlotPosition(7, 3);
            var b = new PlotPosition(7, 3);
            var c = new PlotPosition(7, 4);

            Assert.True(a.Equals(b));
            Assert.False(a.Equals(c));
        }

        // =====================================================================
        // GetHashCode
        // =====================================================================

        [Fact]
        public void GetHashCode_SamePosition_SameHash()
        {
            var a = new PlotPosition(5, 10);
            var b = new PlotPosition(5, 10);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentPositions_DifferentHash()
        {
            var a = new PlotPosition(5, 10);
            var b = new PlotPosition(10, 5);
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        // =====================================================================
        // Clone
        // =====================================================================

        [Fact]
        public void Clone_ReturnsEqualButDistinctObject()
        {
            var original = new PlotPosition(4, 7);
            var clone = original.Clone();

            Assert.True(original.Equals(clone));
            Assert.NotSame(original, clone);
        }

        [Fact]
        public void Clone_MutatingClone_DoesNotAffectOriginal()
        {
            var original = new PlotPosition(4, 7);
            var clone = original.Clone();
            clone.X = 99;

            Assert.Equal(4, original.X);
        }
    }
}
