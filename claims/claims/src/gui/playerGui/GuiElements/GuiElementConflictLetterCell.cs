using System.Linq;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A letter waiting for an answer: a declaration of war, a peace offer, a non-aggression pact,
    /// an ultimatum or a cession confirmation. Says what it is and what accepting it would cost,
    /// then offers the two answers that letter actually allows.
    /// </summary>
    public class GuiElementConflictLetterCell : CANGuiElementCellBase
    {
        private const double ButtonSize = 32;
        private const double EdgePadding = 12;

        public ClientConflictLetterCellElement cell;

        protected override double MinCellHeight => cellHeight;

        /// <summary>All answers are on the buttons, so a lit row would only promise a click it has not got.</summary>
        protected override bool UseHoverHighlights => false;

        private readonly double cellHeight;

        public GuiElementConflictLetterCell(ICoreClientAPI capi, ClientConflictLetterCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;

            double cellWidth = Bounds.fixedWidth > 0 ? Bounds.fixedWidth : 430;
            double textWidth = cellWidth - ButtonSize * 2 - EdgePadding * 3 - 8;
            if (textWidth < 120) textWidth = 120;

            double y = 8;

            // What the letter is. Without it a pact offer, an ultimatum and a declaration of war
            // are three identical rows.
            y = AddLine(capi, PurposeText(), PurposeFont(), textWidth, y, 22);

            y = AddLine(capi, Lang.Get("claims:gui-conflict-letter-from",
                    cell.From, WarTargetTypeHelper.LangLabel(cell.FromType)),
                CairoFont.WhiteDetailText(), textWidth, y, 19);
            y = AddLine(capi, Lang.Get("claims:gui-conflict-letter-to",
                    cell.To, WarTargetTypeHelper.LangLabel(cell.ToType)),
                CairoFont.WhiteDetailText(), textWidth, y, 19);

            y = AddLine(capi, Lang.Get("claims:gui-conflict-exp-date",
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeStampExpire, true)),
                CairoFont.WhiteDetailText(), textWidth, y, 19);

            // Matters most on an ultimatum, where saying nothing counts as a refusal.
            long secondsLeft = cell.TimeStampExpire - TimeFunctions.getEpochSeconds();
            if (secondsLeft > 0)
            {
                y = AddLine(capi, Lang.Get("claims:gui-conflict-time-left", StringFunctions.FormatDuration(secondsLeft)),
                    CairoFont.WhiteDetailText().WithColor(LabelColor), textWidth, y, 19);
            }

            if (cell.TermType != PeaceTermType.None)
            {
                y = AddLine(capi, Lang.Get("claims:gui-conflict-letter-terms", TermText(capi)),
                    CairoFont.WhiteDetailText().WithColor(WarningColor), textWidth, y, 19);
            }

            cellHeight = y + 12;
            if (cellHeight < 74) cellHeight = 74;
            Bounds.fixedHeight = cellHeight;

            AddAnswerButtons(capi, cellWidth);
        }

        // ---- contents ----

        private static readonly double[] WarningColor = new double[] { 0.93, 0.75, 0.30, 1.0 };
        private static readonly double[] DangerColor = new double[] { 0.90, 0.35, 0.30, 1.0 };
        private static readonly double[] PeacefulColor = new double[] { 0.45, 0.85, 0.50, 1.0 };
        private static readonly double[] LabelColor = new double[] { 0.70, 0.70, 0.70, 1.0 };

        private CairoFont PurposeFont()
        {
            double[] color;
            switch (cell.Purpose)
            {
                case LetterPurpose.NON_AGGRESSION:
                case LetterPurpose.END_CONFLICT:
                    color = PeacefulColor;
                    break;
                case LetterPurpose.ULTIMATUM:
                case LetterPurpose.CESSION_CONFIRM:
                    color = WarningColor;
                    break;
                default:
                    color = DangerColor;
                    break;
            }
            return CairoFont.WhiteMediumText().WithFontSize(18).WithColor(color);
        }

        /// <summary>Headline of a letter: what accepting it would actually mean.</summary>
        private string PurposeText()
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.NON_AGGRESSION:
                    return cell.NapDays > 0
                        ? Lang.Get("claims:gui_letter_purpose_nap_days", cell.NapDays)
                        : Lang.Get("claims:gui_letter_purpose_nap");
                case LetterPurpose.ULTIMATUM:
                    return Lang.Get("claims:gui_letter_purpose_ultimatum");
                case LetterPurpose.CESSION_CONFIRM:
                    return Lang.Get("claims:gui_letter_purpose_cession_confirm");
                case LetterPurpose.END_CONFLICT:
                    return Lang.Get("claims:gui_letter_purpose_peace");
                default:
                    return Lang.Get("claims:gui_letter_purpose_war");
            }
        }

        private string TermText(ICoreClientAPI capi)
        {
            switch (cell.TermType)
            {
                case PeaceTermType.Reparations:
                    return Lang.Get("claims:gui_peace_term_reparations_value", cell.TermAmount);
                case PeaceTermType.Vassalage:
                    return Lang.Get("claims:gui_peace_term_vassalage");
                case PeaceTermType.Cession:
                    if (!cell.HasCededPlot) return Lang.Get("claims:gui_peace_term_cession");
                    // The coordinates the player sees on the map, not the raw world ones.
                    var local = PosFunctions.TranslateCoordsToLocalVec3d(capi,
                        new BlockPos(cell.CededPlotBlockX, 0, cell.CededPlotBlockZ));
                    return Lang.Get("claims:gui_peace_term_cession_at", (int)local.X, (int)local.Z);
                default:
                    return Lang.Get("claims:gui_peace_term_none");
            }
        }

        private double AddLine(ICoreClientAPI capi, string text, CairoFont font, double width, double y, double height)
        {
            var lineBounds = ElementBounds.Fixed(EdgePadding, y, width, height).WithParent(Bounds);
            richTexts.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, text, font), lineBounds));
            return y + height;
        }

        // ---- answers ----

        /// <summary>Whether the guid names a party we speak for: our alliance, or our city.</summary>
        private static bool IsOurParty(string guid)
        {
            var info = claims.clientDataStorage.clientPlayerInfo;
            if (info.AllianceInfo?.Guid?.Equals(guid) ?? false) return true;
            if (info.CityInfo?.Guid?.Equals(guid) ?? false) return true;
            return false;
        }

        /// <summary>An independent mayor answers through /city war, an alliance through /a conflict.</summary>
        private static string CmdPrefix =>
            claims.clientDataStorage.clientPlayerInfo.AllianceInfo != null ? "/a conflict " : "/city war ";

        private void AddAnswerButtons(ICoreClientAPI capi, double cellWidth)
        {
            bool isSender = IsOurParty(cell.FromGuid);
            bool isRecipient = IsOurParty(cell.ToGuid);

            // A pact offer, an ultimatum and a cession confirm are answered by the recipient only:
            // the sender has nothing to take back.
            bool recipientOnly = cell.Purpose == LetterPurpose.NON_AGGRESSION
                              || cell.Purpose == LetterPurpose.ULTIMATUM
                              || cell.Purpose == LetterPurpose.CESSION_CONFIRM;

            var font = CairoFont.WhiteDetailText();
            double buttonY = (cellHeight - ButtonSize) / 2;
            double x = cellWidth - EdgePadding - ButtonSize;

            if (isRecipient)
            {
                var acceptBounds = ElementBounds.Fixed(x, buttonY, ButtonSize, ButtonSize).WithParent(Bounds);
                children.Add(new GuiElementToggleButton(capi, AcceptIcon(), "", font, (bool t) =>
                {
                    if (t) Answer(AcceptCommand());
                }, acceptBounds));
                AddTooltip(acceptBounds, AcceptTooltip());

                x -= ButtonSize + 8;
            }

            if (!recipientOnly || isRecipient)
            {
                var denyBounds = ElementBounds.Fixed(x, buttonY, ButtonSize, ButtonSize).WithParent(Bounds);
                children.Add(new GuiElementToggleButton(capi, DenyIcon(), "", font, (bool t) =>
                {
                    if (t) Answer(DenyCommand(isSender));
                }, denyBounds));
                AddTooltip(denyBounds, DenyTooltip(isSender));
            }
        }

        private string AcceptIcon()
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.NON_AGGRESSION:
                case LetterPurpose.ULTIMATUM:
                case LetterPurpose.CESSION_CONFIRM:
                    return "claims:peace-dove";
                default:
                    return "claims:sword-brandish";
            }
        }

        /// <summary>
        /// The refusal never wears the icon its accept button wears: on a peace offer both would
        /// be the dove, leaving the two buttons indistinguishable.
        /// </summary>
        private string DenyIcon()
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.NON_AGGRESSION:
                case LetterPurpose.ULTIMATUM:
                case LetterPurpose.CESSION_CONFIRM:
                case LetterPurpose.END_CONFLICT:
                    return "claims:cancel";
                default:
                    return "claims:peace-dove";
            }
        }

        private string AcceptTooltip()
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.START_CONFLICT: return Lang.Get("claims:gui-conflict-accept-button");
                case LetterPurpose.NON_AGGRESSION: return Lang.Get("claims:gui-conflict-nap-accept-button");
                case LetterPurpose.ULTIMATUM: return Lang.Get("claims:gui-conflict-ultimatum-accept-button");
                case LetterPurpose.CESSION_CONFIRM: return Lang.Get("claims:gui-conflict-cession-accept-button");
                default: return Lang.Get("claims:gui-conflict-stop-button");
            }
        }

        private string DenyTooltip(bool isSender)
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.START_CONFLICT:
                    return Lang.Get(isSender ? "claims:gui-conflict-cancel-button" : "claims:gui-conflict-deny-button");
                case LetterPurpose.NON_AGGRESSION: return Lang.Get("claims:gui-conflict-nap-deny-button");
                case LetterPurpose.ULTIMATUM: return Lang.Get("claims:gui-conflict-ultimatum-deny-button");
                case LetterPurpose.CESSION_CONFIRM: return Lang.Get("claims:gui-conflict-cession-deny-button");
                default: return Lang.Get("claims:gui-conflict-denystop-button");
            }
        }

        private string AcceptCommand()
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.START_CONFLICT: return CmdPrefix + "accept " + cell.From;
                case LetterPurpose.NON_AGGRESSION: return CmdPrefix + "nap accept " + cell.From;
                case LetterPurpose.ULTIMATUM: return CmdPrefix + "ultimatum accept " + cell.From;
                // Handing over a plot is the city mayor's act even when the city is in an alliance.
                case LetterPurpose.CESSION_CONFIRM: return "/city war cession accept " + cell.From;
                default: return CmdPrefix + "acceptstop " + cell.From;
            }
        }

        private string DenyCommand(bool isSender)
        {
            switch (cell.Purpose)
            {
                case LetterPurpose.START_CONFLICT:
                    return CmdPrefix + (isSender ? "revoke " + cell.To : "deny " + cell.From);
                case LetterPurpose.NON_AGGRESSION: return CmdPrefix + "nap deny " + cell.From;
                case LetterPurpose.ULTIMATUM: return CmdPrefix + "ultimatum deny " + cell.From;
                case LetterPurpose.CESSION_CONFIRM: return "/city war cession deny " + cell.From;
                default: return CmdPrefix + "denystop " + (isSender ? cell.To : cell.From);
            }
        }

        private void Answer(string command)
        {
            ClientChat.Send(command);

            // Dropped locally so the list updates without waiting for the server to say so.
            var letters = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements;
            var letter = letters.FirstOrDefault(c => c.Guid == cell.Guid);
            if (letter != null)
            {
                letters.Remove(letter);
                claims.CANCityGui.BuildMainWindow();
            }
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
