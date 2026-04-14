using claims.src;
using claims.src.part;
using claims.src.part.structure;
using Moq;
using Vintagestory.API.MathTools;

namespace claims.tests
{
    public class PrisonTests
    {
        private readonly Mock<DataStorage> _storageMock;

        public PrisonTests()
        {
            _storageMock = new Mock<DataStorage>(false);
            claims.src.claims.dataStorage = _storageMock.Object;
            claims.src.claims.config = new Config();
        }

        private static Prison MakePrison() => new Prison("test", "prison-guid");

        private static PrisonCellInfo MakeCell(int x = 0, int y = 64, int z = 0)
            => new PrisonCellInfo(new Vec3i(x, y, z));

        // =====================================================================
        // Cell management
        // =====================================================================

        [Fact]
        public void AddPrisonCell_IncreasesCount()
        {
            var prison = MakePrison();
            prison.addPrisonCell(MakeCell());
            Assert.Single(prison.getPrisonCells());
        }

        [Fact]
        public void RemovePrisonCell_ByIndex_DecreasesCount()
        {
            var prison = MakePrison();
            prison.addPrisonCell(MakeCell(0, 64, 0));
            prison.addPrisonCell(MakeCell(1, 64, 0));

            prison.removePrisonCell(0);

            Assert.Single(prison.getPrisonCells());
        }

        [Fact]
        public void RemovePrisonCell_ByObject_DecreasesCount()
        {
            var prison = MakePrison();
            var cell = MakeCell();
            prison.addPrisonCell(cell);

            prison.removePrisonCell(cell);

            Assert.Empty(prison.getPrisonCells());
        }

        // =====================================================================
        // TryGetRandomCell
        // =====================================================================

        [Fact]
        public void TryGetRandomCell_EmptyPrison_ReturnsFalse()
        {
            var prison = MakePrison();
            Assert.False(prison.TryGetRandomCell(out _));
        }

        [Fact]
        public void TryGetRandomCell_WithCell_ReturnsTrueAndCell()
        {
            var prison = MakePrison();
            var cell = MakeCell();
            prison.addPrisonCell(cell);

            bool result = prison.TryGetRandomCell(out var found);

            Assert.True(result);
            Assert.NotNull(found);
        }

        // =====================================================================
        // getRandomRespawnPoint — Bug: no guard when empty → crash
        // =====================================================================

        /// <summary>
        /// Bug: getRandomRespawnPoint() has no empty-list guard.
        /// Calling it on an empty prison throws IndexOutOfRangeException
        /// because it does prisonCells[r.Next() % prisonCells.Count] with Count=0.
        /// Fix: add the same `if (Count < 1) return null;` guard as TryGetRandomCell.
        /// </summary>
        [Fact]
        public void GetRandomRespawnPoint_EmptyPrison_ShouldNotThrow()
        {
            var prison = MakePrison();
            // Should return null gracefully, not throw
            var result = prison.getRandomRespawnPoint();
            Assert.Null(result);
        }

        // =====================================================================
        // TryGetCellInWhichPlayer
        // =====================================================================

        [Fact]
        public void TryGetCellInWhichPlayer_PlayerPresent_ReturnsTrueAndCell()
        {
            var prison = MakePrison();
            var cell = MakeCell();
            var player = new PlayerInfo("TestPlayer", "player-guid");
            cell.AddPlayer(player);
            prison.addPrisonCell(cell);

            bool found = prison.TryGetCellInWhichPlayer(player, out var result);

            Assert.True(found);
            Assert.Same(cell, result);
        }

        [Fact]
        public void TryGetCellInWhichPlayer_PlayerAbsent_ReturnsFalse()
        {
            var prison = MakePrison();
            prison.addPrisonCell(MakeCell());
            var player = new PlayerInfo("TestPlayer", "player-guid");

            Assert.False(prison.TryGetCellInWhichPlayer(player, out _));
        }

        [Fact]
        public void TryGetCellInWhichPlayer_EmptyPrison_ReturnsFalse()
        {
            var prison = MakePrison();
            var player = new PlayerInfo("TestPlayer", "player-guid");

            Assert.False(prison.TryGetCellInWhichPlayer(player, out _));
        }

        // =====================================================================
        // RemovePlayerFromAllCells
        // =====================================================================

        [Fact]
        public void RemovePlayerFromAllCells_RemovesFromAllCells()
        {
            var prison = MakePrison();
            var player = new PlayerInfo("TestPlayer", "player-guid");

            var cell1 = MakeCell(0, 64, 0);
            var cell2 = MakeCell(1, 64, 0);
            cell1.AddPlayer(player);
            cell2.AddPlayer(player);
            prison.addPrisonCell(cell1);
            prison.addPrisonCell(cell2);

            prison.RemovePlayerFromAllCells(player);

            Assert.False(prison.TryGetCellInWhichPlayer(player, out _));
            Assert.Empty(cell1.prisonedPlayers);
            Assert.Empty(cell2.prisonedPlayers);
        }

        [Fact]
        public void RemovePlayerFromAllCells_OnlyRemovesTargetPlayer()
        {
            var prison = MakePrison();
            var player1 = new PlayerInfo("Player1", "guid-1");
            var player2 = new PlayerInfo("Player2", "guid-2");

            var cell = MakeCell();
            cell.AddPlayer(player1);
            cell.AddPlayer(player2);
            prison.addPrisonCell(cell);

            prison.RemovePlayerFromAllCells(player1);

            Assert.False(prison.TryGetCellInWhichPlayer(player1, out _));
            Assert.True(prison.TryGetCellInWhichPlayer(player2, out _));
        }

        // =====================================================================
        // SerializeCells / DeserializeCells round-trip (no players)
        // =====================================================================

        [Fact]
        public void SerializeCells_Empty_ReturnsEmptyString()
        {
            var prison = MakePrison();
            Assert.Equal(string.Empty, prison.SerializeCells());
        }

        [Fact]
        public void SerializeCells_SingleCell_ContainsCoords()
        {
            var prison = MakePrison();
            prison.addPrisonCell(new PrisonCellInfo(new Vec3i(10, 64, 20)));

            string s = prison.SerializeCells();

            Assert.Contains("10", s);
            Assert.Contains("64", s);
            Assert.Contains("20", s);
        }

        [Fact]
        public void SerializeCells_MultiCell_SeparatedByPipe()
        {
            var prison = MakePrison();
            prison.addPrisonCell(new PrisonCellInfo(new Vec3i(1, 64, 1)));
            prison.addPrisonCell(new PrisonCellInfo(new Vec3i(2, 64, 2)));

            string s = prison.SerializeCells();

            Assert.Contains("|", s);
        }

        [Fact]
        public void ToString_ThenFromString_RestoresCellCount()
        {
            var original = MakePrison();
            original.addPrisonCell(new PrisonCellInfo(new Vec3i(5, 64, 10)));
            original.addPrisonCell(new PrisonCellInfo(new Vec3i(15, 64, 20)));

            // mock GetPlayerByUid to return false (no players to restore)
            PlayerInfo nullPlayer = null!;
            _storageMock.Setup(s => s.GetPlayerByUid(It.IsAny<string>(), out nullPlayer))
                        .Returns(false);

            string serialized = original.ToString();
            var restored = MakePrison();
            restored.fromString(serialized);

            Assert.Equal(original.getPrisonCells().Count, restored.getPrisonCells().Count);
        }

        [Fact]
        public void ToString_ThenFromString_RestoresSpawnPositions()
        {
            var original = MakePrison();
            original.addPrisonCell(new PrisonCellInfo(new Vec3i(7, 64, 13)));

            PlayerInfo nullPlayer = null!;
            _storageMock.Setup(s => s.GetPlayerByUid(It.IsAny<string>(), out nullPlayer))
                        .Returns(false);

            string serialized = original.ToString();
            var restored = MakePrison();
            restored.fromString(serialized);

            var cell = restored.getPrisonCells()[0];
            Assert.Equal(7,  cell.getSpawnPosition().X);
            Assert.Equal(64, cell.getSpawnPosition().Y);
            Assert.Equal(13, cell.getSpawnPosition().Z);
        }
    }
}
