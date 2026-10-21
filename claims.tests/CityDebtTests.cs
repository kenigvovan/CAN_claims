using claims.src;
using claims.src.economy;
using claims.src.part.structure;
using Moq;

namespace claims.tests
{
    /// <summary>
    /// A deposit into an indebted city's treasury pays the debt first; only the rest stays to spend.
    /// </summary>
    [Collection("ModStatics")]
    public class CityDebtTests : IDisposable
    {
        private readonly ServerStubs stubs = new();
        private readonly IMoneyProvider previousEconomy = claims.src.claims.economyProvider;
        private readonly Mock<IMoneyProvider> economy = new();
        private readonly City city = new("DebtCity", "debt-city-guid");

        public CityDebtTests()
        {
            economy.Setup(e => e.Withdraw(It.IsAny<string>(), It.IsAny<decimal>())).Returns(MoneyOperationResult.Success);
            claims.src.claims.economyProvider = economy.Object;
        }

        public void Dispose()
        {
            claims.src.claims.economyProvider = previousEconomy;
            stubs.Dispose();
        }

        private void Balance(decimal amount) =>
            economy.Setup(e => e.GetBalance(city.MoneyAccountName)).Returns(amount);

        [Fact]
        public void Repay_NoDebt_TakesNothing()
        {
            Balance(100);

            Assert.Equal(0m, CityDebt.Repay(city, 50));
            economy.Verify(e => e.Withdraw(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public void Repay_DepositSmallerThanDebt_AllOfItGoesToDebt()
        {
            city.DebtBalance = 80;
            Balance(30);

            Assert.Equal(30m, CityDebt.Repay(city, 30));
            Assert.Equal(50, city.DebtBalance);
            economy.Verify(e => e.Withdraw(city.MoneyAccountName, 30m), Times.Once);
        }

        [Fact]
        public void Repay_DepositLargerThanDebt_RestStaysInTreasury()
        {
            city.DebtBalance = 20;
            Balance(50);

            Assert.Equal(20m, CityDebt.Repay(city, 50));
            Assert.Equal(0, city.DebtBalance);
            economy.Verify(e => e.Withdraw(city.MoneyAccountName, 20m), Times.Once);
        }

        [Fact]
        public void Repay_TakesNoMoreThanWasDeposited()
        {
            // Money already in the treasury is not touched: the next daily bill settles the debt.
            city.DebtBalance = 100;
            Balance(500);

            Assert.Equal(10m, CityDebt.Repay(city, 10));
            Assert.Equal(90, city.DebtBalance);
        }

        [Fact]
        public void Repay_TreasuryHoldsLessThanDeposit_TakesOnlyWhatIsThere()
        {
            city.DebtBalance = 100;
            Balance(5);

            Assert.Equal(5m, CityDebt.Repay(city, 40));
            Assert.Equal(95, city.DebtBalance);
        }

        [Fact]
        public void Repay_WithdrawalFails_DebtStays()
        {
            city.DebtBalance = 40;
            Balance(40);
            economy.Setup(e => e.Withdraw(It.IsAny<string>(), It.IsAny<decimal>())).Returns(MoneyOperationResult.TargetAccountNotFound);

            Assert.Equal(0m, CityDebt.Repay(city, 40));
            Assert.Equal(40, city.DebtBalance);
            stubs.Db.Verify(d => d.saveCity(It.IsAny<City>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void Repay_SavesTheNewDebt()
        {
            city.DebtBalance = 10;
            Balance(10);

            CityDebt.Repay(city, 10);

            stubs.Db.Verify(d => d.saveCity(city, It.IsAny<bool>()), Times.Once);
        }
    }
}
