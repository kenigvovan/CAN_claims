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

        private const double CellHeight = 110;

        public GuiElementAllianceStatCell(ICoreClientAPI capi, ClientAllianceInfoCellElement alliance, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.alliance = alliance;

            string title = alliance.Name;
            if (alliance.Neutral) title += " [" + Lang.Get("claims:neutral") + "]";

            // Same shape as a city row: arms in a reserved column at the left, text beside them.
            TextExtents extents = CairoFont.WhiteMediumText().GetTextExtents(title);
            var titleBounds = ElementBounds.Fixed(EmblemTextX, 8, extents.Width + 40, 25).WithParent(Bounds);
            AddLine(capi, title, 22, titleBounds);

            var row = titleBounds.BelowCopy(0, 4);
            AddLine(capi, Lang.Get("claims:gui-alliancelist-leader-label") + " " + (alliance.LeaderName ?? ""), 15, row);

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-alliancelist-cities-label") + " " + alliance.CitiesCount, 15, row);

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-city-tab-created") + " "
                + TimeFunctions.getDateFromEpochSeconds(alliance.TimeStampCreated), 15, row);

            AddEmblemColumn(capi, claims.clientDataStorage?.ClientGetEmblem(alliance.Guid) ?? "", CellHeight);

            Bounds.fixedHeight = CellHeight;
        }

        private void AddLine(ICoreClientAPI capi, string line, int fontSize, ElementBounds bounds)
        {
            richTexts.Add(new GuiElementRichtext(capi,
                VtmlUtil.Richtextify(capi, line, CairoFont.WhiteMediumText().WithFontSize(fontSize)), bounds));
        }

        protected override double MinCellHeight => CellHeight;

        /// <summary>Everything visible is drawn by the base from richTexts.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
