using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Confirmation the server asks for before a city is created. Opened from the packet handler,
    /// not from a page, and it carries the proposed city name in Text.
    /// </summary>
    public sealed class AgreeDialog : CANGuiDialogPanel
    {
        protected override void BuildContent(DialogLayout l)
        {
            Text(l, Lang.Get("claims:gui-agree-city-creation", Args.Text));

            Button(l, Lang.Get("claims:gui-agree-button"), () =>
            {
                Send("/agree");
                Close();
                return true;
            }, "create-new-city-button-enter-name");
        }
    }
}
