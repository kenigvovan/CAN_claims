using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Offer to end a war, with the terms the winner may attach. The plain yes/no confirm this
    /// replaces always sent the alliance command without a term, so an independent city could not
    /// offer peace from the GUI at all and the terms were command-only.
    /// </summary>
    public sealed class PeaceOfferDialog : CANGuiDialogPanel
    {
        private const string AmountKey = "peace-reparations-amount";

        private const string TermNone = "none";
        private const string TermReparations = "reparations";
        private const string TermVassalage = "vassalage";
        private const string TermCession = "cession";

        protected override string TitleLangKey => "claims:gui_peace_terms_title";
        protected override double Width => 260;

        protected override void BuildContent(DialogLayout l)
        {
            if (string.IsNullOrEmpty(Args.First)) Args.First = TermNone;
            if (string.IsNullOrEmpty(Args.Text)) Args.Text = "100";

            // Alliance members go through the alliance command, lone cities through the city one -
            // the server rejects the wrong variant. Only the city command knows plot cession.
            bool hasAlliance = Player.AllianceInfo != null;

            Text(l, Lang.Get("claims:gui_peace_offer_to", Args.SelectedSecond));

            var terms = new List<string> { TermNone, TermReparations, TermVassalage };
            var labels = new List<string>
            {
                Lang.Get("claims:gui_peace_term_none"),
                Lang.Get("claims:gui_peace_term_reparations"),
                Lang.Get("claims:gui_peace_term_vassalage")
            };
            if (!hasAlliance)
            {
                terms.Add(TermCession);
                labels.Add(Lang.Get("claims:gui_peace_term_cession"));
            }

            // Picking a term rebuilds the window: the amount field only belongs to reparations.
            DropDown(l, terms.ToArray(), labels.ToArray(), picked =>
            {
                Args.First = picked;
                Gui.BuildUpperWindow();
            }, Args.First);

            if (Args.First == TermReparations)
            {
                Text(l, Lang.Get("claims:gui_peace_reparations_amount"));
                TextInput(l, AmountKey, value => Args.Text = value, Args.Text);
            }
            else if (Args.First == TermCession)
            {
                l.Compo.AddStaticText(Lang.Get("claims:gui_peace_cession_hint"),
                    CairoFont.WhiteDetailText(), l.NextRow(15, null, 45));
            }

            ConfirmDeclineRow(l, () =>
            {
                Send(BuildCommand(hasAlliance));
                Close();
                return true;
            });
        }

        private string BuildCommand(bool hasAlliance)
        {
            string command = hasAlliance
                ? "/alliance conflict offerstop " + Args.SelectedSecond + " " + Args.First
                : "/city war offerpeace " + Args.SelectedSecond + " " + Args.First;

            if (Args.First != TermReparations) return command;

            int amount;
            if (!int.TryParse(Args.Text, out amount) || amount < 0) amount = 0;
            return command + " " + amount;
        }
    }
}
