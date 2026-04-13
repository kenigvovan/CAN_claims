using claims.src.perms;
using claims.src.perms.type;

namespace claims.tests
{
    public class PermsHandlerTests
    {
        public PermsHandlerTests()
        {
            PermsHandler.initDicts();
        }

        // =====================================================================
        // Default state
        // =====================================================================

        [Fact]
        public void Constructor_AllPerms_DefaultFalse()
        {
            var h = new PermsHandler();

            foreach (PermGroup group in Enum.GetValues<PermGroup>())
                foreach (PermType type in Enum.GetValues<PermType>())
                    Assert.False(h.getPerm(group, type));
        }

        [Fact]
        public void Constructor_BlastFlag_DefaultTrue()
        {
            var h = new PermsHandler();
            Assert.True(h.blastFlag);
        }

        [Fact]
        public void Constructor_PvpAndFire_DefaultFalse()
        {
            var h = new PermsHandler();
            Assert.False(h.pvpFlag);
            Assert.False(h.fireFlag);
        }

        // =====================================================================
        // setPerm / getPerm — all groups and types
        // =====================================================================

        [Theory]
        [InlineData(PermGroup.CITIZEN,  PermType.USE_PERM)]
        [InlineData(PermGroup.CITIZEN,  PermType.BUILD_AND_DESTROY_PERM)]
        [InlineData(PermGroup.CITIZEN,  PermType.ATTACK_ANIMALS_PERM)]
        [InlineData(PermGroup.STRANGER, PermType.USE_PERM)]
        [InlineData(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM)]
        [InlineData(PermGroup.STRANGER, PermType.ATTACK_ANIMALS_PERM)]
        [InlineData(PermGroup.ALLY,     PermType.USE_PERM)]
        [InlineData(PermGroup.ALLY,     PermType.BUILD_AND_DESTROY_PERM)]
        [InlineData(PermGroup.ALLY,     PermType.ATTACK_ANIMALS_PERM)]
        [InlineData(PermGroup.COMRADE,  PermType.USE_PERM)]
        [InlineData(PermGroup.COMRADE,  PermType.BUILD_AND_DESTROY_PERM)]
        [InlineData(PermGroup.COMRADE,  PermType.ATTACK_ANIMALS_PERM)]
        public void SetPerm_ThenGetPerm_ReturnsExpected(PermGroup group, PermType type)
        {
            var h = new PermsHandler();

            h.setPerm(group, type, true);
            Assert.True(h.getPerm(group, type));

            h.setPerm(group, type, false);
            Assert.False(h.getPerm(group, type));
        }

        [Fact]
        public void SetPerm_OneGroup_DoesNotAffectOthers()
        {
            var h = new PermsHandler();
            h.setPerm(PermGroup.CITIZEN, PermType.USE_PERM, true);

            Assert.False(h.getPerm(PermGroup.STRANGER, PermType.USE_PERM));
            Assert.False(h.getPerm(PermGroup.ALLY,     PermType.USE_PERM));
            Assert.False(h.getPerm(PermGroup.COMRADE,  PermType.USE_PERM));
        }

        // =====================================================================
        // setAccessPerm(string, string, string)
        // =====================================================================

        [Theory]
        [InlineData("citizen",  "use",    "on",  PermGroup.CITIZEN,  PermType.USE_PERM)]
        [InlineData("stranger", "build",  "on",  PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM)]
        [InlineData("ally",     "attack", "on",  PermGroup.ALLY,     PermType.ATTACK_ANIMALS_PERM)]
        [InlineData("friend",   "use",    "off", PermGroup.COMRADE,  PermType.USE_PERM)]
        public void SetAccessPerm_ValidArgs_SetsCorrectly(
            string group, string type, string val, PermGroup expectedGroup, PermType expectedType)
        {
            var h = new PermsHandler();
            bool result = h.setAccessPerm(group, type, val);

            Assert.True(result);
            Assert.Equal(val == "on", h.getPerm(expectedGroup, expectedType));
        }

        [Fact]
        public void SetAccessPerm_InvalidGroup_ReturnsFalse()
        {
            var h = new PermsHandler();
            Assert.False(h.setAccessPerm("admin", "use", "on"));
        }

        [Fact]
        public void SetAccessPerm_InvalidType_ReturnsFalse()
        {
            var h = new PermsHandler();
            Assert.False(h.setAccessPerm("citizen", "fly", "on"));
        }

        // =====================================================================
        // setPerm(PermsHandler copyFrom) — full copy
        // =====================================================================

        [Fact]
        public void SetPerm_CopyFrom_CopiesAllGroupsAndFlags()
        {
            var src = new PermsHandler();
            src.setPerm(PermGroup.CITIZEN,  PermType.USE_PERM,              true);
            src.setPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM, true);
            src.setPerm(PermGroup.ALLY,     PermType.ATTACK_ANIMALS_PERM,    true);
            src.setPerm(PermGroup.COMRADE,  PermType.USE_PERM,              true);
            src.pvpFlag  = true;
            src.fireFlag = true;
            src.blastFlag = false;

            var dst = new PermsHandler();
            dst.setPerm(src);

            Assert.True(dst.getPerm(PermGroup.CITIZEN,  PermType.USE_PERM));
            Assert.True(dst.getPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM));
            Assert.True(dst.getPerm(PermGroup.ALLY,     PermType.ATTACK_ANIMALS_PERM));
            Assert.True(dst.getPerm(PermGroup.COMRADE,  PermType.USE_PERM));
            Assert.True(dst.pvpFlag);
            Assert.True(dst.fireFlag);
            Assert.False(dst.blastFlag);
        }

        [Fact]
        public void SetPerm_CopyFrom_IsDeepCopy()
        {
            var src = new PermsHandler();
            src.setPerm(PermGroup.CITIZEN, PermType.USE_PERM, true);

            var dst = new PermsHandler();
            dst.setPerm(src);

            // mutate source — destination must not change
            src.setPerm(PermGroup.CITIZEN, PermType.USE_PERM, false);

            Assert.True(dst.getPerm(PermGroup.CITIZEN, PermType.USE_PERM));
        }

        // =====================================================================
        // ApplyFromHandler — one group at a time
        // =====================================================================

        [Fact]
        public void ApplyFromHandler_CopiesOnlyTargetGroup()
        {
            var src = new PermsHandler();
            src.setPerm(PermGroup.ALLY, PermType.USE_PERM, true);
            src.setPerm(PermGroup.CITIZEN, PermType.USE_PERM, true);

            var dst = new PermsHandler();
            dst.ApplyFromHandler(src, PermGroup.ALLY);

            Assert.True(dst.getPerm(PermGroup.ALLY, PermType.USE_PERM));
            Assert.False(dst.getPerm(PermGroup.CITIZEN, PermType.USE_PERM)); // not copied
        }

        // =====================================================================
        // setPvp / setFire / setBlast (string overloads)
        // =====================================================================

        [Fact]
        public void SetPvp_On_SetsTrueReturnsTrue()
        {
            var h = new PermsHandler();
            bool result = h.setPvp("on");
            Assert.True(result);
            Assert.True(h.pvpFlag);
        }

        [Fact]
        public void SetPvp_Off_SetsFalseReturnsTrue()
        {
            var h = new PermsHandler();
            h.pvpFlag = true;
            bool result = h.setPvp("off");
            Assert.True(result);
            Assert.False(h.pvpFlag);
        }

        [Fact]
        public void SetPvp_SameValue_ReturnsFalse()
        {
            var h = new PermsHandler();
            // pvpFlag is already false
            bool result = h.setPvp("off");
            Assert.False(result);
        }

        [Fact]
        public void SetFire_On_SetsTrueReturnsTrue()
        {
            var h = new PermsHandler();
            bool result = h.setFire("on");
            Assert.True(result);
            Assert.True(h.fireFlag);
        }

        [Fact]
        public void SetBlast_Off_SetsFalseReturnsTrue()
        {
            var h = new PermsHandler();
            // blastFlag defaults to true
            bool result = h.setBlast("off");
            Assert.True(result);
            Assert.False(h.blastFlag);
        }

        // =====================================================================
        // setValueForAll — bug: StrangerPerms and AlliancePerms not set
        // =====================================================================

        /// <summary>
        /// Bug: setValueForAll loops over StrangerPerms and AlliancePerms indices
        /// but writes to CitizenPerms[i] each time — Stranger and Alliance stay unchanged.
        /// Fix: each loop body should write to its own array.
        /// </summary>
        [Fact]
        public void SetValueForAll_True_SetsAllGroups()
        {
            var h = new PermsHandler();
            h.setValueForAll(true);

            foreach (PermType type in Enum.GetValues<PermType>())
            {
                Assert.True(h.getPerm(PermGroup.CITIZEN,  type));
                Assert.True(h.getPerm(PermGroup.STRANGER, type)); // fails due to bug
                Assert.True(h.getPerm(PermGroup.ALLY,     type)); // fails due to bug
                Assert.True(h.getPerm(PermGroup.COMRADE,  type));
            }
        }

        [Fact]
        public void SetValueForAll_False_ClearsAllGroups()
        {
            var h = new PermsHandler();
            foreach (PermGroup group in Enum.GetValues<PermGroup>())
                foreach (PermType type in Enum.GetValues<PermType>())
                    h.setPerm(group, type, true);

            h.setValueForAll(false);

            foreach (PermType type in Enum.GetValues<PermType>())
            {
                Assert.False(h.getPerm(PermGroup.CITIZEN,  type));
                Assert.False(h.getPerm(PermGroup.STRANGER, type));
                Assert.False(h.getPerm(PermGroup.ALLY,     type));
                Assert.False(h.getPerm(PermGroup.COMRADE,  type));
            }
        }

        // =====================================================================
        // ToString / setPerms round-trip
        // =====================================================================

        [Fact]
        public void ToString_SetPerms_RoundTrip_RestoresState()
        {
            var original = new PermsHandler();
            original.setPerm(PermGroup.CITIZEN,  PermType.USE_PERM,              true);
            original.setPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM, true);
            original.setPerm(PermGroup.ALLY,     PermType.ATTACK_ANIMALS_PERM,    true);
            original.setPerm(PermGroup.COMRADE,  PermType.USE_PERM,              true);
            original.pvpFlag  = true;
            original.fireFlag = true;

            string serialized = original.ToString();

            var restored = new PermsHandler();
            restored.setPerms(serialized);

            Assert.True(restored.getPerm(PermGroup.CITIZEN,  PermType.USE_PERM));
            Assert.True(restored.getPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM));
            Assert.True(restored.getPerm(PermGroup.ALLY,     PermType.ATTACK_ANIMALS_PERM));
            Assert.True(restored.getPerm(PermGroup.COMRADE,  PermType.USE_PERM));
            Assert.True(restored.pvpFlag);
            Assert.True(restored.fireFlag);
        }

        [Fact]
        public void ToString_EmptyPerms_ReturnsEmptyString()
        {
            var h = new PermsHandler();
            h.blastFlag = false; // default is true, would emit "blast;"

            Assert.Equal(string.Empty, h.ToString());
        }
    }
}
