using Vintagestory.API.Client;
using System;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Prompt, dropdown, one button. Seven branches were this - kick a citizen, remove a friend,
    /// pick a plot type, remove a criminal, pick a plots group member.
    /// </summary>
    public sealed class DropDownDialog : CANGuiDialogPanel
    {
        private readonly string promptLangKey;
        private readonly Func<string[]> optionsProvider;
        private readonly Func<string[]> displayNamesProvider;
        private readonly string commandPrefix;
        private readonly string buttonLangKey;
        private readonly Func<DialogArgs, string> commandBuilder;

        public DropDownDialog(string promptLangKey, Func<string[]> optionsProvider,
                              string commandPrefix, string buttonLangKey,
                              Func<string[]> displayNamesProvider = null,
                              Func<DialogArgs, string> commandBuilder = null)
        {
            this.promptLangKey = promptLangKey;
            this.optionsProvider = optionsProvider;
            this.commandPrefix = commandPrefix;
            this.buttonLangKey = buttonLangKey;
            this.displayNamesProvider = displayNamesProvider;
            this.commandBuilder = commandBuilder;
        }

        protected override void BuildContent(DialogLayout l)
        {
            Text(l, Lang.Get(promptLangKey));

            string[] options = optionsProvider() ?? new string[0];
            string[] names = displayNamesProvider != null ? displayNamesProvider() : options;

            // An empty dropdown crashes the client: opening its list menu composes a zero-sized
            // Cairo surface, and the GL texture upload fails with "invalid texture format". So an
            // empty choice is a message, not a control.
            if (options.Length == 0)
            {
                TextRow(l, Lang.Get("claims:gui-dropdown-nothing-to-pick"));
                return;
            }

            // Nothing is preselected on purpose. These dialogs kick citizens and delete groups, so a
            // mis-click on the button must not act on whichever entry happens to be first; the box
            // starts blank and the button does nothing until something is actually picked.
            Args.Text = "";
            DropDown(l, options, names, picked => Args.Text = picked);

            Button(l, Lang.Get(buttonLangKey), () =>
            {
                if (Args.Text.Length == 0) return true;
                Send(commandBuilder != null ? commandBuilder(Args) : commandPrefix + Args.Text);
                Close();
                return true;
            });
        }
    }
}
