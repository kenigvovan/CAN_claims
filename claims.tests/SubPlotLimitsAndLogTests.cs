using System.Linq;
using System.Reflection;
using claims.src;
using claims.src.citylog;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using claims.src.rights;
using claims.src.subplots;
using claims.src.subplots.geometry;
using Moq;
using Vintagestory.API.Common;

namespace claims.tests
{
    /// <summary>The limits a host sets on sub-plots, and what the city log keeps of them.</summary>
    [Collection("ModStatics")]
    public class SubPlotLimitsAndLogTests : IDisposable
    {
        private readonly ServerStubs stubs = new(new Config
        {
            PLOT_SIZE = 16,
            SUBPLOT_MAX_PER_CITY = 2,
            SUBPLOT_MAX_OWNED_PER_PLAYER = 1,
            SUBPLOT_MAX_VOLUME = 1000,
            SUBPLOT_MIN_VOLUME = 8,
            SUBPLOT_TAX_RAISE_DELAY_HOURS = 24
        });
        private readonly IMoneyProvider previousEconomy = claims.src.claims.economyProvider;
        private readonly Mock<IMoneyProvider> economy = new();
        private readonly City city = new("TestCity", "spl2-city");
        private readonly PlayerInfo manager = new("Manager", "spl2-manager");
        private readonly PlayerInfo citizen = new("Citizen", "spl2-citizen");

        public SubPlotLimitsAndLogTests()
        {
            economy.Setup(e => e.GetBalance(It.IsAny<string>())).Returns(1000m);
            economy.Setup(e => e.Transfer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                   .Returns(MoneyOperationResult.Success);
            claims.src.claims.economyProvider = economy.Object;
            foreach (var p in new[] { manager, citizen })
            {
                typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(p, new object[] { city });
                city.getCityCitizens().Add(p);
            }
            manager.PlayerPermissionsHandler.AddPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);
            var plot = new Plot(new claims.src.auxialiry.PlotPosition(0, 0));
            plot.setCity(city);
            stubs.Storage.addClaimedPlot(plot.plotPosition, plot);
        }

        public void Dispose()
        {
            claims.src.claims.economyProvider = previousEconomy;
            stubs.Dispose();
        }

        private static bool Ok(TextCommandResult r) => r.Status == EnumCommandStatus.Success;

        private SubPlot Make(string name, int y)
        {
            Assert.True(Ok(SubPlotEditor.Create(manager, name, new BlockBox(0, y, 0, 4, y + 4, 4))));
            return SubPlotEditor.FindByName(city, name);
        }

        private bool Logged(EnumCityLogEvent type) => city.EventLog.Any(e => e.EventType == type);

        [Fact]
        public void TooBigOrTooSmall_IsRefused_WithTheLimitInTheReason()
        {
            Assert.Equal("claims:subplot_too_big",
                SubPlotEditor.Create(manager, "hall", new BlockBox(0, 64, 0, 16, 74, 16)).StatusMessage);
            Assert.Equal("claims:subplot_too_small",
                SubPlotEditor.Create(manager, "cupboard", new BlockBox(0, 64, 0, 1, 65, 1)).StatusMessage);
        }

        [Fact]
        public void GrowingPastTheVolume_IsRefused_TooForAnExistingOne()
        {
            var flat = Make("flat", 64);

            Assert.Equal("claims:subplot_too_big", SubPlotEditor.WhyNotChange(flat, new BlockBox(0, 64, 0, 16, 74, 16), add: true));
        }

        [Fact]
        public void CityLimit_StopsTheNextOne()
        {
            Make("a", 64);
            Make("b", 70);

            Assert.Equal("claims:subplot_city_limit",
                SubPlotEditor.Create(manager, "c", new BlockBox(0, 80, 0, 4, 84, 4)).StatusMessage);
        }

        [Fact]
        public void OwnerLimit_StopsGivingAndBuyingASecondOne()
        {
            var a = Make("a", 64);
            var b = Make("b", 70);
            Assert.True(Ok(SubPlotEditor.SetOwner(manager, a, citizen)));

            Assert.Equal("claims:subplot_owner_limit", SubPlotEditor.SetOwner(manager, b, citizen).StatusMessage);
            SubPlotEditor.SetForSale(manager, b, 10);
            Assert.Equal("claims:subplot_owner_limit", SubPlotEditor.Buy(citizen, b).StatusMessage);
        }

        [Fact]
        public void GivingTheSameOwnerAgain_IsNotCountedAgainstThem()
        {
            var a = Make("a", 64);
            SubPlotEditor.SetOwner(manager, a, citizen);

            Assert.True(Ok(SubPlotEditor.SetOwner(manager, a, citizen)));
        }

        [Fact]
        public void Log_KeepsCreation_Sale_Return_AndRemoval()
        {
            var flat = Make("flat", 64);
            SubPlotEditor.SetForSale(manager, flat, 10);
            SubPlotEditor.Buy(citizen, flat);
            SubPlotEditor.GiveBack(citizen, flat);
            SubPlotEditor.Delete(manager, flat);

            Assert.True(Logged(EnumCityLogEvent.SubPlotCreated));
            var sold = city.EventLog.Single(e => e.EventType == EnumCityLogEvent.SubPlotSold);
            Assert.Equal(new[] { "flat", "Citizen", "10" }, sold.Args);
            Assert.True(Logged(EnumCityLogEvent.SubPlotReturned));
            Assert.True(Logged(EnumCityLogEvent.SubPlotRemoved));
        }

        [Fact]
        public void RefusedTax_IsLoggedAsSuch_NotAsAPlainReturn()
        {
            var flat = Make("flat", 64);
            SubPlotEditor.SetOwner(manager, flat, citizen);
            SubPlotTaxHelper.SetTax(manager, flat, 10, 1000);

            SubPlotTaxHelper.SettleDueRaises(1000 + 24 * 3600);

            Assert.True(Logged(EnumCityLogEvent.SubPlotTaxRefused));
            Assert.False(Logged(EnumCityLogEvent.SubPlotReturned));
        }
    }
}
