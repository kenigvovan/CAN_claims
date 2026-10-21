using claims.src.gui.playerGui.Dialogs;

namespace claims.tests
{
    /// <summary>
    /// What a form keeps across a rebuild and what it agrees to send. Every server packet rebuilds
    /// the open form, and a value missing from a command reached the server as a double space.
    /// </summary>
    public class GuiDialogInputTests
    {
        private static readonly string[] Members = { "Alice", "Bob" };

        /*=====================================================================*/
        /* KeptPick                                                            */
        /*=====================================================================*/

        [Fact]
        public void KeptPick_NothingPickedYet_StaysBlank()
        {
            // Kick and delete forms start blank on purpose: no entry is acted on by a stray click.
            Assert.Equal("", CANGuiDialogPanel.KeptPick(Members, ""));
            Assert.Equal("", CANGuiDialogPanel.KeptPick(Members, null!));
        }

        [Fact]
        public void KeptPick_PickStillOnOffer_SurvivesRebuild()
        {
            Assert.Equal("Bob", CANGuiDialogPanel.KeptPick(Members, "Bob"));
        }

        [Fact]
        public void KeptPick_PickedEntryLeftTheList_IsDropped()
        {
            // Bob left the group while the form was open: kicking him must not stay armed.
            Assert.Equal("", CANGuiDialogPanel.KeptPick(new[] { "Alice" }, "Bob"));
        }

        [Fact]
        public void KeptPick_LeftoverFromAnotherForm_IsDropped()
        {
            Assert.Equal("", CANGuiDialogPanel.KeptPick(Members, "250"));
        }

        [Fact]
        public void KeptPick_NoOptions_StaysBlank()
        {
            Assert.Equal("", CANGuiDialogPanel.KeptPick(new string[0], "Bob"));
            Assert.Equal("", CANGuiDialogPanel.KeptPick(null!, "Bob"));
        }

        [Fact]
        public void KeptPick_IsCaseSensitive_LikeTheServerLookup()
        {
            Assert.Equal("", CANGuiDialogPanel.KeptPick(Members, "bob"));
        }

        /*=====================================================================*/
        /* ClientCommandText.Join                                              */
        /*=====================================================================*/

        [Fact]
        public void Join_AllArgumentsPresent_SingleSpaced()
        {
            Assert.Equal("/c plotsgroup kick farm Bob", ClientCommandText.Join("/c plotsgroup kick", "farm", "Bob"));
        }

        [Fact]
        public void Join_NoArguments_IsTheCommandItself()
        {
            Assert.Equal("/alliance leave", ClientCommandText.Join("/alliance leave"));
        }

        [Theory]
        [InlineData(null, "Bob")]   // the group was deleted while its form was open
        [InlineData("farm", "")]    // nothing picked in the member list
        [InlineData("farm", null)]
        [InlineData("farm", "   ")]
        public void Join_AnyArgumentMissing_SendsNothing(string? group, string? player)
        {
            Assert.Null(ClientCommandText.Join("/c plotsgroup kick", group!, player!));
        }

        [Fact]
        public void Join_MissingFirstOfMany_SendsNothing()
        {
            Assert.Null(ClientCommandText.Join("/c plotsgroup set p", null!, "citizen", "build", "on"));
        }
    }
}
