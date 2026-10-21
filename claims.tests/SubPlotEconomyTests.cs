using System.Reflection;
using claims.src;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using claims.src.rights;
using claims.src.subplots;
using claims.src.subplots.geometry;
using claims.src.timers;
using Moq;
using Vintagestory.API.Common;

namespace claims.tests
{
    /// <summary>
    /// Money around sub-plots: sale to citizens, giving back, and the daily tax, whose
    /// raise waits for the owner's consent like a plots group fee.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotEconomyTests : IDisposable
    {
        private const long Now = 1_000_000;

        private readonly ServerStubs stubs = new(new Config
        {
            PLOT_SIZE = 16,
            PLOT_UNCLAIM_REFUND_PERCENT = 50,
            PLOT_UNCLAIM_REFUND_MIN_AGE_SECONDS = 100,
            MAX_SUBPLOT_TAX = 50,
            SUBPLOT_TAX_RAISE_DELAY_HOURS = 24
        });
        private readonly IMoneyProvider previousEconomy = claims.src.claims.economyProvider;
        private readonly Mock<IMoneyProvider> economy = new();
        private readonly City city = new("TestCity", "spm-city");
        private readonly PlayerInfo mayor = new("Mayor", "spm-mayor");
        private readonly PlayerInfo citizen = new("Citizen", "spm-citizen");
        private readonly PlayerInfo outsider = new("Outsider", "spm-outsider");
        private readonly SubPlot flat;

        public SubPlotEconomyTests()
        {
            economy.Setup(e => e.GetBalance(It.IsAny<string>())).Returns(1000m);
            economy.Setup(e => e.Transfer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                   .Returns(MoneyOperationResult.Success);
            claims.src.claims.economyProvider = economy.Object;

            typeof(City).GetField("mayor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(city, mayor);
            foreach (var p in new[] { mayor, citizen })
            {
                typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(p, new object[] { city });
                city.getCityCitizens().Add(p);
            }
            mayor.PlayerPermissionsHandler.AddPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);

            flat = new SubPlot("flat", "spm-flat") { City = city, Shape = new BoxShape(new BlockBox(0, 64, 0, 8, 68, 8)) };
            stubs.Storage.AddSubPlot(flat);
        }

        public void Dispose()
        {
            claims.src.claims.economyProvider = previousEconomy;
            stubs.Dispose();
        }

        private static bool Ok(TextCommandResult r) => r.Status == EnumCommandStatus.Success;

        private void Owned(long paid = 100, long since = Now - 1000)
        {
            flat.Owner = citizen;
            flat.LastPaidPrice = paid;
            flat.TimeStampClaimed = since;
        }

        /*=====================================================================*/
        /* sale                                                                */
        /*=====================================================================*/

        [Fact]
        public void Citizen_BuysASubPlotForSale_PayingTheCity()
        {
            Assert.True(Ok(SubPlotEditor.SetForSale(mayor, flat, 100)));

            Assert.True(Ok(SubPlotEditor.Buy(citizen, flat)));

            Assert.Same(citizen, flat.Owner);
            Assert.False(flat.IsForSale);
            Assert.Equal(100, flat.LastPaidPrice);
            economy.Verify(e => e.Transfer(citizen.MoneyAccountName, city.MoneyAccountName, 100m), Times.Once);
        }

        [Fact]
        public void Buy_IsRefused_NotForSale_OtherCity_TooPoor()
        {
            Assert.False(Ok(SubPlotEditor.Buy(citizen, flat)));

            SubPlotEditor.SetForSale(mayor, flat, 100);
            Assert.False(Ok(SubPlotEditor.Buy(outsider, flat)));

            economy.Setup(e => e.GetBalance(citizen.MoneyAccountName)).Returns(99m);
            Assert.Equal("claims:not_enough_money", SubPlotEditor.Buy(citizen, flat).StatusMessage);
            Assert.Null(flat.Owner);
        }

        [Fact]
        public void FailedPayment_LeavesTheSubPlotUnsold()
        {
            SubPlotEditor.SetForSale(mayor, flat, 100);
            economy.Setup(e => e.Transfer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                   .Returns(MoneyOperationResult.SourceNotEnoughMoney);

            Assert.False(Ok(SubPlotEditor.Buy(citizen, flat)));
            Assert.Null(flat.Owner);
            Assert.True(flat.IsForSale);
        }

        [Fact]
        public void OwnedSubPlot_CannotBePutUpForSale()
        {
            Owned();

            Assert.Equal("claims:subplot_has_owner", SubPlotEditor.SetForSale(mayor, flat, 10).StatusMessage);
        }

        /*=====================================================================*/
        /* giving back                                                         */
        /*=====================================================================*/

        [Fact]
        public void GiveBackRefund_FollowsThePlotRules()
        {
            Owned(paid: 100, since: Now - 1000);
            Assert.Equal(50, SubPlotEditor.GiveBackRefund(flat, Now, 1000m));
            Assert.Equal(20, SubPlotEditor.GiveBackRefund(flat, Now, 20.9m));

            // Too fresh.
            flat.TimeStampClaimed = Now - 10;
            Assert.Equal(0, SubPlotEditor.GiveBackRefund(flat, Now, 1000m));
        }

        [Fact]
        public void Owner_GivesBack_OthersMayNot()
        {
            Owned();

            Assert.False(Ok(SubPlotEditor.GiveBack(mayor, flat)));
            Assert.True(Ok(SubPlotEditor.GiveBack(citizen, flat)));
            Assert.Null(flat.Owner);
        }

        /*=====================================================================*/
        /* tax                                                                 */
        /*=====================================================================*/

        [Fact]
        public void Tax_OnACitySubPlot_AppliesAtOnce_AndIsCapped()
        {
            Assert.True(Ok(SubPlotTaxHelper.SetTax(mayor, flat, 500, Now)));

            Assert.Equal(50, flat.Tax);
            Assert.False(flat.HasPendingTax);
        }

        [Fact]
        public void LowerTax_AppliesAtOnce()
        {
            Owned();
            flat.Tax = 10;

            SubPlotTaxHelper.SetTax(mayor, flat, 5, Now);

            Assert.Equal(5, flat.Tax);
        }

        [Fact]
        public void HigherTax_IsOnlyAnnounced_TheOwnerPaysTheOldOneMeanwhile()
        {
            Owned();
            flat.Tax = 5;

            SubPlotTaxHelper.SetTax(mayor, flat, 20, Now);

            Assert.Equal(5, flat.Tax);
            Assert.Equal(20, flat.PendingTax);
            Assert.Equal(Now + 24 * 3600, flat.PendingTaxAt);
        }

        [Fact]
        public void AcceptedRaise_LandsWhenTheWaitIsOver()
        {
            Owned();
            SubPlotTaxHelper.SetTax(mayor, flat, 20, Now);
            Assert.True(Ok(SubPlotTaxHelper.Accept(citizen, flat)));

            SubPlotTaxHelper.SettleDueRaises(Now + 3600);
            Assert.Equal(0, flat.Tax);

            SubPlotTaxHelper.SettleDueRaises(Now + 24 * 3600);
            Assert.Equal(20, flat.Tax);
            Assert.Same(citizen, flat.Owner);
            Assert.False(flat.HasPendingTax);
        }

        [Fact]
        public void UnacceptedRaise_GivesTheSubPlotBack_WithTheRefund()
        {
            Owned(paid: 100);
            SubPlotTaxHelper.SetTax(mayor, flat, 20, Now);

            SubPlotTaxHelper.SettleDueRaises(Now + 24 * 3600);

            Assert.Null(flat.Owner);
            Assert.Equal(20, flat.Tax);
            economy.Verify(e => e.Transfer(city.MoneyAccountName, citizen.MoneyAccountName, 50m), Times.Once);
        }

        [Fact]
        public void ReAnnouncingLess_KeepsTheDeadlineAndTheConsent()
        {
            Owned();
            SubPlotTaxHelper.SetTax(mayor, flat, 30, Now);
            SubPlotTaxHelper.Accept(citizen, flat);

            SubPlotTaxHelper.SetTax(mayor, flat, 25, Now + 7200);

            Assert.Equal(Now + 24 * 3600, flat.PendingTaxAt);
            Assert.True(flat.PendingTaxAccepted);
            Assert.Equal(25, flat.PendingTax);
        }

        [Fact]
        public void AnnouncingMore_StartsOver_AndNeedsConsentAgain()
        {
            Owned();
            SubPlotTaxHelper.SetTax(mayor, flat, 20, Now);
            SubPlotTaxHelper.Accept(citizen, flat);

            SubPlotTaxHelper.SetTax(mayor, flat, 40, Now + 7200);

            Assert.False(flat.PendingTaxAccepted);
            Assert.Equal(Now + 7200 + 24 * 3600, flat.PendingTaxAt);
        }

        [Fact]
        public void OwnerLeaving_LandsTheAnnouncedRaise()
        {
            Owned();
            SubPlotTaxHelper.SetTax(mayor, flat, 20, Now);

            SubPlotLifecycle.ReturnToCity(flat);

            Assert.Equal(20, flat.Tax);
            Assert.False(flat.HasPendingTax);
        }

        [Fact]
        public void OnlyTheOwnerAccepts()
        {
            Owned();
            SubPlotTaxHelper.SetTax(mayor, flat, 20, Now);

            Assert.False(Ok(SubPlotTaxHelper.Accept(mayor, flat)));
        }

        [Fact]
        public void DailyBill_ChargesTheOwnerTheTax_NotTheMayor()
        {
            Owned();
            flat.Tax = 7;
            var mayorsFlat = new SubPlot("m", "spm-m") { City = city, Owner = mayor, Tax = 9,
                Shape = new BoxShape(new BlockBox(0, 80, 0, 8, 84, 8)) };
            stubs.Storage.AddSubPlot(mayorsFlat);

            var bill = DayTimer.BuildFeeBill(city, new List<Plot>());

            Assert.Equal(7m + (decimal)city.fee, bill[citizen]);
            Assert.False(bill.ContainsKey(mayor));
        }

        [Fact]
        public void NextPayment_ShowsTheSubPlotTax()
        {
            Owned();
            flat.Tax = 7;

            Assert.Equal(7, citizen.GetNextPaymentsDict()["subplots"]);
        }
    }
}
