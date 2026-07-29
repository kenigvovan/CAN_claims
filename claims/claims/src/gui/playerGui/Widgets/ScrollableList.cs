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

            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, width, 30);
            ElementBounds listArea = ElementBounds
                .Fixed(0, 0, width, gui.mainBounds.fixedHeight - opts.HeightReserve)
                .FixedUnder(topTextBounds, 5);

            ElementBounds titleBounds = outer.BelowCopy(0, opts.TitleGap);
            titleBounds.fixedHeight -= opts.TitleHeightShrink;
            titleBounds.WithAlignment(EnumDialogArea.CenterTop);

            ElementBounds clippingBounds = listArea.ForkBoundingParent();
            ElementBounds insetBounds = listArea.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
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
