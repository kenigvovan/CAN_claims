using claims.src;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure.plots;
using Moq;

namespace claims.tests
{
    /// <summary>How much of a refund the city's treasury can cover.</summary>
    [Collection("ModStatics")]
    public class PlotRefundTests
    {
        private readonly Mock<IMoneyProvider> economy = new();
        private readonly City city = new("TestCity", "refund-city-guid");
        private readonly Plot plot = new(new PlotPosition(0, 0));

        public PlotRefundTests()
        {
            claims.src.claims.config = new Config();
            claims.src.claims.economyProvider = economy.Object;
            plot.setCity(city);
        }

        private void Treasury(decimal balance) =>
            economy.Setup(e => e.GetBalance(city.MoneyAccountName)).Returns(balance);

        [Fact]
        public void Affordable_NoEconomyMod_IsTheWholeRefund()
        {
            // NoopMoneyProvider reports decimal.MaxValue; casting it to long threw out of /plot unclaim.
            claims.src.claims.economyProvider = new NoopMoneyProvider();

            Assert.Equal(50, PlotRefundHelper.GetMaxAffordableRefund(plot, 50));
        }

        [Theory]
        [InlineData("1000", 50, 50)]
        [InlineData("50", 50, 50)]
        [InlineData("30.7", 50, 30)]   // only whole coins are paid out
        [InlineData("0.5", 50, 0)]
        [InlineData("0", 50, 0)]
        [InlineData("-10", 50, 0)]     // a treasury in debt pays nothing
        public void Affordable_IsCappedByTheTreasury(string balance, long desired, long expected)
        {
            Treasury(decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture));

            Assert.Equal(expected, PlotRefundHelper.GetMaxAffordableRefund(plot, desired));
        }

        [Fact]
        public void Affordable_NothingDesired_IsZero()
        {
            Treasury(1000);

            Assert.Equal(0, PlotRefundHelper.GetMaxAffordableRefund(plot, 0));
        }

        /*=====================================================================*/
        /* RefundOnCityUnclaim                                                 */
        /*=====================================================================*/

        private void PaidClaimLongAgo()
        {
            claims.src.claims.config.PLOT_UNCLAIM_REFUND_PERCENT = 50;
            claims.src.claims.config.PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS = 0;
            claims.src.claims.config.PLOT_CLAIM_PRICE = 5;
            plot.TimeStampClaimed = 1000;
        }

        [Fact]
        public void CityUnclaim_PaidClaim_RefundsTheTreasury()
        {
            PaidClaimLongAgo();

            PlotRefundHelper.RefundOnCityUnclaim(plot);

            economy.Verify(e => e.Deposit(city.MoneyAccountName, 2.5m), Times.Once);
        }

        [Fact]
        public void CityUnclaim_FreeClaim_RefundsNothing()
        {
            // A village claims for free and leaves the stamp at 0; it stays 0 after an upgrade (VU5).
            PaidClaimLongAgo();
            plot.TimeStampClaimed = 0;

            PlotRefundHelper.RefundOnCityUnclaim(plot);

            economy.Verify(e => e.Deposit(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public void CityUnclaim_CapturedPlot_RefundsNothing()
        {
            PaidClaimLongAgo();
            plot.WasCaptured = true;

            PlotRefundHelper.RefundOnCityUnclaim(plot);

            economy.Verify(e => e.Deposit(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public void Affordable_PlotOutsideACity_IsZero()
        {
            Assert.Equal(0, PlotRefundHelper.GetMaxAffordableRefund(new Plot(new PlotPosition(1, 0)), 50));
        }
    }
}
