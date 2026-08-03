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
    /// One party we may go to war with: why we may, what still stands in the way, what it costs, and
    /// the button to declare it when nothing blocks it.
    /// </summary>
    public class GuiElementCasusBelliCell : CANGuiElementCellBase
    {
        private const double ButtonSize = 32;
        private const double EdgePadding = 12;

        public ClientCasusBelliCellElement cell;

        private readonly double cellHeight;

        protected override double MinCellHeight => cellHeight;

        /// <summary>Declaring war happens on its own button, never by clicking the row.</summary>
        protected override bool UseHoverHighlights => false;

        private static readonly double[] LabelColor = new double[] { 0.70, 0.70, 0.70, 1.0 };
        private static readonly double[] WarningColor = new double[] { 0.93, 0.75, 0.30, 1.0 };
        private static readonly double[] DangerColor = new double[] { 0.90, 0.35, 0.30, 1.0 };

        public GuiElementCasusBelliCell(ICoreClientAPI capi, ClientCasusBelliCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;

            long now = TimeFunctions.getEpochSeconds();
            bool freeWar = cell.Kind == CasusBelliKind.FreeWar;

            double cellWidth = Bounds.fixedWidth > 0 ? Bounds.fixedWidth : 430;
            double textWidth = cellWidth - ButtonSize - EdgePadding * 3;
            if (textWidth < 120) textWidth = 120;

            double y = 8;

            // A free war is the strongest reason there is, so its target is called out.
            CairoFont targetFont = CairoFont.WhiteMediumText().WithFontSize(17);
            if (freeWar) targetFont = targetFont.WithColor(WarningColor);

            y = AddLine(capi, StringFunctions.replaceUnderscore(cell.TargetName)
                    + " (" + WarTargetTypeHelper.LangLabel(cell.TargetType) + ")",
                targetFont, textWidth, y, 22);

            if (cell.Kind != CasusBelliKind.None)
            {
                string reason = Lang.Get(ReasonLangKey(cell.Kind));
                y = AddLine(capi, cell.ExpiresAt > now
                        ? Lang.Get("claims:gui_casus_belli_line_timed", reason,
                            StringFunctions.FormatDuration(cell.ExpiresAt - now))
                        : reason,
                    CairoFont.WhiteDetailText().WithColor(LabelColor), textWidth, y, 19);
            }

            // Blockers. A free war ignores the cooldown, so say so rather than scaring the player off.
            if (cell.CooldownUntil > now)
            {
                y = AddLine(capi, Lang.Get(freeWar ? "claims:gui_war_cooldown_waived" : "claims:gui_war_cooldown_left",
                        StringFunctions.FormatDuration(cell.CooldownUntil - now)),
                    CairoFont.WhiteDetailText().WithColor(freeWar ? LabelColor : DangerColor), textWidth, y, 19);
            }
            if (cell.UnionBreakUntil > now)
            {
                y = AddLine(capi, Lang.Get("claims:gui_war_union_break_left",
                        StringFunctions.FormatDuration(cell.UnionBreakUntil - now)),
                    CairoFont.WhiteDetailText().WithColor(DangerColor), textWidth, y, 19);
            }
            if (cell.PactUntil > now)
            {
                bool canBreak = cell.Kind != CasusBelliKind.None;
                y = AddLine(capi, Lang.Get(canBreak ? "claims:gui_war_pact_breakable" : "claims:gui_war_pact_left",
                        StringFunctions.FormatDuration(cell.PactUntil - now)),
                    CairoFont.WhiteDetailText().WithColor(canBreak ? LabelColor : DangerColor), textWidth, y, 19);
            }

            double cost = freeWar ? 0 : claims.config.WAR_DECLARATION_COST;
            if (cost > 0)
            {
                y = AddLine(capi, Lang.Get("claims:gui_war_declaration_cost", cost),
                    CairoFont.WhiteDetailText().WithColor(LabelColor), textWidth, y, 19);
            }

            cellHeight = y + 12;
            if (cellHeight < 56) cellHeight = 56;
            Bounds.fixedHeight = cellHeight;

            if (cell.CanDeclareNow(now))
            {
                var declareBounds = ElementBounds
                    .Fixed(cellWidth - EdgePadding - ButtonSize, (cellHeight - ButtonSize) / 2, ButtonSize, ButtonSize)
                    .WithParent(Bounds);
                children.Add(new GuiElementToggleButton(capi, "claims:sword-brandish", "", CairoFont.WhiteDetailText(),
                    (bool t) =>
                    {
                        if (t) Declare();
                    }, declareBounds));
                AddTooltip(declareBounds, Lang.Get("claims:gui_war_declare_btn"));
            }
        }

        private static string ReasonLangKey(CasusBelliKind kind)
        {
            switch (kind)
            {
                case CasusBelliKind.FreeWar: return "claims:cb_reason_freewar";
                case CasusBelliKind.AllyAtWar: return "claims:cb_reason_ally";
                default: return "claims:cb_reason_grievance";
            }
        }

        private void Declare()
        {
            bool hasAlliance = claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null;
            ClientChat.Send((hasAlliance ? "/a conflict declare " : "/city war declare ")
                + (cell.TargetType == WarTargetType.Alliance ? "alliance:" : "city:") + cell.TargetName);
        }

        private double AddLine(ICoreClientAPI capi, string text, CairoFont font, double width, double y, double height)
        {
            var lineBounds = ElementBounds.Fixed(EdgePadding, y, width, height).WithParent(Bounds);
            richTexts.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, text, font), lineBounds));
            return y + height;
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
