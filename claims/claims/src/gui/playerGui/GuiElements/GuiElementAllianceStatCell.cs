using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>An alliance in the world-wide list: name, leader, member count, founding date.</summary>
    public class GuiElementAllianceStatCell : CANGuiElementCellBase
    {
        private readonly ClientAllianceInfoCellElement alliance;

        protected override int ClickZones => 1;

        public GuiElementAllianceStatCell(ICoreClientAPI capi, ClientAllianceInfoCellElement alliance, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.alliance = alliance;

            string title = alliance.Name;
            if (alliance.Neutral) title += " [" + Lang.Get("claims:neutral") + "]";

            TextExtents extents = CairoFont.WhiteMediumText().GetTextExtents(title);
            var titleBounds = ElementBounds.Fixed(10, 5, extents.Width + 40, 25).WithParent(Bounds);
            AddLine(capi, title, 25, titleBounds);

            var row = titleBounds.BelowCopy(5, 5);
            AddLine(capi, Lang.Get("claims:gui-alliancelist-leader-label") + " " + (alliance.LeaderName ?? ""), 15, row);

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-alliancelist-cities-label") + " " + alliance.CitiesCount, 15, row);

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-city-tab-created") + " "
                + TimeFunctions.getDateFromEpochSeconds(alliance.TimeStampCreated), 15, row);

            Bounds.fixedHeight = 110;
        }

        private void AddLine(ICoreClientAPI capi, string line, int fontSize, ElementBounds bounds)
        {
            richTexts.Add(new GuiElementRichtext(capi,
                VtmlUtil.Richtextify(capi, line, CairoFont.WhiteMediumText().WithFontSize(fontSize)), bounds));
        }

        protected override double MinCellHeight => 110.0;

        /// <summary>Everything visible is drawn by the base from richTexts.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
