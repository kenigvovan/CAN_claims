using claims.src.auxialiry;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Demands money or a plot from another party under threat of war. The target may be a city or
    /// an alliance; which command carries the demand depends on whether we are in an alliance
    /// ourselves, since the two have different subcommand paths.
    /// </summary>
    public sealed class UltimatumDialog : CANGuiDialogPanel
    {
        private const string TargetKey = "ultimatum-target";
        private const string AmountKey = "ultimatum-amount";

        private const string DemandMoney = "money";
        private const string DemandPlot = "plot";

        protected override string TitleLangKey => "claims:gui_ultimatum_title";
        protected override double Width => 260;

        protected override void BuildContent(DialogLayout l)
        {
            if (string.IsNullOrEmpty(Args.First)) Args.First = "city:";
            if (string.IsNullOrEmpty(Args.Second)) Args.Second = DemandMoney;
            if (string.IsNullOrEmpty(Args.SelectedSecond)) Args.SelectedSecond = "100";

            Text(l, Lang.Get("claims:gui_ultimatum_target"));

            string[] prefixes = { "city:", "alliance:" };
            string[] partyLabels =
            {
                Lang.Get("claims:conflict_target_city"),
                Lang.Get("claims:conflict_target_alliance")
            };
            DropDown(l, prefixes, partyLabels, picked => Args.First = picked);

            TextInput(l, TargetKey, value => Args.Text = value);

            l.Compo.AddStaticText(Lang.Get("claims:gui_ultimatum_demand_title"), CairoFont.WhiteDetailText(), l.NextRow());

            string[] demands = { DemandMoney, DemandPlot };
            string[] demandLabels =
            {
                Lang.Get("claims:gui_ultimatum_demand_money"),
                Lang.Get("claims:gui_ultimatum_demand_plot")
            };
            DropDown(l, demands, demandLabels, picked => Args.Second = picked);

            if (Args.Second == DemandMoney)
            {
                TextInput(l, AmountKey, value => Args.SelectedSecond = value, Args.SelectedSecond);
            }
            else
            {
                // The server takes the plot from the sender's own position, so it cannot be picked
                // from a list - the player has to be standing on it.
                l.Compo.AddStaticText(Lang.Get("claims:gui_ultimatum_plot_hint"), CairoFont.WhiteDetailText(), l.NextRow(15, null, 45));
            }

            l.Compo.AddStaticText(Lang.Get("claims:gui_ultimatum_deadline_hint",
                    StringFunctions.FormatDuration((long)claims.config.WAR_ULTIMATUM_EXPIRE_HOURS * 3600)),
                CairoFont.WhiteDetailText(), l.NextRow(15, null, 45));

            l.Compo.AddStaticText(Lang.Get("claims:gui_ultimatum_refusal_hint"),
                CairoFont.WhiteDetailText(), l.NextRow(5, null, 45));

            ConfirmDeclineRow(l, () =>
            {
                if (string.IsNullOrEmpty(Args.Text)) return true;

                Send(BuildCommand());
                Close();
                return true;
            });
        }

        private string BuildCommand()
        {
            string target = Args.First + Args.Text;

            // Alliance members go through the alliance command, lone cities through the city one -
            // the handler resolves the party from the caller, but the subcommand paths differ.
            bool hasAlliance = Player.AllianceInfo != null;
            string command = hasAlliance
                ? $"/alliance conflict ultimatum offer {target} "
                : $"/city war ultimatum offer {target} ";

            if (Args.Second == DemandPlot) return command + DemandPlot;

            int amount = 1;
            int.TryParse(Args.SelectedSecond, out amount);
            if (amount < 1) amount = 1;
            return command + "money " + amount;
        }
    }
}
