using claims.src;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.commands;
using claims.src.part;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.tests
{
    /// <summary>
    /// /plotsgroupaccept, /plotsgroupdeny and /plotsgroupleave - the member's side of a plots group.
    /// </summary>
    [Collection("ModStatics")]
    public class PlotsGroupMembershipTests : IDisposable
    {
        private readonly ServerStubs stubs = new(new Config { USE_MOD_CHAT_WINDOW = false });
        private readonly Mock<IServerPlayer> playerMock = new();
        private readonly PlayerInfo member = new("Member", "pgm-member-uid");
        private readonly City city = new("TestCity", "pgm-city-guid");
        private readonly CityPlotsGroup group = new("farm", "pgm-group-guid");

        public PlotsGroupMembershipTests()
        {
            Settings.blockedNames = new HashSet<string>();
            playerMock.Setup(p => p.PlayerUID).Returns(member.Guid);
            stubs.Storage.addPlayer(member);
            stubs.Storage.addCity(city);
            stubs.Storage.addPlotsGroup(group);
            group.City = city;
            city.getCityPlotsGroups().Add(group);
        }

        public void Dispose()
        {
            // The invitations live in a static table shared by every test.
            foreach (var invitation in member.groupInvitations.ToArray())
            {
                CityPlotsGroupInvitationsHandler.RemoveInvitation(invitation);
            }
            stubs.Dispose();
        }

        private TextCommandCallingArgs MakeArgs(string? first, string? second)
        {
            var args = new TextCommandCallingArgs { Caller = new Caller { Player = playerMock.Object } };
            foreach (string? value in new[] { first, second })
            {
                var parser = new Mock<ICommandArgumentParser>();
                parser.Setup(p => p.GetValue()).Returns(value!);
                args.Parsers.Add(parser.Object);
            }
            return args;
        }

        private CityPlotsGroupInvitation Invite(string groupName, Action? onAccept = null, Action? onReject = null)
        {
            var invitation = new CityPlotsGroupInvitation(city, member, long.MaxValue, onAccept, onReject, groupName);
            Assert.True(CityPlotsGroupInvitationsHandler.addNewCityPlotGroupInvitation(invitation));
            return invitation;
        }

        /*=====================================================================*/
        /* TryParseGroupRef                                                    */
        /*=====================================================================*/

        [Theory]
        [InlineData("TestCity", "farm", "TestCity", "farm")]
        [InlineData("TestCity:farm", null, "TestCity", "farm")]   // as the listing prints it
        [InlineData("TestCity:farm", "", "TestCity", "farm")]
        public void ParseGroupRef_Accepted(string first, string? second, string city, string group)
        {
            Assert.True(AcceptCommand.TryParseGroupRef(first, second!, out string c, out string g));
            Assert.Equal(city, c);
            Assert.Equal(group, g);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "farm")]
        [InlineData("TestCity", null)]       // a city alone names no group
        [InlineData("TestCity:", null)]
        [InlineData(":farm", null)]
        [InlineData("a:b:c", null)]
        public void ParseGroupRef_Refused(string? first, string? second)
        {
            Assert.False(AcceptCommand.TryParseGroupRef(first!, second!, out _, out _));
        }

        /*=====================================================================*/
        /* /plotsgroupleave                                                    */
        /*=====================================================================*/

        [Theory]
        [InlineData("TestCity", "farm")]
        [InlineData("TestCity:farm", null)]
        public void Leave_BothForms_LeaveTheGroup(string first, string? second)
        {
            group.PlayersList.Add(member);

            var result = AcceptCommand.onLeavePlotGroup(MakeArgs(first, second));

            Assert.Equal("claims:you_left_plotsgroup", result.StatusMessage);
            Assert.DoesNotContain(member, group.PlayersList);
            stubs.Db.Verify(d => d.saveCityPlotGroup(group, It.IsAny<bool>()), Times.AtLeastOnce());
        }

        // The no-argument listing is left out: it measures text with SkiaSharp, whose native
        // library is not next to the test assembly.

        [Fact]
        public void Leave_CityAlone_IsWrongFormat()
        {
            group.PlayersList.Add(member);

            var result = AcceptCommand.onLeavePlotGroup(MakeArgs("TestCity", null));

            Assert.Equal("claims:wrong_format", result.StatusMessage);
            Assert.Contains(member, group.PlayersList);
        }

        [Fact]
        public void Leave_UnknownCity()
        {
            Assert.Equal("claims:no_city_found", AcceptCommand.onLeavePlotGroup(MakeArgs("Nowhere", "farm")).StatusMessage);
        }

        [Fact]
        public void Leave_UnknownGroup()
        {
            Assert.Equal("claims:no_such_group", AcceptCommand.onLeavePlotGroup(MakeArgs("TestCity", "orchard")).StatusMessage);
        }

        [Fact]
        public void Leave_NotAMember()
        {
            Assert.Equal("claims:you_are_not_in_the_group", AcceptCommand.onLeavePlotGroup(MakeArgs("TestCity", "farm")).StatusMessage);
        }

        [Fact]
        public void Leave_ForgetsAcceptanceOfAPendingRaise()
        {
            group.PlayersList.Add(member);
            group.PendingFee = 30;
            group.PendingFeeAt = long.MaxValue;
            group.PendingFeeAccepted.Add(member.Guid);

            AcceptCommand.onLeavePlotGroup(MakeArgs("TestCity", "farm"));

            Assert.DoesNotContain(member.Guid, group.PendingFeeAccepted);
        }

        /*=====================================================================*/
        /* /plotsgroupaccept, /plotsgroupdeny                                  */
        /*=====================================================================*/

        [Fact]
        public void Accept_MatchingInvitation_RunsItAndClearsIt()
        {
            bool accepted = false;
            var invitation = Invite("farm", onAccept: () => accepted = true);

            AcceptCommand.onAcceptPlotGroup(MakeArgs("TestCity", "farm"));

            Assert.True(accepted);
            Assert.DoesNotContain(invitation, member.groupInvitations);
            Assert.DoesNotContain(invitation, city.groupInvitations);
        }

        [Fact]
        public void Accept_PicksTheNamedGroup_NotAnotherFromTheSameCity()
        {
            bool farmAccepted = false, orchardAccepted = false;
            Invite("farm", onAccept: () => farmAccepted = true);
            var orchard = Invite("orchard", onAccept: () => orchardAccepted = true);

            AcceptCommand.onAcceptPlotGroup(MakeArgs("TestCity", "farm"));

            Assert.True(farmAccepted);
            Assert.False(orchardAccepted);
            Assert.Contains(orchard, member.groupInvitations);
        }

        [Fact]
        public void Accept_NoMatchingInvitation_ChangesNothing()
        {
            bool accepted = false;
            var invitation = Invite("farm", onAccept: () => accepted = true);

            AcceptCommand.onAcceptPlotGroup(MakeArgs("OtherCity", "farm"));

            Assert.False(accepted);
            Assert.Contains(invitation, member.groupInvitations);
        }

        [Fact]
        public void Deny_MatchingInvitation_RejectsAndClearsIt()
        {
            bool accepted = false, rejected = false;
            var invitation = Invite("farm", onAccept: () => accepted = true, onReject: () => rejected = true);

            AcceptCommand.onDenyPlotGroup(MakeArgs("TestCity", "farm"));

            Assert.True(rejected);
            Assert.False(accepted);
            Assert.DoesNotContain(invitation, member.groupInvitations);
        }

        [Fact]
        public void UnknownCaller_IsAnError()
        {
            playerMock.Setup(p => p.PlayerUID).Returns("nobody-uid");

            Assert.Equal(EnumCommandStatus.Error, AcceptCommand.onAcceptPlotGroup(MakeArgs("TestCity", "farm")).Status);
            Assert.Equal(EnumCommandStatus.Error, AcceptCommand.onDenyPlotGroup(MakeArgs("TestCity", "farm")).Status);
            Assert.Equal(EnumCommandStatus.Error, AcceptCommand.onLeavePlotGroup(MakeArgs("TestCity", "farm")).Status);
        }
    }
}
