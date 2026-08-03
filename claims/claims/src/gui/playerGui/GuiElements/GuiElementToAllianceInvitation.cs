using System.Linq;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>An invitation for this city to join an alliance.</summary>
    public class GuiElementToAllianceInvitation : CANGuiElementCellBase
    {
        public ClientToAllianceInvitationCellElement cell;

        private readonly IAsset cancelIcon;
        private readonly IAsset approveIcon;

        public GuiElementToAllianceInvitation(ICoreClientAPI capi, ClientToAllianceInvitationCellElement cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;
            this.cancelIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/cancel.svg"));
            this.approveIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/check-mark.svg"));

            // Only the middle column ever acted; the other two had their handlers commented out.
            OnMouseDownOnCellMiddle = _ => Accept();
            AddZoneTooltip(HighlightZone.Middle, Lang.Get("claims:gui-city-tab-accept"));
        }

        private void Accept()
        {
            ClientChat.Send("/c inviteaccept " + cell.AllianceName);

            var invite = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations
                .FirstOrDefault(c => c.AllianceName == cell.AllianceName);
            if (invite != null)
            {
                claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations.Remove(invite);
                claims.CANCityGui.BuildMainWindow();
            }
        }

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            TextExtents extents = Font.GetTextExtents(cell.AllianceName);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cell.AllianceName,
                Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(10), extents.Width + 1.0, EnumTextOrientation.Left);

            string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeoutStamp, true).ToString();
            extents = Font.GetTextExtents(expDate);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, CairoFont.WhiteDetailText(), expDate,
                Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(36), extents.Width + 1.0, EnumTextOrientation.Left);

            double switchSize = GuiElement.scaled(UnscaledSwitchSize);
            double switchPadding = GuiElement.scaled(UnscaledSwitchPadding);
            double iconX = Bounds.absPaddingX + Bounds.InnerWidth - switchSize - switchPadding;
            double iconY = Bounds.absPaddingY + Bounds.absPaddingY;

            capi.Gui.DrawSvg(cancelIcon, surface,
                (int)(iconX - GuiElement.scaled(3.0)), (int)(iconY + GuiElement.scaled(15.0)),
                (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), ColorUtil.ColorFromRgba(255, 128, 0, 255));

            capi.Gui.DrawSvg(approveIcon, surface,
                (int)(iconX - GuiElement.scaled(UnscaledRightBoxWidth) - GuiElement.scaled(10.0)), (int)(iconY + GuiElement.scaled(15.0)),
                (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), ColorUtil.ColorFromRgba(0, 153, 0, 255));
        }
    }
}
