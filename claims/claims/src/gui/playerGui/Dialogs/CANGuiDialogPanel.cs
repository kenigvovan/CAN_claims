using System;
using claims.src.gui.playerGui.structures;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// One form of the secondary window.
    ///
    /// Build() is not virtual: the frame, the title bar and the composing are the base's job, so a
    /// subclass only says what goes inside. The ImGui side got this wrong the other way round - its
    /// dialog base carries no helpers at all, so 27 of its 28 dialogs repeat the same window setup.
    /// </summary>
    public abstract class CANGuiDialogPanel
    {
        protected CANClaimsGui Gui { get; private set; }
        protected ClaimsGuiState State => Gui.State;
        protected DialogArgs Args => Gui.State.DialogArgs;
        protected static ClientPlayerInfo Player => claims.clientDataStorage.clientPlayerInfo;

        internal void Build(CANClaimsGui gui)
        {
            Gui = gui;
            var layout = DialogFrame.Create(gui, Width);
            gui.Composers[DialogFrame.ComposerKey] = layout.Compo;

            BuildContent(layout);

            layout.Compo
                  .AddDialogTitleBar(TitleLangKey == null ? "" : Lang.Get(TitleLangKey), () => Gui.CloseDialog())
                  .Compose();
        }

        protected abstract void BuildContent(DialogLayout l);

        protected virtual string TitleLangKey => null;
        protected virtual double Width => 235;

        // ---- building blocks ----

        /// <summary>Writes into the row the cursor is on, without advancing it.</summary>
        protected static void Text(DialogLayout l, string text)
        {
            l.Compo.AddStaticText(text, CairoFont.WhiteDetailText(), l.Row);
        }

        /// <summary>
        /// Writes on a row of its own. Text() stays on the current row, so a second line drawn with
        /// it lands on top of the first - use this whenever a dialog says more than one thing.
        /// </summary>
        protected static void TextRow(DialogLayout l, string text)
        {
            l.Compo.AddStaticText(text, CairoFont.WhiteDetailText(), l.NextRow());
        }

        protected static void TextInput(DialogLayout l, string key, Action<string> onChange, string initial = null)
        {
            var bounds = l.NextRow();
            l.Compo.AddTextInput(bounds, onChange, null, key);
            if (initial != null) l.Compo.GetTextInput(key).SetValue(initial);
        }

        /// <summary>
        /// <paramref name="selectedValue"/> preselects an entry, which matters for dialogs that
        /// rebuild themselves when the pick changes - otherwise the box would come back empty.
        /// </summary>
        protected static void DropDown(DialogLayout l, string[] values, string[] names, Action<string> onPicked,
                                       string selectedValue = null)
        {
            var bounds = l.NextRow();
            int selectedIndex = selectedValue == null ? -1 : Array.IndexOf(values, selectedValue);
            l.Compo.AddDropDown(values, names, selectedIndex, (code, selected) => onPicked(code), bounds);
        }

        protected static void Button(DialogLayout l, string label, Func<bool> onClick, string key = null)
        {
            var bounds = l.NextRow();
            l.Compo.AddButton(label, new ActionConsumable(onClick), bounds, EnumButtonStyle.Normal, key);
        }

        /// <summary>
        /// Confirm on the left, decline on the right. Thirteen branches carried a verbatim copy of
        /// this, three of them with a hardcoded English "No" instead of the translated label.
        /// </summary>
        protected void ConfirmDeclineRow(DialogLayout l, Func<bool> onConfirm,
                                         string confirmLangKey = "claims:gui-confirm-button",
                                         string declineLangKey = "claims:gui-decline-button")
        {
            ElementBounds yes = l.SplitRow(out ElementBounds no);

            l.Compo.AddButton(Lang.Get(confirmLangKey), new ActionConsumable(onConfirm), yes, EnumButtonStyle.Normal);
            l.Compo.AddButton(Lang.Get(declineLangKey), new ActionConsumable(() =>
            {
                Close();
                return true;
            }), no, EnumButtonStyle.Normal);
        }

        protected void Close() => Gui.CloseDialog();

        protected static void Send(string command) => ClientChat.Send(command);
    }
}
