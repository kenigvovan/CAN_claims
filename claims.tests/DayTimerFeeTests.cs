using System.Reflection;
using claims.src;
using claims.src.part;
using claims.src.part.structure;
using claims.src.timers;
using static claims.src.timers.DayTimer;

namespace claims.tests
{
    /// <summary>
    /// The daily charge: how many missed days are caught up on start, who owes what, and what
    /// happens to someone who cannot pay. An eviction for non-payment is silent to a mayor who was
    /// offline, and looked to players like the mod dropping citizens on relog.
    /// </summary>
    [Collection("ModStatics")]
    public class DayTimerFeeTests
    {
        private const long Day = 86400;

        private readonly City city = new("TestCity", "fee-city-guid");
        private readonly PlayerInfo mayor = new("Mayor", "fee-mayor-uid");
        private readonly PlayerInfo citizen = new("Citizen", "fee-citizen-uid");

        public DayTimerFeeTests()
        {
            claims.src.claims.config = new Config { ADDITIONAL_COST_OF_NO_PVP_PLOT = false };
            typeof(City).GetField("mayor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(city, mayor);
            AddCitizen(city, mayor);
            AddCitizen(city, citizen);
        }

        // Sets PlayerInfo.City via the private setter, bypassing setCity() which needs claims.sapi.
        private static void SetCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo)
                .GetProperty("City")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(playerInfo, new object[] { city });
        }

        private static void AddCitizen(City city, PlayerInfo playerInfo)
        {
            SetCity(playerInfo, city);
            city.getCityCitizens().Add(playerInfo);
        }

        private static Plot PlotOf(int x, CityPlotsGroup? group = null, PlayerInfo? owner = null)
        {
            var plot = new Plot(new PlotPosition(x, 0));
            if (group != null) plot.setPlotGroup(group);
            if (owner != null) plot.setPlotOwner(owner);
            return plot;
        }

        private static CityPlotsGroup GroupWithFee(int fee, params PlayerInfo[] members)
        {
            var group = new CityPlotsGroup("farm", "fee-group-guid-" + fee) { PlotsGroupFee = fee };
            group.PlayersList.AddRange(members);
            return group;
        }

        /*=====================================================================*/
        /* CyclesToRun                                                         */
        /*=====================================================================*/

        [Fact]
        public void CyclesToRun_SameDay_RunsNothing()
        {
            Assert.Equal(0, CyclesToRun(10 * Day, 10 * Day, afterStart: true, Day, maxMissed: 5));
            Assert.Equal(0, CyclesToRun(10 * Day, 9 * Day, afterStart: false, Day, maxMissed: 5));
        }

        [Fact]
        public void CyclesToRun_RunningServer_CatchesOneCycleWhateverTheGap()
        {
            Assert.Equal(1, CyclesToRun(10 * Day, 13 * Day, afterStart: false, Day, maxMissed: 5));
        }

        [Theory]
        [InlineData(1, 1, 1)]   // the usual overnight restart
        [InlineData(5, 1, 1)]   // default: a long downtime charges one day, not five
        [InlineData(5, 3, 3)]
        [InlineData(2, 10, 2)]
        [InlineData(5, 0, 0)]   // missed days skipped altogether
        [InlineData(5, -4, 0)]  // a negative setting reads as zero
        public void CyclesToRun_OnStart_CatchesUpToTheLimit(int daysDown, int maxMissed, int expected)
        {
            Assert.Equal((long)expected, CyclesToRun(10 * Day, (10 + daysDown) * Day, afterStart: true, Day, maxMissed));
        }

        [Fact]
        public void CyclesToRun_OnStart_LessThanADayButANewDay_RunsOnce()
        {
            // The day start moved (anchor or length changed) by less than a full day.
            Assert.Equal(1, CyclesToRun(10 * Day, 10 * Day + 3600, afterStart: true, Day, maxMissed: 5));
        }

        /*=====================================================================*/
        /* DecideFee                                                           */
        /*=====================================================================*/

        [Theory]
        [InlineData("0", "0", true, true, FeeOutcome.Nothing)]
        [InlineData("0.99", "0", true, true, FeeOutcome.Nothing)]    // under a coin is not charged
        [InlineData("1", "1", true, true, FeeOutcome.Pay)]          // exactly enough
        [InlineData("10", "50", true, true, FeeOutcome.Pay)]
        [InlineData("10", "9.99", true, true, FeeOutcome.Kick)]     // the relog "kick"
        [InlineData("10", "9.99", true, false, FeeOutcome.TakePlots)]
        [InlineData("10", "0", false, true, FeeOutcome.TakePlots)]  // embassy owner, member from elsewhere
        public void DecideFee_Cases(string toPay, string balance, bool isOwnCitizen, bool kickNonPayers, FeeOutcome expected)
        {
            Assert.Equal(expected, DecideFee(decimal.Parse(toPay, System.Globalization.CultureInfo.InvariantCulture),
                decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture), isOwnCitizen, kickNonPayers));
        }

        /*=====================================================================*/
        /* BuildFeeBill                                                        */
        /*=====================================================================*/

        [Fact]
        public void Bill_CitizenPaysCityFee_MayorIsNotBilled()
        {
            city.fee = 15;

            var bill = BuildFeeBill(city, new List<Plot>());

            Assert.Equal(15m, bill[citizen]);
            Assert.False(bill.ContainsKey(mayor));
        }

        [Fact]
        public void Bill_ZeroCityFee_CitizenOwesNothing_SoCannotBeEvicted()
        {
            city.fee = 0;

            var bill = BuildFeeBill(city, new List<Plot>());

            Assert.Equal(0m, bill[citizen]);
            Assert.Equal(FeeOutcome.Nothing, DecideFee(bill[citizen], 0m, true, true));
        }

        [Fact]
        public void Bill_NullPlotList_StillBillsCitizens()
        {
            city.fee = 5;

            var bill = BuildFeeBill(city, null!);

            Assert.Equal(5m, bill[citizen]);
        }

        [Fact]
        public void Bill_GroupFee_ChargedOnceHoweverManyPlotsItHolds()
        {
            var group = GroupWithFee(7, citizen);

            var bill = BuildFeeBill(city, new List<Plot> { PlotOf(0, group), PlotOf(1, group), PlotOf(2, group) });

            Assert.Equal(7m, bill[citizen]);
        }

        [Fact]
        public void Bill_GroupWithoutLand_IsNotCharged()
        {
            GroupWithFee(7, citizen);

            var bill = BuildFeeBill(city, new List<Plot>());

            Assert.Equal(0m, bill[citizen]);
        }

        [Fact]
        public void Bill_MayorInGroup_IsNotCharged()
        {
            var group = GroupWithFee(7, mayor, citizen);

            var bill = BuildFeeBill(city, new List<Plot> { PlotOf(0, group) });

            Assert.False(bill.ContainsKey(mayor));
            Assert.Equal(7m, bill[citizen]);
        }

        [Fact]
        public void Bill_MemberFromAnotherCity_PaysGroupFeeButNotCityFee()
        {
            city.fee = 15;
            var outsider = new PlayerInfo("Outsider", "fee-outsider-uid");
            SetCity(outsider, new City("Elsewhere", "fee-elsewhere-guid"));
            var group = GroupWithFee(7, outsider);

            var bill = BuildFeeBill(city, new List<Plot> { PlotOf(0, group) });

            Assert.Equal(7m, bill[outsider]);
            Assert.Equal(15m, bill[citizen]);
        }

        [Fact]
        public void Bill_GroupFeeAndCityFee_AddUp()
        {
            city.fee = 15;
            var group = GroupWithFee(7, citizen);

            var bill = BuildFeeBill(city, new List<Plot> { PlotOf(0, group) });

            Assert.Equal(22m, bill[citizen]);
        }

        [Fact]
        public void Bill_CustomTax_ChargedToOwner_NotOnMayorsPlot()
        {
            var taxed = PlotOf(0, owner: citizen);
            taxed.setCustomTax(4);
            var mayorsPlot = PlotOf(1, owner: mayor);
            mayorsPlot.setCustomTax(4);

            var bill = BuildFeeBill(city, new List<Plot> { taxed, mayorsPlot });

            Assert.Equal(4m, bill[citizen]);
            Assert.False(bill.ContainsKey(mayor));
        }

        /*=====================================================================*/
        /* EvictedForUnpaidSince - what the mayor is told on login             */
        /*=====================================================================*/

        [Fact]
        public void Evicted_OnlyEvictionsAfterLastSeen_OldestFirst()
        {
            city.EventLog.Add(new claims.src.citylog.CityLogEntry(100, claims.src.citylog.EnumCityLogEvent.CitizenEvictedUnpaid, "Early", "10"));
            city.EventLog.Add(new claims.src.citylog.CityLogEntry(300, claims.src.citylog.EnumCityLogEvent.CitizenEvictedUnpaid, "Alice", "10"));
            city.EventLog.Add(new claims.src.citylog.CityLogEntry(400, claims.src.citylog.EnumCityLogEvent.CitizenKicked, "Kicked"));
            city.EventLog.Add(new claims.src.citylog.CityLogEntry(500, claims.src.citylog.EnumCityLogEvent.CitizenEvictedUnpaid, "Bob", "12"));

            Assert.Equal(new[] { "Alice", "Bob" }, city.EvictedForUnpaidSince(200));
        }

        [Fact]
        public void Evicted_AtTheMomentOfLastSeen_IsNotNew()
        {
            city.EventLog.Add(new claims.src.citylog.CityLogEntry(200, claims.src.citylog.EnumCityLogEvent.CitizenEvictedUnpaid, "Alice", "10"));

            Assert.Empty(city.EvictedForUnpaidSince(200));
        }

        [Fact]
        public void Evicted_EntryWithoutName_IsSkipped()
        {
            city.EventLog.Add(new claims.src.citylog.CityLogEntry(300, claims.src.citylog.EnumCityLogEvent.CitizenEvictedUnpaid));

            Assert.Empty(city.EvictedForUnpaidSince(0));
        }

        [Fact]
        public void EvictionIsOffByDefault()
        {
            Assert.False(new Config().DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE);
        }

        [Fact]
        public void Bill_SafetyFlagsOfOwnedPlot_AreTheOwnersBill()
        {
            claims.src.claims.config = new Config { ADDITIONAL_COST_OF_NO_PVP_PLOT = true, PLOT_NO_PVP_FLAG_COST = 3 };
            // pvpFlag is off by default, so the plot counts as no-pvp.
            var bill = BuildFeeBill(city, new List<Plot> { PlotOf(0, owner: citizen) });

            Assert.Equal(3m, bill[citizen]);
        }
    }
}
