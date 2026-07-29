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
    /// <summary>An invitation into a city's plots group.</summary>
    public class GuiElementPlotsGroupInvitation : CANGuiElementCellBase
    {
        public ClientToPlotsGroupInvitation cell;

        private readonly IAsset cancelIcon;
        private readonly IAsset approveIcon;

        public GuiElementPlotsGroupInvitation(ICoreClientAPI capi, ClientToPlotsGroupInvitation cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;
            this.cancelIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/cancel.svg"));
            this.approveIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/check-mark.svg"));

            // Both answers are wired here rather than by the page: only the middle column ever did
            // anything, the handlers for the other two were commented out - so the drawn cancel icon
            // was not a button at all.
            OnMouseDownOnCellMiddle = _ => Answer("/plotsgroupaccept ");
            OnMouseDownOnCellRight = _ => Answer("/plotsgroupdeny ");

            AddZoneTooltip(HighlightZone.Middle, Lang.Get("claims:gui-plotsgroup-accept-tooltip"));
            AddZoneTooltip(HighlightZone.Right, Lang.Get("claims:gui-plotsgroup-decline-tooltip"));
        }

        private void Answer(string command)
        {
            ClientChat.Send(command + cell.CityName + " " + cell.PlotsGroupName);

            var invite = claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations
                .FirstOrDefault(c => c.CityName == cell.CityName && c.PlotsGroupName == cell.PlotsGroupName);
            if (invite != null)
            {
                claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.Remove(invite);
                claims.CANCityGui.BuildMainWindow();
            }
        }

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            string cellName = cell.CityName + ": " + cell.PlotsGroupName;
            TextExtents extents = Font.GetTextExtents(cellName);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellName,
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
