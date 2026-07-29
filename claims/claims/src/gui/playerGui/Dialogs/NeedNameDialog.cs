using Vintagestory.API.Client;
using System;
using System.Globalization;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>What the field accepts. Prices, taxes and amounts are not free text.</summary>
    public enum EnumDialogInput
    {
        Text, Integer, Decimal
    }

    /// <summary>
    /// Prompt, text field, one button. Roughly twenty branches of the old chain were this, differing
    /// only in the prompt, the command prefix and the button label.
    /// </summary>
    public sealed class NeedNameDialog : CANGuiDialogPanel
    {
        private const string InputKey = "dialog-text-input";

        private readonly string promptLangKey;
        private readonly string commandPrefix;
        private readonly string buttonLangKey;
        private readonly Func<DialogArgs, string> commandBuilder;
        private readonly Action<string> onSubmitted;
        private readonly bool closeAfter;
        private readonly EnumDialogInput inputKind;

        /// <param name="commandBuilder">
        /// Overrides commandPrefix + typed text when the command needs more than the one value.
        /// </param>
        /// <param name="onSubmitted">
        /// Optional local update applied right after sending, so the dialog does not have to wait for
        /// the server round trip. The ImGui version instead branched on what the command string
        /// started with, which is why this is a callback.
        /// </param>
        /// <param name="inputKind">
        /// Rejects anything that is not a number for the price / tax / amount prompts, which the
        /// ImGui side handled with dedicated int and double dialogs.
        /// </param>
        public NeedNameDialog(string promptLangKey, string commandPrefix, string buttonLangKey,
                              Func<DialogArgs, string> commandBuilder = null,
                              Action<string> onSubmitted = null,
                              bool closeAfter = true,
                              EnumDialogInput inputKind = EnumDialogInput.Text)
        {
            this.promptLangKey = promptLangKey;
            this.commandPrefix = commandPrefix;
            this.buttonLangKey = buttonLangKey;
            this.commandBuilder = commandBuilder;
            this.onSubmitted = onSubmitted;
            this.closeAfter = closeAfter;
            this.inputKind = inputKind;
        }

        protected override void BuildContent(DialogLayout l)
        {
            Text(l, Lang.Get(promptLangKey));

            TextInput(l, InputKey, value => Args.Text = value);

            Button(l, Lang.Get(buttonLangKey), () =>
            {
                string typed = Args.Text;
                if (!IsAccepted(typed))
                {
                    claims.capi.ShowChatMessage(Lang.Get("claims:gui-input-not-a-number"));
                    return true;
                }

                // The command parser only reads a dot as the decimal separator.
                if (inputKind == EnumDialogInput.Decimal)
                {
                    typed = typed.Replace(',', '.');
                    Args.Text = typed;
                }

                Send(commandBuilder != null ? commandBuilder(Args) : commandPrefix + typed);
                onSubmitted?.Invoke(typed);

                l.Compo.GetTextInput(InputKey).SetValue("");
                Args.Text = "";
                if (closeAfter) Close();
                return true;
            });
        }

        private bool IsAccepted(string typed)
        {
            switch (inputKind)
            {
                case EnumDialogInput.Integer:
                    return int.TryParse(typed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                case EnumDialogInput.Decimal:
                    // Both separators: the field is typed into by hand and either is natural
                    // depending on the player's keyboard layout.
                    return double.TryParse(typed?.Replace(',', '.'), NumberStyles.Float,
                                           CultureInfo.InvariantCulture, out _);
                default:
                    return true;
            }
        }
    }
}
