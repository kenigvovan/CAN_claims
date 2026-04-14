using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using System.Reflection;

namespace claims.tests
{
    public class IConflictPartyTests
    {
        // Sets PlayerInfo.City via private setter, bypassing setCity() (which needs claims.sapi).
        private static void SetPlayerCity(PlayerInfo playerInfo, City city)
        {
            typeof(PlayerInfo)
                .GetProperty("City")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(playerInfo, new object[] { city });
        }

        /*=====================================================================*/
        /* City.GetCities()                                                      */
        /*=====================================================================*/

        [Fact]
        public void City_GetCities_ReturnsSingletonListWithSelf()
        {
            var city = new City("TestCity", "city-guid");

            var result = city.GetCities();

            Assert.Single(result);
            Assert.Same(city, result[0]);
        }

        /*=====================================================================*/
        /* Alliance.GetCities()                                                  */
        /*=====================================================================*/

        [Fact]
        public void Alliance_GetCities_ReturnsAllCities()
        {
            var alliance = new Alliance("TestAlliance", "alliance-guid");
            var city1 = new City("City1", "c1");
            var city2 = new City("City2", "c2");
            alliance.Cities.Add(city1);
            alliance.Cities.Add(city2);

            var result = alliance.GetCities();

            Assert.Equal(2, result.Count);
            Assert.Contains(city1, result);
            Assert.Contains(city2, result);
        }

        [Fact]
        public void Alliance_GetCities_Empty_ReturnsEmptyList()
        {
            var alliance = new Alliance("TestAlliance", "alliance-guid");

            var result = alliance.GetCities();

            Assert.Empty(result);
        }

        /*=====================================================================*/
        /* City.AddHostileParty(city)                                            */
        /*=====================================================================*/

        [Fact]
        public void City_AddHostileParty_City_AddsToHostileCities()
        {
            var city1 = new City("City1", "c1");
            var city2 = new City("City2", "c2");

            city1.AddHostileParty(city2);

            Assert.Contains(city2, city1.HostileCities);
        }

        [Fact]
        public void City_AddHostileParty_City_NoDuplicates()
        {
            var city1 = new City("City1", "c1");
            var city2 = new City("City2", "c2");

            city1.AddHostileParty(city2);
            city1.AddHostileParty(city2);

            Assert.Single(city1.HostileCities);
        }

        /*=====================================================================*/
        /* City.AddHostileParty(alliance)                                        */
        /*=====================================================================*/

        [Fact]
        public void City_AddHostileParty_Alliance_AddsAllAllianceCities()
        {
            var ourCity = new City("OurCity", "oc");
            var alliance = new Alliance("EnemyAlliance", "ea");
            var enemy1 = new City("Enemy1", "e1");
            var enemy2 = new City("Enemy2", "e2");
            alliance.Cities.Add(enemy1);
            alliance.Cities.Add(enemy2);

            ourCity.AddHostileParty(alliance);

            Assert.Contains(enemy1, ourCity.HostileCities);
            Assert.Contains(enemy2, ourCity.HostileCities);
        }

        [Fact]
        public void City_AddHostileParty_Alliance_NoDuplicates()
        {
            var ourCity = new City("OurCity", "oc");
            var alliance = new Alliance("EnemyAlliance", "ea");
            var enemy = new City("Enemy", "e1");
            alliance.Cities.Add(enemy);

            ourCity.AddHostileParty(alliance);
            ourCity.AddHostileParty(alliance);

            Assert.Single(ourCity.HostileCities);
        }

        /*=====================================================================*/
        /* City.RemoveHostileParty(city)                                         */
        /*=====================================================================*/

        [Fact]
        public void City_RemoveHostileParty_City_RemovesFromHostileCities()
        {
            var city1 = new City("City1", "c1");
            var city2 = new City("City2", "c2");
            city1.HostileCities.Add(city2);

            city1.RemoveHostileParty(city2);

            Assert.DoesNotContain(city2, city1.HostileCities);
        }

        [Fact]
        public void City_RemoveHostileParty_Alliance_RemovesAllAllianceCities()
        {
            var ourCity = new City("OurCity", "oc");
            var alliance = new Alliance("EnemyAlliance", "ea");
            var enemy1 = new City("Enemy1", "e1");
            var enemy2 = new City("Enemy2", "e2");
            alliance.Cities.Add(enemy1);
            alliance.Cities.Add(enemy2);
            ourCity.HostileCities.Add(enemy1);
            ourCity.HostileCities.Add(enemy2);

            ourCity.RemoveHostileParty(alliance);

            Assert.DoesNotContain(enemy1, ourCity.HostileCities);
            Assert.DoesNotContain(enemy2, ourCity.HostileCities);
        }

        /*=====================================================================*/
        /* Alliance.AddHostileParty                                              */
        /*=====================================================================*/

        [Fact]
        public void Alliance_AddHostileParty_Alliance_AddsToHostileParties()
        {
            var a1 = new Alliance("Alliance1", "a1");
            var a2 = new Alliance("Alliance2", "a2");

            a1.AddHostileParty(a2);

            Assert.Contains(a2, a1.HostileParties);
        }

        [Fact]
        public void Alliance_AddHostileParty_City_AddsToHostileParties()
        {
            var alliance = new Alliance("Alliance1", "a1");
            var city = new City("StandaloneCity", "sc");

            alliance.AddHostileParty(city);

            Assert.Contains(city, alliance.HostileParties);
        }

        [Fact]
        public void Alliance_AddHostileParty_NoDuplicates()
        {
            var a1 = new Alliance("Alliance1", "a1");
            var a2 = new Alliance("Alliance2", "a2");

            a1.AddHostileParty(a2);
            a1.AddHostileParty(a2);

            Assert.Single(a1.HostileParties);
        }

        /*=====================================================================*/
        /* Alliance.RemoveHostileParty                                           */
        /*=====================================================================*/

        [Fact]
        public void Alliance_RemoveHostileParty_RemovesFromHostileParties()
        {
            var a1 = new Alliance("Alliance1", "a1");
            var a2 = new Alliance("Alliance2", "a2");
            a1.HostileParties.Add(a2);

            a1.RemoveHostileParty(a2);

            Assert.DoesNotContain(a2, a1.HostileParties);
        }

        /*=====================================================================*/
        /* GetHostileParties                                                     */
        /*=====================================================================*/

        [Fact]
        public void City_GetHostileParties_ReturnsHostileCitiesAsIConflictParty()
        {
            var city1 = new City("City1", "c1");
            var city2 = new City("City2", "c2");
            city1.HostileCities.Add(city2);

            var result = city1.GetHostileParties().ToList();

            Assert.Single(result);
            Assert.Same(city2, result[0]);
        }

        [Fact]
        public void Alliance_GetHostileParties_ReturnsHostileParties()
        {
            var a1 = new Alliance("Alliance1", "a1");
            var a2 = new Alliance("Alliance2", "a2");
            a1.HostileParties.Add(a2);

            var result = a1.GetHostileParties().ToList();

            Assert.Single(result);
            Assert.Same(a2, result[0]);
        }
    }
}
