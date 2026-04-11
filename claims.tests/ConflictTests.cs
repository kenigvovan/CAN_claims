using claims.src;
using claims.src.part.structure.conflict;
using Moq;
using Vintagestory.API.Config;

namespace claims.tests
{
    /// <summary>
    /// Tests for Conflict (pure date/time logic) and ConflictHandler (letter/conflict registry).
    /// </summary>
    public class ConflictTests
    {
        private readonly Mock<DataStorage> _storageMock;
        private readonly Mock<IConflictParty> _partyA;
        private readonly Mock<IConflictParty> _partyB;

        public ConflictTests()
        {
            _storageMock = new Mock<DataStorage>(false);
            claims.src.claims.dataStorage = _storageMock.Object;
            claims.src.claims.config = new Config();

            _partyA = new Mock<IConflictParty>();
            _partyA.Setup(p => p.Guid).Returns("party-a-guid");
            _partyA.Setup(p => p.GetHashCode()).Returns(1);

            _partyB = new Mock<IConflictParty>();
            _partyB.Setup(p => p.Guid).Returns("party-b-guid");
            _partyB.Setup(p => p.GetHashCode()).Returns(2);

            if (!Lang.AvailableLanguages.ContainsKey("en"))
            {
                var langMock = new Mock<ITranslationService>();
                langMock.Setup(t => t.Get(It.IsAny<string>(), It.IsAny<object[]>()))
                    .Returns<string, object[]>((key, _) => key);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(false);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(false);
                Lang.AvailableLanguages["en"] = langMock.Object;
            }
            Lang.ChangeLanguage("en");

            // each test starts with a clean letter list
            ConflictHandler.clearAll();
        }

        // =====================================================================
        // Conflict — MinimumDaysHasPassed
        // =====================================================================

        [Fact]
        public void MinimumDaysHasPassed_NoLastBattle_ReturnsTrue()
        {
            var conflict = new Conflict("war", "g1");
            // LastBattleDateStart == UnixEpoch by default

            Assert.True(conflict.MinimumDaysHasPassed());
        }

        [Fact]
        public void MinimumDaysHasPassed_EnoughTimeElapsed_ReturnsTrue()
        {
            var conflict = new Conflict("war", "g1") { MinimumDaysBetweenBattles = 3 };
            conflict.LastBattleDateStart = DateTime.Now.AddDays(-4);

            Assert.True(conflict.MinimumDaysHasPassed());
        }

        [Fact]
        public void MinimumDaysHasPassed_NotEnoughTimeElapsed_ReturnsFalse()
        {
            var conflict = new Conflict("war", "g1") { MinimumDaysBetweenBattles = 6 };
            conflict.LastBattleDateStart = DateTime.Now.AddDays(-2);

            Assert.False(conflict.MinimumDaysHasPassed());
        }

        // =====================================================================
        // Conflict — MinutesTillMinPassed
        // =====================================================================

        [Fact]
        public void MinutesTillMinPassed_NoLastBattle_ReturnsZero()
        {
            var conflict = new Conflict("war", "g1");

            Assert.Equal(0, conflict.MinutesTillMinPassed());
        }

        [Fact]
        public void MinutesTillMinPassed_JustStarted_ReturnsApproxFullCooldown()
        {
            var conflict = new Conflict("war", "g1") { MinimumDaysBetweenBattles = 1 };
            conflict.LastBattleDateStart = DateTime.Now;

            int expectedMinutes = 1 * 24 * 60;
            int result = conflict.MinutesTillMinPassed();

            // allow a few seconds of margin
            Assert.InRange(result, expectedMinutes - 2, expectedMinutes);
        }

        [Fact]
        public void MinutesTillMinPassed_CooldownExpired_ReturnsZero()
        {
            var conflict = new Conflict("war", "g1") { MinimumDaysBetweenBattles = 1 };
            conflict.LastBattleDateStart = DateTime.Now.AddDays(-2);

            Assert.Equal(0, conflict.MinutesTillMinPassed());
        }

        // =====================================================================
        // Conflict — CalculateNextBattleDate
        // =====================================================================

        [Fact]
        public void CalculateNextBattleDate_NoRanges_LeavesEpoch()
        {
            var conflict = new Conflict("war", "g1");
            // WarRanges is empty

            conflict.CalculateNextBattleDate();

            Assert.Equal(DateTime.UnixEpoch, conflict.NextBattleDateStart);
        }

        [Fact]
        public void CalculateNextBattleDate_OneRange_SetsNextDate()
        {
            var conflict = new Conflict("war", "g1");
            // pick a day that is guaranteed to be within the next 8 days
            DayOfWeek targetDay = DateTime.Now.AddDays(2).DayOfWeek;
            var range = new SelectedWarRange(targetDay, targetDay, TimeSpan.FromHours(18), TimeSpan.FromHours(2), "a");
            conflict.WarRanges.Add(range);

            conflict.CalculateNextBattleDate();

            Assert.NotEqual(DateTime.UnixEpoch, conflict.NextBattleDateStart);
            Assert.Equal(targetDay, conflict.NextBattleDateStart.DayOfWeek);
        }

        [Fact]
        public void CalculateNextBattleDate_TwoRanges_PicksEarlier()
        {
            var conflict = new Conflict("war", "g1");
            DayOfWeek earlier = DateTime.Now.AddDays(1).DayOfWeek;
            DayOfWeek later   = DateTime.Now.AddDays(3).DayOfWeek;

            var rangeEarly = new SelectedWarRange(earlier, earlier, TimeSpan.FromHours(20), TimeSpan.FromHours(1), "a");
            var rangeLate  = new SelectedWarRange(later,  later,  TimeSpan.FromHours(20), TimeSpan.FromHours(1), "b");
            conflict.WarRanges.Add(rangeLate);
            conflict.WarRanges.Add(rangeEarly);

            conflict.CalculateNextBattleDate();

            Assert.Equal(earlier, conflict.NextBattleDateStart.DayOfWeek);
        }

        [Fact]
        public void CalculateNextBattleDate_EndDateIsStartPlusDuration()
        {
            var conflict = new Conflict("war", "g1");
            DayOfWeek targetDay = DateTime.Now.AddDays(2).DayOfWeek;
            var duration = TimeSpan.FromHours(3);
            var range = new SelectedWarRange(targetDay, targetDay, TimeSpan.FromHours(18), duration, "a");
            conflict.WarRanges.Add(range);

            conflict.CalculateNextBattleDate();

            Assert.Equal(conflict.NextBattleDateStart + duration, conflict.NextBattleDateEnd);
        }

        // =====================================================================
        // ConflictHandler — letter management (no dataStorage dependency)
        // =====================================================================

        private ConflictLetter MakeLetter(IConflictParty from, IConflictParty to,
                                          LetterPurpose purpose, string guid = "test-guid")
            => new ConflictLetter(from, to, purpose, long.MaxValue,
                                  new Thread(() => { }), new Thread(() => { }), guid);

        [Fact]
        public void AddConflictLetter_NewLetter_ReturnsTrue()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);

            Assert.True(ConflictHandler.addConflictLetter(letter));
        }

        [Fact]
        public void AddConflictLetter_Duplicate_ReturnsFalse()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            ConflictHandler.addConflictLetter(letter);

            // same From/To/Purpose → duplicate
            var duplicate = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "other-guid");
            Assert.False(ConflictHandler.addConflictLetter(duplicate));
        }

        [Fact]
        public void RemoveConflictLetter_ExistingLetter_ReturnsTrue()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            ConflictHandler.addConflictLetter(letter);

            Assert.True(ConflictHandler.removeConflictLetter(letter));
        }

        [Fact]
        public void RemoveConflictLetter_NonExistent_ReturnsFalse()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);

            Assert.False(ConflictHandler.removeConflictLetter(letter));
        }

        [Fact]
        public void RemoveConflictLetter_ByParties_Bidirectional()
        {
            // add A→B, remove by specifying B,A (reverse order)
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            ConflictHandler.addConflictLetter(letter);

            bool removed = ConflictHandler.removeConflictLetter(_partyB.Object, _partyA.Object, LetterPurpose.START_CONFLICT);

            Assert.True(removed);
        }

        [Fact]
        public void TryGetConflictLetter_ByParties_FindsForward()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            ConflictHandler.addConflictLetter(letter);

            bool found = ConflictHandler.TryGetConflictLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, out var result);

            Assert.True(found);
            Assert.Same(letter, result);
        }

        [Fact]
        public void TryGetConflictLetter_ByParties_FindsReverse()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            ConflictHandler.addConflictLetter(letter);

            bool found = ConflictHandler.TryGetConflictLetter(_partyB.Object, _partyA.Object, LetterPurpose.START_CONFLICT, out var result);

            Assert.True(found);
            Assert.Same(letter, result);
        }

        [Fact]
        public void TryGetConflictLetter_ByParties_WrongPurpose_NotFound()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            ConflictHandler.addConflictLetter(letter);

            bool found = ConflictHandler.TryGetConflictLetter(_partyA.Object, _partyB.Object, LetterPurpose.END_CONFLICT, out _);

            Assert.False(found);
        }

        [Fact]
        public void TryGetConflictLetter_ByGuid_FindsExisting()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "my-guid");
            ConflictHandler.addConflictLetter(letter);

            bool found = ConflictHandler.TryGetConflictLetter("my-guid", out var result);

            Assert.True(found);
            Assert.Same(letter, result);
        }

        [Fact]
        public void TryGetConflictLetter_ByGuid_NotFound_ReturnsFalse()
        {
            // Bug regression: line 181 of ConflictHandler returned `true` even when not found
            bool found = ConflictHandler.TryGetConflictLetter("nonexistent-guid", out var result);

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void GetAllLettersForParty_ReturnsOnlyRelevantLetters()
        {
            var partyC = new Mock<IConflictParty>();
            partyC.Setup(p => p.Guid).Returns("party-c-guid");
            partyC.Setup(p => p.GetHashCode()).Returns(3);

            var letterAB = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "ab");
            var letterBC = MakeLetter(_partyB.Object, partyC.Object,  LetterPurpose.END_CONFLICT,   "bc");
            ConflictHandler.addConflictLetter(letterAB);
            ConflictHandler.addConflictLetter(letterBC);

            var forA = ConflictHandler.GetAllLettersForParty(_partyA.Object);
            var forB = ConflictHandler.GetAllLettersForParty(_partyB.Object);

            Assert.Single(forA);
            Assert.Equal(2, forB.Count);
        }

        [Fact]
        public void GetSentLetters_ReturnsOnlySentByParty()
        {
            var letterAB = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "ab");
            var letterBA = MakeLetter(_partyB.Object, _partyA.Object, LetterPurpose.END_CONFLICT,   "ba");
            ConflictHandler.addConflictLetter(letterAB);
            ConflictHandler.addConflictLetter(letterBA);

            var sent = ConflictHandler.getSentLettersForParty(_partyA.Object);

            Assert.Single(sent);
            Assert.Same(letterAB, sent[0]);
        }

        [Fact]
        public void GetReceivedLetters_ReturnsOnlyReceivedByParty()
        {
            var letterAB = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "ab");
            var letterBA = MakeLetter(_partyB.Object, _partyA.Object, LetterPurpose.END_CONFLICT,   "ba");
            ConflictHandler.addConflictLetter(letterAB);
            ConflictHandler.addConflictLetter(letterBA);

            var received = ConflictHandler.getReceivedLettersForParty(_partyB.Object);

            Assert.Single(received);
            Assert.Same(letterAB, received[0]);
        }

        [Fact]
        public void GuidIsFree_EmptyList_ReturnsTrue()
        {
            Assert.True(ConflictHandler.GuidIsFree(Guid.NewGuid()));
        }

        [Fact]
        public void GuidIsFree_GuidPresent_ReturnsFalse()
        {
            var guid = Guid.NewGuid();
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, guid.ToString());
            ConflictHandler.addConflictLetter(letter);

            Assert.False(ConflictHandler.GuidIsFree(guid));
        }

        // =====================================================================
        // ConflictHandler — conflict registry (uses dataStorage.conflicts)
        // =====================================================================

        private Conflict MakeConflict(IConflictParty first, IConflictParty second, string guid = "c-guid")
        {
            return new Conflict("war", guid) { First = first, Second = second };
        }

        [Fact]
        public void ConflictAlreadyExist_ConflictPresent_ReturnsTrue()
        {
            var conflict = MakeConflict(_partyA.Object, _partyB.Object);
            _storageMock.Object.conflicts.Add(conflict);

            Assert.True(ConflictHandler.conflictAlreadyExist(_partyA.Object, _partyB.Object));
        }

        [Fact]
        public void ConflictAlreadyExist_ReverseOrder_ReturnsTrue()
        {
            var conflict = MakeConflict(_partyA.Object, _partyB.Object);
            _storageMock.Object.conflicts.Add(conflict);

            Assert.True(ConflictHandler.conflictAlreadyExist(_partyB.Object, _partyA.Object));
        }

        [Fact]
        public void ConflictAlreadyExist_NoConflict_ReturnsFalse()
        {
            Assert.False(ConflictHandler.conflictAlreadyExist(_partyA.Object, _partyB.Object));
        }

        [Fact]
        public void TryGetConflictWithSides_Found_ReturnsTrueAndConflict()
        {
            var conflict = MakeConflict(_partyA.Object, _partyB.Object);
            _storageMock.Object.conflicts.Add(conflict);

            bool found = ConflictHandler.TryGetConflictWithSides(_partyA.Object, _partyB.Object, out var result);

            Assert.True(found);
            Assert.Same(conflict, result);
        }

        [Fact]
        public void TryGetConflictWithSides_NotFound_ReturnsFalse()
        {
            bool found = ConflictHandler.TryGetConflictWithSides(_partyA.Object, _partyB.Object, out var result);

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void TryGetConflictByGuid_Found_ReturnsTrueAndConflict()
        {
            var conflict = MakeConflict(_partyA.Object, _partyB.Object, "unique-guid");
            _storageMock.Object.conflicts.Add(conflict);

            bool found = ConflictHandler.TryGetConflictByGuid("unique-guid", out var result);

            Assert.True(found);
            Assert.Same(conflict, result);
        }

        [Fact]
        public void TryGetConflictByGuid_NotFound_ReturnsFalse()
        {
            bool found = ConflictHandler.TryGetConflictByGuid("ghost-guid", out var result);

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void GetAllConflictsForParty_ReturnsOnlyRelated()
        {
            var partyC = new Mock<IConflictParty>();
            partyC.Setup(p => p.Guid).Returns("party-c");

            var conflictAB = MakeConflict(_partyA.Object, _partyB.Object, "ab");
            var conflictBC = MakeConflict(_partyB.Object, partyC.Object,  "bc");
            _storageMock.Object.conflicts.Add(conflictAB);
            _storageMock.Object.conflicts.Add(conflictBC);

            var forA = ConflictHandler.GetAllConflictsForParty(_partyA.Object);
            var forB = ConflictHandler.GetAllConflictsForParty(_partyB.Object);

            Assert.Single(forA);
            Assert.Equal(2, forB.Count);
        }

        // =====================================================================
        // ConflictHandler — getReceivedLettersForPartyWithPurpose
        // =====================================================================

        [Fact]
        public void GetReceivedLettersWithPurpose_FiltersByPurpose()
        {
            var startLetter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "s");
            var endLetter   = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.END_CONFLICT,   "e");
            ConflictHandler.addConflictLetter(startLetter);
            ConflictHandler.addConflictLetter(endLetter);

            var received = ConflictHandler.getReceivedLettersForPartyWithPurpose(_partyB.Object, LetterPurpose.START_CONFLICT);

            Assert.Single(received);
            Assert.Same(startLetter, received[0]);
        }

        [Fact]
        public void GetReceivedLettersWithPurpose_NoMatch_ReturnsEmpty()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "s");
            ConflictHandler.addConflictLetter(letter);

            var received = ConflictHandler.getReceivedLettersForPartyWithPurpose(_partyB.Object, LetterPurpose.END_CONFLICT);

            Assert.Empty(received);
        }

        // =====================================================================
        // ConflictLetter — Equals / GetHashCode
        // =====================================================================

        [Fact]
        public void ConflictLetter_Equals_SameObject_ReturnsTrue()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            Assert.True(letter.Equals(letter));
        }

        [Fact]
        public void ConflictLetter_Equals_Null_ReturnsFalse()
        {
            var letter = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT);
            Assert.False(letter.Equals(null));
        }

        [Fact]
        public void ConflictLetter_Equals_Symmetric_AB_EqualsBA()
        {
            // A→B and B→A with same purpose must be equal (duplicate war declaration)
            var ab = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "ab");
            var ba = MakeLetter(_partyB.Object, _partyA.Object, LetterPurpose.START_CONFLICT, "ba");

            Assert.True(ab.Equals(ba));
            Assert.True(ba.Equals(ab));
        }

        [Fact]
        public void ConflictLetter_Equals_DifferentPurpose_ReturnsFalse()
        {
            var start = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "s");
            var end   = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.END_CONFLICT,   "e");

            Assert.False(start.Equals(end));
        }

        /// <summary>
        /// Bug: GetHashCode is asymmetric — hash(A→B) != hash(B→A) even though Equals(A→B, B→A) == true.
        /// This breaks HashSet semantics: two "equal" letters get different buckets so the set
        /// accepts both, violating the uniqueness contract.
        /// Fix: make GetHashCode order-independent, e.g. XOR the two party hashes instead of chaining.
        /// </summary>
        [Fact]
        public void ConflictLetter_GetHashCode_MustBeSymmetric()
        {
            var ab = MakeLetter(_partyA.Object, _partyB.Object, LetterPurpose.START_CONFLICT, "ab");
            var ba = MakeLetter(_partyB.Object, _partyA.Object, LetterPurpose.START_CONFLICT, "ba");

            Assert.Equal(ab.GetHashCode(), ba.GetHashCode());
        }

        // =====================================================================
        // SelectedWarRange — EndTime
        // =====================================================================

        [Fact]
        public void SelectedWarRange_EndTime_IsStartPlusDuration()
        {
            var range = new SelectedWarRange(
                DayOfWeek.Monday, DayOfWeek.Monday,
                TimeSpan.FromHours(18), TimeSpan.FromHours(2), "a");

            Assert.Equal(TimeSpan.FromHours(20), range.EndTime);
        }

        // =====================================================================
        // Conflict — GetNextDateForRange: only works on first battle (logic bug)
        // =====================================================================

        [Fact]
        public void GetNextDateForRange_FirstBattle_FindsMatchingDay()
        {
            var conflict = new Conflict("war", "g1");
            // LastBattleDateStart == UnixEpoch → first battle path
            DayOfWeek target = DateTime.Now.AddDays(1).DayOfWeek;
            var range = new SelectedWarRange(target, target, TimeSpan.FromHours(20), TimeSpan.FromHours(1), "a");

            bool found = conflict.GetNextDateForRange(range, out DateTime dt);

            Assert.True(found);
            Assert.Equal(target, dt.DayOfWeek);
        }

        /// <summary>
        /// Bug: after the first battle (LastBattleDateStart != UnixEpoch) GetNextDateForRange
        /// always returns false because the scheduling loop is guarded by
        /// `if (LastBattleDateStart == DateTime.UnixEpoch)` with no else branch.
        /// Fix: the loop should also run when cooldown has passed.
        /// </summary>
        [Fact]
        public void GetNextDateForRange_AfterFirstBattle_CooldownPassed_ShouldFindNextDate()
        {
            var conflict = new Conflict("war", "g1") { MinimumDaysBetweenBattles = 1 };
            conflict.LastBattleDateStart = DateTime.Now.AddDays(-2); // cooldown passed
            conflict.LastBattleDateEnd   = DateTime.Now.AddDays(-2).AddHours(2);

            DayOfWeek target = DateTime.Now.AddDays(1).DayOfWeek;
            var range = new SelectedWarRange(target, target, TimeSpan.FromHours(20), TimeSpan.FromHours(1), "a");

            bool found = conflict.GetNextDateForRange(range, out _);

            Assert.True(found);
        }

        [Fact]
        public void CalculateNextBattleDate_AfterFirstBattle_CooldownPassed_ShouldSchedule()
        {
            var conflict = new Conflict("war", "g1") { MinimumDaysBetweenBattles = 1 };
            conflict.LastBattleDateStart = DateTime.Now.AddDays(-2);
            conflict.LastBattleDateEnd   = DateTime.Now.AddDays(-2).AddHours(2);

            DayOfWeek target = DateTime.Now.AddDays(1).DayOfWeek;
            conflict.WarRanges.Add(new SelectedWarRange(target, target, TimeSpan.FromHours(20), TimeSpan.FromHours(1), "a"));

            conflict.CalculateNextBattleDate();

            Assert.NotEqual(DateTime.UnixEpoch, conflict.NextBattleDateStart);
        }
    }
}
