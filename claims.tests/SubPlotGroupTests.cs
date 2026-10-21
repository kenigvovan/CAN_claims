using System.Reflection;
using claims.src;
using claims.src.cityplotsgroups;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using claims.src.perms;
using claims.src.perms.type;
using claims.src.rights;
using claims.src.subplots;
using claims.src.subplots.geometry;
using claims.src.timers;
using Moq;
using Vintagestory.API.Common;

namespace claims.tests
{
    /// <summary>
    /// Sub-plots in plots groups, and rule presets: a group's members get its rights inside, the
    /// group charges for it as for its land, and a removed group leaves its sub-plots behind.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotGroupTests : IDisposable
    {
        private readonly ServerStubs stubs = new(new Config { PLOT_SIZE = 16 });
        private readonly IMoneyProvider previousEconomy = claims.src.claims.economyProvider;
        private readonly City city = new("TestCity", "spg-city");
        private readonly PlayerInfo manager = new("Manager", "spg-manager");
        private readonly PlayerInfo member = new("Member", "spg-member");
        private readonly PlayerInfo citizen = new("Citizen", "spg-citizen");
        private readonly CityPlotsGroup group = new("guild", "spg-group") { PlotsGroupFee = 4 };
        private readonly SubPlot hall;

        public SubPlotGroupTests()
        {
            claims.src.claims.economyProvider = new Mock<IMoneyProvider>().Object;
            foreach (var p in new[] { manager, member, citizen })
            {
                typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(p, new object[] { city });
                city.getCityCitizens().Add(p);
            }
            manager.PlayerPermissionsHandler.AddPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);
            group.City = city;
            group.PlayersList.Add(member);
            city.getCityPlotsGroups().Add(group);

            hall = new SubPlot("hall", "spg-hall") { City = city, Shape = new BoxShape(new BlockBox(0, 64, 0, 8, 68, 8)) };
            stubs.Storage.AddSubPlot(hall);
        }

        public void Dispose()
        {
            claims.src.claims.economyProvider = previousEconomy;
            stubs.Dispose();
        }

        private static bool Ok(TextCommandResult r) => r.Status == EnumCommandStatus.Success;

        private bool? Build(PlayerInfo who) => SubPlotAccess.Decide(who, hall, PermType.BUILD_AND_DESTROY_PERM);

        [Fact]
        public void GroupMember_GetsTheGroupsRights_OtherCitizensDoNot()
        {
            group.PermsHandler.CitizenPerms[(int)PermType.BUILD_AND_DESTROY_PERM] = true;

            Assert.True(Ok(SubPlotEditor.SetGroup(manager, hall, "guild")));

            Assert.True(Build(member));
            Assert.False(Build(citizen));
        }

        [Fact]
        public void GroupRuleOff_TakesNothingAway_FromTheSubPlotsOwnRules()
        {
            hall.PermsHandler.CitizenPerms[(int)PermType.USE_PERM] = true;
            SubPlotEditor.SetGroup(manager, hall, "guild");

            Assert.True(SubPlotAccess.Decide(member, hall, PermType.USE_PERM));
        }

        [Fact]
        public void OnlyAManager_PutsItIntoAGroup_OfTheSameCity()
        {
            Assert.False(Ok(SubPlotEditor.SetGroup(citizen, hall, "guild")));
            Assert.Equal("claims:no_such_group_found", SubPlotEditor.SetGroup(manager, hall, "nosuch").StatusMessage);
            Assert.Null(hall.Group);
        }

        [Fact]
        public void Ungroup_TakesTheRightsAway()
        {
            group.PermsHandler.CitizenPerms[(int)PermType.BUILD_AND_DESTROY_PERM] = true;
            SubPlotEditor.SetGroup(manager, hall, "guild");

            SubPlotEditor.SetGroup(manager, hall, null);

            Assert.False(Build(member));
        }

        [Fact]
        public void RemovedGroup_LeavesItsSubPlots_OutOfAnyGroup()
        {
            SubPlotEditor.SetGroup(manager, hall, "guild");

            SubPlotLifecycle.OnGroupDeleted(group);

            Assert.Null(hall.Group);
            Assert.True(stubs.Storage.TryGetSubPlot(hall.Guid, out _));
        }

        [Fact]
        public void GroupHoldingOnlyASubPlot_ChargesItsFee()
        {
            SubPlotEditor.SetGroup(manager, hall, "guild");

            var bill = DayTimer.BuildFeeBill(city, new List<Plot>());

            Assert.Equal(4m + city.fee, bill[member]);
        }

        [Fact]
        public void GroupWithoutLand_ChargesNothing()
        {
            var bill = DayTimer.BuildFeeBill(city, new List<Plot>());

            Assert.Equal((decimal)city.fee, bill[member]);
        }

        [Theory]
        [InlineData("private", false, false, false)]
        [InlineData("shop", true, false, false)]
        [InlineData("open", true, true, true)]
        public void Preset_SetsEveryGroup(string preset, bool use, bool build, bool attack)
        {
            Assert.True(Ok(SubPlotEditor.ApplyPreset(manager, hall, preset)));

            foreach (PermGroup g in new[] { PermGroup.CITIZEN, PermGroup.STRANGER, PermGroup.ALLY, PermGroup.COMRADE })
            {
                Assert.Equal(use, hall.PermsHandler.getPerm(g, PermType.USE_PERM));
                Assert.Equal(build, hall.PermsHandler.getPerm(g, PermType.BUILD_AND_DESTROY_PERM));
                Assert.Equal(attack, hall.PermsHandler.getPerm(g, PermType.ATTACK_ANIMALS_PERM));
            }
        }

        [Fact]
        public void UnknownPreset_ChangesNothing()
        {
            Assert.Equal("claims:wrong_value", SubPlotEditor.ApplyPreset(manager, hall, "castle").StatusMessage);
        }

        [Fact]
        public void Cell_CarriesTheGroup()
        {
            SubPlotEditor.SetGroup(manager, hall, "guild");

            Assert.Equal("guild", SubPlotSync.ToCell(hall).GroupName);
        }
    }
}
