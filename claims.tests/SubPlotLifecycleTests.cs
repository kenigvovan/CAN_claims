using System.Linq;
using System.Reflection;
using claims.src;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using claims.src.subplots;
using claims.src.subplots.geometry;
using Moq;
using Vintagestory.API.MathTools;

namespace claims.tests
{
    /// <summary>
    /// What happens to sub-plots when a chunk under them changes: a change the city
    /// chooses is refused, a forced one cuts the chunk out and removes what cannot stand alone.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotLifecycleTests : IDisposable
    {
        private readonly ServerStubs stubs = new(new Config { PLOT_SIZE = 16 });
        private readonly IMoneyProvider previousEconomy = claims.src.claims.economyProvider;
        private readonly Mock<IMoneyProvider> economy = new();
        private readonly City city = new("TestCity", "spl-city");
        private readonly PlayerInfo owner = new("Owner", "spl-owner");
        private readonly PlayerInfo member = new("Member", "spl-member");

        public SubPlotLifecycleTests()
        {
            economy.Setup(e => e.GetBalance(It.IsAny<string>())).Returns(100m);
            economy.Setup(e => e.Transfer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                   .Returns(MoneyOperationResult.Success);
            claims.src.claims.economyProvider = economy.Object;
        }

        public void Dispose()
        {
            claims.src.claims.economyProvider = previousEconomy;
            stubs.Dispose();
        }

        private SubPlot Add(string guid, BlockBox box, long paid = 0)
        {
            var subPlot = new SubPlot(guid, guid) { City = city, Owner = owner, Shape = new BoxShape(box), LastPaidPrice = paid };
            subPlot.Members.Add(member);
            stubs.Storage.AddSubPlot(subPlot);
            return subPlot;
        }

        private bool Exists(SubPlot subPlot) => stubs.Storage.TryGetSubPlot(subPlot.Guid, out _);

        [Fact]
        public void Without_TakesTheAreaOut_AndIsNullWhenNothingIsLeft()
        {
            var shape = new BoxShape(new BlockBox(8, 64, 0, 24, 68, 8));

            BoxShape rest = shape.Without(new BlockBox(16, 0, 0, 32, 256, 16));

            Assert.Equal(8L * 4 * 8, rest.Volume);
            Assert.False(rest.Contains(16, 64, 0));
            Assert.Null(shape.Without(new BlockBox(0, 0, 0, 32, 256, 16)));
            Assert.Same(shape, shape.Without(new BlockBox(100, 0, 100, 116, 256, 116)));
        }

        [Fact]
        public void ChunkWithSubPlot_RefusesWhatTheCityChooses()
        {
            Add("spl-a", new BlockBox(8, 64, 0, 24, 68, 8));

            Assert.NotNull(SubPlotLifecycle.BlockingProblem(new Vec2i(0, 0)));
            Assert.NotNull(SubPlotLifecycle.BlockingProblem(new Vec2i(1, 0)));
            Assert.Null(SubPlotLifecycle.BlockingProblem(new Vec2i(2, 0)));
        }

        [Fact]
        public void LostChunk_IsCutOut_AndTheRestStays()
        {
            var flat = Add("spl-b", new BlockBox(8, 64, 0, 24, 68, 8));

            SubPlotLifecycle.CutChunk(new Vec2i(1, 0));

            Assert.True(Exists(flat));
            Assert.Equal(16, flat.Shape.Bounds.MaxX);
            Assert.Empty(stubs.Storage.SubPlotsInChunk(new Vec2i(1, 0)));
            Assert.Single(stubs.Storage.SubPlotsInChunk(new Vec2i(0, 0)));
            economy.Verify(e => e.Transfer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public void SubPlotWholeInTheLostChunk_IsRemoved_AndTheOwnerRefunded()
        {
            var flat = Add("spl-c", new BlockBox(0, 64, 0, 8, 68, 8), paid: 40);

            SubPlotLifecycle.CutChunk(new Vec2i(0, 0));

            Assert.False(Exists(flat));
            economy.Verify(e => e.Transfer(city.MoneyAccountName, owner.MoneyAccountName, 40m), Times.Once);
        }

        [Fact]
        public void SubPlotCutInTwo_IsRemoved()
        {
            // Over chunks 0, 1 and 2; losing the middle one leaves two pieces.
            var flat = Add("spl-d", new BlockBox(8, 64, 0, 40, 68, 8));

            SubPlotLifecycle.CutChunk(new Vec2i(1, 0));

            Assert.False(Exists(flat));
            Assert.Empty(stubs.Storage.SubPlotsInChunk(new Vec2i(0, 0)));
            Assert.Empty(stubs.Storage.SubPlotsInChunk(new Vec2i(2, 0)));
        }

        [Fact]
        public void Refund_IsWhatWasPaid_CappedAtTheTreasury()
        {
            var flat = new SubPlot("r", "spl-r") { City = city, Owner = owner, LastPaidPrice = 50 };

            Assert.Equal(50, SubPlotLifecycle.RefundAmount(flat, 100m));
            Assert.Equal(30, SubPlotLifecycle.RefundAmount(flat, 30.7m));
            Assert.Equal(0, SubPlotLifecycle.RefundAmount(flat, 0m));
            flat.Owner = null;
            Assert.Equal(0, SubPlotLifecycle.RefundAmount(flat, 100m));
        }

        [Fact]
        public void OwnerLeavingTheCity_GivesTheSubPlotBack()
        {
            var flat = Add("spl-e", new BlockBox(0, 64, 0, 8, 68, 8), paid: 40);
            flat.Price = 10;

            SubPlotLifecycle.OnPlayerLeftCity(owner, city);

            Assert.True(Exists(flat));
            Assert.Null(flat.Owner);
            Assert.Empty(flat.Members);
            Assert.False(flat.IsForSale);
            Assert.Equal(0, flat.LastPaidPrice);
            economy.Verify(e => e.Transfer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public void MemberLeavingTheCity_LosesOnlyTheMembership()
        {
            var flat = Add("spl-f", new BlockBox(0, 64, 0, 8, 68, 8));

            SubPlotLifecycle.OnPlayerLeftCity(member, city);

            Assert.Same(owner, flat.Owner);
            Assert.Empty(flat.Members);
        }

        [Fact]
        public void CityGoingAway_TakesAllItsSubPlots_LeavesOthers()
        {
            var ours = Add("spl-g", new BlockBox(0, 64, 0, 8, 68, 8));
            var other = new SubPlot("h", "spl-h") { City = new City("Other", "spl-other"), Shape = new BoxShape(new BlockBox(64, 64, 0, 72, 68, 8)) };
            stubs.Storage.AddSubPlot(other);

            SubPlotLifecycle.DissolveAllOf(city);

            Assert.False(Exists(ours));
            Assert.True(Exists(other));
        }
    }
}
