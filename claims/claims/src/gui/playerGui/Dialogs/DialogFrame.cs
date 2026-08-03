using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// The secondary window being built: its composer, its background, and a cursor that walks down
    /// it one row at a time. Every branch of the old BuildUpperWindow open-coded
    /// "var b = prev.BelowCopy(0, 15); bgBounds.WithChildren(b);" - about 120 times.
    /// </summary>
    public sealed class DialogLayout
    {
        public GuiComposer Compo;
        public ElementBounds Bg;
        public ElementBounds Row;

        /// <summary>
        /// Grows the current row to hold <paramref name="text"/> in full.
        ///
        /// A row is 30 tall, which is one line: a prompt that wraps to three drew over whatever came
        /// next, because the following row is measured from this one's height. Only ever grows - a
        /// short prompt keeps the row height the rest of the layout is spaced by.
        /// </summary>
        public void FitRowToText(string text, CairoFont font)
        {
            if (string.IsNullOrEmpty(text)) return;
            double width = Row.fixedWidth > 0 ? Row.fixedWidth : 180;
            // The engine measures in rendered pixels; rows are in unscaled ones, as AutoHeight does.
            double height = claims.capi.Gui.Text.GetMultilineTextHeight(font, text, width)
                            / RuntimeEnv.GUIScale;
            if (height > Row.fixedHeight) Row.fixedHeight = height;
        }

        /// <summary>Next row down, already registered as a child of the background.</summary>
        public ElementBounds NextRow(double gap = 15, double? width = null, double? height = null)
        {
            ElementBounds next = Row.BelowCopy(0, gap);
            if (width.HasValue) next.WithFixedWidth(width.Value);
            if (height.HasValue) next.WithFixedHeight(height.Value);
            Bg.WithChildren(next);
            Row = next;
            return next;
        }

        /// <summary>
        /// A row split into two halves side by side, for the confirm/decline button pairs.
        /// </summary>
        public ElementBounds SplitRow(out ElementBounds right, double gap = 15)
        {
            ElementBounds left = NextRow(gap);
            left.fixedWidth /= 2;
            right = left.RightCopy(0, 0);
            Bg.WithChildren(right);
            return left;
        }
    }

    internal static class DialogFrame
    {
        public const string ComposerKey = "canclaimsgui-upper";

        /// <summary>
        /// Builds the window that sits to the right of the main dialog and returns a cursor into it.
        /// </summary>
        public static DialogLayout Create(CANClaimsGui gui, double width = 235)
        {
            ElementBounds leftDlgBounds = gui.Composers["canclaimsgui"].Bounds;
            double extraHeight = leftDlgBounds.InnerHeight / RuntimeEnv.GUIScale + 10.0;

            ElementBounds bgBounds = ElementBounds.Fixed(0.0, 0.0, width,
                leftDlgBounds.InnerHeight / RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + extraHeight)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);

            ElementBounds dialogBounds = bgBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0)
                .WithAlignment(EnumDialogArea.None)
                .WithFixedAlignmentOffset((leftDlgBounds.renderX + leftDlgBounds.OuterWidth + 10.0) / RuntimeEnv.GUIScale,
                                          leftDlgBounds.renderY / RuntimeEnv.GUIScale);

            bgBounds.BothSizing = ElementSizing.FitToChildren;

            dialogBounds.fixedX += leftDlgBounds.fixedWidth + 20;
            dialogBounds.fixedY = leftDlgBounds.absFixedY;
            dialogBounds.BothSizing = ElementSizing.FitToChildren;
            dialogBounds.WithChild(bgBounds);

            ElementBounds textBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 0, 0);
            bgBounds.WithChildren(textBounds);

            var compo = claims.capi.Gui.CreateCompo(ComposerKey, dialogBounds)
                                    .AddShadedDialogBG(bgBounds, false, 5.0, 0.75f);

            ElementBounds firstRow = textBounds.CopyOffsetedSibling().WithFixedSize(180, 30);
            bgBounds.WithChildren(firstRow);
            // No inset behind the first row: it framed nothing but the prompt text. The original
            // added one, but to the main window's composer at this window's coordinates, so it was
            // never visible here in the first place.
            firstRow.fixedY += 20;

            return new DialogLayout { Compo = compo, Bg = bgBounds, Row = firstRow };
        }
    }
}
