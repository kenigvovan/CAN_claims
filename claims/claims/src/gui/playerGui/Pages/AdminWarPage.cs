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
    /// Start and end wars by hand, override the next battle window, and edit the war settings the
    /// server accepts at runtime.
    /// </summary>
    public sealed class AdminWarPage : AdminPageBase
    {
        private const double InputHeight = 28;
        private const double RowGap = 6;
        private const double LabelHeight = 22;

        /// <summary>Width the scrollbar needs beside the settings list.</summary>
        private const double ScrollbarReserve = 27;

        /// <summary>The settings list never shrinks below this, however tall the cards above are.</summary>
        private const double MinConfigHeight = 90;

        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // No page title of its own: the admin tab strip above already names the page, and the
            // window has no vertical room to spare.
            var column = ctx.Current.BelowCopy(0, 5);
            column.fixedWidth = ctx.Line.fixedWidth;
            column.WithAlignment(EnumDialogArea.LeftTop);

            double y = BuildWarCard(compo, column, column.fixedY);
            BuildConfigCard(ctx, compo, column, y);
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

        /// <summary>
        /// The runtime settings: a filter and an edit row over the list of current values. Clicking a
        /// row acts on it directly - a switch flips, a number loads into the edit field - so an admin
        /// no longer has to read a key off the screen and retype it, misspellings and all.
        ///
        /// The values are still not edited in place: fifty inline inputs would not fit the dialog,
        /// and every one of them would be its own composer element rebuilt on each config packet.
        /// </summary>
        private void BuildConfigCard(PageBuildContext ctx, GuiComposer compo, ElementBounds column, double y)
        {
            // The card runs down to just above the navigation row, so the list gets whatever the
            // card above did not use, rather than a height counted back from the window bottom.
            double cardHeight = System.Math.Max(
                Card.HeaderHeight + InputHeight * 2 + 8 + MinConfigHeight + Card.Padding * 2,
                Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction - y - Card.Gap);

            ElementBounds inner = Card.Frame(compo, column, y, cardHeight,
                Lang.Get("claims:gui-admin-section-warcfg"));

            // Filter and edit share one row: two rows of controls over a list this short left barely
            // three settings visible at a time.
            var filterBounds = inner.FlatCopy().WithFixedSize(120, InputHeight);
            compo.AddTextInput(filterBounds, v => Admin.WarCfgFilter = v, null, "warcfg-filter");
            compo.GetTextInput("warcfg-filter").SetValue(Admin.WarCfgFilter ?? "");
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-warcfg-filter-hint"), filterBounds, "tip-warcfg-filter");

            string filterCaption = Lang.Get("claims:gui-admin-filter");
            var filterButton = filterBounds.RightCopy(6).WithFixedSize(ButtonWidth(filterCaption), InputHeight);
            compo.AddButton(filterCaption, new ActionConsumable(() =>
            {
                // A different set of rows makes the old scroll position meaningless.
                Admin.WarCfgScroll = 0;
                Gui.BuildMainWindow();
                return true;
            }), filterButton, EnumButtonStyle.Normal);

            // --- edit fields: filled in by clicking a row, or typed by hand as before ---
            var keyBounds = filterButton.RightCopy(14).WithFixedSize(130, InputHeight);
            compo.AddTextInput(keyBounds, v => Admin.ClaimRadius = v, null, "warcfg-key");
            compo.GetTextInput("warcfg-key").SetValue(Admin.ClaimRadius);
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-warcfg-key-hint"), keyBounds, "tip-warcfg-key");

            var valueBounds = keyBounds.RightCopy(6).WithFixedSize(70, InputHeight);
            compo.AddTextInput(valueBounds, v => Admin.NewCityName = v, null, "warcfg-value");
            compo.GetTextInput("warcfg-value").SetValue(Admin.NewCityName);

            string applyCaption = Lang.Get("claims:gui-admin-warcfg-apply");
            var applyBounds = valueBounds.RightCopy(6).WithFixedSize(ButtonWidth(applyCaption), InputHeight);
            compo.AddButton(applyCaption, new ActionConsumable(() =>
            {
                Send("/cadmin setcfg " + Admin.ClaimRadius + " " + Admin.NewCityName);
                return true;
            }), applyBounds, EnumButtonStyle.Normal);

            var rows = VisibleRows();

            double editRowsHeight = InputHeight + 8;

            // The clip is added to the composer and everything else forks from it: a bounds that is
            // only forked from and never added has no parent, and the renderer dereferences that
            // parent while pushing the scissor.
            ElementBounds clipBounds = ElementBounds.Fixed(0, editRowsHeight,
                inner.fixedWidth - ScrollbarReserve, inner.fixedHeight - editRowsHeight);
            ElementBounds scrollbarBounds = clipBounds.RightCopy(7).WithFixedWidth(20);
            ElementBounds listBounds = clipBounds.ForkContainingChild(0, 0, 0, -3).WithFixedPadding(3);

            compo.BeginChildElements(inner)
                    .BeginClip(clipBounds)
                        .AddCellList(listBounds,
                            (WarCfgRow row, ElementBounds bounds) => new GuiElementWarCfgCell(compo.Api, row, bounds)
                            {
                                On = true,
                                OnMouseDownOnCellLeft = index => OnRowClicked(rows, index)
                            },
                            rows, "warcfg-cells")
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        // Remembered, so acting on a row - which rebuilds the window - does not throw
                        // the admin back to the top of a fifty-row list.
                        Admin.WarCfgScroll = value;

                        ElementBounds bounds = compo.GetCellList<WarCfgRow>("warcfg-cells").Bounds;
                        bounds.fixedY = 0 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "warcfg-scrollbar")
                .EndChildElements();

            compo.GetCellList<WarCfgRow>("warcfg-cells").BeforeCalcBounds();

            float restoreTo = Admin.WarCfgScroll;
            ctx.AfterCompose(() =>
            {
                var scrollbar = compo.GetScrollbar("warcfg-scrollbar");
                scrollbar.SetHeights(
                    (float)clipBounds.fixedHeight,
                    (float)compo.GetCellList<WarCfgRow>("warcfg-cells").Bounds.fixedHeight);

                // After the heights, or the position would be clamped against a total height of zero.
                scrollbar.CurrentYPosition = restoreTo;
                scrollbar.TriggerChanged();
            });
        }

        /// <summary>
        /// The rows the filter leaves. A group heading is kept only when something under it survived,
        /// so a filtered list is not a run of empty headings.
        /// </summary>
        private List<WarCfgRow> VisibleRows()
        {
            string filter = (Admin.WarCfgFilter ?? "").Trim().ToLowerInvariant();
            if (filter.Length == 0) return WarConfigTable.Rows.ToList();

            var kept = new List<WarCfgRow>();
            WarCfgRow pendingGroup = null;

            foreach (var row in WarConfigTable.Rows)
            {
                if (row.Kind == EnumCfgKind.Group)
                {
                    pendingGroup = row;
                    continue;
                }

                bool matches = (row.CfgKey ?? "").ToLowerInvariant().Contains(filter)
                            || Lang.Get(row.LabelLangKey).ToLowerInvariant().Contains(filter);
                if (!matches) continue;

                if (pendingGroup != null)
                {
                    kept.Add(pendingGroup);
                    pendingGroup = null;
                }
                kept.Add(row);
            }

            return kept;
        }

        /// <summary>
        /// A switch is flipped on the spot - that is the whole edit. Anything with a number loads
        /// into the edit field instead, so the admin types the new value and nothing else.
        /// </summary>
        private void OnRowClicked(List<WarCfgRow> rows, int index)
        {
            if (index < 0 || index >= rows.Count) return;

            WarCfgRow row = rows[index];
            if (row.Kind == EnumCfgKind.Group) return;

            if (row.Kind == EnumCfgKind.Flag)
            {
                Send("/cadmin setcfg " + row.CfgKey + (row.GetFlag() ? " off" : " on"));
                return;
            }

            Admin.ClaimRadius = row.CfgKey;
            Admin.NewCityName = GuiElementWarCfgCell.CurrentValue(row);
            Gui.BuildMainWindow();
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
