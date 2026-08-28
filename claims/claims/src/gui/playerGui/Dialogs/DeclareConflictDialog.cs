using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Names a target that may be either a city or an alliance, and sends a command prefixed
    /// accordingly - "city:Name" or "alliance:Name". Used for declaring war and for offering a
    /// non-aggression pact.
    /// </summary>
    public sealed class DeclareConflictDialog : CANGuiDialogPanel
    {
        private const string InputKey = "conflict-target-name";

        private readonly string baseCommand;
        private readonly string promptLangKey;

        public DeclareConflictDialog(string baseCommand, string promptLangKey = "claims:name_of_target_to_send_conflict_letter")
        {
            this.baseCommand = baseCommand;
            this.promptLangKey = promptLangKey;
        }

        protected override void BuildContent(DialogLayout l)
        {
            Text(l, Lang.Get(promptLangKey));

            // A two-entry dropdown keeps the command prefixes as the stored values.
            string[] prefixes = { "city:", "alliance:" };
            string[] labels =
            {
                Lang.Get("claims:conflict_target_city"),
                Lang.Get("claims:conflict_target_alliance")
            };

            if (string.IsNullOrEmpty(Args.First)) Args.First = prefixes[0];
            DropDown(l, prefixes, labels, picked => Args.First = picked);

            TextInput(l, InputKey, value => Args.Text = value);

            Button(l, Lang.Get("claims:gui-confirm-button"), () =>
            {
                Send(baseCommand + Args.First + Args.Text);
                Close();
                return true;
            });
        }
    }
}
