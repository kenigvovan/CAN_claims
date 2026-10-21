using System.Reflection;
using claims.src;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.part;
using claims.src.part.structure;
using claims.src.perms.type;
using claims.src.rights;
using claims.src.subplots;
using claims.src.subplots.geometry;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.tests
{
    /// <summary>
    /// What /subplot does: who may change what, and that every change keeps the rules a
    /// sub-plot is made under. Chunks (0,0) and (1,0) are the city's.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotEditorTests : IDisposable
    {
        private readonly ServerStubs stubs = new(new Config { PLOT_SIZE = 16 });
        private readonly IMoneyProvider previousEconomy = claims.src.claims.economyProvider;
        private readonly City city = new("TestCity", "spe-city");
        private readonly PlayerInfo manager = new("Manager", "spe-manager");
        private readonly PlayerInfo citizen = new("Citizen", "spe-citizen");
        private readonly PlayerInfo friend = new("Friend", "spe-friend");
        private readonly Plot left, right;

        public SubPlotEditorTests()
        {
            claims.src.claims.economyProvider = new Mock<IMoneyProvider>().Object;
            foreach (var p in new[] { manager, citizen })
            {
                typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(p, new object[] { city });
                city.getCityCitizens().Add(p);
            }
            manager.PlayerPermissionsHandler.AddPermission(EnumPlayerPermissions.CITY_SUBPLOT_MANAGE);
            left = Claim(0, 0);
            right = Claim(1, 0);
        }

        public void Dispose()
        {
            claims.src.claims.economyProvider = previousEconomy;
            stubs.Dispose();
        }

        private Plot Claim(int x, int z)
        {
            var plot = new Plot(new PlotPosition(x, z));
            plot.setCity(city);
            stubs.Storage.addClaimedPlot(plot.plotPosition, plot);
            return plot;
        }

        private static bool Ok(TextCommandResult r) => r.Status == EnumCommandStatus.Success;

        private SubPlot Made(string name = "flat", BlockBox? box = null)
        {
            var result = SubPlotEditor.Create(manager, name, box ?? new BlockBox(0, 64, 0, 8, 68, 8));
            Assert.True(Ok(result), result.StatusMessage);
            return SubPlotEditor.FindByName(city, name);
        }

        [Fact]
        public void Manager_Creates_ACitizenMayNot()
        {
            Assert.NotNull(Made());
            Assert.False(Ok(SubPlotEditor.Create(citizen, "other", new BlockBox(8, 64, 8, 12, 68, 12))));
        }

        [Fact]
        public void Create_RefusesATakenName_AnyCase()
        {
            Made("flat");

            var result = SubPlotEditor.Create(manager, "FLAT", new BlockBox(8, 64, 8, 12, 68, 12));

            Assert.Equal("claims:subplot_name_taken", result.StatusMessage);
        }

        [Fact]
        public void Create_RefusesGroundOutsideTheCity_AndOverlaps()
        {
            Made();

            Assert.Equal("claims:subplot_outside_city",
                SubPlotEditor.Create(manager, "far", new BlockBox(40, 64, 0, 44, 68, 4)).StatusMessage);
            Assert.Equal("claims:subplot_overlaps",
                SubPlotEditor.Create(manager, "over", new BlockBox(4, 64, 4, 12, 68, 12)).StatusMessage);
        }

        [Fact]
        public void Create_RefusesAPlotForSale()
        {
            right.Price = 100;

            Assert.Equal("claims:subplot_on_plot_for_sale",
                SubPlotEditor.Create(manager, "flat", new BlockBox(8, 64, 0, 24, 68, 8)).StatusMessage);
        }

        [Fact]
        public void NewSubPlot_IsTheCitys_AndShutToEveryone()
        {
            var flat = Made();

            Assert.Null(flat.Owner);
            Assert.False(SubPlotAccess.Decide(citizen, flat, PermType.BUILD_AND_DESTROY_PERM));
        }

        [Fact]
        public void AddBox_GrowsTheShape_OverTheChunkBorder()
        {
            var flat = Made();

            Assert.True(Ok(SubPlotEditor.AddBox(manager, flat, new BlockBox(8, 64, 0, 20, 68, 8))));

            Assert.Equal(20, flat.Shape.Bounds.MaxX);
            Assert.Single(stubs.Storage.SubPlotsInChunk(new Vec2i(1, 0)));
        }

        [Fact]
        public void AddBox_KeepsOnlyWhatIsNew_WhenTheBoxOverlapsTheShape()
        {
            var flat = Made();

            Assert.True(Ok(SubPlotEditor.AddBox(manager, flat, new BlockBox(4, 64, 0, 12, 68, 8))));

            Assert.Equal(12L * 4 * 8, flat.Shape.Volume);
            Assert.Equal("claims:subplot_box_already_inside",
                SubPlotEditor.AddBox(manager, flat, new BlockBox(0, 64, 0, 4, 68, 4)).StatusMessage);
        }

        [Fact]
        public void AddBox_RefusesABoxThatDoesNotTouch()
        {
            var flat = Made();

            Assert.Equal("claims:subplot_not_connected",
                SubPlotEditor.AddBox(manager, flat, new BlockBox(0, 80, 0, 4, 84, 4)).StatusMessage);
        }

        [Fact]
        public void CutBox_TakesPartOut_ButNotEverything()
        {
            var flat = Made();

            Assert.True(Ok(SubPlotEditor.CutBox(manager, flat, new BlockBox(4, 64, 0, 8, 68, 8))));
            Assert.Equal(4L * 4 * 8, flat.Shape.Volume);
            Assert.Equal("claims:subplot_cut_everything",
                SubPlotEditor.CutBox(manager, flat, new BlockBox(0, 0, 0, 16, 256, 16)).StatusMessage);
            Assert.Equal("claims:subplot_box_outside",
                SubPlotEditor.CutBox(manager, flat, new BlockBox(40, 0, 0, 44, 4, 4)).StatusMessage);
        }

        [Fact]
        public void BoxOverlappingTheSubPlot_IsFine_ForAdding_ThoughNotForANewOne()
        {
            var flat = Made();
            var box = new BlockBox(4, 64, 0, 12, 68, 8);

            // What the wand's highlight used to show in add mode: red, "overlaps".
            Assert.Equal("claims:subplot_overlaps", SubPlotEditor.Validate(new BoxShape(box), city, null));
            Assert.Null(SubPlotEditor.WhyNotChange(flat, box, add: true));
        }

        [Fact]
        public void WhyNotChange_GivesTheSameReasons_AsTheCommands()
        {
            var flat = Made();

            Assert.Equal("claims:subplot_box_already_inside", SubPlotEditor.WhyNotChange(flat, new BlockBox(0, 64, 0, 4, 68, 4), add: true));
            Assert.Equal("claims:subplot_not_connected", SubPlotEditor.WhyNotChange(flat, new BlockBox(0, 80, 0, 4, 84, 4), add: true));
            Assert.Equal("claims:subplot_box_outside", SubPlotEditor.WhyNotChange(flat, new BlockBox(40, 0, 0, 44, 4, 4), add: false));
            Assert.Equal("claims:subplot_cut_everything", SubPlotEditor.WhyNotChange(flat, new BlockBox(0, 0, 0, 16, 256, 16), add: false));
            Assert.Null(SubPlotEditor.WhyNotChange(flat, new BlockBox(4, 64, 0, 8, 68, 8), add: false));
        }

        [Fact]
        public void Owner_IsACitizen_AndThenSharesAndSetsRules()
        {
            var flat = Made();

            Assert.Equal("claims:subplot_owner_not_citizen", SubPlotEditor.SetOwner(manager, flat, friend).StatusMessage);
            Assert.True(Ok(SubPlotEditor.SetOwner(manager, flat, citizen)));

            Assert.True(Ok(SubPlotEditor.AddMember(citizen, flat, friend)));
            Assert.True(SubPlotAccess.Decide(friend, flat, PermType.BUILD_AND_DESTROY_PERM));
            Assert.True(Ok(SubPlotEditor.SetPerm(citizen, flat, "stranger", "use", "on")));
            Assert.True(flat.PermsHandler.StrangerPerms[(int)PermType.USE_PERM]);
        }

        [Fact]
        public void Owner_MayNotReshapeOrHandOver_OnlyShareAndSetRules()
        {
            var flat = Made();
            SubPlotEditor.SetOwner(manager, flat, citizen);

            Assert.False(Ok(SubPlotEditor.AddBox(citizen, flat, new BlockBox(8, 64, 0, 12, 68, 8))));
            Assert.False(Ok(SubPlotEditor.SetOwner(citizen, flat, null)));
            Assert.False(Ok(SubPlotEditor.Delete(citizen, flat)));
        }

        [Fact]
        public void Stranger_MayNotShareSomeoneElsesSubPlot()
        {
            var flat = Made();
            SubPlotEditor.SetOwner(manager, flat, citizen);

            Assert.False(Ok(SubPlotEditor.AddMember(friend, flat, friend)));
        }

        [Fact]
        public void NewOwner_StartsWithoutTheOldMembers()
        {
            var flat = Made();
            SubPlotEditor.AddMember(manager, flat, friend);

            SubPlotEditor.SetOwner(manager, flat, citizen);

            Assert.Empty(flat.Members);
        }

        [Fact]
        public void Delete_RemovesIt()
        {
            var flat = Made();

            Assert.True(Ok(SubPlotEditor.Delete(manager, flat)));

            Assert.Null(SubPlotEditor.FindByName(city, "flat"));
            Assert.Empty(stubs.Storage.SubPlotsInChunk(new Vec2i(0, 0)));
        }

        [Fact]
        public void SubPlot_BlocksPuttingItsPlotUpForSale()
        {
            Made();

            Assert.NotNull(SubPlotLifecycle.BlockingProblem(left));
            Assert.Null(SubPlotLifecycle.BlockingProblem(right));
        }
    }
}
