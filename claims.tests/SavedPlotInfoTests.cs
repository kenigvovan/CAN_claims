using claims.src.clientMapHandling;
using claims.src.perms;

namespace claims.tests
{
    /// <summary>The group rights the plot markers show, packed into one number for the client.</summary>
    public class SavedPlotInfoTests
    {
        // Groups in RightsGroupOrder: friend, citizen, ally, stranger; types: use, build, attack.
        [Fact]
        public void GroupRights_RoundTrip_EachRightOnItsOwn()
        {
            var handler = new PermsHandler();
            handler.ComradePerms[2] = true;   // friend may attack
            handler.CitizenPerms[1] = true;   // citizen may build
            handler.StrangerPerms[0] = true;  // stranger may use

            var info = new SavedPlotInfo { GroupRights = SavedPlotInfo.EncodeGroupRights(handler) };

            for (int g = 0; g < 4; g++)
            {
                for (int t = 0; t < 3; t++)
                {
                    bool expected = (g == 0 && t == 2) || (g == 1 && t == 1) || (g == 3 && t == 0);
                    Assert.True(expected == info.GroupMay(g, t), $"group {g}, type {t}");
                }
            }
        }

        [Fact]
        public void GroupRights_NothingAllowed_IsZero()
        {
            Assert.Equal(0, SavedPlotInfo.EncodeGroupRights(new PermsHandler()));
            Assert.Equal(0, SavedPlotInfo.EncodeGroupRights(null!));
        }

        [Fact]
        public void GroupRights_EverythingAllowed_IsTwelveBits()
        {
            var handler = new PermsHandler();
            handler.setValueForAll(true);

            Assert.Equal((1 << 12) - 1, SavedPlotInfo.EncodeGroupRights(handler));
        }
    }
}
