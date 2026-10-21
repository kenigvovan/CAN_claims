using claims.src.gui.plotMovementGui;

namespace claims.tests
{
    /// <summary>The fade of the location banner and the colour its city title is drawn in.</summary>
    public class LocationBannerTests
    {
        [Theory]
        [InlineData(-1, 0f)]      // not started
        [InlineData(0, 0f)]       // fading in from nothing
        [InlineData(150, 0.5f)]   // half way into the 300 ms fade-in
        [InlineData(1000, 1f)]    // held
        [InlineData(3100, 0.5f)]  // half way through the 800 ms fade-out of a 3500 ms title
        [InlineData(3500, 0f)]    // over
        [InlineData(9000, 0f)]
        public void Alpha_FadesInHoldsAndFadesOut(long elapsed, float expected)
        {
            Assert.Equal((double)expected, (double)LocationBanner.Alpha(elapsed, 3500), 3);
        }

        [Fact]
        public void Lighten_ReadsTheColourAsTexturePixels()
        {
            // ABGR: red in the lowest byte.
            double[] red = LocationBanner.Lighten(unchecked((int)0xFF0000FF));

            Assert.True(red[0] > red[1] && red[0] > red[2], "red came out as " + string.Join(", ", red));
        }

        [Fact]
        public void Lighten_LiftsBlackToReadableGrey_AndKeepsWhite()
        {
            double[] black = LocationBanner.Lighten(unchecked((int)0xFF000000));
            double[] white = LocationBanner.Lighten(unchecked((int)0xFFFFFFFF));

            Assert.All(black.Take(3), c => Assert.Equal(0.35, c, 3));
            Assert.All(white.Take(3), c => Assert.Equal(1.0, c, 3));
            Assert.Equal(1.0, black[3]);
        }
    }
}
