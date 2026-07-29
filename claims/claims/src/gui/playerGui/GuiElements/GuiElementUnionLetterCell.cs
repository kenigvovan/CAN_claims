using System.Linq;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.union;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// An offer to form a union, or to dissolve one. Accepting means opposite things in the two
    /// cases, so the cell says which it is rather than just showing the two party names.
    /// </summary>
    public class GuiElementUnionLetterCell : CANGuiElementCellBase
    {
        public ClientUnionLetterCellElement cell;

        private readonly IAsset cancelIcon;
        private readonly IAsset approveIcon;

        public GuiElementUnionLetterCell(ICoreClientAPI capi, ClientUnionLetterCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;
            this.cancelIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/cancel.svg"));
            this.approveIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/check-mark.svg"));

            OnMouseDownOnCellMiddle = _ => Accept();
            OnMouseDownOnCellRight = _ => Decline();
        }

        /// <summary>Whether we sent this letter rather than received it.</summary>
        private bool SentByUs => cell.FromGuid == claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Guid;

        private void Accept()
        {
            // The sender has nothing to accept - only the recipient does.
            if (SentByUs) return;

            ClientChat.Send("/a union accept " + cell.From);
            RemoveLetter(c => c.From == cell.From);
        }

        private void Decline()
        {
            ClientChat.Send("/a union decline " + cell.To);
            RemoveLetter(c => c.Guid == cell.Guid);
        }

        /// <summary>Drops the letter locally so the list updates without waiting for the server.</summary>
        private void RemoveLetter(System.Func<ClientUnionLetterCellElement, bool> match)
        {
            var letters = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements;
            var letter = letters.FirstOrDefault(match);
            if (letter != null)
            {
                letters.Remove(letter);
                claims.CANCityGui.BuildMainWindow();
            }
        }

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            string parties = string.Format("{0} x {1}", cell.From, cell.To);
            TextExtents extents = Font.GetTextExtents(parties);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, parties,
                Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(10), extents.Width + 1.0, EnumTextOrientation.Left);

            bool dissolve = cell.Purpose == UnionLetterPurpose.Dissolve;
            string purpose = Lang.Get(dissolve ? "claims:gui_union_letter_dissolve" : "claims:gui_union_letter_form");
            extents = Font.GetTextExtents(purpose);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, CairoFont.WhiteDetailText(), purpose,
                Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(36), extents.Width + 1.0, EnumTextOrientation.Left);

            string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeStampExpire, true);
            extents = Font.GetTextExtents(expDate);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, CairoFont.WhiteDetailText(), expDate,
                Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(54), extents.Width + 1.0, EnumTextOrientation.Left);

            double switchSize = GuiElement.scaled(UnscaledSwitchSize);
            double switchPadding = GuiElement.scaled(UnscaledSwitchPadding);
            double iconX = Bounds.absPaddingX + Bounds.InnerWidth - switchSize - switchPadding;
            double iconY = Bounds.absPaddingY + Bounds.absPaddingY;

            capi.Gui.DrawSvg(cancelIcon, surface,
                (int)(iconX - GuiElement.scaled(3.0)), (int)(iconY + GuiElement.scaled(15.0)),
                (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), ColorUtil.ColorFromRgba(255, 128, 0, 255));

            // No accept icon on letters we sent ourselves - there is nothing for us to accept.
            if (!SentByUs)
            {
                capi.Gui.DrawSvg(approveIcon, surface,
                    (int)(iconX - GuiElement.scaled(UnscaledRightBoxWidth) - GuiElement.scaled(10.0)), (int)(iconY + GuiElement.scaled(15.0)),
                    (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), ColorUtil.ColorFromRgba(0, 153, 0, 255));
            }
        }
    }
}
