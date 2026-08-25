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
    /// One day of the war schedule: 48 half-hour slots as a 2x24 grid of toggles, labelled with the
    /// day name.
    /// </summary>
    public class GuiElementWarRangeCell : CANGuiElementCellBase
    {
        /// <summary>
        /// Two rows of 24 rather than three of 16: a row is then one hour per two slots left to
        /// right, and the whole day is 30 pixels tall instead of 54. Seven of the old cells did not
        /// fit the window, so the list showed a single day at a time.
        /// </summary>
        internal const int Rows = 2;
        internal const int Columns = 24;
        internal const double SlotSize = 15;

        /// <summary>Where the grid starts, leaving room for the day name on the left.</summary>
        internal const double GridX = 100;

        internal const double TopPad = 8;

        private const double ContentHeight = TopPad * 2 + Rows * SlotSize;

        public ClientWarRangeCellElement cell;

        private readonly DayOfWeek dayOfWeek;

        /// <summary>Whether the server lets battles fall on this day at all.</summary>
        private readonly bool dayAllowed;

        protected override bool UseHoverHighlights => false;

        /// <summary>
        /// The cell is exactly as tall as its grid. It used to inherit the shared 73, which had
        /// nothing to do with what it draws.
        /// </summary>
        protected override double MinCellHeight => ContentHeight;

        public GuiElementWarRangeCell(ICoreClientAPI capi, ClientWarRangeCellElement cell, ElementBounds bounds, bool toggleable = true)
            : base(capi, bounds)
        {
            this.cell = cell;
            this.dayOfWeek = cell.DayOfWeek;
            this.dayAllowed = WarScheduleHelper.IsDayAllowed(cell.DayOfWeek);

            var font = CairoFont.WhiteDetailText();
            ElementBounds slotBounds = ElementBounds.Fixed(GridX, TopPad, SlotSize, SlotSize).WithParent(Bounds);
            double firstX = slotBounds.fixedX;

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    int slot = col + row * Columns;

                    // CAN's own toggle rather than the vanilla one: it is the only one that can be
                    // made read-only, which days the server forbids battles on have to be.
                    var toggle = new CANGuiElementToggleButton(capi, "claims:stairs-goal", (bool t) =>
                    {
                        this.cell.WarRangeArray[slot] = !this.cell.WarRangeArray[slot];
                    }, slotBounds, true, true);
                    toggle.On = this.cell.WarRangeArray[slot];
                    toggle.ReadOnly = !dayAllowed;
                    children.Add(toggle);

                    string hover = dayAllowed
                        ? WarScheduleDisplay.SlotLabel(this.dayOfWeek, slot, SlotLabel(slot))
                        : Lang.Get("claims:gui-warrange-day-not-allowed", WarScheduleHelper.AllowedDaysText());
                    children.Add(new GuiElementHoverText(capi, hover, font, 160, slotBounds));

                    slotBounds = slotBounds.RightCopy();
                }
                slotBounds = slotBounds.BelowCopy();
                slotBounds.fixedX = firstX;
            }

            Bounds.fixedHeight = ContentHeight;
        }

        /// <summary>Half-hour slot index rendered as "HH:MM - HH:MM".</summary>
        internal static string SlotLabel(int slot)
        {
            return string.Format("{0:00}:{1:00} - {2:00}:{3:00}",
                Math.Floor(slot * 0.5), (slot * 0.5) % 1 * 60,
                Math.Floor((slot + 1) * 0.5), ((slot + 1) * 0.5) % 1 * 60);
        }

        /// <summary>
        /// The day, short enough to fit beside the grid. The full names were what forced the grid a
        /// third of the way across the cell.
        /// </summary>
        internal static string DayLabel(DayOfWeek day)
            => Lang.Get("claims:gui_day_short_" + day.ToString().ToLower());

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            string dayName = DayLabel(dayOfWeek);
            // Short marker, not the spelled-out note the two-sided cell uses: here the day shares its
            // line with the grid, and "(no battles)" ran straight under the slot buttons. What it
            // means is in the hover text of every slot on the row.
            if (!dayAllowed) dayName += " " + Lang.Get("claims:gui-warrange-day-off-short");

            // Never past the column the grid starts in, however long the translated day name is.
            double maxWidth = GuiElement.scaled(GridX - 12);
            TextExtents extents = Font.GetTextExtents(dayName);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, dayName,
                Bounds.absPaddingX + GuiElement.scaled(6), Bounds.absPaddingY + GuiElement.scaled(TopPad),
                Math.Min(extents.Width + 1.0, maxWidth), EnumTextOrientation.Left);
        }
    }
}
