using claims.src;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.commands;
using claims.src.part;
using claims.src.part.structure;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using Moq;
using System.Reflection;
using System.Runtime.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Xunit.Abstractions;

namespace claims.tests
{
    public class PlotClaimTests
    {
        private readonly ITestOutputHelper _output;
        private Mock<DataStorage> _storageMock;
        private Mock<IMoneyProvider> _economyMock;
        private Mock<IServerPlayer> _playerMock;
        private DataStorage realDataStorage;

        private const string TestPlayerUid = "test-uid-1";
        private const string TestPlayerUid2 = "test-uid-2";
        private const string NotFoundTestPlayerUid = "not-found-uid";

        public PlotClaimTests(ITestOutputHelper output)
        {
            // Pass false to use the client-side constructor branch (no game API calls)
            _storageMock = new Mock<DataStorage>(false);
            _economyMock = new Mock<IMoneyProvider>();
            _playerMock = new Mock<IServerPlayer>();

            claims.src.claims.dataStorage = _storageMock.Object;
            claims.src.claims.economyProvider = _economyMock.Object;

            _playerMock.Setup(p => p.PlayerUID).Returns(TestPlayerUid);
            _output = output;
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] New instance");

            // Initialize Lang so Lang.Get returns the key as-is instead of crashing
            if (!Vintagestory.API.Config.Lang.AvailableLanguages.ContainsKey("en"))
            {
                var langMock = new Mock<Vintagestory.API.Config.ITranslationService>();
                langMock.Setup(t => t.Get(It.IsAny<string>(), It.IsAny<object[]>()))
                    .Returns<string, object[]>((key, _) => key);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(false);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(false);
                Vintagestory.API.Config.Lang.AvailableLanguages["en"] = langMock.Object;
            }
            Vintagestory.API.Config.Lang.ChangeLanguage("en");
        }

        // Creates a TextCommandCallingArgs with the given player as caller.
        private static TextCommandCallingArgs MakeArgs(IServerPlayer player)
        {
            var caller = new Caller();
            // Caller.Player setter calls player.Entity; null entity is handled gracefully.
            caller.Player = player;
            return new TextCommandCallingArgs { Caller = caller };
        }

        // Creates an EntityPlayer with Pos initialized, bypassing constructors.
        private static EntityPlayer CreateEntityPlayer(double x = 0, double z = 0)
        {
            var entity = new EntityPlayer();

            entity.Pos.SetPos(0, 0, 0);
            return entity;
        }
        private static EntityPlayer CreateNotFoundEntityPlayer(double x = 0, double z = 0)
        {
            var entity = new EntityPlayer();

            entity.Pos.SetPos(0, 0, 0);
            return entity;
        }

        // Sets playerInfo.City via the private setter, bypassing setCity() which needs claims.sapi.
        private static void SetPlayerCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo)
                .GetProperty("City")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(playerInfo, new object[] { city });
        }

        // Sets up both data-storage mocks for the common case.
        private void SetupPlayerAndPlot(PlayerInfo playerInfo, Plot plot)
        {
            _playerMock.Setup(p => p.Entity).Returns(CreateEntityPlayer());

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            _storageMock
                .Setup(s => s.GetPlot(It.IsAny<PlotPosition>(), out plot))
                .Returns(true);
        }
        private void SetupPlayers()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var playerInfo2 = new PlayerInfo("TestPlayer2", TestPlayerUid2);
            realDataStorage = new(false);
            realDataStorage.addPlayer(playerInfo);
            realDataStorage.addPlayer(playerInfo2);
        }

        /*=====================================================================*/
        /* Early-exit guard tests (no payment logic reached)                   */
        /*=====================================================================*/

        [Fact]
        public void PlotClaim_PlayerNotFound_ReturnsError()
        {
            _output.WriteLine($"1.[{DateTime.Now:HH:mm:ss.fff}] START");
            SetupPlayers();
            claims.src.claims.config = new Config
            {
                PLOT_SIZE = 16
            };
            claims.src.claims.dataStorage = this.realDataStorage;
            _playerMock.Setup(p => p.PlayerUID).Returns(NotFoundTestPlayerUid);
            _playerMock.Setup(p => p.Entity).Returns(CreateEntityPlayer());
            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));
            
            _output.WriteLine($"1.[{DateTime.Now:HH:mm:ss.fff}] END");
            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_such_player", result.StatusMessage);

        }

        [Fact]
        public void PlotClaim_NoPlotHere_ReturnsError()
        {
            _output.WriteLine($"2.[{DateTime.Now:HH:mm:ss.fff}] START");
            SetupPlayers();
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            _playerMock.Setup(p => p.Entity).Returns(CreateEntityPlayer());
            claims.src.claims.config = new Config
            {
                PLOT_SIZE = 16
            };
            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            Plot nullPlot = null!;
            _storageMock
                .Setup(s => s.GetPlot(It.IsAny<PlotPosition>(), out nullPlot))
                .Returns(false);

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));
            _output.WriteLine($"2.[{DateTime.Now:HH:mm:ss.fff}] END");
            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_plots_here", result.StatusMessage);
        }

        /*[Fact]
        public void PlotClaim_PlotAlreadyHasOwner_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var existingOwner = new PlayerInfo("Owner", "owner-uid");
            var plot = new Plot(new PlotPosition(0, 0));
            plot.setPlotOwner(existingOwner);

            SetupPlayerAndPlot(playerInfo, plot);

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:plot_has_owner_already", result.StatusMessage);
        }

        [Fact]
        public void PlotClaim_PlotNotForSale_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var plot = new Plot(new PlotPosition(0, 0));
            // Price == -1 by default → IsForSale == false

            SetupPlayerAndPlot(playerInfo, plot);

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:not_for_sale", result.StatusMessage);
        }

        [Fact]
        public void PlotClaim_PlotHasNoCity_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var plot = new Plot(new PlotPosition(0, 0));
            plot.Price = 100;

            SetupPlayerAndPlot(playerInfo, plot);

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_city_here", result.StatusMessage);
        }

        [Fact]
        public void PlotClaim_PlotBelongsToPlotsGroup_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var city = new City("TestCity", "city-guid");
            var group = new CityPlotsGroup("TestGroup", "group-guid");
            var plot = new Plot(new PlotPosition(0, 0));
            plot.Price = 100;
            plot.setCity(city);
            plot.setPlotGroup(group);

            SetupPlayerAndPlot(playerInfo, plot);

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:has_plot_group", result.StatusMessage);
        }

        [Fact]
        public void PlotClaim_NotEnoughMoney_ReturnsError()
        {
            claims.src.claims.config = new Config();
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var city = new City("TestCity", "city-guid");
            var plot = new Plot(new PlotPosition(0, 0));
            //claims.src.claims.config.PLOT_SIZE = 16;
            //PlotPosition.plotSize = 16;
            plot.Price = 1000;
            plot.setCity(city);

            SetupPlayerAndPlot(playerInfo, plot);
            _economyMock.Setup(e => e.getBalance(TestPlayerUid)).Returns(50m);

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:not_enough_money", result.StatusMessage);
        }

        //=====================================================================
        //Payment-path tests (VIRTUAL_MONEY handler)                          
        //=====================================================================

        [Fact]
        public void PlotClaim_DepositFromAToBFails_ReturnsEconomyError()
        {
            caneconomy.caneconomy.config = new caneconomy.src.Config
            {
                SELECTED_ECONOMY_HANDLER = "VIRTUAL_MONEY"
            };

            var city = new City("TestCity", "city-guid");
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            SetPlayerCity(playerInfo, city);

            var plot = new Plot(new PlotPosition(0, 0));
            plot.Price = 100;
            plot.setCity(city);

            SetupPlayerAndPlot(playerInfo, plot);
            _economyMock.Setup(e => e.getBalance(TestPlayerUid)).Returns(500m);
            _economyMock
                .Setup(e => e.depositFromAToB(TestPlayerUid, city.MoneyAccountName, 100m))
                .Returns(new OperationResult(OperationResult.EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY));

            var result = PlotCommand.PlotClaim(MakeArgs(_playerMock.Object));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:economy_money_transaction_error", result.StatusMessage);
        }*/
    }
}
