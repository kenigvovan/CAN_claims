using Cairo;
using claims.src.gui.playerGui.Pages;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// One row of the war settings list: a group heading, or a setting with its current value.
    ///
    /// Clickable, unlike the plain rich text this replaces - an admin had to read a key off the list
    /// and type it into the edit field by hand, with no feedback if they mistyped it.
    /// </summary>
    public class GuiElementWarCfgCell : CANGuiElementCellBase
    {
        private const double EdgePadding = 10;
        private const double RowHeight = 26;
        private const double GroupHeight = 30;

        private readonly bool isGroup;

        /// <summary>The row is one line; there are no columns down the right edge to light up.</summary>
        protected override int ClickZones => 1;

        protected override double MinCellHeight => isGroup ? GroupHeight : RowHeight;

        protected override bool ShowModifyIcons => false;

        public GuiElementWarCfgCell(ICoreClientAPI capi, WarCfgRow row, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.isGroup = row.Kind == EnumCfgKind.Group;

            string label = Lang.Get(row.LabelLangKey);
            double width = Bounds.fixedWidth > 0 ? Bounds.fixedWidth : 430;

            if (isGroup)
            {
                var titleBounds = ElementBounds.Fixed(EdgePadding, 5, width - EdgePadding * 2, 22).WithParent(Bounds);
                AddText(label, titleBounds, ClaimsColors.Section, 17);
            }
            else
            {
                // Name on the left, value hard against the right edge, so the values line up down the
                // list instead of starting wherever each name happens to end.
                const double valueWidth = 90;

                var nameBounds = ElementBounds
                    .Fixed(EdgePadding, 4, width - valueWidth - EdgePadding * 2, 20).WithParent(Bounds);
                AddText(label, nameBounds, ClaimsColors.Label, 14);

                var valueBounds = ElementBounds
                    .Fixed(width - valueWidth - EdgePadding, 4, valueWidth, 20).WithParent(Bounds);
                AddText(DisplayValue(row), valueBounds, ColorFor(row), 14);

                // The key is what /cadmin setcfg takes, and it is not otherwise visible anywhere.
                AddTooltip(ElementBounds.Fixed(0, 0, width, RowHeight).WithParent(Bounds),
                    row.CfgKey + "  -  " + Lang.Get("claims:gui-admin-warcfg-click-hint"));
            }

            Bounds.fixedHeight = MinCellHeight;
        }

        /// <summary>A switch reads as on or off at a glance; numbers are just values.</summary>
        private static double[] ColorFor(WarCfgRow row)
        {
            if (row.Kind != EnumCfgKind.Flag) return ClaimsColors.Value;
            return row.GetFlag() ? ClaimsColors.Success : ClaimsColors.Danger;
        }

        /// <summary>What the list shows - the same as the editable value unless the row shortens it.</summary>
        private static string DisplayValue(WarCfgRow row)
        {
            return row.GetDisplay != null ? row.GetDisplay() : CurrentValue(row);
        }

        /// <summary>What clicking the row loads into the edit field: valid setcfg input.</summary>
        public static string CurrentValue(WarCfgRow row)
        {
            switch (row.Kind)
            {
                case EnumCfgKind.Flag: return row.GetFlag() ? "on" : "off";
                case EnumCfgKind.Int: return row.GetInt().ToString(System.Globalization.CultureInfo.InvariantCulture);
                case EnumCfgKind.Double: return row.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                case EnumCfgKind.Text:
                case EnumCfgKind.Choice:
                case EnumCfgKind.MultiChoice:
                    return row.GetText() ?? "";
                default: return "";
            }
        }

        /// <summary>Everything visible is drawn by the base from richTexts.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
