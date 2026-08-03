using System.Collections.Generic;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>Knobs of a scrollable list that actually differ between pages.</summary>
    public sealed class ScrollableListOptions
    {
        /// <summary>
        /// Vertical space left for whatever sits above the list. Pages used bare -230 / -250 / -300
        /// against the dialog height.
        /// </summary>
        public double HeightReserve = 300;

        /// <summary>Element key of the cell list. The scrollbar gets this key plus "-scrollbar".</summary>
        public string Key = "listcells";

        /// <summary>Null falls back to centered white medium text.</summary>
        public CairoFont TitleFont;

        /// <summary>
        /// Whether the list container starts one row below the title instead of on top of it.
        /// Pages disagreed on this, so it stays explicit.
        /// </summary>
        public bool ContainerBelowTitle;

        /// <summary>How much shorter than its anchor the title row is. Every page but one used 50.</summary>
        public double TitleHeightShrink = 50;

        /// <summary>Extra gap between the anchor and the title row.</summary>
        public double TitleGap = 0;

        /// <summary>
        /// Explicit parent for the clipped list. Null means the title row, which is what pages with
        /// a list heading want; the conflict info page instead titles itself and parents the list
        /// under its own cursor.
        /// </summary>
        public ElementBounds Container;
    }

    /// <summary>
    /// The bounds a built list is made of, handed back so a page can hang extra controls off them
    /// (the "add group" / "remove group" buttons sit below the inset) and apply the scrollbar
    /// heights once the composer has been composed.
    /// </summary>
    public sealed class ScrollableListLayout
    {
        public ElementBounds Title;
        public ElementBounds Inset;
        public ElementBounds Clip;
        public ElementBounds List;
        public string ListKey;
        public string ScrollbarKey;

        /// <summary>
        /// Must run after GuiComposer.Compose(): the scrollbar needs the final laid-out heights to
        /// know how far it may travel.
        /// </summary>
        public void ApplyScrollbarHeights(GuiComposer compo)
        {
            compo.GetScrollbar(ScrollbarKey).SetHeights((float)Clip.fixedHeight, (float)List.fixedHeight);
        }
    }

    /// <summary>
    /// The clipped, scrollable cell list every page builds. This was copied out by hand eleven
    /// times - same eleven ElementBounds, same BeginClip/AddInset/AddCellList/AddVerticalScrollbar
    /// chain, same SetHeights afterwards - differing only in the cell type, the list key and how
    /// much vertical space to leave above.
    /// </summary>
    public static class ScrollableList
    {
        /// <summary>Where the title row sits inside the container, and how tall it is.</summary>
        private const double TitleRowY = 40;
        private const double TitleRowHeight = 30;

        /// <summary>Gap between the title row and the clipped area, and the inset drawn around it.</summary>
        private const double ListGap = 5;
        private const double InsetGrow = 6;

        /// <summary>
        /// Vertical space the whole thing takes below its anchor, the clipped area itself excluded.
        /// A page stacking a second list under the first had to guess this from the first list's
        /// inset - bounds that belong to the first list's clip, so the guess put the second list off
        /// the bottom of the window.
        /// </summary>
        /// <param name="hasTitle">Whether the list is given a heading. A list without one reserves
        /// no room for it - a titleless list used to leave 75 empty pixels above its frame, with the
        /// scrollbar starting below them.</param>
        public static double Overhead(ElementBounds anchor, ScrollableListOptions opts = null, bool hasTitle = true)
        {
            opts = opts ?? new ScrollableListOptions();

            // An explicit container is where the list starts, full stop - the title row the anchor
            // would otherwise push it past belongs to the default layout only.
            double y = 0;
            if (opts.Container == null)
            {
                y = anchor.fixedHeight + opts.TitleGap;
                if (opts.ContainerBelowTitle) y += anchor.fixedHeight - opts.TitleHeightShrink;
            }

            return y + TitleRowOffset(hasTitle) - InsetGrow / 2 + InsetGrow;
        }

        /// <summary>Where the clipped area starts inside the container.</summary>
        private static double TitleRowOffset(bool hasTitle)
            => hasTitle ? TitleRowY + TitleRowHeight + ListGap : 0;

        /// <summary>
        /// The <see cref="ScrollableListOptions.HeightReserve"/> that gives the clipped area exactly
        /// <paramref name="listHeight"/>. Pages used to reach the same end by trying reserve numbers.
        /// </summary>
        public static double ReserveFor(CANClaimsGui gui, double listHeight)
            => gui.mainBounds.fixedHeight - listHeight;

        /// <summary>
        /// Adds the list to the page composer. Does not compose: the caller owns that, so it can
        /// keep adding elements afterwards.
        /// </summary>
        public static ScrollableListLayout Add<TCell>(
            CANClaimsGui gui,
            ElementBounds anchor,
            string title,
            IEnumerable<TCell> items,
            OnRequireCell<TCell> cellFactory,
            ScrollableListOptions opts = null)
        {
            opts = opts ?? new ScrollableListOptions();
            var compo = gui.SingleComposer;

            ElementBounds outer = anchor.FlatCopy();
            double width = outer.fixedWidth - 30;

            // Without a heading the list starts at the top of its container: the spacer row used to
            // be added regardless, leaving an empty band above the frame that the scrollbar tracked.
            ElementBounds listArea = ElementBounds
                .Fixed(0, TitleRowOffset(title != null), width, gui.mainBounds.fixedHeight - opts.HeightReserve);

            ElementBounds titleBounds = outer.BelowCopy(0, opts.TitleGap);
            titleBounds.fixedHeight -= opts.TitleHeightShrink;
            titleBounds.WithAlignment(EnumDialogArea.CenterTop);

            ElementBounds clippingBounds = listArea.ForkBoundingParent();
            ElementBounds insetBounds = listArea.FlatCopy().FixedGrow(InsetGrow).WithFixedOffset(-InsetGrow / 2, -InsetGrow / 2);
            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(listArea.fixedWidth + 7).WithFixedWidth(20);

            if (title != null)
            {
                compo.AddStaticText(title,
                    opts.TitleFont ?? CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                    titleBounds);
            }

            // Kept as locals. These used to be four public fields on CANClaimsGui that pages assigned
            // inline (AddCellList(gui.listRanksBounds = ...)) and read back for SetHeights, which meant
            // every list on every page shared the same two slots.
            ElementBounds clipInner = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);
            ElementBounds listBounds = clipInner.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0);

            string listKey = opts.Key;
            string scrollbarKey = listKey + "-scrollbar";

            ElementBounds container = opts.Container
                ?? (opts.ContainerBelowTitle ? titleBounds.BelowCopy() : titleBounds);

            compo.BeginChildElements(container)
                .BeginClip(clippingBounds)
                .AddInset(insetBounds, 3)
                .AddCellList(listBounds, cellFactory, items, listKey)
                .EndClip()
                .AddVerticalScrollbar((float value) =>
                {
                    ElementBounds bounds = compo.GetCellList<TCell>(listKey).Bounds;
                    bounds.fixedY = 0f - value;
                    bounds.CalcWorldBounds();
                }, scrollbarBounds, scrollbarKey)
                .EndChildElements();

            compo.GetCellList<TCell>(listKey).BeforeCalcBounds();

            return new ScrollableListLayout
            {
                Title = titleBounds,
                Inset = insetBounds,
                Clip = clipInner,
                List = listBounds,
                ListKey = listKey,
                ScrollbarKey = scrollbarKey
            };
        }
    }
}
