using System.Reflection;
using claims.src;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.part;
using claims.src.part.structure;
using Moq;

namespace claims.tests
{
    /// <summary>
    /// What a plots group charges and how a mayor may change it: a cut applies at once, a raise only
    /// to members who accepted it once the wait is over, and the rest leave instead of paying.
    /// </summary>
    [Collection("ModStatics")]
    public class PlotsGroupFeeTests : IDisposable
    {
        private const int DelayHours = 24;

        private readonly ServerStubs stubs = new(new Config
        {
            MAX_PLOTSGROUP_FEE = 50,
            PLOTSGROUP_FEE_RAISE_DELAY_HOURS = DelayHours
        });

        private readonly City city = new("TestCity", "pgfee-city-guid");
        private readonly PlayerInfo mayor = new("Mayor", "pgfee-mayor-uid");
        private readonly PlayerInfo alice = new("Alice", "pgfee-alice-uid");
        private readonly PlayerInfo bob = new("Bob", "pgfee-bob-uid");
        private readonly CityPlotsGroup group = new("farm", "pgfee-group-guid");

        public PlotsGroupFeeTests()
        {
            typeof(City).GetField("mayor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(city, mayor);
            group.City = city;
            group.PlotsGroupFee = 10;
            group.PlayersList.AddRange(new[] { mayor, alice, bob });
            stubs.Storage.addPlotsGroup(group);
        }

        public void Dispose() => stubs.Dispose();

        private void Announce(double fee) => Assert.True(PlotsGroupFeeHelper.SetFee(group, fee, out _));

        private void VerifySaved(Times times) =>
            stubs.Db.Verify(d => d.saveCityPlotGroup(group, It.IsAny<bool>()), times);

        /*=====================================================================*/
        /* SetFee                                                              */
        /*=====================================================================*/

        [Fact]
        public void SetFee_NoGroup_Refused()
        {
            Assert.False(PlotsGroupFeeHelper.SetFee(null!, 5, out string err));
            Assert.Equal("claims:no_such_group_found", err);
        }

        [Fact]
        public void SetFee_Negative_Refused()
        {
            Assert.False(PlotsGroupFeeHelper.SetFee(group, -1, out string err));
            Assert.Equal("claims:not_negative", err);
            Assert.Equal(10, group.PlotsGroupFee);
        }

        [Fact]
        public void SetFee_Cut_AppliesAtOnce()
        {
            Announce(4);

            Assert.Equal(4, group.PlotsGroupFee);
            Assert.False(group.HasPendingFee);
            VerifySaved(Times.Once());
        }

        [Fact]
        public void SetFee_CutWhileRaisePending_DropsTheRaise()
        {
            Announce(30);
            Announce(4);

            Assert.Equal(4, group.PlotsGroupFee);
            Assert.False(group.HasPendingFee);
        }

        [Fact]
        public void SetFee_Raise_OnlyAnnounced()
        {
            long before = TimeFunctions.getEpochSeconds();

            Announce(30);

            Assert.Equal(10, group.PlotsGroupFee);
            Assert.True(group.HasPendingFee);
            Assert.Equal(30, group.PendingFee);
            Assert.InRange(group.PendingFeeAt, before + DelayHours * 3600L, TimeFunctions.getEpochSeconds() + DelayHours * 3600L);
        }

        [Fact]
        public void SetFee_RaiseWithZeroDelay_AppliesAtOnce()
        {
            claims.src.claims.config.PLOTSGROUP_FEE_RAISE_DELAY_HOURS = 0;

            Announce(30);

            Assert.Equal(30, group.PlotsGroupFee);
            Assert.False(group.HasPendingFee);
        }

        [Fact]
        public void SetFee_AboveMaximum_IsCapped()
        {
            Announce(500);

            Assert.Equal(50, group.PendingFee);
        }

        [Fact]
        public void SetFee_SmallerReannouncement_KeepsDeadlineAndAcceptances()
        {
            Announce(30);
            long deadline = group.PendingFeeAt;
            PlotsGroupFeeHelper.Accept(group, alice, out _);

            Announce(29);

            Assert.Equal(29, group.PendingFee);
            Assert.Equal(deadline, group.PendingFeeAt);
            Assert.Contains(alice.Guid, group.PendingFeeAccepted);
        }

        [Fact]
        public void SetFee_SameReannouncement_ChangesNothing()
        {
            Announce(30);
            stubs.Db.Invocations.Clear();

            Announce(30);

            VerifySaved(Times.Never());
        }

        [Fact]
        public void SetFee_BiggerReannouncement_RestartsTheWaitAndClearsAcceptances()
        {
            Announce(30);
            group.PendingFeeAt = 1000; // an older deadline, to see it move
            PlotsGroupFeeHelper.Accept(group, alice, out _);

            Announce(40);

            Assert.Equal(40, group.PendingFee);
            Assert.True(group.PendingFeeAt > 1000);
            Assert.Empty(group.PendingFeeAccepted);
        }

        /*=====================================================================*/
        /* Accept / join / leave                                               */
        /*=====================================================================*/

        [Fact]
        public void Accept_NothingPending_Refused()
        {
            Assert.False(PlotsGroupFeeHelper.Accept(group, alice, out string err));
            Assert.Equal("claims:plotsgroup_fee_no_pending", err);
        }

        [Fact]
        public void Accept_NotAMember_Refused()
        {
            Announce(30);
            var stranger = new PlayerInfo("Stranger", "pgfee-stranger-uid");

            Assert.False(PlotsGroupFeeHelper.Accept(group, stranger, out string err));
            Assert.Equal("claims:plotsgroup_not_a_member", err);
            Assert.DoesNotContain(stranger.Guid, group.PendingFeeAccepted);
        }

        [Fact]
        public void Accept_Twice_SavesOnce()
        {
            Announce(30);
            stubs.Db.Invocations.Clear();

            Assert.True(PlotsGroupFeeHelper.Accept(group, alice, out _));
            Assert.True(PlotsGroupFeeHelper.Accept(group, alice, out _));

            VerifySaved(Times.Once());
        }

        [Fact]
        public void Joining_WhileRaisePending_CountsAsAccepting()
        {
            Announce(30);
            var newcomer = new PlayerInfo("New", "pgfee-new-uid");

            PlotsGroupFeeHelper.OnMemberJoined(group, newcomer);

            Assert.Contains(newcomer.Guid, group.PendingFeeAccepted);
        }

        [Fact]
        public void Joining_NothingPending_RecordsNothing()
        {
            PlotsGroupFeeHelper.OnMemberJoined(group, alice);

            Assert.Empty(group.PendingFeeAccepted);
            VerifySaved(Times.Never());
        }

        [Fact]
        public void Leaving_ForgetsTheAcceptance()
        {
            Announce(30);
            PlotsGroupFeeHelper.Accept(group, alice, out _);

            PlotsGroupFeeHelper.OnMemberLeft(group, alice);

            Assert.DoesNotContain(alice.Guid, group.PendingFeeAccepted);
        }

        /*=====================================================================*/
        /* SettleDueRaises                                                     */
        /*=====================================================================*/

        [Fact]
        public void Settle_NotDueYet_ChangesNothing()
        {
            Announce(30);

            PlotsGroupFeeHelper.SettleDueRaises();

            Assert.Equal(10, group.PlotsGroupFee);
            Assert.Equal(3, group.PlayersList.Count);
        }

        [Fact]
        public void Settle_Due_AcceptedStay_OthersLeave_MayorStays()
        {
            Announce(30);
            PlotsGroupFeeHelper.Accept(group, alice, out _);
            group.PendingFeeAt = TimeFunctions.getEpochSeconds() - 1;

            PlotsGroupFeeHelper.SettleDueRaises();

            Assert.Equal(30, group.PlotsGroupFee);
            Assert.False(group.HasPendingFee);
            Assert.Contains(alice, group.PlayersList);
            Assert.Contains(mayor, group.PlayersList);
            Assert.DoesNotContain(bob, group.PlayersList);
        }

        /*=====================================================================*/
        /* ToCell                                                              */
        /*=====================================================================*/

        [Fact]
        public void ToCell_CarriesMembersAndThePendingRaise()
        {
            Announce(30);
            PlotsGroupFeeHelper.Accept(group, alice, out _);

            var cell = PlotsGroupFeeHelper.ToCell(group);

            Assert.Equal(new[] { "Mayor", "Alice", "Bob" }, cell.PlayersNames);
            Assert.Equal(30, cell.PendingFee);
            Assert.Equal(group.PendingFeeAt, cell.PendingFeeAt);
            Assert.Equal(new[] { alice.Guid }, cell.PendingFeeAccepted);
        }

        [Fact]
        public void ToCell_CountsOnlyThisGroupsPlots()
        {
            var own = new Plot(new PlotPosition(0, 0));
            own.setPlotGroup(group);
            var other = new Plot(new PlotPosition(1, 0));
            other.setPlotGroup(new CityPlotsGroup("orchard", "pgfee-other-guid"));
            city.getCityPlots().Add(own);
            city.getCityPlots().Add(other);
            city.getCityPlots().Add(new Plot(new PlotPosition(2, 0)));

            Assert.Equal(1, PlotsGroupFeeHelper.ToCell(group).PlotsCount);
        }
    }
}
