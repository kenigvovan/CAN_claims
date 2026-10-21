using System.Globalization;
using claims.src;
using claims.src.auxialiry;
using claims.src.commands;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using Moq;

namespace claims.tests
{
    // The checks of /plot claim (PlotCommand.CheckCanClaim): null means the player may buy.
    // Cases: section 4.
    [Collection("ModStatics")]
    public class PlotClaimRulesTests
    {
        private const string BuyerUid = "buyer-uid";
        private const string? Allowed = null;

        private readonly Mock<IMoneyProvider> economy = new();
        private readonly PlayerInfo buyer = new("Buyer", BuyerUid);
        private readonly City city = new("TestCity", "city-guid");
        private readonly Plot plot = new(new PlotPosition(0, 0));

        public PlotClaimRulesTests()
        {
            claims.src.claims.config = new Config();
            claims.src.claims.economyProvider = economy.Object;
            plot.setCity(city);
            SetCity(buyer, city);
        }

        // TC01, C3, section 4.1 (T1-T7)
        [Theory]
        [InlineData(100, "99.99", "claims:not_enough_money")]               // T1
        [InlineData(100, "100", Allowed)]                                   // T2
        [InlineData(100, "100.01", Allowed)]                                // T3
        [InlineData(100, "0", "claims:not_enough_money")]                   // T4
        [InlineData(0, "0", Allowed)]                                       // T5
        [InlineData(int.MaxValue, "2147483647", Allowed)]                   // T6
        [InlineData(100, "99.999999999999999999", "claims:not_enough_money")] // T7
        public void Claim_Should_CompareBalanceWithPrice(int price, string balance, string? expected)
        {
            plot.Price = price;
            economy.Setup(e => e.GetBalance(BuyerUid)).Returns(decimal.Parse(balance, CultureInfo.InvariantCulture));

            Assert.Equal(expected, PlotCommand.CheckCanClaim(buyer, plot)?.StatusMessage);
        }

        // TC03, C2 + C6, decision table 4.2 (columns 1-6)
        [Theory]
        [InlineData("own", false, true, Allowed)]                                           // 1
        [InlineData("own", true, true, Allowed)]                                            // 1, A6
        [InlineData("own", false, false, "claims:not_enough_money")]                        // 2
        [InlineData("other", true, true, Allowed)]                                          // 3
        [InlineData("none", true, true, Allowed)]                                           // 3
        [InlineData("other", true, false, "claims:not_enough_money")]                       // 4
        [InlineData("none", true, false, "claims:not_enough_money")]                        // 4
        [InlineData("other", false, true, "claims:cannot_buy_plot_in_another_city")]        // 5
        [InlineData("none", false, true, "claims:cannot_buy_plot_in_another_city")]         // 5
        [InlineData("other", false, false, "claims:cannot_buy_plot_in_another_city")]       // 6, R25
        [InlineData("none", false, false, "claims:cannot_buy_plot_in_another_city")]        // 6, R25
        public void Claim_Should_ApplyWhoMayBuyRules(string buyerCity, bool embassy, bool enoughMoney, string? expected)
        {
            SetCity(buyer, buyerCity switch
            {
                "own" => city,
                "other" => new City("OtherCity", "other-guid"),
                _ => null
            });
            plot.Type = embassy ? PlotType.EMBASSY : PlotType.DEFAULT;
            plot.Price = 50;
            economy.Setup(e => e.GetBalance(BuyerUid)).Returns(enoughMoney ? 100m : 0m);

            Assert.Equal(expected, PlotCommand.CheckCanClaim(buyer, plot)?.StatusMessage);
        }

        // PlayerInfo.City has a private setter; setCity() needs the running server.
        private static void SetCity(PlayerInfo player, City? c) =>
            typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(player, new object?[] { c });
    }
}
