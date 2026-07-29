using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>An invitation to join a city, with accept and decline icons on the right.</summary>
    public class GuiElementCityInvitation : CANGuiElementCellBase
    {
        public ClientToCityInvitation cell;

        private readonly IAsset cancelIcon;
        private readonly IAsset approveIcon;

        public GuiElementCityInvitation(ICoreClientAPI capi, ClientToCityInvitation cell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.cell = cell;
            this.cancelIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/cancel.svg"));
            this.approveIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/check-mark.svg"));

            // The page wires what the columns do; naming them is this cell's job. Until now the two
            // drawn icons were the only hint of which half accepts and which declines.
            AddZoneTooltip(HighlightZone.Middle, Lang.Get("claims:gui-city-tab-accept"));
            AddZoneTooltip(HighlightZone.Right, Lang.Get("claims:gui-city-tab-decline"));
        }

        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            TextExtents extents = Font.GetTextExtents(cell.CityName);
            textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cell.CityName,
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
