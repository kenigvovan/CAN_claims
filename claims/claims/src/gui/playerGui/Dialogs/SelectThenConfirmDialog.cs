using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Pick something from a dropdown, then move on to another dialog that confirms it. Used for
    /// kicking a plots group member and for deleting a plots group.
    /// </summary>
    public sealed class SelectThenConfirmDialog : CANGuiDialogPanel
    {
        private readonly string promptLangKey;
        private readonly Func<string[]> optionsProvider;
        private readonly string proceedLangKey;
        private readonly EnumUpperWindowSelectedState next;
        private readonly Func<object[]> textArgs;

        public SelectThenConfirmDialog(string promptLangKey, Func<string[]> optionsProvider,
                                       string proceedLangKey, EnumUpperWindowSelectedState next,
                                       Func<object[]> textArgs = null)
        {
            this.promptLangKey = promptLangKey;
            this.optionsProvider = optionsProvider;
            this.proceedLangKey = proceedLangKey;
            this.next = next;
            this.textArgs = textArgs;
        }

        protected override void BuildContent(DialogLayout l)
        {
            Text(l, textArgs == null ? Lang.Get(promptLangKey) : Lang.Get(promptLangKey, textArgs()));

            string[] options = optionsProvider() ?? new string[0];
            DropDown(l, options, options, picked => Args.Text = picked);

            ElementBounds proceed = l.SplitRow(out ElementBounds close);

            l.Compo.AddButton(Lang.Get(proceedLangKey), new ActionConsumable(() =>
            {
                Gui.OpenDialog(next);
                return true;
            }), proceed, EnumButtonStyle.Normal);

            l.Compo.AddButton(Lang.Get("claims:gui-close-button"), new ActionConsumable(() =>
            {
                Close();
                return true;
            }), close, EnumButtonStyle.Normal);
        }
    }
}
