using claims.src.perms;

namespace claims.tests
{
    // One value of a plot's 16 must never move another. Cases: FI.
    // PermsHandler reads static name dictionaries.
    [Collection("ModStatics")]
    public class PermsHandlerTests
    {
        private static readonly string[] Groups = { "citizen", "stranger", "ally", "friend" };
        private static readonly string[] Types = { "use", "build", "attack" };

        public PermsHandlerTests()
        {
            PermsHandler.initDicts();
        }

        public static TheoryData<string> AllValues()
        {
            var data = new TheoryData<string> { "pvp", "fire", "blast", "nomobspawn" };
            foreach (string g in Groups)
                foreach (string t in Types)
                    data.Add(g + ":" + t);
            return data;
        }

        /// <summary>All 16 values by name.</summary>
        private static Dictionary<string, bool> Snapshot(PermsHandler h)
        {
            var s = new Dictionary<string, bool>
            {
                ["pvp"] = h.pvpFlag,
                ["fire"] = h.fireFlag,
                ["blast"] = h.blastFlag,
                ["nomobspawn"] = h.noMobSpawnFlag,
            };
            bool[][] byGroup = { h.CitizenPerms, h.StrangerPerms, h.AlliancePerms, h.ComradePerms };
            for (int g = 0; g < Groups.Length; g++)
                for (int t = 0; t < Types.Length; t++)
                    s[Groups[g] + ":" + Types[t]] = byGroup[g][t];
            return s;
        }

        /// <summary>Flips one value the way the game does: flags by their setters, rights by setAccessPerm.</summary>
        private static void Flip(PermsHandler h, string value)
        {
            bool now = Snapshot(h)[value];
            switch (value)
            {
                case "pvp": h.setPvp(!now); break;
                case "fire": h.setFire(!now); break;
                case "blast": h.setBlast(!now); break;
                case "nomobspawn": h.setNoMobSpawn(!now); break;
                default:
                    string[] parts = value.Split(':');
                    Assert.True(h.setAccessPerm(parts[0], parts[1], now ? "off" : "on"), "setAccessPerm refused " + value);
                    break;
            }
        }

        // FI1, R35
        [Theory]
        [MemberData(nameof(AllValues))]
        public void Values_Should_SurviveSaveAndLoad_When_OneIsChanged(string value)
        {
            var saved = new PermsHandler();
            Flip(saved, value);

            var loaded = new PermsHandler();
            loaded.setPerms(saved.ToString());

            Assert.Equal(Snapshot(saved), Snapshot(loaded));
        }

        // FI2, R35
        [Theory]
        [MemberData(nameof(AllValues))]
        public void Change_Should_TouchOnlyThatValue(string value)
        {
            var h = new PermsHandler();
            Dictionary<string, bool> before = Snapshot(h);

            Flip(h, value);

            Dictionary<string, bool> after = Snapshot(h);
            string[] changed = before.Keys.Where(k => before[k] != after[k]).ToArray();
            Assert.Equal(new[] { value }, changed);
        }

        /// <summary>A handler where every one of the 16 values is the opposite of the default.</summary>
        private static PermsHandler AllFlipped()
        {
            var h = new PermsHandler();
            foreach (string value in Snapshot(h).Keys) Flip(h, value);
            return h;
        }

        // A plot bought by a citizen takes the buyer's rights and keeps its own flags (FI5).
        [Fact]
        public void SetRights_Should_CopyTheRights_And_KeepTheFlags()
        {
            var plot = new PermsHandler();
            Dictionary<string, bool> before = Snapshot(plot);
            PermsHandler buyer = AllFlipped();

            plot.setRights(buyer);

            Dictionary<string, bool> after = Snapshot(plot), source = Snapshot(buyer);
            foreach (string flag in new[] { "pvp", "fire", "blast", "nomobspawn" })
                Assert.True(before[flag] == after[flag], $"setRights changed the flag {flag}");
            foreach (string right in after.Keys.Where(k => k.Contains(':')))
                Assert.True(source[right] == after[right], $"setRights did not copy {right}");
        }

        // setPerm, used when a city takes a plot, still copies all 16.
        [Fact]
        public void SetPerm_Should_CopyEverything()
        {
            var plot = new PermsHandler();
            PermsHandler city = AllFlipped();

            plot.setPerm(city);

            Assert.Equal(Snapshot(city), Snapshot(plot));
        }

        // The saved string lists only what is on, so loading it onto a handler that held other
        // values must turn those off - pvp, fire and the rights used to survive the load.
        [Fact]
        public void Load_Should_ResetEverythingTheStringLeavesOut()
        {
            var defaults = new PermsHandler();
            PermsHandler loaded = AllFlipped();

            loaded.setPerms(defaults.ToString());

            Assert.Equal(Snapshot(defaults), Snapshot(loaded));
        }

        // An unknown right in a saved row used to throw KeyNotFoundException out of loading the plot.
        [Theory]
        [InlineData("citizen:fly;stanger:build")]
        [InlineData("stanger:build;citizen:")]
        [InlineData("stanger:build;citizen")]
        [InlineData("stanger:build;;;garbage")]
        public void Load_Should_SkipWhatItDoesNotKnow(string saved)
        {
            var h = new PermsHandler();

            h.setPerms(saved);

            Assert.True(h.StrangerPerms[1], "the known right next to the unknown one was lost");
            Assert.DoesNotContain(true, h.CitizenPerms);
        }

        [Fact]
        public void Load_Should_TakeNull_AsNothingOn()
        {
            PermsHandler h = AllFlipped();

            h.setPerms(null!);

            Assert.DoesNotContain(true, Snapshot(h).Values);
        }

        // ToString was declared with new: through an object reference it gave the type name.
        [Fact]
        public void ToString_Should_BeTheSavedForm_ThroughAnyReference()
        {
            var h = new PermsHandler();
            h.setAccessPerm("stranger", "build", "on");
            object asObject = h;

            Assert.Equal(h.ToString(), asObject.ToString());
            Assert.Equal(h.ToString(), $"{h}");
            Assert.Contains("stanger:build", asObject.ToString());
        }

        // The chat line printed the blast flag inverted.
        [Theory]
        [InlineData(true, "blast: on")]
        [InlineData(false, "blast: off")]
        public void ChatString_Should_ShowBlastAsItIs(bool blast, string expected)
        {
            var h = new PermsHandler();
            h.setBlast(blast);

            Assert.Contains(expected, h.getStringForChat());
        }

        // FI4, R35: only "on" and "off" are states; anything else used to mean "off".
        [Theory]
        [InlineData("onn")]
        [InlineData("On")]
        [InlineData("true")]
        [InlineData("")]
        public void Right_Should_StayAsItWas_When_TheStateIsMistyped(string state)
        {
            var h = new PermsHandler();
            Assert.True(h.setAccessPerm("stranger", "attack", "on"), "precondition: setAccessPerm refused 'on'");
            Dictionary<string, bool> before = Snapshot(h);

            Assert.False(h.setAccessPerm("stranger", "attack", state), $"'{state}' was accepted as a state");
            Assert.Equal(before, Snapshot(h));
        }
    }
}
