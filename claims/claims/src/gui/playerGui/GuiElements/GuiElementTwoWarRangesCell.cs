using System;
using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One day of the war schedule shown twice: our proposed slots on top, editable, and the
    /// enemy's below them, read-only.
    /// </summary>
    public class GuiElementTwoWarRangesCell : CANGuiElementCellBase
    {
        private const int Rows = 3;
        private const int Columns = 16;
        private const int SlotsPerDay = Rows * Columns;

        public ClientTwoWarRangesCellElement Cell;

        private readonly DayOfWeek dayOfWeek;

        protected override bool UseHoverHighlights => false;

        public GuiElementTwoWarRangesCell(ICoreClientAPI capi, ClientTwoWarRangesCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.Cell = cell;
            this.dayOfWeek = cell.DayOfWeek;

            var font = CairoFont.WhiteDetailText();

            ElementBounds slotBounds = ElementBounds.Fixed(130, 10, 18, 18).WithParent(Bounds);
            slotBounds.fixedOffsetY += 10;
            double firstX = slotBounds.fixedX;

            string ourLabel = "Our";
            TextExtents extents = CairoFont.WhiteMediumText().GetTextExtents(ourLabel);
            ElementBounds labelBounds = ElementBounds.Fixed(10, 10, extents.Width + 40, 25).WithParent(Bounds);
            labelBounds.fixedOffsetX -= 20;
            richTexts.Add(new GuiElementRichtext(capi,
                VtmlUtil.Richtextify(capi, ourLabel, CairoFont.WhiteMediumText().WithFontSize(25)), labelBounds));

            slotBounds = AddGrid(capi, font, slotBounds, firstX, ours: true);
            slotBounds.fixedOffsetY += 10;
            AddGrid(capi, font, slotBounds, firstX, ours: false);
        }

        /// <summary>
        /// Lays out one 3x16 grid of half-hour slots. The enemy's grid is display-only, so its
        /// toggles are not clickable.
        /// </summary>
        private ElementBounds AddGrid(ICoreClientAPI capi, CairoFont font, ElementBounds slotBounds, double firstX, bool ours)
        {
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
                    if (!ours) toggle.Toggleable = false;
                    children.Add(toggle);

                    children.Add(new GuiElementHoverText(capi, SlotLabel(slot), font, 120, slotBounds));

                    slotBounds = slotBounds.RightCopy();
                }
                slotBounds = slotBounds.BelowCopy();
                slotBounds.fixedX = firstX;
            }
            return slotBounds;
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
