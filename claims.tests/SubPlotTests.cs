using System.Reflection;
using claims.src;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.subplots;
using claims.src.subplots.geometry;
using Vintagestory.API.MathTools;

namespace claims.tests
{
    /// <summary>
    /// Sub-plots: the boxes, how they are stored, the rules for where a sub-plot may stand,
    /// and the index that finds the sub-plot holding a block. 16-block chunks throughout.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotTests
    {
        private const int Size = 16;
        private const int WorldHeight = 256;

        private readonly City city = new("TestCity", "sp-city-guid");
        private readonly City otherCity = new("OtherCity", "sp-other-guid");
        private readonly Dictionary<Vec2i, Plot> plots = new();

        public SubPlotTests()
        {
            claims.src.claims.config = new Config { PLOT_SIZE = Size };
            // Chunks (0,0), (1,0) and (0,1) belong to the city; (2,0) to another one.
            Claim(0, 0, city);
            Claim(1, 0, city);
            Claim(0, 1, city);
            Claim(2, 0, otherCity);
        }

        private Plot Claim(int x, int z, City owner)
        {
            var plot = new Plot(new PlotPosition(x, z));
            plot.setCity(owner);
            plots[new Vec2i(x, z)] = plot;
            return plot;
        }

        private Plot PlotAt(Vec2i chunk) => plots.TryGetValue(chunk, out Plot plot) ? plot : null!;

        private string? Check(BoxShape shape, params SubPlot[] others) =>
            SubPlotRules.Check(shape, city, PlotAt, others, 8, Size, WorldHeight);

        private static BoxShape Shape(params BlockBox[] boxes) => new(boxes);

        /*=====================================================================*/
        /* geometry                                                            */
        /*=====================================================================*/

        [Fact]
        public void Box_FarCornerIsOutside_OnEveryAxis()
        {
            var box = new BlockBox(0, 0, 0, 4, 4, 4);

            Assert.True(box.Contains(3, 3, 3));
            Assert.False(box.Contains(4, 3, 3));
            Assert.False(box.Contains(3, 4, 3));
            Assert.False(box.Contains(3, 3, 4));
        }

        [Fact]
        public void Box_FromCorners_IncludesBothCornerBlocks_InAnyOrder()
        {
            Assert.Equal(new BlockBox(2, 5, 7, 10, 9, 12), BlockBox.FromCorners(9, 8, 7, 2, 5, 11));
        }

        [Fact]
        public void Shape_ClipTo_KeepsOnlyThePartsInsideTheArea()
        {
            // Two chunks wide: one part in chunk 0, one in chunk 1.
            var shape = Shape(new BlockBox(10, 0, 0, 20, 4, 4));

            var parts = shape.ClipTo(SubPlot.ChunkColumn(new Vec2i(1, 0), Size, WorldHeight));

            Assert.Equal(new[] { new BlockBox(16, 0, 0, 20, 4, 4) }, parts);
            Assert.Empty(shape.ClipTo(SubPlot.ChunkColumn(new Vec2i(3, 3), Size, WorldHeight)));
        }

        [Fact]
        public void Shape_OverlappingBoxes_AreRefused()
        {
            Assert.Throws<ArgumentException>(() => Shape(new BlockBox(0, 0, 0, 4, 4, 4), new BlockBox(3, 0, 0, 6, 4, 4)));
        }

        /*=====================================================================*/
        /* chunks and storage                                                  */
        /*=====================================================================*/

        [Fact]
        public void Chunks_AreEveryChunkTheBoxesReach()
        {
            var shape = Shape(new BlockBox(10, 0, 10, 20, 4, 20));

            var chunks = SubPlot.ChunksOf(shape, Size);

            Assert.Equal(4, chunks.Count);
            Assert.Contains(new Vec2i(1, 1), chunks);
        }

        [Fact]
        public void Chunks_ABoxEndingOnABorder_DoesNotReachTheNextChunk()
        {
            // Max is outside: x up to 15 stays in chunk 0.
            Assert.Single(SubPlot.ChunksOf(Shape(new BlockBox(0, 0, 0, 16, 4, 16)), Size));
        }

        [Fact]
        public void Boxes_RoundTripThroughText()
        {
            var shape = Shape(new BlockBox(0, 0, 0, 4, 4, 4), new BlockBox(4, 0, 0, 8, 2, 4));

            var back = SubPlot.ParseBoxes(SubPlot.FormatBoxes(shape));

            Assert.NotNull(back);
            Assert.Equal(shape.Boxes, back!.Boxes);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1,2,3")]
        [InlineData("0,0,0,a,4,4")]
        [InlineData("4,4,4,0,0,0")]                 // max below min
        [InlineData("0,0,0,4,4,4;2,0,0,6,4,4")]     // overlapping
        [InlineData("0,0,0,0,4,4")]                 // empty
        public void Boxes_BadText_IsNotAShape(string text)
        {
            Assert.Null(SubPlot.ParseBoxes(text));
        }

        /*=====================================================================*/
        /* rules                                                               */
        /*=====================================================================*/

        [Fact]
        public void Rules_AFlatOverTwoChunksOfTheCity_IsAllowed()
        {
            Assert.Null(Check(Shape(new BlockBox(10, 64, 2, 22, 68, 8))));
        }

        [Fact]
        public void Rules_ReachingIntoAnotherCity_IsRefused()
        {
            Assert.Equal("claims:subplot_outside_city", Check(Shape(new BlockBox(28, 64, 2, 36, 68, 8))));
        }

        [Fact]
        public void Rules_ReachingIntoTheWild_IsRefused()
        {
            // Chunk (0,2): nobody's.
            Assert.Equal("claims:subplot_outside_city", Check(Shape(new BlockBox(2, 64, 36, 8, 68, 40))));
        }

        [Fact]
        public void Rules_OnAChunkACitizenBought_IsRefused()
        {
            plots[new Vec2i(1, 0)].setPlotOwner(new PlayerInfo("Owner", "sp-owner-uid"));

            Assert.Equal("claims:subplot_on_owned_plot", Check(Shape(new BlockBox(10, 64, 2, 22, 68, 8))));
        }

        [Fact]
        public void Rules_OnAPlotsGroupChunk_IsRefused()
        {
            plots[new Vec2i(0, 0)].setPlotGroup(new CityPlotsGroup("farm", "sp-group-guid"));

            Assert.Equal("claims:subplot_on_plotsgroup", Check(Shape(new BlockBox(2, 64, 2, 8, 68, 8))));
        }

        [Fact]
        public void Rules_TwoPiecesThatDoNotTouch_AreRefused()
        {
            Assert.Equal("claims:subplot_not_connected",
                Check(Shape(new BlockBox(0, 64, 0, 4, 68, 4), new BlockBox(8, 64, 0, 12, 68, 4))));
        }

        [Fact]
        public void Rules_MoreBoxesThanAllowed_AreRefused()
        {
            var boxes = Enumerable.Range(0, 9).Select(i => new BlockBox(i, 64, 0, i + 1, 68, 4)).ToArray();

            Assert.Equal("claims:subplot_too_many_boxes", Check(Shape(boxes)));
        }

        [Fact]
        public void Rules_OutsideTheWorldHeight_IsRefused()
        {
            Assert.Equal("claims:subplot_outside_world", Check(Shape(new BlockBox(2, 250, 2, 8, 260, 8))));
        }

        [Fact]
        public void Rules_OverlappingAnotherSubPlot_IsRefused_TouchingIsFine()
        {
            var flat = new SubPlot("flat", "sp-flat") { City = city, Shape = Shape(new BlockBox(2, 64, 2, 8, 68, 8)) };

            Assert.Equal("claims:subplot_overlaps", Check(Shape(new BlockBox(6, 64, 2, 12, 68, 8)), flat));
            // The flat above shares only a floor with it.
            Assert.Null(Check(Shape(new BlockBox(2, 68, 2, 8, 72, 8)), flat));
        }

        [Fact]
        public void Rules_AnLShape_IsJudgedByTheChunksItReallyReaches()
        {
            // Its bounds cover chunk (1,1), which the city does not hold; its boxes never go there.
            var lShape = Shape(new BlockBox(10, 64, 2, 22, 68, 8), new BlockBox(10, 64, 8, 16, 68, 22));

            Assert.Null(Check(lShape));
        }

        /*=====================================================================*/
        /* index                                                               */
        /*=====================================================================*/

        [Fact]
        public void Index_FindsTheSubPlotHoldingABlock_InEveryChunkItReaches()
        {
            var storage = new DataStorage(false);
            var flat = new SubPlot("flat", "sp-idx") { City = city, Shape = Shape(new BlockBox(10, 64, 2, 22, 68, 8)) };
            storage.AddSubPlot(flat);

            Assert.True(storage.TryGetSubPlotAt(12, 65, 4, out var inFirst) && inFirst == flat);
            Assert.True(storage.TryGetSubPlotAt(20, 65, 4, out var inSecond) && inSecond == flat);
            Assert.False(storage.TryGetSubPlotAt(12, 70, 4, out _));   // above it: the chunk's rules
            Assert.Contains(flat, storage.SubPlotsInChunk(new Vec2i(1, 0)));
        }

        [Fact]
        public void Index_Reshape_MovesItBetweenChunks_Remove_DropsIt()
        {
            var storage = new DataStorage(false);
            var flat = new SubPlot("flat", "sp-move") { City = city, Shape = Shape(new BlockBox(2, 64, 2, 8, 68, 8)) };
            storage.AddSubPlot(flat);

            storage.ReshapeSubPlot(flat, Shape(new BlockBox(18, 64, 2, 24, 68, 8)));
            Assert.Empty(storage.SubPlotsInChunk(new Vec2i(0, 0)));
            Assert.True(storage.TryGetSubPlotAt(20, 65, 4, out _));

            Assert.True(storage.RemoveSubPlot(flat));
            Assert.False(storage.TryGetSubPlotAt(20, 65, 4, out _));
            Assert.False(storage.TryGetSubPlot("sp-move", out _));
        }

        [Fact]
        public void Index_SameGuidTwice_IsRefused()
        {
            var storage = new DataStorage(false);
            var a = new SubPlot("a", "sp-dup") { City = city, Shape = Shape(new BlockBox(2, 64, 2, 8, 68, 8)) };
            var b = new SubPlot("b", "sp-dup") { City = city, Shape = Shape(new BlockBox(2, 70, 2, 8, 74, 8)) };

            Assert.True(storage.AddSubPlot(a));
            Assert.False(storage.AddSubPlot(b));
        }
    }
}
