using Vintagestory.API.Client;
using System;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>Question plus a confirm/decline pair. Thirteen branches were exactly this.</summary>
    public sealed class YesNoDialog : CANGuiDialogPanel
    {
        private readonly string questionLangKey;
        private readonly Func<DialogArgs, string> commandBuilder;
        private readonly string confirmLangKey;
        private readonly string declineLangKey;
        private readonly Action onConfirmed;
        private readonly Func<DialogArgs, object[]> textArgs;

        public YesNoDialog(string questionLangKey, string command,
                           string confirmLangKey = "claims:gui-confirm-button",
                           string declineLangKey = "claims:gui-decline-button",
                           Action onConfirmed = null,
                           Func<DialogArgs, object[]> textArgs = null)
            : this(questionLangKey, _ => command, confirmLangKey, declineLangKey, onConfirmed, textArgs)
        {
        }

        /// <param name="textArgs">
        /// Placeholder values for the question. Explicit because the old prompts each fed a different
        /// pair of buffers in a different order.
        /// </param>
        public YesNoDialog(string questionLangKey, Func<DialogArgs, string> commandBuilder,
                           string confirmLangKey = "claims:gui-confirm-button",
                           string declineLangKey = "claims:gui-decline-button",
                           Action onConfirmed = null,
                           Func<DialogArgs, object[]> textArgs = null)
        {
            this.questionLangKey = questionLangKey;
            this.commandBuilder = commandBuilder;
            this.confirmLangKey = confirmLangKey;
            this.declineLangKey = declineLangKey;
            this.onConfirmed = onConfirmed;
            this.textArgs = textArgs;
        }

        protected override void BuildContent(DialogLayout l)
        {
            Text(l, textArgs == null ? Lang.Get(questionLangKey) : Lang.Get(questionLangKey, textArgs(Args)));

            ConfirmDeclineRow(l, () =>
            {
                Send(commandBuilder(Args));
                onConfirmed?.Invoke();
                Close();
                return true;
            }, confirmLangKey, declineLangKey);
        }
    }
}
