using System.Collections.Concurrent;
using System.Reflection;
using caneconomy.src.interfaces;
using claims.src;
using claims.src.auxialiry;
using claims.src.commands;
using claims.src.gui.playerGui.structures;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.tests
{
    public class DeclareCityConflictTests
    {
        private readonly Mock<DataStorage> _storageMock;
        private readonly Mock<IServerPlayer> _playerMock;

        private const string TestPlayerUid = "city-war-test-uid";
        private const string TargetName = "TargetCity";

        public DeclareCityConflictTests()
        {
            _storageMock = new Mock<DataStorage>(false);
            _playerMock = new Mock<IServerPlayer>();

            claims.src.claims.dataStorage = _storageMock.Object;
            claims.src.claims.config = new Config();
            claims.src.claims.economyHandler = new Mock<EconomyHandler>().Object;

            Settings.blockedNames = new HashSet<string>();

            _playerMock.Setup(p => p.PlayerUID).Returns(TestPlayerUid);

            // Reset static ConflictHandler state between tests
            ConflictHandler.clearAll();

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

        private TextCommandCallingArgs MakeArgs(string targetName)
        {
            var parser = new Mock<ICommandArgumentParser>();
            parser.Setup(p => p.GetValue()).Returns(targetName);
            return new TextCommandCallingArgs
            {
                Caller = new Caller { Player = _playerMock.Object },
                Parsers = { parser.Object }
            };
        }

        private static void SetMayor(City city, PlayerInfo mayor)
        {
            typeof(City)
                .GetField("mayor", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(city, mayor);
        }

        private static void SetPlayerCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo)
                .GetProperty("City")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(playerInfo, new object[] { city });
        }

        // Builds a standalone city (no alliance) where playerInfo is mayor.
        private PlayerInfo MakeMayorWithCity(string cityName = "OurCity")
        {
            var player = new PlayerInfo("TestPlayer", TestPlayerUid);
            var city = new City(cityName, "our-city-guid");
            SetPlayerCity(player, city);
            SetMayor(city, player);
            _storageMock.Setup(s => s.GetPlayerByUid(TestPlayerUid, out player)).Returns(true);
            return player;
        }

        // Sets up storage to return a standalone target city by name.
        private City SetupStandaloneTargetCity(string name = TargetName)
        {
            var targetCity = new City(name, "target-guid");
            City nullCity = null!;
            _storageMock.Setup(s => s.GetCityByName(name, out targetCity)).Returns(true);
            Alliance nullAlliance = null!;
            _storageMock.Setup(s => s.GetAllianceByName(name, out nullAlliance)).Returns(false);
            return targetCity;
        }

        /*=====================================================================*/
        /* Guard checks — early exits                                            */
        /*=====================================================================*/

        [Fact]
        public void Declare_PlayerNotFound_ReturnsError()
        {
            PlayerInfo nullPlayer = null!;
            _storageMock.Setup(s => s.GetPlayerByUid(TestPlayerUid, out nullPlayer)).Returns(false);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:no_such_player_info", result.StatusMessage);
        }

        [Fact]
        public void Declare_PlayerHasNoCity_ReturnsError()
        {
            var player = new PlayerInfo("TestPlayer", TestPlayerUid);
            _storageMock.Setup(s => s.GetPlayerByUid(TestPlayerUid, out player)).Returns(true);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:no_city", result.StatusMessage);
        }

        [Fact]
        public void Declare_PlayerInAlliance_ReturnsError()
        {
            var player = new PlayerInfo("TestPlayer", TestPlayerUid);
            var city = new City("OurCity", "our-guid");
            var alliance = new Alliance("OurAlliance", "alliance-guid");
            city.Alliance = alliance;
            SetPlayerCity(player, city);
            SetMayor(city, player);
            _storageMock.Setup(s => s.GetPlayerByUid(TestPlayerUid, out player)).Returns(true);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:city_in_alliance_use_alliance_war", result.StatusMessage);
        }

        [Fact]
        public void Declare_PlayerNotMayor_ReturnsError()
        {
            var player = new PlayerInfo("TestPlayer", TestPlayerUid);
            var city = new City("OurCity", "our-guid");
            // No mayor set → isMayor() == false
            SetPlayerCity(player, city);
            _storageMock.Setup(s => s.GetPlayerByUid(TestPlayerUid, out player)).Returns(true);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:you_are_not_mayor", result.StatusMessage);
        }

        [Fact]
        public void Declare_OurCityNeutral_ReturnsError()
        {
            var player = MakeMayorWithCity();
            player.City.Neutral = true;

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:our_alliance_is_neutral", result.StatusMessage);
        }

        /*=====================================================================*/
        /* TryResolveWarTarget                                                   */
        /*=====================================================================*/

        [Fact]
        public void Declare_TargetCityInAlliance_ReturnsError()
        {
            MakeMayorWithCity();

            var targetCity = new City(TargetName, "target-guid");
            targetCity.Alliance = new Alliance("TheirAlliance", "their-alliance-guid");
            _storageMock.Setup(s => s.GetCityByName(TargetName, out targetCity)).Returns(true);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:city_in_alliance_attack_alliance", result.StatusMessage);
        }

        [Fact]
        public void Declare_TargetNotFound_ReturnsError()
        {
            MakeMayorWithCity();

            City nullCity = null!;
            Alliance nullAlliance = null!;
            _storageMock.Setup(s => s.GetCityByName(TargetName, out nullCity)).Returns(false);
            _storageMock.Setup(s => s.GetAllianceByName(TargetName, out nullAlliance)).Returns(false);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:no_such_city_or_alliance", result.StatusMessage);
        }

        [Fact]
        public void Declare_InvalidName_ReturnsError()
        {
            MakeMayorWithCity();

            var result = CityCommand.DeclareCityConflict(MakeArgs("!@#$%"));

            Assert.True(result.Status == EnumCommandStatus.Error,
                $"Expected Error but got {result.Status}: '{result.StatusMessage}'");
        }

        [Fact]
        public void Declare_TargetIsOurCity_ReturnsError()
        {
            var player = MakeMayorWithCity("OurCity");
            // Return our own city as the target
            var ourCity = player.City;
            _storageMock.Setup(s => s.GetCityByName(TargetName, out ourCity)).Returns(true);
            Alliance nullAlliance = null!;
            _storageMock.Setup(s => s.GetAllianceByName(TargetName, out nullAlliance)).Returns(false);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:same_alliance", result.StatusMessage);
        }

        [Fact]
        public void Declare_TargetNeutral_ReturnsError()
        {
            MakeMayorWithCity();
            var target = SetupStandaloneTargetCity();
            target.Neutral = true;

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:target_alliance_is_neutral", result.StatusMessage);
        }

        [Fact]
        public void Declare_ConflictAlreadyExists_ReturnsError()
        {
            var player = MakeMayorWithCity();
            var target = SetupStandaloneTargetCity();

            // Add a pre-existing conflict between the two cities
            var existing = new Conflict("", "existing-guid") { First = player.City, Second = target };
            _storageMock.Object.conflicts.Add(existing);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:conflict_already_exists", result.StatusMessage);
        }

        /*=====================================================================*/
        /* Letter flow (NEED_AGREE_FOR_CONFLICT = true)                          */
        /*=====================================================================*/

        [Fact]
        public void Declare_LetterSent_ReturnsLetterSent()
        {
            claims.src.claims.config.NEED_AGREE_FOR_CONFLICT = true;

            var player = MakeMayorWithCity();
            SetupStandaloneTargetCity(); // target city has no citizens → sendMsgInCity is a no-op

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:conflict_letter_sent", result.StatusMessage);
        }

        [Fact]
        public void Declare_LetterDuplicate_ReturnsDuplicate()
        {
            claims.src.claims.config.NEED_AGREE_FOR_CONFLICT = true;

            var player = MakeMayorWithCity();
            var target = SetupStandaloneTargetCity();

            // Pre-insert a letter so the second attempt is a duplicate
            var existingLetter = new ConflictLetter(
                player.City, target, LetterPurpose.START_CONFLICT,
                long.MaxValue,
                new Thread(() => { }),
                new Thread(() => { }),
                "existing-letter-guid");
            ConflictHandler.addConflictLetter(existingLetter);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:conflict_letter_is_duplicate", result.StatusMessage);
        }

        /*=====================================================================*/
        /* TryResolveWarTarget — standalone city vs alliance target              */
        /*=====================================================================*/

        [Fact]
        public void Declare_TargetIsAlliance_LetterSent()
        {
            claims.src.claims.config.NEED_AGREE_FOR_CONFLICT = true;

            MakeMayorWithCity();

            City nullCity = null!;
            _storageMock.Setup(s => s.GetCityByName(TargetName, out nullCity)).Returns(false);
            var targetAlliance = new Alliance(TargetName, "target-alliance-guid");
            // Alliance has no cities → sendMsgInCity is a no-op
            _storageMock.Setup(s => s.GetAllianceByName(TargetName, out targetAlliance)).Returns(true);

            var result = CityCommand.DeclareCityConflict(MakeArgs(TargetName));

            Assert.Equal("claims:conflict_letter_sent", result.StatusMessage);
        }

        /*=====================================================================*/
        /* ALLIANCE_LETTER_REMOVE sent on deny / revoke / accept                */
        /*=====================================================================*/

        private const string TargetPlayerUid = "target-player-uid";

        /// <summary>
        /// Helper: sets up a second player who is mayor of the target city,
        /// and returns args for that player.
        /// </summary>
        private TextCommandCallingArgs MakeTargetMayorArgs(City targetCity, string targetName)
        {
            var targetPlayer = new Mock<IServerPlayer>();
            targetPlayer.Setup(p => p.PlayerUID).Returns(TargetPlayerUid);

            var targetPlayerInfo = new PlayerInfo("TargetPlayer", TargetPlayerUid);
            SetPlayerCity(targetPlayerInfo, targetCity);
            SetMayor(targetCity, targetPlayerInfo);
            _storageMock.Setup(s => s.GetPlayerByUid(TargetPlayerUid, out targetPlayerInfo)).Returns(true);

            var parser = new Mock<ICommandArgumentParser>();
            parser.Setup(p => p.GetValue()).Returns(targetName);
            return new TextCommandCallingArgs
            {
                Caller = new Caller { Player = targetPlayer.Object },
                Parsers = { parser.Object }
            };
        }

        private bool CityHasQueuedUpdate(string cityGuid, EnumPlayerRelatedInfo updateType)
        {
            return UsefullPacketsSend.cityDelayedInfoCollector.TryGetValue(cityGuid, out var dict)
                && dict.ContainsKey(updateType);
        }

        [Fact]
        public void Deny_CityConflict_SendsLetterRemoveToBothSides()
        {
            claims.src.claims.config.NEED_AGREE_FOR_CONFLICT = true;
            UsefullPacketsSend.cityDelayedInfoCollector.Clear();

            var player = MakeMayorWithCity("OurCity");
            var targetCity = SetupStandaloneTargetCity();

            // Attacker declares conflict
            var declareResult = CityCommand.DeclareCityConflict(MakeArgs(TargetName));
            Assert.Equal("claims:conflict_letter_sent", declareResult.StatusMessage);

            // Retrieve the letter so we can wait on its OnDeny thread
            Assert.True(ConflictHandler.TryGetConflictLetter(
                player.City, targetCity, LetterPurpose.START_CONFLICT, out var letter));

            // Clear the collector so we only see deny-triggered updates
            UsefullPacketsSend.cityDelayedInfoCollector.Clear();

            // Setup target city resolution for deny command (target looks up attacker's city)
            City ourCity = player.City;
            _storageMock.Setup(s => s.GetCityByName("OurCity", out ourCity)).Returns(true);
            Alliance nullAlliance = null!;
            _storageMock.Setup(s => s.GetAllianceByName("OurCity", out nullAlliance)).Returns(false);

            // Target mayor denies the conflict
            var denyArgs = MakeTargetMayorArgs(targetCity, "OurCity");
            var denyResult = CityCommand.DenyStartCityConflict(denyArgs);

            // Wait for the OnDeny thread to finish
            letter.OnDeny.Join(5000);

            // Both sides should have ALLIANCE_LETTER_REMOVE queued
            Assert.True(CityHasQueuedUpdate(player.City.Guid, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE),
                "Attacker city should receive ALLIANCE_LETTER_REMOVE");
            Assert.True(CityHasQueuedUpdate(targetCity.Guid, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE),
                "Target city should receive ALLIANCE_LETTER_REMOVE");
        }

        [Fact]
        public void Revoke_CityConflict_SendsLetterRemoveToBothSides()
        {
            claims.src.claims.config.NEED_AGREE_FOR_CONFLICT = true;
            UsefullPacketsSend.cityDelayedInfoCollector.Clear();

            var player = MakeMayorWithCity("OurCity");
            var targetCity = SetupStandaloneTargetCity();

            // Attacker declares conflict
            var declareResult = CityCommand.DeclareCityConflict(MakeArgs(TargetName));
            Assert.Equal("claims:conflict_letter_sent", declareResult.StatusMessage);

            // Clear so we only see revoke-triggered updates
            UsefullPacketsSend.cityDelayedInfoCollector.Clear();

            // Attacker revokes the conflict letter
            var revokeResult = CityCommand.RevokeCityConflict(MakeArgs(TargetName));

            Assert.True(CityHasQueuedUpdate(player.City.Guid, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE),
                "Attacker city should receive ALLIANCE_LETTER_REMOVE on revoke");
            Assert.True(CityHasQueuedUpdate(targetCity.Guid, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE),
                "Target city should receive ALLIANCE_LETTER_REMOVE on revoke");
        }

        [Fact]
        public void Accept_CityConflict_SendsLetterRemoveToBothSides()
        {
            claims.src.claims.config.NEED_AGREE_FOR_CONFLICT = true;
            UsefullPacketsSend.cityDelayedInfoCollector.Clear();

            var player = MakeMayorWithCity("OurCity");
            var targetCity = SetupStandaloneTargetCity();

            // Attacker declares conflict
            var declareResult = CityCommand.DeclareCityConflict(MakeArgs(TargetName));
            Assert.Equal("claims:conflict_letter_sent", declareResult.StatusMessage);

            // Retrieve the letter so we can wait on its OnAccept thread
            Assert.True(ConflictHandler.TryGetConflictLetter(
                player.City, targetCity, LetterPurpose.START_CONFLICT, out var letter));

            // Clear so we only see accept-triggered updates
            UsefullPacketsSend.cityDelayedInfoCollector.Clear();

            // Setup target city resolution for accept command
            City ourCity = player.City;
            _storageMock.Setup(s => s.GetCityByName("OurCity", out ourCity)).Returns(true);
            Alliance nullAlliance = null!;
            _storageMock.Setup(s => s.GetAllianceByName("OurCity", out nullAlliance)).Returns(false);

            // Target mayor accepts the conflict
            var acceptArgs = MakeTargetMayorArgs(targetCity, "OurCity");
            var acceptResult = CityCommand.AcceptStartCityConflict(acceptArgs);

            // Wait for the OnAccept thread to finish
            letter.OnAccept.Join(5000);

            Assert.True(CityHasQueuedUpdate(player.City.Guid, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE),
                "Attacker city should receive ALLIANCE_LETTER_REMOVE on accept");
            Assert.True(CityHasQueuedUpdate(targetCity.Guid, EnumPlayerRelatedInfo.ALLIANCE_LETTER_REMOVE),
                "Target city should receive ALLIANCE_LETTER_REMOVE on accept");
        }
    }
}
