using System.Linq;
using System.Reflection;
using claims.src;
using claims.src.auxialiry;
using claims.src.auxialiry.innerclaims;
using claims.src.part;
using claims.src.part.structure;
using claims.src.subplots;
using claims.src.subplots.geometry;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace claims.tests
{
    /// <summary>
    /// What the client is sent about sub-plots: each chunk gets the part of a sub-plot
    /// inside it, with the flags worked out for the one player who receives it.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotSyncTests
    {
        private readonly City city = new("TestCity", "sps-city");
        private readonly PlayerInfo owner = new("Owner", "sps-owner");
        private readonly PlayerInfo citizen = new("Citizen", "sps-citizen");
        private readonly SubPlot flat;

        public SubPlotSyncTests()
        {
            claims.src.claims.config = new Config { PLOT_SIZE = 16 };
            claims.src.claims.dataStorage = new DataStorage(false);
            foreach (var p in new[] { owner, citizen })
            {
                typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(p, new object[] { city });
                city.getCityCitizens().Add(p);
            }

            // Across the border of chunks (0,0) and (1,0).
            flat = new SubPlot("flat", "sps-flat")
            {
                City = city,
                Owner = owner,
                Shape = new BoxShape(new BlockBox(8, 64, 0, 24, 68, 8))
            };
            claims.src.claims.dataStorage.AddSubPlot(flat);
        }

        private static Plot PlotAt(int x, int z) => new Plot(new PlotPosition(x, z));

        [Fact]
        public void EachChunk_GetsOnlyItsOwnPartOfTheSubPlot()
        {
            var left = SubPlotSync.PiecesFor(PlotAt(0, 0), owner, false, true, true, true).Single();
            var right = SubPlotSync.PiecesFor(PlotAt(1, 0), owner, false, true, true, true).Single();

            Assert.Equal((8, 16), (left.MinX, left.MaxX));
            Assert.Equal((16, 24), (right.MinX, right.MaxX));
            Assert.Equal((64, 68), (left.MinY, left.MaxY));
        }

        [Fact]
        public void ChunkWithoutSubPlots_GetsNothing()
        {
            Assert.Empty(SubPlotSync.PiecesFor(PlotAt(5, 5), owner, false, true, true, true));
        }

        [Fact]
        public void Owner_MayAll_CitizenIsShutOut_ThoughTheChunkLetsThemBuild()
        {
            var forOwner = SubPlotSync.PiecesFor(PlotAt(0, 0), owner, false, false, false, false).Single();
            var forCitizen = SubPlotSync.PiecesFor(PlotAt(0, 0), citizen, false, true, true, true).Single();

            Assert.True(forOwner.May(0) && forOwner.May(1) && forOwner.May(2));
            Assert.False(forCitizen.May(0) || forCitizen.May(1) || forCitizen.May(2));
        }

        [Fact]
        public void Creative_MayAll()
        {
            var piece = SubPlotSync.PiecesFor(PlotAt(0, 0), citizen, true, false, false, false).Single();

            Assert.True(piece.May(0) && piece.May(1) && piece.May(2));
        }

        [Fact]
        public void EnemyAtWar_GetsTheChunksFlags()
        {
            var enemyCity = new City("Enemy", "sps-enemy");
            city.HostileCities.Add(enemyCity);
            var enemy = new PlayerInfo("Enemy", "sps-enemyplayer");
            typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(enemy, new object[] { enemyCity });
            enemyCity.getCityCitizens().Add(enemy);

            var piece = SubPlotSync.PiecesFor(PlotAt(0, 0), enemy, false, true, true, false).Single();

            Assert.True(piece.May(0));
            Assert.True(piece.May(1));
            Assert.False(piece.May(2));
        }

        [Fact]
        public void RemovedSubPlot_IsNoLongerSent()
        {
            claims.src.claims.dataStorage.RemoveSubPlot(flat);

            Assert.Empty(SubPlotSync.PiecesFor(PlotAt(0, 0), owner, false, true, true, true));
        }

        [Fact]
        public void ReshapedSubPlot_LeavesTheChunkItMovedOutOf()
        {
            claims.src.claims.dataStorage.ReshapeSubPlot(flat, new BoxShape(new BlockBox(16, 64, 0, 24, 68, 8)));

            Assert.Empty(SubPlotSync.PiecesFor(PlotAt(0, 0), owner, false, true, true, true));
            Assert.Single(SubPlotSync.PiecesFor(PlotAt(1, 0), owner, false, true, true, true));
        }

        [Fact]
        public void ClientInnerClaim_MaxCornerIsOutside_OnEveryAxis()
        {
            var piece = new ClientInnerClaim(new Vec3i(0, 64, 0), new Vec3i(8, 68, 8), new[] { true, true, true });

            Assert.True(piece.Contains(new BlockPos(7, 67, 7)));
            Assert.False(piece.Contains(new BlockPos(8, 64, 0)));
            Assert.False(piece.Contains(new BlockPos(0, 68, 0)));
            Assert.False(piece.Contains(new BlockPos(0, 64, 8)));
        }

        [Fact]
        public void ClientInnerClaim_KeepsItsThreeFlags_ThroughTheProtobufMapCache()
        {
            var piece = new ClientInnerClaim(new Vec3i(0, 64, 0), new Vec3i(8, 68, 8), new[] { true, false, true });

            var back = SerializerUtil.Deserialize<ClientInnerClaim>(SerializerUtil.Serialize(piece));

            Assert.Equal(new[] { true, false, true }, back.permissionsFlags);
        }

        [Fact]
        public void Pieces_CarryNameOwnerPriceAndTax()
        {
            flat.Price = 0;
            flat.Tax = 3;

            var piece = SubPlotSync.PiecesFor(PlotAt(0, 0), citizen, false, true, true, true).Single();

            Assert.Equal("flat", piece.Name);
            Assert.Equal("Owner", piece.Owner);
            Assert.Equal(0, piece.Price);
            Assert.Equal(3, piece.Tax);
            Assert.True(piece.IsSubPlot);
        }

        [Fact]
        public void FreePrice_SurvivesTheProtobufMapCache()
        {
            // 0 is protobuf's default; without IsRequired it was skipped and came back as -1, "not for sale".
            var piece = new ClientInnerClaim(new Vec3i(0, 64, 0), new Vec3i(8, 68, 8), new[] { true, true, true })
            {
                Name = "flat", Owner = "Bob", Price = 0, Tax = 2.5
            };

            var back = SerializerUtil.Deserialize<ClientInnerClaim>(SerializerUtil.Serialize(piece));

            Assert.Equal(0, back.Price);
            Assert.Equal("flat", back.Name);
            Assert.Equal("Bob", back.Owner);
            Assert.Equal(2.5, back.Tax);
        }

        [Fact]
        public void SavedPlot_FindsTheSubPlotAtABlock_AndLeavesTavernRoomsOut()
        {
            var room = new ClientInnerClaim(new Vec3i(0, 0, 0), new Vec3i(4, 4, 4), new[] { true, true, true });
            var piece = SubPlotSync.PiecesFor(PlotAt(0, 0), owner, false, true, true, true).Single();
            var saved = new claims.src.clientMapHandling.SavedPlotInfo(0, false, true, true, true, "TestCity", "", "",
                new List<ClientInnerClaim> { room, piece }, "");

            Assert.Same(piece, saved.SubPlotAt(10, 65, 3));
            Assert.Null(saved.SubPlotAt(1, 1, 1));
            Assert.Single(saved.SubPlotPieces());
        }

        [Fact]
        public void PiecesOfTwoChunks_AreJoinedBack_IntoTheBoxTheyWereCutFrom()
        {
            var left = SubPlotSync.PiecesFor(PlotAt(0, 0), owner, false, true, true, true).Single();
            var right = SubPlotSync.PiecesFor(PlotAt(1, 0), owner, false, true, true, true).Single();

            var joined = BoxShape.MergeAligned(new[]
            {
                new BlockBox(left.MinX, left.MinY, left.MinZ, left.MaxX, left.MaxY, left.MaxZ),
                new BlockBox(right.MinX, right.MinY, right.MinZ, right.MaxX, right.MaxY, right.MaxZ),
            });

            Assert.Equal(new BlockBox(8, 64, 0, 24, 68, 8), Assert.Single(joined));
        }

        [Fact]
        public void BoxesThatDoNotLineUp_StaySeparate()
        {
            // An L: touching, but the union would not be the two boxes.
            var joined = BoxShape.MergeAligned(new[] { new BlockBox(0, 0, 0, 4, 4, 4), new BlockBox(4, 0, 0, 8, 4, 2) });

            Assert.Equal(2, joined.Count);
        }

        [Fact]
        public void ClientInnerClaim_WithoutFlags_AllowsNothing()
        {
            var piece = new ClientInnerClaim { pos1 = new Vec3i(0, 0, 0), pos2 = new Vec3i(1, 1, 1) };

            Assert.False(piece.May(0));
            Assert.False(piece.May(5));
        }
    }
}
