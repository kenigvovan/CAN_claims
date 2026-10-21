using claims.src;
using claims.src.rights;

namespace claims.tests
{
    /// <summary>
    /// Reading claims_permissions.json, which admins edit by hand. One unknown name in it used to
    /// throw out of StartServerSide and keep the server from starting.
    /// </summary>
    public class RightsFileTests
    {
        private static Dictionary<string, List<string>> Raw(params (string Group, string[] Names)[] groups) =>
            groups.ToDictionary(g => g.Group, g => g.Names.ToList());

        [Fact]
        public void KnownNames_AreRead()
        {
            var groups = RightsHandler.ParsePermissionGroups(
                Raw(("MAYOR", new[] { "CITY_KICK", "CITY_INVITE" }), ("DEFAULT", new[] { "PLOT_CLAIM" })),
                out List<string> unknown);

            Assert.Empty(unknown);
            Assert.Equal(new HashSet<EnumPlayerPermissions> { EnumPlayerPermissions.CITY_KICK, EnumPlayerPermissions.CITY_INVITE }, groups["MAYOR"]);
            Assert.Equal(new HashSet<EnumPlayerPermissions> { EnumPlayerPermissions.PLOT_CLAIM }, groups["DEFAULT"]);
        }

        [Fact]
        public void UnknownName_IsSkippedAndReported_TheRestOfTheGroupStays()
        {
            var groups = RightsHandler.ParsePermissionGroups(
                Raw(("MAYOR", new[] { "CITY_KICK", "CITY_KIKC", "CITY_INVITE" })),
                out List<string> unknown);

            Assert.Equal(new[] { "MAYOR.CITY_KIKC" }, unknown);
            Assert.Equal(new HashSet<EnumPlayerPermissions> { EnumPlayerPermissions.CITY_KICK, EnumPlayerPermissions.CITY_INVITE }, groups["MAYOR"]);
        }

        [Theory]
        [InlineData("1024")]          // a number is not a name, even if it is a valid value
        [InlineData("city_kick")]     // names are case-sensitive, as the file has always been written
        [InlineData("CITY_KICK, CITY_INVITE")]
        [InlineData("")]
        public void NotAName_IsUnknown(string name)
        {
            var groups = RightsHandler.ParsePermissionGroups(Raw(("MAYOR", new[] { name })), out List<string> unknown);

            Assert.Single(unknown);
            Assert.Empty(groups["MAYOR"]);
        }

        [Fact]
        public void EmptyGroup_StaysAsAnEmptyGroup()
        {
            // An admin who emptied a group meant it: it must not be refilled as "missing".
            var raw = new Dictionary<string, List<string>> { ["LEADER"] = null! };

            var groups = RightsHandler.ParsePermissionGroups(raw, out List<string> unknown);

            Assert.Empty(unknown);
            Assert.Empty(groups["LEADER"]);
        }

        [Fact]
        public void EmptyFile_GivesNoGroups()
        {
            Assert.Empty(RightsHandler.ParsePermissionGroups(null!, out List<string> unknown));
            Assert.Empty(unknown);
        }
    }
}
