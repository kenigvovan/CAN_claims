using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// An ongoing conflict: who is fighting whom, since when, the score, and whether a battle is
    /// running right now. One button offers peace, the other opens the details.
    /// </summary>
    public class GuiElementConflictCell : CANGuiElementCellBase
    {
        private const double ButtonSize = 32;
        private const double EdgePadding = 12;

        public ClientConflictCellElement cell;

        private readonly double cellHeight;

        protected override double MinCellHeight => cellHeight;

        /// <summary>Both actions are on buttons now, so the row itself is not a click target.</summary>
        protected override bool UseHoverHighlights => false;

        private static readonly double[] LabelColor = new double[] { 0.70, 0.70, 0.70, 1.0 };
        private static readonly double[] BattleColor = new double[] { 0.95, 0.25, 0.25, 1.0 };

        /// <summary>Edge of a side's arms, and the room the column of two takes from the text.</summary>
        private const double EmblemSize = 24;
        private const double EmblemColumn = EmblemSize + 8;

        /// <summary>Where the text starts - moved right when the row shows the sides' arms.</summary>
        private readonly double textX;

        public GuiElementConflictCell(ICoreClientAPI capi, ClientConflictCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;

            double cellWidth = Bounds.fixedWidth > 0 ? Bounds.fixedWidth : 430;

            // Arms of both sides, stacked at the left edge in name order. The column only appears
            // when at least one side has arms, so other rows keep their full width.
            string firstEmblem = claims.clientDataStorage?.ClientGetEmblem(cell.FirstPartyGuid) ?? "";
            string secondEmblem = claims.clientDataStorage?.ClientGetEmblem(cell.SecondPartyGuid) ?? "";
            bool anyEmblem = firstEmblem.Length > 0 || secondEmblem.Length > 0;
            textX = EdgePadding + (anyEmblem ? EmblemColumn : 0);

            double textWidth = cellWidth - ButtonSize * 2 - EdgePadding * 3 - 8 - (anyEmblem ? EmblemColumn : 0);
            if (textWidth < 120) textWidth = 120;

            double y = 8;

            // Party types matter: a war against a city is not a war against its whole alliance.
            y = AddLine(capi, Lang.Get("claims:gui_conflict_cell_first_line",
                    cell.FirstPartyName + " (" + WarTargetTypeHelper.LangLabel(cell.FirstPartyType) + ")",
                    cell.SecondPartyName + " (" + WarTargetTypeHelper.LangLabel(cell.SecondPartyType) + ")"),
                CairoFont.WhiteMediumText().WithFontSize(17), textWidth, y, 22);

            y = AddLine(capi, Lang.Get("claims:gui_conflict_cell_started_line",
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeStampCreated, true)),
                CairoFont.WhiteDetailText().WithColor(LabelColor), textWidth, y, 19);

            y = AddLine(capi, Lang.Get("claims:gui_conflict_cell_score_line", cell.FirstScore, cell.SecondScore),
                CairoFont.WhiteDetailText(), textWidth, y, 19);

            if (cell.ActiveWarTime)
            {
                y = AddLine(capi, Lang.Get("claims:gui_battle_active"),
                    CairoFont.WhiteDetailText().WithColor(BattleColor), textWidth, y, 19);
            }

            cellHeight = y + 12;
            if (cellHeight < 74) cellHeight = 74;
            Bounds.fixedHeight = cellHeight;

            if (anyEmblem)
            {
                AddEmblem(capi, firstEmblem, 8);
                AddEmblem(capi, secondEmblem, 8 + EmblemSize + 4);
            }

            AddButtons(capi, cellWidth);
        }

        /// <summary>One side's arms at the given height, or an empty slot when that side has none.</summary>
        private void AddEmblem(ICoreClientAPI capi, string emblem, double y)
        {
            if (emblem.Length == 0) return;

            var emblemBounds = ElementBounds.Fixed(EdgePadding, y, EmblemSize, EmblemSize).WithParent(Bounds);
            children.Add(new GuiElementEmblem(capi, emblemBounds, emblem, drawPlaceholder: false, surfaceOrigin: Bounds));
        }

        private double AddLine(ICoreClientAPI capi, string text, CairoFont font, double width, double y, double height)
        {
            var lineBounds = ElementBounds.Fixed(textX, y, width, height).WithParent(Bounds);
            richTexts.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, text, font), lineBounds));
            return y + height;
        }

        private void AddButtons(ICoreClientAPI capi, double cellWidth)
        {
            var font = CairoFont.WhiteDetailText();
            double buttonY = (cellHeight - ButtonSize) / 2;

            var infoBounds = ElementBounds
                .Fixed(cellWidth - EdgePadding - ButtonSize, buttonY, ButtonSize, ButtonSize)
                .WithParent(Bounds);
            children.Add(new GuiElementToggleButton(capi, "claims:info", "", font, (bool t) =>
            {
                if (!t) return;
                claims.CANCityGui.State.DialogArgs.Selected = this.cell.Guid;
                claims.CANCityGui.State.SelectedTab = EnumSelectedTab.ConflictInfoPage;
                claims.CANCityGui.BuildMainWindow();
            }, infoBounds));
            AddTooltip(infoBounds, Lang.Get("claims:gui_conflict_info_btn"));

            var peaceBounds = ElementBounds
                .Fixed(cellWidth - EdgePadding - ButtonSize * 2 - 8, buttonY, ButtonSize, ButtonSize)
                .WithParent(Bounds);
            children.Add(new GuiElementToggleButton(capi, "claims:peace-dove", "", font, (bool t) =>
            {
                if (t) OfferPeace();
            }, peaceBounds));
            AddTooltip(peaceBounds, Lang.Get("claims:gui_conflict_peace_offer_btn"));
        }

        private void OfferPeace()
        {
            // An independent city fights under its own name, so an alliance must not be assumed here.
            var info = claims.clientDataStorage.clientPlayerInfo;
            string ourName = info.AllianceInfo?.Name ?? info.CityInfo?.Name ?? "";

            string target = cell.FirstPartyName.Equals(ourName)
                ? cell.SecondPartyName
                : cell.FirstPartyName;

            claims.CANCityGui.OpenDialog(EnumUpperWindowSelectedState.ALLIANCE_SEND_PEACE_OFFER_CONFIRM, args =>
            {
                args.Selected = this.cell.Guid;
                args.SelectedSecond = target;
            });
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
