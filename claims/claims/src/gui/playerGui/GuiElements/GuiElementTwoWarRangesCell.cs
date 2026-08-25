using System;
using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.part.structure.war;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One day of the war schedule shown twice: our proposed slots on top, editable, and the
    /// enemy's below them, read-only.
    /// </summary>
    public class GuiElementTwoWarRangesCell : CANGuiElementCellBase
    {
        // Grid geometry is shared with the one-sided cell so both tabs line up.
        private const int Rows = GuiElementWarRangeCell.Rows;
        private const int Columns = GuiElementWarRangeCell.Columns;
        private const double SlotSize = GuiElementWarRangeCell.SlotSize;
        private const double GridX = GuiElementWarRangeCell.GridX;

        /// <summary>Room for the day name above the two grids.</summary>
        private const double DayRowHeight = 20;

        private const double GridHeight = Rows * SlotSize;
        private const double OursY = DayRowHeight + 4;
        private const double EnemyY = OursY + GridHeight + 6;

        /// <summary>
        /// Exactly what the two grids need. The cell used to keep the shared 73 while drawing some
        /// 140 pixels of content, so every day overlapped the one under it.
        /// </summary>
        private const double ContentHeight = EnemyY + GridHeight + 8;

        public ClientTwoWarRangesCellElement Cell;

        private readonly DayOfWeek dayOfWeek;

        /// <summary>Whether the server lets battles fall on this day at all.</summary>
        private readonly bool dayAllowed;

        protected override bool UseHoverHighlights => false;

        protected override double MinCellHeight => ContentHeight;

        public GuiElementTwoWarRangesCell(ICoreClientAPI capi, ClientTwoWarRangesCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.Cell = cell;
            this.dayOfWeek = cell.DayOfWeek;
            this.dayAllowed = WarScheduleHelper.IsDayAllowed(cell.DayOfWeek);

            var font = CairoFont.WhiteDetailText();

            // Which row is whose, said in words next to it. The old "Our" heading was 25pt and had
            // no counterpart, so the lower grid was unlabelled.
            AddSideLabel(Lang.Get("claims:gui-warrange-ours"), OursY, ClaimsColors.Value);
            AddSideLabel(Lang.Get("claims:gui-warrange-enemy"), EnemyY, ClaimsColors.Danger);

            AddGrid(capi, font, OursY, ours: true);
            AddGrid(capi, font, EnemyY, ours: false);

            Bounds.fixedHeight = ContentHeight;
        }

        /// <summary>Names one of the two grids, in the column left of it.</summary>
        private void AddSideLabel(string text, double y, double[] color)
        {
            var labelBounds = ElementBounds.Fixed(6, y, GridX - 12, SlotSize).WithParent(Bounds);
            AddText(text, labelBounds, color, 13);
        }

        /// <summary>
        /// Lays out one 2x24 grid of half-hour slots. The enemy's grid is display-only, so its
        /// toggles are not clickable.
        /// </summary>
        private void AddGrid(ICoreClientAPI capi, CairoFont font, double y, bool ours)
        {
            ElementBounds slotBounds = ElementBounds.Fixed(GridX, y, SlotSize, SlotSize).WithParent(Bounds);
            double firstX = slotBounds.fixedX;

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    int slot = col + row * Columns;
                    bool[] schedule = ours ? Cell.OurWarRangeArray : Cell.EnemyWarRangeArray;

                    var toggle = new CANGuiElementToggleButton(capi, "claims:stairs-goal", (bool t) =>
                    {
                        schedule[slot] = !schedule[slot];
                    }, slotBounds, true, ours);
                    toggle.On = schedule[slot];
                    // The enemy's row is display-only, and so is any day the server forbids battles on.
                    toggle.ReadOnly = !ours || !dayAllowed;
                    children.Add(toggle);

                    string hover = dayAllowed
                        ? WarScheduleDisplay.SlotLabel(dayOfWeek, slot, GuiElementWarRangeCell.SlotLabel(slot))
                        : Lang.Get("claims:gui-warrange-day-not-allowed", WarScheduleHelper.AllowedDaysText());
                    children.Add(new GuiElementHoverText(capi, hover, font, 160, slotBounds));

                    slotBounds = slotBounds.RightCopy();
                }
                slotBounds = slotBounds.BelowCopy();
                slotBounds.fixedX = firstX;
            }
        }

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            string dayName = GuiElementWarRangeCell.DayLabel(dayOfWeek);
            if (!dayAllowed) dayName += " " + Lang.Get("claims:gui-warrange-day-off");
            TextExtents extents = Font.GetTextExtents(dayName);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, dayName,
                Bounds.absPaddingX + GuiElement.scaled(6), Bounds.absPaddingY + GuiElement.scaled(4),
                extents.Width + 1.0, EnumTextOrientation.Left);
        }
    }
}
