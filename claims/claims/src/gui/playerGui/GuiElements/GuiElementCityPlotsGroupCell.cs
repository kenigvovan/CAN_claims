using System.Collections.Generic;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A plots group of the player's city: what it holds, who is in it and what it costs them.
    /// Clicking anywhere opens the group's info page - the whole cell is one zone.
    /// </summary>
    public class GuiElementCityPlotsGroupCell : CANGuiElementCellBase
    {
        private readonly PlotsGroupCellElement plotsGroupCell;

        protected override int ClickZones => 1;

        private const double TopPadding = 8;
        private const double TitleHeight = 24;
        private const double RowHeight = 19;
        private const double LabelWidth = 96;

        /// <summary>One caption/value line of the row.</summary>
        private struct Line
        {
            public string Label;
            public string Value;
            public double[] Color;
        }

        public GuiElementCityPlotsGroupCell(ICoreClientAPI capi, PlotsGroupCellElement plotsGroupCell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.plotsGroupCell = plotsGroupCell;

            OnMouseDownOnCellLeft = _ =>
            {
                claims.CANCityGui.State.DialogArgs.Selected = this.plotsGroupCell.Guid;
                claims.CANCityGui.State.SelectedTab = EnumSelectedTab.PlotsGroupInfoPage;
                claims.CANCityGui.BuildMainWindow();
            };
            // One tooltip for the whole cell: what clicking does, and who is in the group - a group
            // can hold more members than the cell has room to print.
            string hint = Lang.Get("claims:gui-plotsgroup-open-info");
            if (plotsGroupCell.PlayersNames.Count > 0)
            {
                hint += "\n" + StringFunctions.concatStringsWithDelim(plotsGroupCell.PlayersNames, ',');
            }
            AddZoneTooltip(HighlightZone.Left, hint);

            List<Line> lines = BuildLines();
            double cellHeight = TopPadding * 2 + TitleHeight + 4 + lines.Count * RowHeight;

            // Same shape as a card row: heading, then caption/value lines. The city name is not among
            // them - every group in this list belongs to the same city, the player's own.
            double textWidth = bounds.fixedWidth - UnscaledRightBoxWidth - 20;

            ElementBounds row = ElementBounds.Fixed(12, TopPadding, textWidth, TitleHeight).WithParent(Bounds);
            AddTitle(plotsGroupCell.Name, row);

            row = row.BelowCopy(0, 4).WithFixedHeight(RowHeight);
            foreach (Line line in lines)
            {
                AddLabelValue(line.Label, line.Value, row, LabelWidth, line.Color);
                row = row.BelowCopy();
            }

            Bounds.fixedHeight = cellHeight;
        }

        /// <summary>
        /// What the row says about the group. The announced raise is a line of its own and only when
        /// there is one, so a group nobody is changing stays three quiet lines.
        /// </summary>
        private List<Line> BuildLines()
        {
            var lines = new List<Line>
            {
                new Line
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-plots"),
                    Value = plotsGroupCell.PlotsCount.ToString()
                },
                new Line
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-members"),
                    Value = plotsGroupCell.PlayersNames.Count.ToString()
                },
                new Line
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-fee"),
                    Value = Number(plotsGroupCell.PlotsGroupFee),
                    // A group that costs nothing says so in the muted colour rather than shouting a
                    // zero in the same weight as a real price.
                    Color = plotsGroupCell.PlotsGroupFee > 0 ? null : Widgets.ClaimsColors.Label
                }
            };

            if (plotsGroupCell.HasPendingFee)
            {
                bool accepted = plotsGroupCell.AcceptedBy(claims.capi?.World?.Player?.PlayerUID ?? "");
                lines.Add(new Line
                {
                    Label = Lang.Get("claims:gui-plotsgroup-label-fee-pending"),
                    Value = Number(plotsGroupCell.PendingFee),
                    Color = accepted ? Widgets.ClaimsColors.Success : Widgets.ClaimsColors.Warning
                });
            }
            return lines;
        }

        /// <summary>Fees are doubles; whole values should not read "5.0".</summary>
        private static string Number(double value) =>
            value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Everything visible is drawn by the base from richTexts.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
