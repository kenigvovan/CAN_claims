using claims.src;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure.plots;

namespace claims.tests
{
    /// <summary>What a plot keeps and what it gives back when its owner loses it.</summary>
    [Collection("ModStatics")]
    public class PlotOwnershipTests
    {
        private readonly City city = new("TestCity", "own-city-guid");
        private readonly PlayerInfo owner = new("Owner", "own-owner-uid");
        private readonly Plot plot = new(new PlotPosition(0, 0));

        public PlotOwnershipTests()
        {
            claims.src.claims.config = new Config();
            plot.setCity(city);
            plot.setPlotOwner(owner);
        }

        [Fact]
        public void ResetOwner_PutsTheCitysRightsBack()
        {
            // The city lets its citizens do everything; the owner shut them out and let strangers build (R28).
            city.getPermsHandler().CitizenPerms = new[] { true, true, true };
            plot.getPermsHandler().StrangerPerms[1] = true;

            plot.resetOwner();

            Assert.False(plot.hasPlotOwner());
            Assert.Equal(city.getPermsHandler().StrangerPerms, plot.getPermsHandler().StrangerPerms);
            Assert.Equal(city.getPermsHandler().CitizenPerms, plot.getPermsHandler().CitizenPerms);
        }

        [Fact]
        public void ResetOwner_KeepsThePlotsFlags()
        {
            plot.getPermsHandler().setPvp(true);
            city.getPermsHandler().setPvp(false);

            plot.resetOwner();

            Assert.True(plot.getPermsHandler().pvpFlag);
        }

        [Fact]
        public void ResetOwner_RightsAreCopied_NotShared()
        {
            plot.resetOwner();
            plot.getPermsHandler().StrangerPerms[1] = !city.getPermsHandler().StrangerPerms[1];

            Assert.NotEqual(city.getPermsHandler().StrangerPerms[1], plot.getPermsHandler().StrangerPerms[1]);
        }

        [Fact]
        public void ResetOwner_PlotWithoutCity_OnlyDropsTheOwner()
        {
            var loose = new Plot(new PlotPosition(1, 0));
            loose.setPlotOwner(owner);
            loose.getPermsHandler().StrangerPerms[1] = true;

            loose.resetOwner();

            Assert.False(loose.hasPlotOwner());
            Assert.True(loose.getPermsHandler().StrangerPerms[1]);
        }
    }
}
