using System.Reflection;
using claims.src;
using claims.src.part;
using claims.src.part.structure;
using claims.src.perms.type;
using claims.src.subplots;
using claims.src.subplots.geometry;

namespace claims.tests
{
    /// <summary>
    /// Who may do what inside a sub-plot. Its rules win over the chunk's both ways; war is
    /// not its call.
    /// </summary>
    [Collection("ModStatics")]
    public class SubPlotAccessTests
    {
        private readonly City city = new("TestCity", "spa-city");
        private readonly PlayerInfo mayor = new("Mayor", "spa-mayor");
        private readonly PlayerInfo owner = new("Owner", "spa-owner");
        private readonly PlayerInfo member = new("Member", "spa-member");
        private readonly PlayerInfo citizen = new("Citizen", "spa-citizen");
        private readonly PlayerInfo stranger = new("Stranger", "spa-stranger");
        private readonly SubPlot flat;

        public SubPlotAccessTests()
        {
            claims.src.claims.config = new Config { PLOT_SIZE = 16 };
            typeof(City).GetField("mayor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(city, mayor);
            foreach (var p in new[] { mayor, owner, member, citizen }) AddCitizen(city, p);

            flat = new SubPlot("flat", "spa-flat")
            {
                City = city,
                Owner = owner,
                Shape = new BoxShape(new BlockBox(0, 64, 0, 8, 68, 8))
            };
            flat.Members.Add(member);
        }

        private static void SetCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo).GetProperty("City")!.GetSetMethod(nonPublic: true)!.Invoke(playerInfo, new object[] { city });
        }

        private static void AddCitizen(City city, PlayerInfo playerInfo)
        {
            SetCity(playerInfo, city);
            city.getCityCitizens().Add(playerInfo);
        }

        private bool? Build(PlayerInfo who) => SubPlotAccess.Decide(who, flat, PermType.BUILD_AND_DESTROY_PERM);

        [Fact]
        public void OwnerMemberAndMayor_MayDoEverything_WhateverTheRules()
        {
            // Nothing allowed to any group.
            Assert.True(Build(owner));
            Assert.True(Build(member));
            Assert.True(Build(mayor));
            Assert.True(SubPlotAccess.Decide(owner, flat, PermType.USE_PERM));
            Assert.True(SubPlotAccess.Decide(member, flat, PermType.ATTACK_ANIMALS_PERM));
        }

        [Fact]
        public void Citizen_IsShutOut_ByDefault_EvenWhereTheChunkWouldLetThemBuild()
        {
            Assert.False(Build(citizen));
        }

        [Fact]
        public void Citizen_FollowsTheCitizenRuleOfTheSubPlot()
        {
            flat.PermsHandler.CitizenPerms[(int)PermType.BUILD_AND_DESTROY_PERM] = true;

            Assert.True(Build(citizen));
            Assert.False(Build(stranger));
        }

        [Fact]
        public void Stranger_FollowsTheStrangerRule()
        {
            flat.PermsHandler.StrangerPerms[(int)PermType.USE_PERM] = true;

            Assert.True(SubPlotAccess.Decide(stranger, flat, PermType.USE_PERM));
            Assert.False(Build(stranger));
        }

        [Fact]
        public void OwnersFriend_GetsTheFriendRule_OnTopOfTheirOwn()
        {
            owner.Friends.Add(stranger);
            flat.PermsHandler.ComradePerms[(int)PermType.BUILD_AND_DESTROY_PERM] = true;

            Assert.True(Build(stranger));
        }

        [Fact]
        public void OwnersFriend_DoesNotLoseTheirCitizenRight_ToAStricterFriendRule()
        {
            owner.Friends.Add(citizen);
            flat.PermsHandler.CitizenPerms[(int)PermType.BUILD_AND_DESTROY_PERM] = true;

            Assert.True(Build(citizen));
        }

        [Fact]
        public void Ally_FollowsTheAllyRule()
        {
            var alliance = new Alliance("Our", "spa-alliance");
            var friendlyAlliance = new Alliance("Theirs", "spa-friendly");
            alliance.ComradAlliancies.Add(friendlyAlliance);
            city.Alliance = alliance;
            var allyCity = new City("AllyCity", "spa-allycity") { Alliance = friendlyAlliance };
            var ally = new PlayerInfo("Ally", "spa-ally");
            AddCitizen(allyCity, ally);
            flat.PermsHandler.AlliancePerms[(int)PermType.USE_PERM] = true;

            Assert.True(SubPlotAccess.Decide(ally, flat, PermType.USE_PERM));
            Assert.False(Build(ally));
        }

        [Fact]
        public void EnemyAtWar_IsNotTheSubPlotsCall()
        {
            var enemyCity = new City("Enemy", "spa-enemy");
            city.HostileCities.Add(enemyCity);
            var enemy = new PlayerInfo("Enemy", "spa-enemyplayer");
            AddCitizen(enemyCity, enemy);

            Assert.Null(Build(enemy));
        }

        [Fact]
        public void UnsoldSubPlot_CitizensFollowItsRules_TheMayorStillMayAll()
        {
            flat.Owner = null;
            flat.Members.Clear();

            Assert.False(Build(citizen));
            Assert.True(Build(mayor));
        }
    }
}
