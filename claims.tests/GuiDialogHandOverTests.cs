using claims.src.gui.playerGui;

namespace claims.tests
{
    /// <summary>
    /// Switching the secondary window between forms. The select step of "kick from plots group" and
    /// "delete plots group" opened its confirm step with the buffers cleared, so the confirm built
    /// "/c plotsgroup kick group " and the server answered "Argument 2 (playerName) is missing".
    /// </summary>
    public class GuiDialogHandOverTests
    {
        [Fact]
        public void OpenDialog_ClearsWhatThePreviousFormTyped()
        {
            var state = new ClaimsGuiState();
            state.DialogArgs.Text = "Member";
            state.DialogArgs.First = "a";
            state.DialogArgs.Second = "b";

            state.OpenDialog(EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_CONFIRM);

            Assert.Equal(EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_CONFIRM, state.Dialog);
            Assert.Equal("", state.DialogArgs.Text);
            Assert.Equal("", state.DialogArgs.First);
            Assert.Equal("", state.DialogArgs.Second);
        }

        [Fact]
        public void OpenDialog_FillRunsAfterClearing_SoThePickReachesTheConfirmStep()
        {
            var state = new ClaimsGuiState();
            state.DialogArgs.Text = "Member";
            string picked = state.DialogArgs.Text;

            state.OpenDialog(EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_CONFIRM, a => a.Text = picked);

            Assert.Equal("Member", state.DialogArgs.Text);
        }

        [Fact]
        public void OpenDialog_KeepsWhatThePageBehindPointsAt()
        {
            // The plots group page puts the group's guid in Selected; the confirm step names the
            // group through it, so it has to survive the switch.
            var state = new ClaimsGuiState();
            state.DialogArgs.Selected = "group-guid";
            state.DialogArgs.SelectedSecond = "second";

            state.OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_CONFIRM);

            Assert.Equal("group-guid", state.DialogArgs.Selected);
            Assert.Equal("second", state.DialogArgs.SelectedSecond);
        }

        [Fact]
        public void CloseDialog_DropsTypedTextButKeepsSelection()
        {
            var state = new ClaimsGuiState();
            state.OpenDialog(EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_CONFIRM, a => a.Text = "farm");
            state.DialogArgs.Selected = "group-guid";

            state.CloseDialog();

            Assert.Equal(EnumUpperWindowSelectedState.NONE, state.Dialog);
            Assert.Equal("", state.DialogArgs.Text);
            Assert.Equal("group-guid", state.DialogArgs.Selected);
        }
    }
}
