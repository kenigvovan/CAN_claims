using claims.src;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.commands;
using claims.src.part;
using claims.src.part.structure;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.tests
{
    /// <summary>
    /// The early exits of /city plotsgroup delete, kick and unadd. The success paths write to the
    /// database and queue packets, so they are covered by claims.scenarios instead.
    /// </summary>
    [Collection("ModStatics")]
    public class PlotsGroupCommandTests
    {
        private const string MayorUid = "pg-mayor-uid";
        private const string MemberUid = "pg-member-uid";
        private const string GroupName = "farm";

        private readonly Mock<IServerPlayer> _playerMock = new();
        private readonly DataStorage _storage = new(false);
        private readonly PlayerInfo _mayor = new("Mayor", MayorUid);
        private readonly PlayerInfo _member = new("Member", MemberUid);
        private readonly City _city = new("TestCity", "pg-city-guid");
        private readonly CityPlotsGroup _group = new(GroupName, "pg-group-guid");

        public PlotsGroupCommandTests()
        {
            claims.src.claims.config = new Config { USE_MOD_CHAT_WINDOW = false };
            claims.src.claims.dataStorage = _storage;
            Settings.blockedNames = new HashSet<string>();

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

            _playerMock.Setup(p => p.PlayerUID).Returns(MayorUid);
            _storage.addPlayer(_mayor);
            _storage.addPlayer(_member);
            _city.getCityPlotsGroups().Add(_group);
        }

        // Sets PlayerInfo.City via the private setter, bypassing setCity() which needs claims.sapi.
        private static void SetPlayerCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo)
                .GetProperty("City")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(playerInfo, new object[] { city });
        }

        // The handlers read either LastArg or Parsers[i], so both are filled from the same values.
        private TextCommandCallingArgs MakeArgs(params string[] values)
        {
            var args = new TextCommandCallingArgs { Caller = new Caller { Player = _playerMock.Object } };
            foreach (string value in values)
            {
                var parser = new Mock<ICommandArgumentParser>();
                parser.Setup(p => p.GetValue()).Returns(value);
                args.Parsers.Add(parser.Object);
            }
            return args;
        }

        private void VerifyToldCaller(string langKey)
        {
            _playerMock.Verify(p => p.SendMessage(It.IsAny<int>(), langKey, It.IsAny<EnumChatType>(), It.IsAny<string>()),
                Times.Once);
        }

        /*=====================================================================*/
        /* delete                                                              */
        /*=====================================================================*/

        [Fact]
        public void Delete_CallerHasNoCity_ReturnsError()
        {
            var result = CityCommand.PlotsGroupDelete(MakeArgs(GroupName));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:you_dont_have_city", result.StatusMessage);
        }

        [Fact]
        public void Delete_NameFiltersToNothing_ReturnsInvalidName()
        {
            SetPlayerCity(_mayor, _city);

            var result = CityCommand.PlotsGroupDelete(MakeArgs("!@#"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:invalid_group_name", result.StatusMessage);
        }

        [Fact]
        public void Delete_UnknownGroup_ReturnsNoSuchGroup()
        {
            SetPlayerCity(_mayor, _city);

            var result = CityCommand.PlotsGroupDelete(MakeArgs("orchard"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_such_group_found", result.StatusMessage);
            Assert.Contains(_group, _city.getCityPlotsGroups());
        }

        [Fact]
        public void Delete_GroupOfAnotherCity_IsNotFound()
        {
            var otherCity = new City("OtherCity", "pg-other-city-guid");
            SetPlayerCity(_mayor, otherCity);

            var result = CityCommand.PlotsGroupDelete(MakeArgs(GroupName));

            Assert.Equal("claims:no_such_group_found", result.StatusMessage);
            Assert.Contains(_group, _city.getCityPlotsGroups());
        }

        /*=====================================================================*/
        /* kick - answers in chat, not in StatusMessage                        */
        /*=====================================================================*/

        [Fact]
        public void Kick_CallerHasNoCity_TellsCaller()
        {
            var result = CityCommand.PlotsGroupKickPlayerFromGroup(MakeArgs(GroupName, "Member"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            VerifyToldCaller("claims:you_dont_have_city");
        }

        [Fact]
        public void Kick_UnknownGroup_TellsCaller()
        {
            SetPlayerCity(_mayor, _city);

            var result = CityCommand.PlotsGroupKickPlayerFromGroup(MakeArgs("orchard", "Member"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            VerifyToldCaller("claims:no_such_group_found");
        }

        [Fact]
        public void Kick_UnknownPlayer_TellsCaller()
        {
            SetPlayerCity(_mayor, _city);

            var result = CityCommand.PlotsGroupKickPlayerFromGroup(MakeArgs(GroupName, "Nobody"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            VerifyToldCaller("claims:no_such_player");
        }

        [Fact]
        public void Kick_PlayerNotInGroup_TellsCallerAndKeepsMembers()
        {
            SetPlayerCity(_mayor, _city);
            var other = new PlayerInfo("Other", "pg-other-uid");
            _storage.addPlayer(other);
            _group.PlayersList.Add(other);

            var result = CityCommand.PlotsGroupKickPlayerFromGroup(MakeArgs(GroupName, "Member"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            VerifyToldCaller("claims:not_in_the_group");
            Assert.Single(_group.PlayersList);
        }

        /*=====================================================================*/
        /* unadd                                                               */
        /*=====================================================================*/

        [Fact]
        public void Unadd_CallerHasNoCity_ReturnsError()
        {
            var result = CityCommand.PlotsGroupUnaddTo(MakeArgs(GroupName, "Member"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:you_dont_have_city", result.StatusMessage);
        }

        [Fact]
        public void Unadd_UnknownPlayer_ReturnsNoSuchPlayer()
        {
            SetPlayerCity(_mayor, _city);

            var result = CityCommand.PlotsGroupUnaddTo(MakeArgs(GroupName, "Nobody"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_such_player", result.StatusMessage);
        }

        [Fact]
        public void Unadd_NoInvitation_ReturnsNoSuchInvite()
        {
            SetPlayerCity(_mayor, _city);

            var result = CityCommand.PlotsGroupUnaddTo(MakeArgs(GroupName, "Member"));

            Assert.Equal(EnumCommandStatus.Error, result.Status);
            Assert.Equal("claims:no_such_plotsgroup_invite", result.StatusMessage);
        }

        [Fact]
        public void Unadd_InvitationFromAnotherCity_IsNotWithdrawn()
        {
            SetPlayerCity(_mayor, _city);
            var otherCity = new City("OtherCity", "pg-other-city-guid");
            var foreign = new CityPlotsGroupInvitation(otherCity, _member, long.MaxValue, null, null, GroupName);
            _member.groupInvitations.Add(foreign);

            var result = CityCommand.PlotsGroupUnaddTo(MakeArgs(GroupName, "Member"));

            Assert.Equal("claims:no_such_plotsgroup_invite", result.StatusMessage);
            Assert.Contains(foreign, _member.groupInvitations);
        }

        [Fact]
        public void Unadd_InvitationToAnotherGroup_IsNotWithdrawn()
        {
            SetPlayerCity(_mayor, _city);
            var otherGroup = new CityPlotsGroupInvitation(_city, _member, long.MaxValue, null, null, "orchard");
            _member.groupInvitations.Add(otherGroup);

            var result = CityCommand.PlotsGroupUnaddTo(MakeArgs(GroupName, "Member"));

            Assert.Equal("claims:no_such_plotsgroup_invite", result.StatusMessage);
            Assert.Contains(otherGroup, _member.groupInvitations);
        }
    }
}
