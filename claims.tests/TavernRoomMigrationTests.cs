using System.Linq;
using claims.src;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using claims.src.subplots;
using claims.src.subplots.geometry;

namespace claims.tests
{
    /// <summary>
    /// The old tavern rooms become sub-plots on load, with what they let everyone do and
    /// where the server let them do it.
    /// </summary>
    [Collection("ModStatics")]
    public class TavernRoomMigrationTests : IDisposable
    {
        private readonly ServerStubs stubs = new(new Config { PLOT_SIZE = 16 });
        private readonly City city = new("TestCity", "trm-city");

        public void Dispose() => stubs.Dispose();

        private Plot Tavern(int x, int z, string rooms)
        {
            var plot = new Plot(new PlotPosition(x, z)) { Type = PlotType.TAVERN, PlotDesc = new PlotDescTavern { LegacyRooms = rooms } };
            plot.setCity(city);
            stubs.Storage.addClaimedPlot(plot.plotPosition, plot);
            return plot;
        }

        [Fact]
        public void Room_FarCornerInsideOnXAndY_OutsideOnZ_AsTheServerTestedIt()
        {
            var room = TavernRoomMigration.ParseRooms("1,64,2:4,66,6:0,1,0:uid1,uid2#").Single();

            Assert.Equal(new BlockBox(1, 64, 2, 5, 67, 6), room.Box);
            Assert.Equal(new[] { false, true, false }, room.Flags);
        }

        [Fact]
        public void UnreadableOrFlatRooms_AreLeftOut()
        {
            // Bad number, missing flags, no depth on Z.
            Assert.Empty(TavernRoomMigration.ParseRooms("1,x,2:4,66,6:0,1,0:#1,2,3:4,5,6#0,0,5:3,3,5:1,1,1:"));
        }

        [Fact]
        public void Rooms_BecomeSubPlots_AndTheTavernForgetsThem()
        {
            var plot = Tavern(0, 0, "1,64,2:4,66,6:0,1,0:#8,64,8:9,65,12:1,0,0:#");

            TavernRoomMigration.Run();

            var made = stubs.Storage.AllSubPlots().OrderBy(s => s.GetPartName()).ToList();
            Assert.Equal(new[] { "room1", "room2" }, made.Select(s => s.GetPartName()));
            Assert.All(made, s => Assert.Same(city, s.City));
            Assert.Equal("", ((PlotDescTavern)plot.PlotDesc).LegacyRooms);
        }

        [Fact]
        public void RoomRights_AreTheRoomsFlags_OrWhatTheChunkAllowedAlready()
        {
            var plot = Tavern(0, 0, "1,64,2:4,66,6:0,1,0:#");
            plot.getPermsHandler().CitizenPerms[(int)PermType.USE_PERM] = true;

            TavernRoomMigration.Run();

            var room = stubs.Storage.AllSubPlots().Single();
            // The room opened building to everyone; the chunk opened use to citizens.
            Assert.True(room.PermsHandler.getPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM));
            Assert.True(room.PermsHandler.getPerm(PermGroup.CITIZEN, PermType.USE_PERM));
            Assert.False(room.PermsHandler.getPerm(PermGroup.STRANGER, PermType.USE_PERM));
        }

        [Fact]
        public void BoughtTavern_KeepsItsOwnerInsideTheRooms()
        {
            var owner = new PlayerInfo("Owner", "trm-owner");
            var plot = Tavern(0, 0, "1,64,2:4,66,6:0,0,0:#");
            plot.setPlotOwner(owner);

            TavernRoomMigration.Run();

            Assert.Same(owner, stubs.Storage.AllSubPlots().Single().Owner);
        }

        [Fact]
        public void RoomPastItsTavern_IsSkipped()
        {
            Tavern(0, 0, "10,64,2:20,66,6:1,1,1:#");

            TavernRoomMigration.Run();

            Assert.Empty(stubs.Storage.AllSubPlots());
        }

        [Fact]
        public void SecondLoad_MakesNothingAgain()
        {
            Tavern(0, 0, "1,64,2:4,66,6:0,1,0:#");

            TavernRoomMigration.Run();
            TavernRoomMigration.Run();

            Assert.Single(stubs.Storage.AllSubPlots());
        }
    }
}
