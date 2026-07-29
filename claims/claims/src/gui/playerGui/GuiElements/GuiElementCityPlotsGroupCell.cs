using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A plots group of the player's city, with its members listed. Clicking anywhere opens the
    /// group's info page - the whole cell is one zone.
    /// </summary>
    public class GuiElementCityPlotsGroupCell : CANGuiElementCellBase
    {
        private readonly PlotsGroupCellElement plotsGroupCell;

        protected override int ClickZones => 1;

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

            // Same shape as a card row: heading, muted subtitle, then caption/value lines. The names
            // used to be laid out one box per player, wrapping the cell to any height it liked.
            double textWidth = bounds.fixedWidth - UnscaledRightBoxWidth - 20;

            ElementBounds row = ElementBounds.Fixed(12, 6, textWidth, 22).WithParent(Bounds);
            AddTitle(plotsGroupCell.Name, row);

            row = row.BelowCopy(0, 0).WithFixedHeight(20);
            AddSubtitle(plotsGroupCell.CityName, row);

            row = row.BelowCopy(0, 2).WithFixedHeight(20);
            AddLabelValue(Lang.Get("claims:gui-plotsgroup-label-members"),
                plotsGroupCell.PlayersNames.Count.ToString(), row, 90);
        }

        /// <summary>Everything visible is drawn by the base from richTexts.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
