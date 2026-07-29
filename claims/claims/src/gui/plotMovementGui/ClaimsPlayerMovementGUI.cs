using claims.src.auxialiry;
using claims.src.clientMapHandling;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.plotMovementGui
{
    public class ClaimsPlayerMovementGUI : GuiDialog
    {
        public override EnumDialogType DialogType => EnumDialogType.HUD;
        public long timeStampShouldBeClosed = 0;
        public ClaimsPlayerMovementGUI(ICoreClientAPI capi) : base(capi)
        {
            SetupDialog();
        }
        public override string ToggleKeyCombinationCode => "claimsplayermovementgui";
        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
        }
        public void SetupDialog()
        {

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("claims-plot-hud", dialogBounds)
                //.AddShadedDialogBG(bgBounds)
                .BeginChildElements(bgBounds);
            SingleComposer.AddRichtext(Lang.Get("claims:movementgui-city-name", ""),
                CairoFont.WhiteDetailText().WithFontSize(20).WithOrientation(EnumTextOrientation.Right),
                ElementBounds.Fixed(0, 0, 200, 20), "line_1");
            SingleComposer.AddRichtext(Lang.Get("claims:movementgui-city-name", ""),
                CairoFont.WhiteDetailText().WithFontSize(20),
                ElementBounds.Fixed(0, 20, 200, 20), "line_2");
            SingleComposer.AddRichtext(Lang.Get("claims:movementgui-city-name", ""),
                CairoFont.WhiteDetailText().WithFontSize(20),
                ElementBounds.Fixed(0, 40, 200, 20), "line_3");
            SingleComposer.AddRichtext(Lang.Get("claims:movementgui-city-name", ""),
                CairoFont.WhiteDetailText().WithFontSize(18),
                ElementBounds.Fixed(0, 60, 200, 20), "line_4");
            SingleComposer.AddRichtext(Lang.Get("claims:movementgui-city-name", ""),
                CairoFont.WhiteDetailText().WithFontSize(20),
                ElementBounds.Fixed(0, 80, 200, 20), "line_5");
            SingleComposer
                .EndChildElements()
                .Compose();
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            //return;
            if (claims.clientDataStorage.getSavedPlot(new Vec2i((int)claims.capi.World.Player.Entity.Pos.X / PlotPosition.plotSize,
                                                                        (int)claims.capi.World.Player.Entity.Pos.Z / PlotPosition.plotSize),
                                                               out SavedPlotInfo savedPlotInfo))
            {
                claims.updateMovementGUIInfo(savedPlotInfo);
            }
            else
            {
                claims.updateMovementGUIInfo();
            }
        }
    }
}
