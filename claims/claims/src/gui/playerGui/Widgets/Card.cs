using System.Collections.Generic;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>One caption/value line of a card: what it is, what it says, what it means on hover.</summary>
    public sealed class CardRow
    {
        public string Label;
        public string Value;

        /// <summary>Null falls back to the shared accent colour.</summary>
        public double[] ValueColor;

        public string Tooltip;

        /// <summary>Suffix of the element keys, unique within the page.</summary>
        public string Key;
    }

    /// <summary>
    /// The framed block the pages are built out of: a heading, a rule, then rows or whatever the
    /// page wants to put inside. Kept here rather than in one page so every tab looks the same.
    /// </summary>
    public static class Card
    {
        public const double LineHeight = 17;

        /// <summary>A list line is taller than a caption line: it carries icon buttons.</summary>
        public const double ItemHeight = 22;
        private const double ItemButtonSize = 20;

        /// <summary>Heading text, the gap before its rule, and the gap between rule and first row.</summary>
        private const double HeaderTextHeight = 20;
        private const double HeaderRuleGap = 5;
        private const double RuleBottomGap = 6;

        /// <summary>Vertical space a heading takes, rule and both gaps included.</summary>
        public const double HeaderHeight = HeaderTextHeight + HeaderRuleGap + 2 + RuleBottomGap;

        /// <summary>Breathing room inside a card, between two cards, and between two columns.</summary>
        public const double Padding = 8;
        public const double Gap = 12;
        public const double ColumnGap = 18;

        /// <summary>Width reserved at the right of a card for a row's value.</summary>
        public const double ValueWidth = 66;

        /// <summary>Size of an icon button in a card's action strip, and the gap between two.</summary>
        public const double ActionSize = 30;
        public const double ActionGap = 7;

        /// <summary>Height a card of <paramref name="rowCount"/> rows takes up.</summary>
        public static double HeightFor(int rowCount) => HeaderHeight + rowCount * LineHeight + Padding * 2;

        /// <summary>
        /// Draws the frame and its heading, and returns the bounds of the area inside it - what a
        /// page fills with its own elements. Use <see cref="Rows"/> for plain caption/value blocks.
        /// </summary>
        public static ElementBounds Frame(GuiComposer compo, ElementBounds column, double y, double height, string title)
        {
            var card = column.FlatCopy();
            card.fixedY = y;
            card.fixedHeight = height;
            compo.AddInset(card, 2);

            double innerWidth = column.fixedWidth - Padding * 2;

            var inner = column.FlatCopy().WithFixedSize(innerWidth, height - Padding * 2);
            inner.fixedX += Padding;
            inner.fixedY = y + Padding;

            if (title != null)
            {
                var header = inner.FlatCopy().WithFixedHeight(HeaderTextHeight);
                var headerFont = CairoFont.WhiteSmallishText().WithColor(ClaimsColors.Section);
                // A long heading wrapped onto the rule and the first row; cut short, full on hover.
                string shown = Fit(title, headerFont, innerWidth);
                compo.AddStaticText(shown, headerFont, header, UniqueKey(compo, "sec-" + title));
                if (shown != title) Tooltip.Add(compo, title, header, UniqueKey(compo, "sectip-" + title));

                var rule = header.BelowCopy(0, HeaderRuleGap).WithFixedHeight(2);
                compo.AddInset(rule);

                inner.fixedY += HeaderHeight;
                inner.fixedHeight -= HeaderHeight;
            }

            return inner;
        }

        /// <summary>
        /// A key no element on this composer holds yet. Two cards on one page may legitimately carry
        /// the same heading, and a repeated key makes the composer throw.
        /// </summary>
        public static string UniqueKey(GuiComposer compo, string key)
        {
            string result = key;
            for (int i = 2; compo.GetElement(result) != null; i++)
            {
                result = key + "-" + i;
            }
            return result;
        }

        /// <summary>
        /// A list card: one line per entry with its own buttons, an empty-state text when there is
        /// nothing, and an optional action strip at the bottom (null for none). Replaces a count with
        /// the names in a tooltip and a separate "type the name" form for acting on one of them.
        /// It does not scroll: cut a long list with <see cref="Limit"/>, or keep ScrollableList.
        /// </summary>
        public static double Items(GuiComposer compo, ElementBounds column, double y, string title,
                                   List<ItemRow> items, string emptyText, System.Action<ElementBounds> actions = null)
        {
            int lines = System.Math.Max(1, items.Count);
            double height = HeaderHeight + lines * ItemHeight + Padding * 2;
            if (actions != null) height += ActionGap + ActionSize;
            ElementBounds inner = Frame(compo, column, y, height, title);

            if (items.Count == 0)
            {
                var empty = inner.FlatCopy().WithFixedHeight(ItemHeight);
                compo.AddStaticText(emptyText ?? "", CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    empty, UniqueKey(compo, "empty-" + title));
            }

            for (int i = 0; i < items.Count; i++)
            {
                DrawItem(compo, inner, items[i], inner.fixedY + i * ItemHeight);
            }

            if (actions != null)
            {
                var slot = inner.FlatCopy().WithFixedSize(ActionSize, ActionSize);
                slot.fixedY = inner.fixedY + lines * ItemHeight + ActionGap;
                actions(slot);
            }
            return y + height + Gap;
        }

        /// <summary>
        /// A card holding one block of wrapped text, such as a hint. Measured, not given a height:
        /// a fixed one cut a long translation short or left a short one floating in empty space.
        /// </summary>
        public static double Note(GuiComposer compo, ElementBounds column, double y, string title, string text,
                                  System.Action<ElementBounds> actions = null)
        {
            CairoFont font = CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label);
            double width = column.fixedWidth - Padding * 2;
            // Measured in screen pixels, as the font is scaled; the card works in unscaled units.
            double textHeight = compo.Api.Gui.Text.GetMultilineTextHeight(font, text, GuiElement.scaled(width))
                                / Vintagestory.API.Config.RuntimeEnv.GUIScale + 4;

            // No heading, no room for one: Frame only draws it when there is a title.
            double height = (title != null ? HeaderHeight : 0) + textHeight + Padding * 2;
            if (actions != null) height += ActionGap + ActionSize;
            ElementBounds inner = Frame(compo, column, y, height, title);

            var textBounds = inner.FlatCopy().WithFixedHeight(textHeight);
            compo.AddStaticText(text, font, textBounds, UniqueKey(compo, "note-" + (title ?? "")));

            if (actions != null)
            {
                var slot = inner.FlatCopy().WithFixedSize(ActionSize, ActionSize);
                slot.fixedY = inner.fixedY + textHeight + ActionGap;
                actions(slot);
            }
            return y + height + Gap;
        }

        /// <summary>
        /// At most <paramref name="max"/> entries, the last one replaced by "and N more" when the
        /// list is longer - what a list card does instead of scrolling.
        /// </summary>
        public static List<ItemRow> Limit(List<ItemRow> items, int max)
        {
            if (items.Count <= max || max < 2) return items;
            var shown = items.GetRange(0, max - 1);
            shown.Add(new ItemRow
            {
                Label = Vintagestory.API.Config.Lang.Get("claims:gui-list-and-more", items.Count - (max - 1)),
                Key = "more-" + items[0].Key
            });
            return shown;
        }

        /// <summary>
        /// A whole card of caption/value rows. Returns the y the next card in the column starts at.
        /// </summary>
        public static double Rows(GuiComposer compo, ElementBounds column, double y, string title, List<CardRow> rows)
            => Rows(compo, column, y, title, rows, 1);

        /// <summary>
        /// The same, laid out over <paramref name="subColumns"/> side-by-side runs. A long list -
        /// the plot types - is a third of the window tall stacked, and pushed the card past the
        /// bottom edge.
        /// </summary>
        public static double Rows(GuiComposer compo, ElementBounds column, double y, string title,
                                  List<CardRow> rows, int subColumns)
        {
            int perColumn = PerColumn(rows.Count, subColumns);
            double height = HeightFor(perColumn);
            ElementBounds inner = Frame(compo, column, y, height, title);

            DrawRows(compo, inner, rows, subColumns);

            return y + height + Gap;
        }

        /// <summary>
        /// A card of rows with a strip of icon buttons under them - the actions that belong to what
        /// the rows describe. <paramref name="actions"/> is handed the first button slot.
        /// </summary>
        public static double RowsWithActions(GuiComposer compo, ElementBounds column, double y, string title,
                                             List<CardRow> rows, System.Action<ElementBounds> actions,
                                             double actionSize = ActionSize)
        {
            double height = HeightFor(rows.Count) + ActionGap + actionSize;
            ElementBounds inner = Frame(compo, column, y, height, title);

            DrawRows(compo, inner, rows, 1);

            var slot = inner.FlatCopy().WithFixedSize(actionSize, actionSize);
            slot.fixedY = inner.fixedY + rows.Count * LineHeight + ActionGap;
            actions(slot);

            return y + height + Gap;
        }

        /// <summary>A card that is nothing but a strip of icon buttons.</summary>
        public static double Actions(GuiComposer compo, ElementBounds column, double y, string title,
                                     System.Action<ElementBounds> actions, double actionSize = ActionSize)
            => RowsWithActions(compo, column, y, title, new List<CardRow>(), actions, actionSize);

        /// <summary>The slot right of <paramref name="slot"/>, for the next button in a strip.</summary>
        public static ElementBounds NextAction(ElementBounds slot) => slot.RightCopy(ActionGap);

        private static int PerColumn(int rowCount, int subColumns)
        {
            if (subColumns < 1) subColumns = 1;
            return (rowCount + subColumns - 1) / subColumns;
        }

        private static void DrawRows(GuiComposer compo, ElementBounds inner, List<CardRow> rows, int subColumns)
        {
            if (subColumns < 1) subColumns = 1;
            int perColumn = PerColumn(rows.Count, subColumns);

            const double subGap = 10;
            double subWidth = (inner.fixedWidth - subGap * (subColumns - 1)) / subColumns;
            double valueWidth = subColumns == 1 ? ValueWidth : subWidth * 0.4;

            for (int i = 0; i < rows.Count; i++)
            {
                CardRow row = rows[i];
                double subX = (i / perColumn) * (subWidth + subGap);
                double rowY = inner.fixedY + (i % perColumn) * LineHeight;

                var labelFont = CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label);
                var valueFont = CairoFont.WhiteSmallText().WithColor(row.ValueColor ?? ClaimsColors.Value)
                                         .WithOrientation(EnumTextOrientation.Right);

                // Numbers fit the reserved width; a name may not, so the value borrows room from its
                // caption rather than wrapping onto the row below.
                double rowValueWidth = System.Math.Max(valueWidth,
                    System.Math.Min(valueFont.GetTextExtents(row.Value).Width + 6, subWidth * 0.7));
                double labelWidth = subWidth - rowValueWidth - 4;

                var labelBounds = inner.FlatCopy().WithFixedSize(labelWidth, LineHeight);
                labelBounds.fixedX += subX;
                labelBounds.fixedY = rowY;

                // Static text wraps when it does not fit, and a wrapped caption overlaps the row
                // under it - so it is cut short instead, with the full text on hover.
                string shown = Fit(row.Label, labelFont, labelWidth);
                compo.AddStaticText(shown, labelFont, labelBounds, "cardlabel-" + row.Key);
                if (shown != row.Label && row.Tooltip == null)
                {
                    Tooltip.Add(compo, row.Label, labelBounds, "cardfull-" + row.Key);
                }

                var valueBounds = inner.FlatCopy().WithFixedSize(rowValueWidth, LineHeight);
                valueBounds.fixedX += subX + subWidth - rowValueWidth;
                valueBounds.fixedY = rowY;
                compo.AddStaticText(Fit(row.Value, valueFont, rowValueWidth), valueFont,
                    valueBounds, "cardvalue-" + row.Key);

                if (row.Tooltip != null) Tooltip.Add(compo, row.Tooltip, labelBounds, "cardtip-" + row.Key);
            }
        }

        private static void DrawItem(GuiComposer compo, ElementBounds inner, ItemRow item, double rowY)
        {
            double buttonsWidth = item.Buttons.Count * (ItemButtonSize + 4);
            var labelFont = CairoFont.WhiteSmallText();
            var valueFont = CairoFont.WhiteSmallText().WithColor(item.ValueColor ?? ClaimsColors.Value)
                                     .WithOrientation(EnumTextOrientation.Right);

            double valueWidth = string.IsNullOrEmpty(item.Value) ? 0
                : System.Math.Min(valueFont.GetTextExtents(item.Value).Width + 6, inner.fixedWidth * 0.4);
            double labelWidth = inner.fixedWidth - buttonsWidth - valueWidth - 6;

            // Text sits a little lower than the buttons so both read as one line.
            var labelBounds = inner.FlatCopy().WithFixedSize(labelWidth, LineHeight);
            labelBounds.fixedY = rowY + 2;
            string shown = Fit(item.Label, labelFont, labelWidth);
            compo.AddStaticText(shown, labelFont, labelBounds, UniqueKey(compo, "itemlabel-" + item.Key));
            if (item.Tooltip != null) Tooltip.Add(compo, item.Tooltip, labelBounds, UniqueKey(compo, "itemtip-" + item.Key));
            else if (shown != item.Label) Tooltip.Add(compo, item.Label, labelBounds, UniqueKey(compo, "itemfull-" + item.Key));

            if (valueWidth > 0)
            {
                var valueBounds = inner.FlatCopy().WithFixedSize(valueWidth, LineHeight);
                valueBounds.fixedX += labelWidth + 2;
                valueBounds.fixedY = rowY + 2;
                compo.AddStaticText(item.Value, valueFont, valueBounds, UniqueKey(compo, "itemvalue-" + item.Key));
            }

            for (int b = 0; b < item.Buttons.Count; b++)
            {
                RowButton button = item.Buttons[b];
                var bounds = inner.FlatCopy().WithFixedSize(ItemButtonSize, ItemButtonSize);
                bounds.fixedX += inner.fixedWidth - (item.Buttons.Count - b) * (ItemButtonSize + 4) + 4;
                bounds.fixedY = rowY;
                string key = UniqueKey(compo, "itembtn-" + item.Key + "-" + b);
                compo.AddIconButton(button.Icon, _ => button.OnClick(), bounds, key);
                Tooltip.Add(compo, button.Tooltip, bounds, "tip-" + key);
            }
        }

        /// <summary>Shortens a caption with an ellipsis until it fits the width it was given.</summary>
        public static string Fit(string text, CairoFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text) || font.GetTextExtents(text).Width <= maxWidth) return text;

            string shortened = text;
            while (shortened.Length > 1 && font.GetTextExtents(shortened + "...").Width > maxWidth)
            {
                shortened = shortened.Substring(0, shortened.Length - 1);
            }
            return shortened + "...";
        }
    }
}
