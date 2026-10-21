using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A city in the world-wide list: name over one line of facts, the city's own advertisement
    /// under it, and a join button when the city is open.
    /// </summary>
    public class GuiElementCityStatCell : CANGuiElementCellBase
    {
        private readonly ClientCityInfoCellElement cityStatCell;

        /// <summary>Joining is the cell's own button, so the row itself is not clickable and must
        /// not be lit as three columns.</summary>
        protected override bool UseHoverHighlights => false;

        /// <summary>Right edge kept clear for the join button.</summary>
        private const double ButtonColumn = 80;

        private const double PadY = 12;
        private const double TitleHeight = 28;
        private const double LineHeight = 22;
        private const double LineGap = 4;

        /// <summary>A city with nothing to advertise is one line shorter, so the base's 73 would
        /// stretch it back out with empty space.</summary>
        protected override double MinCellHeight => PadY * 2 + TitleHeight + (LineGap + LineHeight) * 2;

        private const string Separator = "  ·  ";

        public GuiElementCityStatCell(ICoreClientAPI capi, ClientCityInfoCellElement cityStatCell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cityStatCell = cityStatCell;

            bool hasInvMsg = cityStatCell.InvMsg.Length > 0;
            double cellHeight = MinCellHeight + (hasInvMsg ? LineGap + LineHeight : 0);

            double textWidth = bounds.fixedWidth - EmblemTextX - ButtonColumn;
            if (textWidth < 120) textWidth = 120;

            // Openness rides the name in its own colour: it is what a player scans the list for, and
            // anywhere further down the row it ends up behind an ellipsis on a narrow window.
            string title = string.Format("{0} {1}{2}{3}", cityStatCell.Name,
                cityStatCell.AllianceName.Length > 0 ? "[" + cityStatCell.AllianceName + "]" : "",
                cityStatCell.Tier == part.structure.CityTier.VILLAGE ? " " + Lang.Get("claims:gui-village-mark") : "",
                cityStatCell.Open ? " <font color=\"#73cc73\">" + Lang.Get("claims:gui-city-open-mark") + "</font>" : "");

            ElementBounds row = ElementBounds.Fixed(EmblemTextX, PadY, textWidth, TitleHeight).WithParent(Bounds);
            AddLine(capi, title, 20, row);

            // Who runs the place. Neutrality belongs here rather than on the name: it matters once
            // you are weighing the city, not while you are scanning the list.
            string mayorLine = Lang.Get("claims:gui-mayor-name", cityStatCell.MayorName);
            if (cityStatCell.Neutral) mayorLine += Separator + Lang.Get("claims:neutral");

            double lineY = PadY + TitleHeight + LineGap;
            row = ElementBounds.Fixed(EmblemTextX, lineY, textWidth, LineHeight).WithParent(Bounds);
            AddLine(capi, Fit(mayorLine, 15, textWidth, out bool mayorCut), 15, row);
            if (mayorCut) AddTooltip(row, mayorLine);

            // Size and age together: three numbers nobody reads one at a time.
            string sizeLine = Lang.Get("claims:gui-city-population", cityStatCell.CitizensAmount)
                            + Separator + Lang.Get("claims:gui-claimed-plots", cityStatCell.ClaimedPlotsAmount)
                            + Separator + Lang.Get("claims:gui-date-created",
                                   TimeFunctions.getDateFromEpochSeconds(cityStatCell.TimeStampCreated));

            lineY += LineHeight + LineGap;
            row = ElementBounds.Fixed(EmblemTextX, lineY, textWidth, LineHeight).WithParent(Bounds);
            AddLine(capi, Fit(sizeLine, 15, textWidth, out bool sizeCut), 15, row, ClaimsColors.Label);
            if (sizeCut) AddTooltip(row, sizeLine);

            // What the mayor wants newcomers to read. It used to hide in a tooltip behind an icon,
            // which is exactly where a player browsing open cities never looks.
            if (hasInvMsg)
            {
                lineY += LineHeight + LineGap;
                var msgRow = ElementBounds
                    .Fixed(EmblemTextX, lineY, textWidth, LineHeight)
                    .WithParent(Bounds);
                AddLine(capi, "«" + Fit(cityStatCell.InvMsg, 15, textWidth - 20, out _) + "»",
                        15, msgRow, ClaimsColors.Section);
                // The line is a preview; the hover is where the long ones stay readable.
                AddTooltip(msgRow, cityStatCell.InvMsg);
            }

            // Resolved by guid from the world-wide emblem cache - the list itself carries no emblem.
            AddEmblemColumn(capi, claims.clientDataStorage?.ClientGetEmblem(cityStatCell.Guid) ?? "", cellHeight);

            // The server refuses a join from a player who already has one, so offering the button to
            // a citizen - the mayor of this very city included - only earns them a refusal in chat.
            var self = claims.clientDataStorage?.clientPlayerInfo;
            bool canJoin = self?.CityInfo == null || self.CityInfo.Name == "";

            if (cityStatCell.Open && canJoin)
            {
                ElementBounds joinBounds = new ElementBounds().WithFixedSize(32, 32);
                joinBounds.fixedX = bounds.fixedWidth - 64;
                joinBounds.fixedY = (cellHeight - 32) / 2;
                bounds.WithChild(joinBounds);
                children.Add(new GuiElementToggleButton(capi, "claims:stairs-goal", "",
                    CairoFont.WhiteDetailText(), (bool t) =>
                {
                    if (t) ClientChat.Send("/c join " + this.cityStatCell.Name);
                }, joinBounds));
                AddTooltip(joinBounds, Lang.Get("claims:gui-city-join-tooltip"));
            }

            Bounds.fixedHeight = cellHeight;
        }

        /// <summary>
        /// Cuts a line down to what fits and marks it with an ellipsis. Rich text wraps instead of
        /// clipping, so an over-long line would otherwise push itself into the row below.
        /// </summary>
        private string Fit(string text, int fontSize, double maxWidth, out bool cut)
        {
            cut = false;
            if (maxWidth <= 0) return text;

            CairoFont font = CairoFont.WhiteMediumText().WithFontSize(fontSize);
            if (font.GetTextExtents(text).Width <= maxWidth) return text;

            cut = true;
            for (int len = text.Length - 1; len > 0; len--)
            {
                string candidate = text.Substring(0, len).TrimEnd() + "…";
                if (font.GetTextExtents(candidate).Width <= maxWidth) return candidate;
            }
            return "…";
        }

        private void AddLine(ICoreClientAPI capi, string line, int fontSize, ElementBounds bounds,
                             double[] color = null, EnumTextOrientation orientation = EnumTextOrientation.Left)
        {
            CairoFont font = CairoFont.WhiteMediumText().WithFontSize(fontSize);
            if (color != null) font = font.WithColor(color);
            if (orientation != EnumTextOrientation.Left) font = font.WithOrientation(orientation);
            richTexts.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, line, font), bounds));
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
