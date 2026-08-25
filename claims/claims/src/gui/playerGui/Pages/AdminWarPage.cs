using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Start and end wars by hand and override the next battle window. The runtime settings live on
    /// their own page (<see cref="AdminWarConfigPage"/>): sharing this one left their list about
    /// three rows tall.
    /// </summary>
    public sealed class AdminWarPage : AdminPageBase
    {
        private const double InputHeight = 28;
        private const double RowGap = 6;
        private const double LabelHeight = 22;

        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // No page title of its own: the admin tab strip above already names the page, and the
            // window has no vertical room to spare.
            var column = ctx.Current.BelowCopy(0, 5);
            column.fixedWidth = ctx.Line.fixedWidth;
            column.WithAlignment(EnumDialogArea.LeftTop);

            BuildWarCard(compo, column, column.fixedY);
        }

        /// <summary>The two sides, the two commands that act on them, and a picker that fills them in.</summary>
        private double BuildWarCard(GuiComposer compo, ElementBounds column, double y)
        {
            string startCaption = Lang.Get("claims:gui-admin-force-start-war");
            string endCaption = Lang.Get("claims:gui-admin-force-end-war");

            double innerWidth = column.fixedWidth - Card.Padding * 2;
            double buttonsHeight = ButtonRow.HeightFor(innerWidth, 0, startCaption, endCaption);

            var conflicts = claims.clientDataStorage?.clientPlayerInfo?.CityInfo?.ClientConflictCellElements;
            bool hasConflicts = conflicts != null && conflicts.Count > 0;

            // With conflicts the picker names itself through its tooltip; the caption line is only
            // there to say when there are none. Every line here is a line the settings list loses.
            double pickHeight = hasConflicts ? InputHeight : LabelHeight;

            // The battle window row lives in this card too: as a card of its own it cost a heading, a
            // frame and two gaps, and the settings list below had no room left to be usable.
            double body = InputHeight + RowGap + buttonsHeight + RowGap + pickHeight
                        + RowGap + InputHeight;

            ElementBounds inner = Card.Frame(compo, column, y, Card.HeaderHeight + body + Card.Padding * 2,
                Lang.Get("claims:gui-admin-section-war"));

            var firstBounds = inner.FlatCopy().WithFixedSize(150, InputHeight);
            compo.AddTextInput(firstBounds, v => Admin.RenameTo = v, null, "admin-war-first");
            compo.GetTextInput("admin-war-first").SetValue(Admin.RenameTo);

            var secondBounds = firstBounds.RightCopy(10).WithFixedSize(150, InputHeight);
            compo.AddTextInput(secondBounds, v => Admin.PlayerName = v, null, "admin-war-second");
            compo.GetTextInput("admin-war-second").SetValue(Admin.PlayerName);

            var buttonAnchor = inner.FlatCopy().WithFixedHeight(ButtonRow.ButtonHeight);
            buttonAnchor.fixedY = inner.fixedY + InputHeight + RowGap;

            var commands = new ButtonRow(compo, buttonAnchor, inner.fixedWidth);
            AddCommand(commands, startCaption,
                () => "/cadmin startwar " + Admin.RenameTo + " " + Admin.PlayerName,
                "claims:gui-admin-force-start-war-tooltip");
            AddCommand(commands, endCaption,
                () => "/cadmin endwar " + Admin.RenameTo + " " + Admin.PlayerName,
                "claims:gui-admin-force-end-war-tooltip");

            // Picking a conflict fills both party fields, so the commands above act on it without
            // the admin retyping two names.
            double pickY = buttonAnchor.fixedY + buttonsHeight + RowGap;

            var labelBounds = inner.FlatCopy().WithFixedHeight(LabelHeight);
            labelBounds.fixedY = pickY;

            if (!hasConflicts)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-active-conflicts"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), labelBounds, "admin-no-conflicts");

                AddBattleWindowRow(compo, inner, pickY + pickHeight + RowGap);
                return y + Card.HeaderHeight + body + Card.Padding * 2 + Card.Gap;
            }

            var values = new string[conflicts.Count];
            var names = new string[conflicts.Count];
            for (int i = 0; i < conflicts.Count; i++)
            {
                var c = conflicts[i];
                values[i] = i.ToString(CultureInfo.InvariantCulture);
                names[i] = c.FirstPartyName + " " + Lang.Get("claims:gui-admin-vs") + " " + c.SecondPartyName
                           + "  [" + c.FirstScore + ":" + c.SecondScore + "]";
            }

            var pickBounds = inner.FlatCopy().WithFixedSize(inner.fixedWidth - 20, InputHeight);
            pickBounds.fixedY = pickY;

            compo.AddDropDown(values, names, -1, (code, selected) =>
            {
                if (!selected) return;
                int index;
                if (!int.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) return;
                if (index < 0 || index >= conflicts.Count) return;

                Admin.RenameTo = conflicts[index].FirstPartyName;
                Admin.PlayerName = conflicts[index].SecondPartyName;
                Gui.BuildMainWindow();
            }, pickBounds, "admin-war-conflict");
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-use-tooltip"), pickBounds, "tip-admin-war-conflict");

            AddBattleWindowRow(compo, inner, pickY + pickHeight + RowGap);

            return y + Card.HeaderHeight + body + Card.Padding * 2 + Card.Gap;
        }

        /// <summary>When the next battle starts and how long it runs, for the two parties above.</summary>
        private void AddBattleWindowRow(GuiComposer compo, ElementBounds inner, double y)
        {
            var startInBounds = inner.FlatCopy().WithFixedSize(80, InputHeight);
            startInBounds.fixedY = y;
            compo.AddTextInput(startInBounds, v => Admin.BonusClaims = v, null, "admin-war-startin");
            compo.GetTextInput("admin-war-startin").SetValue(Admin.BonusClaims);

            var durationBounds = startInBounds.RightCopy(10).WithFixedSize(80, InputHeight);
            compo.AddTextInput(durationBounds, v => Admin.CityFee = v, null, "admin-war-duration");
            compo.GetTextInput("admin-war-duration").SetValue(Admin.CityFee);

            string caption = Lang.Get("claims:gui-admin-set-battle-date");
            var buttonBounds = durationBounds.RightCopy(10).WithFixedSize(ButtonWidth(caption), InputHeight);
            compo.AddButton(caption, new ActionConsumable(() =>
            {
                Send("/cadmin setbattledate " + Admin.RenameTo + " " + Admin.PlayerName
                     + " " + Admin.BonusClaims + " " + Admin.CityFee);
                return true;
            }), buttonBounds, EnumButtonStyle.Normal);
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-set-battle-date-tooltip"), buttonBounds, "tip-admin-battledate");
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-override-battle"), startInBounds, "tip-admin-battlewindow");
        }

        /// <summary>A command button in a wrapping row, named by its tooltip.</summary>
        private void AddCommand(ButtonRow row, string caption, System.Func<string> command, string tooltipKey)
        {
            row.Add(caption, () =>
            {
                Send(command());
                return true;
            }, tooltipKey != null ? Lang.Get(tooltipKey) : null);
        }
    }
}
