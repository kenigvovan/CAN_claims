using claims.src.auxialiry;
using claims.src.clientMapHandling;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.plotMovementGui
{
    public class ClaimsPlayerMovementGUI : GuiDialog
    {
        /// <summary>How many text slots the panel has, and how tall each one is.</summary>
        public const int LineCount = 5;
        private const int LineHeight = 20;

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
            // Five identical slots, filled in by claims.updateMovementGUIInfo. They used to differ in
            // size and even orientation - the first line was right-aligned, the rest left - so the
            // panel changed shape depending on how many lines a plot happened to have.
            for (int i = 1; i <= LineCount; i++)
            {
                SingleComposer.AddRichtext("",
                    CairoFont.WhiteDetailText().WithFontSize(18),
                    ElementBounds.Fixed(0, (i - 1) * LineHeight, 200, LineHeight), "line_" + i);
            }
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
