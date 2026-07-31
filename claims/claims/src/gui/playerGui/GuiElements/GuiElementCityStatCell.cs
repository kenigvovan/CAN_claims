using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A city in the world-wide list: name, mayor, size, founding date, plus a join button when the
    /// city is open and an info tooltip when it advertises one.
    /// </summary>
    public class GuiElementCityStatCell : CANGuiElementCellBase
    {
        private readonly ClientCityInfoCellElement cityStatCell;

        /// <summary>Joining and the info hover are the cell's own buttons, so the row itself is not
        /// clickable and must not be lit as three columns.</summary>
        protected override bool UseHoverHighlights => false;

        /// <summary>Right edge kept clear for the join and info buttons.</summary>
        private const double ButtonColumn = 110;

        private const double CellHeight = 105;

        public GuiElementCityStatCell(ICoreClientAPI capi, ClientCityInfoCellElement cityStatCell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cityStatCell = cityStatCell;
            var font = CairoFont.WhiteDetailText();

            string cellName = string.Format("{0} {1}", cityStatCell.Name,
                cityStatCell.AllianceName.Length > 0 ? "[" + cityStatCell.AllianceName + "]" : "");

            double textWidth = bounds.fixedWidth - EmblemTextX - ButtonColumn;
            if (textWidth < 120) textWidth = 120;

            ElementBounds row = ElementBounds.Fixed(EmblemTextX, 8, textWidth, 25).WithParent(Bounds);
            AddLine(capi, cellName, 22, row);

            row = row.BelowCopy(0, 4);
            AddLine(capi, Lang.Get("claims:gui-mayor-name", cityStatCell.MayorName), 15, row);

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-city-population", cityStatCell.CitizensAmount)
                        + " " + Lang.Get("claims:gui-claimed-plots", cityStatCell.ClaimedPlotsAmount), 15, row);

            // Resolved by guid from the world-wide emblem cache - the list itself carries no emblem.
            AddEmblemColumn(capi, claims.clientDataStorage?.ClientGetEmblem(cityStatCell.Guid) ?? "", CellHeight);

            // Both buttons ride the vertical centre of the row, like the arms do.
            double buttonY = (CellHeight - 32) / 2;

            if (cityStatCell.Open)
            {
                ElementBounds joinBounds = new ElementBounds().WithFixedSize(32, 32);
                joinBounds.fixedX = bounds.fixedWidth - 64;
                joinBounds.fixedY = buttonY;
                bounds.WithChild(joinBounds);
                children.Add(new GuiElementToggleButton(capi, "claims:stairs-goal", "", font, (bool t) =>
                {
                    if (t) ClientChat.Send("/c join " + this.cityStatCell.Name);
                }, joinBounds));
            }

            if (cityStatCell.InvMsg.Length > 0)
            {
                ElementBounds infoBounds = new ElementBounds().WithFixedSize(32, 32);
                infoBounds.fixedX = bounds.fixedWidth - 96;
                infoBounds.fixedY = buttonY;
                bounds.WithChild(infoBounds);
                children.Add(new GuiElementToggleButton(capi, "claims:info", "", font, (bool t) => { }, infoBounds));
                children.Add(new GuiElementHoverText(capi, cityStatCell.InvMsg, font, 300, infoBounds));
            }

            row = row.BelowCopy();
            AddLine(capi, Lang.Get("claims:gui-date-created",
                TimeFunctions.getDateFromEpochSeconds(cityStatCell.TimeStampCreated)), 15, row);

            Bounds.fixedHeight = CellHeight;
        }

        private void AddLine(ICoreClientAPI capi, string line, int fontSize, ElementBounds bounds)
        {
            richTexts.Add(new GuiElementRichtext(capi,
                VtmlUtil.Richtextify(capi, line, CairoFont.WhiteMediumText().WithFontSize(fontSize)), bounds));
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
