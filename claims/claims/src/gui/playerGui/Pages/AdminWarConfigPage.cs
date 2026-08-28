using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The runtime war settings, on a page of their own. They used to share the War page with the
    /// start/end-war commands, which left the list about three rows tall - unusable for a table of
    /// some fifty settings.
    ///
    /// Clicking a row acts on it directly: a switch flips, anything else loads into the edit field.
    /// The values are still not edited in place - fifty inline inputs would each be a composer
    /// element rebuilt on every config packet.
    /// </summary>
    public sealed class AdminWarConfigPage : AdminPageBase
    {
        private const double InputHeight = 28;

        /// <summary>Width the scrollbar needs beside the settings list.</summary>
        private const double ScrollbarReserve = 27;

        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var column = ctx.Current.BelowCopy(0, 5);
            column.fixedWidth = ctx.Line.fixedWidth;
            column.WithAlignment(EnumDialogArea.LeftTop);

            // Everything down to the navigation row: the whole point of the page is that the list
            // gets the room, so nothing is held back for a second card.
            double cardHeight = Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                                - column.fixedY - Card.Gap;

            ElementBounds inner = Card.Frame(compo, column, column.fixedY, cardHeight,
                Lang.Get("claims:gui-admin-section-warcfg"));

            // Filter and edit share one row - two rows of controls would cost the list four settings.
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

            // Narrower than the value field: keys are picked by clicking a row, values are typed, and
            // a day list ("saturday,sunday") needs the room more than the key does.
            var keyBounds = filterButton.RightCopy(14).WithFixedSize(110, InputHeight);
            compo.AddTextInput(keyBounds, v => Admin.ClaimRadius = v, null, "warcfg-key");
            compo.GetTextInput("warcfg-key").SetValue(Admin.ClaimRadius);
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-warcfg-key-hint"), keyBounds, "tip-warcfg-key");

            var valueBounds = keyBounds.RightCopy(6).WithFixedSize(110, InputHeight);
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
        /// A switch is flipped on the spot - that is the whole edit. Anything else loads into the
        /// edit field instead, so the admin types the new value and nothing else.
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

            // Values that come from a fixed set are picked in a dialog: typing "saturday,sunday" into
            // the value field by hand is how a setting ends up half-applied over a typo.
            if (row.Kind == EnumCfgKind.Choice || row.Kind == EnumCfgKind.MultiChoice)
            {
                Gui.OpenDialog(EnumUpperWindowSelectedState.WARCFG_CHOICE, dialogArgs =>
                {
                    dialogArgs.Selected = row.CfgKey;
                    dialogArgs.SelectedSecond = GuiElementWarCfgCell.CurrentValue(row);
                });
                return;
            }

            Admin.ClaimRadius = row.CfgKey;
            Admin.NewCityName = GuiElementWarCfgCell.CurrentValue(row);
            Gui.BuildMainWindow();
        }
    }
}
