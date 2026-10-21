using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Grants a rank to a citizen picked from a dropdown. Its own class because the heading names
    /// the rank and the command takes both the rank and the citizen.
    /// </summary>
    public sealed class RankAddDialog : CANGuiDialogPanel
    {
        protected override void BuildContent(DialogLayout l)
        {
            l.Row.fixedWidth += 40;
            Text(l, Lang.Get("claims:gui-add-rank-to-player", Args.First));

            string[] citizens = Player.CityInfo?.PlayersNames.ToArray() ?? new string[0];
            // An empty dropdown crashes the client - see DropDownDialog. Empty until the city info
            // has arrived.
            if (citizens.Length == 0)
            {
                TextRow(l, Lang.Get("claims:gui-dropdown-nothing-to-pick"));
                return;
            }
            Args.Text = KeptPick(citizens, Args.Text);
            DropDown(l, citizens, citizens, picked => Args.Text = picked, Preselect(Args.Text));

            ElementBounds add = l.SplitRow(out ElementBounds close);

            l.Compo.AddButton(Lang.Get("claims:gui-add-button"), new ActionConsumable(() =>
            {
                if (Args.Text.Length == 0) return true;
                Send(ClientCommandText.Join("/city rank add", Args.First, Args.Text));
                Close();
                return true;
            }), add, EnumButtonStyle.Normal);

            l.Compo.AddButton(Lang.Get("claims:gui-close-button"), new ActionConsumable(() =>
            {
                Close();
                return true;
            }), close, EnumButtonStyle.Normal);
        }
    }
}
