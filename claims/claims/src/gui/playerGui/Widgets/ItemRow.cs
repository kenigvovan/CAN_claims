using System;
using System.Collections.Generic;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>A line of a <see cref="Card.Items"/> list: a name, an optional value, the buttons acting on that one entry.</summary>
    public sealed class ItemRow
    {
        public string Label;
        public string Value = "";
        /// <summary>Null falls back to the shared accent colour.</summary>
        public double[] ValueColor;
        public List<RowButton> Buttons = new List<RowButton>();
        /// <summary>Shown over the label; without it a cut label shows its full text.</summary>
        public string Tooltip;
        /// <summary>Suffix of the element keys, unique within the page.</summary>
        public string Key;
    }

    /// <summary>A small icon button at the right end of an <see cref="ItemRow"/>.</summary>
    public sealed class RowButton
    {
        public string Icon;
        public Action OnClick;
        /// <summary>Already translated.</summary>
        public string Tooltip;

        public RowButton(string icon, Action onClick, string tooltip)
        {
            Icon = icon;
            OnClick = onClick;
            Tooltip = tooltip;
        }
    }
}
