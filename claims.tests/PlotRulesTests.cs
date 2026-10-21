using claims.src;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;

namespace claims.tests
{
    // Plot rules that need no server. Cases: section 6.
    // The refund reads the static claims.config, which other test classes replace too.
    [Collection("ModStatics")]
    public class PlotRulesTests
    {
        private const int MinAge = 300;

        public PlotRulesTests()
        {
            claims.src.claims.config = new Config
            {
                PLOT_UNCLAIM_REFUND_PERCENT = 50,
                PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS = MinAge
            };
        }

        // A plot that gets a refund: bought for lastPaidPrice, held for ageSeconds.
        private static Plot BoughtPlot(long lastPaidPrice = 100, long ageSeconds = MinAge + 60)
        {
            var plot = new Plot(new PlotPosition(0, 0));
            plot.setCity(new City("TestCity", "city-guid"));
            plot.lastPaidPrice = lastPaidPrice;
            plot.TimeStampClaimed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ageSeconds;
            return plot;
        }

        // TC16, U3, U4
        [Theory]
        [InlineData(100, 50)]
        [InlineData(99, 49)]   // integer division rounds down
        [InlineData(1, 0)]
        public void Refund_Should_BePercentOfPaidPrice(long paid, long expected)
        {
            Assert.Equal(expected, PlotRefundHelper.CalculatePersonalUnclaimRefund(BoughtPlot(paid)));
        }

        // TC16, U3
        [Fact]
        public void Refund_Should_BeZero_When_RefundIsOff()
        {
            claims.src.claims.config.PLOT_UNCLAIM_REFUND_PERCENT = 0;
            Assert.Equal(0, PlotRefundHelper.CalculatePersonalUnclaimRefund(BoughtPlot()));
        }

        // TC16, U3
        [Fact]
        public void Refund_Should_BeZero_When_PlotWasCaptured()
        {
            Plot plot = BoughtPlot();
            plot.WasCaptured = true;
            Assert.Equal(0, PlotRefundHelper.CalculatePersonalUnclaimRefund(plot));
        }

        // TC16, U3
        [Fact]
        public void Refund_Should_BeZero_When_PlotWasNeverBought()
        {
            Assert.Equal(0, PlotRefundHelper.CalculatePersonalUnclaimRefund(BoughtPlot(lastPaidPrice: 0)));
        }

        // TC16, U3: the boundary is MinAge. The clock is real, so the "too young" case keeps a
        // margin instead of MinAge - 1: a second ticking over mid-test would make it flaky.
        [Theory]
        [InlineData(MinAge, 50)]
        [InlineData(MinAge - 10, 0)]
        public void Refund_Should_DependOnPlotAge(long ageSeconds, long expected)
        {
            Assert.Equal(expected, PlotRefundHelper.CalculatePersonalUnclaimRefund(BoughtPlot(ageSeconds: ageSeconds)));
        }

        // R10 H3, P5: the city unclaim refund comes from the current PLOT_CLAIM_PRICE, not from
        // what the city paid. Claimed at 5, the admin raises the price to 100, the city gives the
        // plot back: it must get at most 50% of the 5 it paid.
        [Fact]
        public void CityUnclaimRefund_Should_NotExceedWhatTheCityPaid_When_PriceWasRaisedSince()
        {
            var economy = new Moq.Mock<claims.src.economy.IMoneyProvider>();
            claims.src.claims.economyProvider = economy.Object;
            claims.src.claims.config.PLOT_CLAIM_PRICE = 100; // was 5 when the plot was claimed
            Plot plot = BoughtPlot(lastPaidPrice: 0);

            PlotRefundHelper.RefundOnCityUnclaim(plot);

            economy.Verify(e => e.Deposit(Moq.It.IsAny<string>(), Moq.It.Is<decimal>(a => a > 2.5m)), Moq.Times.Never(),
                "the refund grew with the price the city never paid");
        }

        // The percent is clamped to 0..100: above 100 a claim/unclaim round would earn money.
        // 0 itself is Refund_Should_BeZero_When_RefundIsOff.
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(1, 1)]
        [InlineData(100, 100)]
        [InlineData(101, 100)]
        [InlineData(1000, 100)]
        public void Refund_Should_ClampPercentTo0To100(int percent, long expected)
        {
            claims.src.claims.config.PLOT_UNCLAIM_REFUND_PERCENT = percent;

            Assert.Equal(expected, PlotRefundHelper.CalculatePersonalUnclaimRefund(BoughtPlot(lastPaidPrice: 100)));
        }

        // Same clamp for the city's own refund: never more than the claim price.
        [Fact]
        public void CityUnclaimRefund_Should_NotExceedClaimPrice_When_PercentIsAbove100()
        {
            var economy = new Moq.Mock<claims.src.economy.IMoneyProvider>();
            claims.src.claims.economyProvider = economy.Object;
            claims.src.claims.config.PLOT_CLAIM_PRICE = 10;
            claims.src.claims.config.PLOT_UNCLAIM_REFUND_PERCENT = 150;

            PlotRefundHelper.RefundOnCityUnclaim(BoughtPlot());

            economy.Verify(e => e.Deposit(Moq.It.IsAny<string>(), Moq.It.Is<decimal>(a => a > 10m)), Moq.Times.Never(),
                "the city got back more than the claim price");
        }

        // TC25, R23
        [Theory]
        [InlineData(-2, false)]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void IsForSale_Should_BeTrueOnlyForNonNegativePrice(int price, bool expected)
        {
            var plot = new Plot(new PlotPosition(0, 0)) { Price = price };

            Assert.Equal(expected, plot.IsForSale);
        }
    }
}
