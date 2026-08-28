using System;
using System.Collections.Generic;
using claims.src.gui.playerGui.Pages;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// Edits any config row whose value comes from a fixed set of words, rather than making the admin
    /// type it into the settings editor's value field. Works off the row's own
    /// <see cref="WarCfgRow.Options"/>, so a new setting of this shape needs no dialog of its own -
    /// only a Choice/MultiChoice entry in <see cref="WarConfigTable"/>.
    ///
    /// A Choice row sends the moment one is picked. A MultiChoice row toggles entries and sends the
    /// list on confirm, since half a selection is not worth applying.
    ///
    /// State lives in the dialog args that <see cref="CANClaimsGui.OpenDialog"/> does not clear:
    /// Selected is the config key, SelectedSecond the working value.
    /// </summary>
    public sealed class CfgChoiceDialog : CANGuiDialogPanel
    {
        protected override double Width => 240;

        protected override string TitleLangKey =>
            WarConfigTable.TryGetRow(Args.Selected, out var row) ? row.LabelLangKey : null;

        protected override void BuildContent(DialogLayout l)
        {
            if (!WarConfigTable.TryGetRow(Args.Selected, out WarCfgRow row) || row.Options == null)
            {
                TextRow(l, Lang.Get("claims:gui-warcfg-choice-missing"));
                ConfirmDeclineRow(l, () => { Close(); return true; });
                return;
            }

            bool multi = row.Kind == EnumCfgKind.MultiChoice;
            var picked = Parse(Args.SelectedSecond);

            TextRow(l, Lang.Get(multi ? "claims:gui-warcfg-choice-hint-multi" : "claims:gui-warcfg-choice-hint-one"));

            for (int i = 0; i < row.Options.Length; i++)
            {
                CfgOption option = row.Options[i];
                bool on = picked.Contains(option.Value);

                // The state is spelled out next to the label: a row of near-identical buttons gives
                // no other clue which ones are on.
                string label = Lang.Get(option.LabelLangKey);
                if (multi) label += "  -  " + Lang.Get(on ? "claims:gui-warcfg-choice-on" : "claims:gui-warcfg-choice-off");
                else if (on) label += "  <";

                Button(l, label, () =>
                {
                    if (!multi)
                    {
                        Send("/cadmin setcfg " + row.CfgKey + " " + option.Value);
                        Close();
                        return true;
                    }

                    var next = Parse(Args.SelectedSecond);
                    if (!next.Remove(option.Value)) next.Add(option.Value);
                    Args.SelectedSecond = Format(row, next);
                    // Rebuilds in place: OpenDialog would clear the buffers this dialog works from.
                    Gui.BuildUpperWindow();
                    return true;
                }, "cfg-choice-" + i);
            }

            if (!multi)
            {
                // Nothing to confirm - picking is the edit. The row is here only to back out.
                Button(l, Lang.Get("claims:gui-decline-button"), () => { Close(); return true; });
                return;
            }

            TextRow(l, Lang.Get("claims:gui-warcfg-choice-current",
                picked.Count == 0 ? row.EmptyValue : Format(row, picked)));

            ConfirmDeclineRow(l, () =>
            {
                var chosen = Parse(Args.SelectedSecond);
                Send("/cadmin setcfg " + row.CfgKey + " "
                     + (chosen.Count == 0 ? row.EmptyValue : Format(row, chosen)));
                Close();
                return true;
            });
        }

        /// <summary>
        /// The picked values in the row's own option order, not the order they were clicked - a
        /// weekday list reads wrong when it comes back as "sunday,saturday".
        /// </summary>
        private static string Format(WarCfgRow row, List<string> picked)
        {
            var parts = new List<string>();
            foreach (CfgOption option in row.Options)
            {
                if (picked.Contains(option.Value)) parts.Add(option.Value);
            }
            return string.Join(",", parts);
        }

        private static List<string> Parse(string raw)
        {
            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return values;

            foreach (string part in raw.Split(','))
            {
                string token = part.Trim().ToLowerInvariant();
                // "none"/"any" are how a cleared MultiChoice is written down; they are not options.
                if (token.Length == 0 || token == "none" || token == "any" || token == "all") continue;
                if (!values.Contains(token)) values.Add(token);
            }
            return values;
        }
    }
}
