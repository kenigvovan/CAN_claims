using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A city rank: its name, a plus button to grant it, and one button per holder that strips it.
    /// </summary>
    public class GuiElementCityRanks : CANGuiElementCellBase
    {
        private readonly CityRankCellElement rankCell;

        /// <summary>
        /// The whole row is one target. Three separate lit columns implied three separate actions,
        /// but every one of them opens the same rank page.
        /// </summary>
        protected override int ClickZones => 1;

        public GuiElementCityRanks(ICoreClientAPI capi, CityRankCellElement rankCell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.rankCell = rankCell;
            this.text = rankCell.Name;

            double rowHeight = 35.0;
            var font = CairoFont.WhiteDetailText();

            TextExtents extents = CairoFont.WhiteMediumText().GetTextExtents(rankCell.Name);
            var labelBounds = ElementBounds.Fixed(0.0, 0.0, extents.Width + 10, rowHeight).WithParent(Bounds);
            richTexts.Add(new GuiElementRichtext(capi,
                VtmlUtil.Richtextify(capi, rankCell.Name, CairoFont.WhiteMediumText()), labelBounds));

            // No buttons on the row itself: clicking anywhere opens the rank's page, and everything
            // that can be done to a rank - granting it, its permissions, deleting it - lives there.
            OnMouseDownOnCellLeft = _ => OpenRankPage();
            AddZoneTooltip(HighlightZone.Left, Lang.Get("claims:gui-info-rank-tooltip"));

            // One button per holder; clicking it asks to strip the rank from that player.
            double offsetY = 35;
            double offsetX = 0;
            foreach (var citizen in rankCell.Citizens)
            {
                extents = Font.GetTextExtents(citizen);
                if ((extents.Width + 40 + offsetX + 20) > bounds.fixedWidth + Bounds.fixedX)
                {
                    offsetX = 0;
                    offsetY += 30;
                }

                var holderBounds = ElementBounds.Fixed(offsetX, offsetY, extents.Width + 40, 25).WithParent(Bounds);
                string holder = citizen;
                children.Add(new GuiElementButtonWithAdditionalText(capi, holder, this.Font, this.Font, new ActionConsumable(() =>
                {
                    claims.CANCityGui.OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_REMOVE_CONFIRM, args =>
                    {
                        args.First = this.rankCell.Name;
                        args.Second = holder;
                    });
                    return true;
                }), holderBounds));

                offsetX += extents.Width + 45 + 20;
            }

            if (offsetY > 30)
            {
                Bounds.fixedHeight = offsetY + 60;
            }
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }

        private void OpenRankPage()
        {
            claims.CANCityGui.State.DialogArgs.Selected = rankCell.Name;
            claims.CANCityGui.State.SelectedTab = EnumSelectedTab.RankInfoPage;
            claims.CANCityGui.BuildMainWindow();
        }
    }
}
