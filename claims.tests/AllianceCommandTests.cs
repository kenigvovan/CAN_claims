using System.Reflection;
using System.Runtime.Serialization;
using caneconomy.src.interfaces;
using claims.src;
using claims.src.auxialiry;
using claims.src.commands;
using claims.src.part;
using claims.src.part.structure;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.tests
{
    public class AllianceCommandTests
    {
        private readonly Mock<DataStorage> _storageMock;
        private readonly Mock<EconomyHandler> _economyMock;
        private readonly Mock<IServerPlayer> _playerMock;

        private const string TestPlayerUid = "alliance-test-uid";
        private const string ValidAllianceName = "TestAlliance";

        public AllianceCommandTests()
        {
            _storageMock = new Mock<DataStorage>(false);
            _economyMock = new Mock<EconomyHandler>();
            _playerMock = new Mock<IServerPlayer>();

            claims.src.claims.dataStorage = _storageMock.Object;
            claims.src.claims.economyHandler = _economyMock.Object;
            claims.src.claims.config = new Config();

            // Filter.checkForBlockedNames requires Settings.blockedNames to be initialized
            Settings.blockedNames = new HashSet<string>();

            _playerMock.Setup(p => p.PlayerUID).Returns(TestPlayerUid);

            // Initialize Lang so Lang.Get returns the key as-is instead of crashing
            if (!Lang.AvailableLanguages.ContainsKey("en"))
            {
                var langMock = new Mock<ITranslationService>();
                langMock.Setup(t => t.Get(It.IsAny<string>(), It.IsAny<object[]>()))
                    .Returns<string, object[]>((key, _) => key);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(false);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(false);
                Lang.AvailableLanguages["en"] = langMock.Object;
            }
            Lang.ChangeLanguage("en");
        }

        // Creates args with a string parser returning the given name value.
        private TextCommandCallingArgs MakeArgs(string allianceName)
        {
            var parser = new Mock<ICommandArgumentParser>();
            parser.Setup(p => p.GetValue()).Returns(allianceName);

            var caller = new Caller { Player = _playerMock.Object };
            return new TextCommandCallingArgs
            {
                Caller = caller,
                Parsers = { parser.Object }
            };
        }

        // Sets the private `mayor` field on a City via reflection.
        private static void SetMayor(City city, PlayerInfo mayor)
        {
            typeof(City)
                .GetField("mayor", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(city, mayor);
        }

        // Sets PlayerInfo.City via private setter, bypassing setCity() (which needs claims.sapi).
        private static void SetPlayerCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo)
                .GetProperty("City")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(playerInfo, new object[] { city });
        }

        /*=====================================================================*/
        /* Early-exit guard tests                                               */
        /*=====================================================================*/

        [Fact]
        public void CreateAlliance_PlayerNotFound_ReturnsError()
        {
            PlayerInfo nullPlayer = null!;
            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out nullPlayer))
                .Returns(false);

            var result = AllianceCommand.CreateAlliance(MakeArgs(ValidAllianceName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_such_player_info", result.StatusMessage);
        }

        [Fact]
        public void CreateAlliance_PlayerHasNoCity_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            // City is null by default → hasCity() == false

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            _lang
                .Setup(c => c.Get("claims:no_city", new object[] { }))
                .Returns("claims:no_city");

            var result = AllianceCommand.CreateAlliance(MakeArgs(ValidAllianceName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

       /* [Fact]
        public void CreateAlliance_CityAlreadyHasAlliance_ReturnsError()
        {
            var city = new City("TestCity", "city-guid");
            city.Alliance = new Alliance("ExistingAlliance", "alliance-guid");

            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            SetPlayerCity(playerInfo, city);

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            var result = AllianceCommand.CreateAlliance(MakeArgs(ValidAllianceName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void CreateAlliance_InvalidName_EmptyAfterFilter_ReturnsError()
        {
            var city = new City("TestCity", "city-guid");
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            SetPlayerCity(playerInfo, city);

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            // Special chars only → filterName returns ""
            var result = AllianceCommand.CreateAlliance(MakeArgs("!@#$%"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void CreateAlliance_NameAlreadyExists_ReturnsError()
        {
            var city = new City("TestCity", "city-guid");
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            SetPlayerCity(playerInfo, city);

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            var existingAlliance = new Alliance(ValidAllianceName, "existing-guid");
            _storageMock
                .Setup(s => s.GetAllianceByName(ValidAllianceName, out existingAlliance))
                .Returns(true);

            var result = AllianceCommand.CreateAlliance(MakeArgs(ValidAllianceName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void CreateAlliance_NotMayor_ReturnsError()
        {
            var city = new City("TestCity", "city-guid");
            // No mayor set → isMayor returns false

            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            SetPlayerCity(playerInfo, city);

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            Alliance nullAlliance = null!;
            _storageMock
                .Setup(s => s.GetAllianceByName(ValidAllianceName, out nullAlliance))
                .Returns(false);

            var result = AllianceCommand.CreateAlliance(MakeArgs(ValidAllianceName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void CreateAlliance_NotEnoughMoney_ReturnsError()
        {
            var city = new City("TestCity", "city-guid");
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            SetPlayerCity(playerInfo, city);
            SetMayor(city, playerInfo);

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            Alliance nullAlliance = null!;
            _storageMock
                .Setup(s => s.GetAllianceByName(ValidAllianceName, out nullAlliance))
                .Returns(false);

            // Balance less than NEW_ALLIANCE_COST (default 300)
            _economyMock
                .Setup(e => e.getBalance(city.MoneyAccountName))
                .Returns(10m);

            var result = AllianceCommand.CreateAlliance(MakeArgs(ValidAllianceName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        //=====================================================================
        // DeleteAlliance tests                                                 
        //=====================================================================

        // Creates a minimal TextCommandCallingArgs without any parsers (DeleteAlliance needs none).
        private TextCommandCallingArgs MakeArgsNoParser()
        {
            var caller = new Caller { Player = _playerMock.Object };
            return new TextCommandCallingArgs { Caller = caller };
        }

        // Builds a city+alliance with playerInfo as leader (mayor of MainCity).
        private (City city, Alliance alliance) MakeCityWithAlliance(PlayerInfo playerInfo, bool asLeader)
        {
            var city = new City("TestCity", "city-guid");
            var alliance = new Alliance("TestAlliance", "alliance-guid");
            alliance.MainCity = city;
            city.Alliance = alliance;

            if (asLeader)
                SetMayor(city, playerInfo);  // IsLeader checks city.getMayor()

            SetPlayerCity(playerInfo, city);
            return (city, alliance);
        }

        [Fact]
        public void DeleteAlliance_PlayerNotFound_ReturnsError()
        {
            PlayerInfo nullPlayer = null!;
            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out nullPlayer))
                .Returns(false);

            var result = AllianceCommand.DeleteAlliance(MakeArgsNoParser());

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void DeleteAlliance_PlayerHasNoAlliance_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            // City is null → HasAlliance() == false

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            var result = AllianceCommand.DeleteAlliance(MakeArgsNoParser());

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void DeleteAlliance_NotLeader_ReturnsError()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            MakeCityWithAlliance(playerInfo, asLeader: false);
            // No mayor set → IsLeader returns false

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            var result = AllianceCommand.DeleteAlliance(MakeArgsNoParser());

            Assert.Equal(EnumCommandStatus.Error, result.Status);
        }

        [Fact]
        public void DeleteAlliance_ActiveWarTime_ReturnsSuccess_CannotDelete()
        {
            var playerInfo = new PlayerInfo("TestPlayer", TestPlayerUid);
            var (_, alliance) = MakeCityWithAlliance(playerInfo, asLeader: true);

            var conflict = new claims.src.part.structure.conflict.Conflict("War", "conflict-guid")
            {
                ActiveWarTime = true
            };
            alliance.RunningConflicts.Add(conflict);

            _storageMock
                .Setup(s => s.GetPlayerByUid(TestPlayerUid, out playerInfo))
                .Returns(true);

            var result = AllianceCommand.DeleteAlliance(MakeArgsNoParser());

            // Method returns Success (not Error) with a "cannot delete during battle" message
            Assert.Equal(EnumCommandStatus.Success, result.Status);
        }*/
    }
}
