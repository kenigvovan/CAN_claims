using System;
using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One day of the war schedule: 48 half-hour slots as a 3x16 grid of toggles, labelled with the
    /// day name.
    /// </summary>
    public class GuiElementWarRangeCell : CANGuiElementCellBase
    {
        private const int Rows = 3;
        private const int Columns = 16;

        public ClientWarRangeCellElement cell;

        private readonly DayOfWeek dayOfWeek;

        protected override bool UseHoverHighlights => false;

        public GuiElementWarRangeCell(ICoreClientAPI capi, ClientWarRangeCellElement cell, ElementBounds bounds, bool toggleable = true)
            : base(capi, bounds)
        {
            this.cell = cell;
            this.dayOfWeek = cell.DayOfWeek;

            var font = CairoFont.WhiteDetailText();
            ElementBounds slotBounds = ElementBounds.Fixed(130, 10, 18, 18).WithParent(Bounds);
            double firstX = slotBounds.fixedX;

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    int slot = col + row * Columns;

                    var toggle = new GuiElementToggleButton(capi, "claims:stairs-goal", "", font, (bool t) =>
                    {
                        this.cell.WarRangeArray[slot] = !this.cell.WarRangeArray[slot];
                    }, slotBounds, true);
                    toggle.On = this.cell.WarRangeArray[slot];
                    children.Add(toggle);

                    children.Add(new GuiElementHoverText(capi, SlotLabel(slot), font, 120, slotBounds));

                    slotBounds = slotBounds.RightCopy();
                }
                slotBounds = slotBounds.BelowCopy();
                slotBounds.fixedX = firstX;
            }
        }

        /// <summary>Half-hour slot index rendered as "HH:MM - HH:MM".</summary>
        private static string SlotLabel(int slot)
        {
            return string.Format("{0:00}:{1:00} - {2:00}:{3:00}",
                Math.Floor(slot * 0.5), (slot * 0.5) % 1 * 60,
                Math.Floor((slot + 1) * 0.5), ((slot + 1) * 0.5) % 1 * 60);
        }

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            string dayName = dayOfWeek.ToString();
            TextExtents extents = Font.GetTextExtents(dayName);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, dayName,
                Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(10), extents.Width + 1.0, EnumTextOrientation.Left);
        }
    }
}
