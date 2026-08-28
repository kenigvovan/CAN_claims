using System;
using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.gui.hud;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.plotMovementGui
{
    public class ClaimsPlayerMovementGUI : GuiDialog, IMovableHudPanel
    {
        /// <summary>How many text slots the panel has, and how tall each one is.</summary>
        public const int LineCount = 5;
        private const int LineHeight = 20;
        private const int LineWidth = 200;

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

            // The spot the player dragged the panel to in the layout editor, if any.
            var pos = ClaimsHudLayout.Current?.Get(LayoutKey);
            if (pos != null)
            {
                double screenW = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
                double screenH = capi.Render.FrameHeight / RuntimeEnv.GUIScale;
                double x = GameMath.Clamp(pos.X * screenW, 0, Math.Max(0, screenW - EditorWidth));
                double y = GameMath.Clamp(pos.Y * screenH, 0, Math.Max(0, screenH - EditorHeight));
                dialogBounds = dialogBounds.WithAlignment(EnumDialogArea.None).WithFixedPosition(x, y);
            }

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            // The setter does not dispose the composer it replaces.
            SingleComposer?.Dispose();
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
                    ElementBounds.Fixed(0, (i - 1) * LineHeight, LineWidth, LineHeight), "line_" + i);
            }
            SingleComposer
                .EndChildElements()
                .Compose();
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            RefillLines();
        }

        private void RefillLines()
        {
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

        // Layout editor support

        public string LayoutKey => "claims-plot-hud";
        public string EditorLabel => Lang.Get("claims:gui-hud-edit-panel-plot");
        public double EditorWidth => LineWidth + 2 * GuiStyle.ElementToDialogPadding;
        public double EditorHeight => LineCount * LineHeight + 2 * GuiStyle.ElementToDialogPadding;

        public (double X, double Y) DefaultTopLeft(double screenW, double screenH, double height)
            => (GuiStyle.LeftDialogMargin, 0);

        public void OnLayoutChanged()
        {
            // Rebuilding the composer leaves the line texts empty until the next plot change.
            SetupDialog();
            if (IsOpened()) RefillLines();
        }
    }
}
