using claims.src.network.packets;

namespace claims.tests
{
    /// <summary>The pvp a plot shows on the client: its own rule through the world overrides, as OnPVP decides.</summary>
    public class WorldPvpShownTests
    {
        private static AdminWorldFlags World(bool everywhere, bool forbidden) =>
            new AdminWorldFlags { PvpEverywhere = everywhere, PvpForbidden = forbidden };

        [Theory]
        // everywhere, forbidden, plot's rule -> shown
        [InlineData(false, false, false, false)]
        [InlineData(false, false, true, true)]
        [InlineData(true, false, false, true)]
        [InlineData(true, false, true, true)]
        [InlineData(false, true, false, false)]
        [InlineData(false, true, true, false)]
        // both on: OnPVP checks "everywhere" first
        [InlineData(true, true, false, true)]
        [InlineData(true, true, true, true)]
        public void EffectivePvp_FollowsTheWorldOverrides(bool everywhere, bool forbidden, bool plotPvp, bool expected)
        {
            Assert.Equal(expected, AdminWorldFlags.EffectivePvp(World(everywhere, forbidden), plotPvp));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EffectivePvp_BeforeTheFlagsArrive_IsThePlotsRule(bool plotPvp)
        {
            Assert.Equal(plotPvp, AdminWorldFlags.EffectivePvp(null, plotPvp));
        }
    }
}
