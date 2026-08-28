using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.hud
{
    /// <summary>
    /// Drag-to-move editor for the HUD panels, opened with /claimshudedit. Each panel gets a
    /// draggable ghost of a typical size while the HUD keeps rendering behind it; positions are
    /// stored normalised (see <see cref="ClaimsHudLayout"/>) and saved when a drag ends.
    ///
    /// Ghosts rather than the HUD panels themselves: a HUD element receives no mouse input, so the
    /// thing you grab has to be a dialog. It also lets a panel with nothing to show right now
    /// (a war HUD outside a battle) still be placed.
    /// </summary>
    public class HudLayoutEditor : GuiDialog
    {
        private readonly List<IMovableHudPanel> panels;

        private readonly List<Ghost> ghosts = new List<Ghost>();
        private Ghost dragging;
        private double grabOffsetX, grabOffsetY;
        private bool dirty;

        // Barely-there fills: the ghosts sit on top of the still-rendering HUD, and opaque colours
        // would turn them into slabs hiding what is being positioned.
        private static readonly double[] GhostIdle = { 1.00, 0.83, 0.42, 0.16 };
        private static readonly double[] GhostDragged = { 0.47, 0.78, 0.36, 0.30 };

        private class Ghost
        {
            public IMovableHudPanel Panel;
            public double Width, Height;
            public double X, Y;      // unscaled screen coords of the top-left corner
        }

        public HudLayoutEditor(ICoreClientAPI capi, List<IMovableHudPanel> panels) : base(capi)
        {
            this.panels = panels;
        }

        // Opened explicitly, never by a key of its own.
        public override string ToggleKeyCombinationCode => null;
        public override bool PrefersUngrabbedMouse => true;

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            try
            {
                BuildGhosts();
                Compose();
            }
            catch (Exception e)
            {
                // A broken editor must not be able to end the session.
                capi.Logger.Error("[claims] HUD layout editor failed to open: {0}", e);
                TryClose();
            }
        }

        // Same sizes and default spots the HUD composes its panels at.
        private void BuildGhosts()
        {
            double screenW = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
            double screenH = capi.Render.FrameHeight / RuntimeEnv.GUIScale;

            ghosts.Clear();
            foreach (var panel in panels)
            {
                var g = new Ghost { Panel = panel, Width = panel.EditorWidth, Height = panel.EditorHeight };

                var pos = ClaimsHudLayout.Current?.Get(panel.LayoutKey);
                if (pos != null)
                {
                    g.X = pos.X * screenW;
                    g.Y = pos.Y * screenH;
                }
                else
                {
                    (g.X, g.Y) = panel.DefaultTopLeft(screenW, screenH, g.Height);
                }
                Clamp(g);
                ghosts.Add(g);
            }
        }

        private void Clamp(Ghost g)
        {
            double screenW = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
            double screenH = capi.Render.FrameHeight / RuntimeEnv.GUIScale;
            g.X = GameMath.Clamp(g.X, 0, Math.Max(0, screenW - g.Width));
            g.Y = GameMath.Clamp(g.Y, 0, Math.Max(0, screenH - g.Height));
        }

        private void Compose()
        {
            Composers.ClearComposers();

            foreach (var g in ghosts)
            {
                var bounds = ElementBounds.Fixed(g.X, g.Y, g.Width, g.Height);
                var inner = ElementBounds.Fixed(0, 0, g.Width, g.Height);

                Composers[g.Panel.LayoutKey] = capi.Gui
                    .CreateCompo("claims-hudghost-" + g.Panel.LayoutKey, bounds)
                    .AddGameOverlay(inner, dragging == g ? GhostDragged : GhostIdle)
                    .BeginChildElements(inner)
                        .AddStaticText(g.Panel.EditorLabel,
                            CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center),
                            ElementBounds.Fixed(0, g.Height / 2 - 12, g.Width, 22))
                    .EndChildElements()
                    .Compose();
            }

            ComposeToolbox();
        }

        private void ComposeToolbox()
        {
            var dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterBottom)
                .WithFixedAlignmentOffset(0, -60);
            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            Composers["toolbox"] = capi.Gui
                .CreateCompo("claims-hudedit", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("claims:gui-hud-edit-title"), OnDone)
                .BeginChildElements(bgBounds)
                    // Children start below the title bar, which the background draws inside these
                    // same bounds. Height fixed: a self-measuring text element inside a
                    // FitToChildren parent makes the layout pass feed on itself.
                    .AddStaticText(Lang.Get("claims:gui-hud-edit-hint"), CairoFont.WhiteDetailText(),
                        ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 320, 40))
                    .AddSmallButton(Lang.Get("claims:gui-hud-edit-reset"), OnReset,
                        ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0, GuiStyle.TitleBarHeight + 46, 0, 0).WithFixedPadding(9, 4))
                    .AddSmallButton(Lang.Get("claims:gui-hud-edit-done"), () => { OnDone(); return true; },
                        ElementBounds.Fixed(EnumDialogArea.LeftFixed, 110, GuiStyle.TitleBarHeight + 46, 0, 0).WithFixedPadding(9, 4))
                .EndChildElements()
                .Compose();
        }

        private bool OnReset()
        {
            ClaimsHudLayout.Current?.ResetAll();
            dirty = true;
            BuildGhosts();
            Compose();
            NotifyPanels();
            return true;
        }

        private void OnDone()
        {
            Save();
            TryClose();
        }

        private void Save()
        {
            if (!dirty) return;
            ClaimsHudLayout.Current?.Save(capi);
            dirty = false;
        }

        private void NotifyPanels()
        {
            foreach (var panel in panels) panel.OnLayoutChanged();
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            Save();
        }

        // Dragging

        public override void OnMouseDown(MouseEvent args)
        {
            double mx = capi.Input.MouseX / RuntimeEnv.GUIScale;
            double my = capi.Input.MouseY / RuntimeEnv.GUIScale;

            // Reverse order: the last ghost composed draws on top, so it wins the click too.
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var g = ghosts[i];
                if (mx < g.X || mx > g.X + g.Width || my < g.Y || my > g.Y + g.Height) continue;

                dragging = g;
                grabOffsetX = mx - g.X;
                grabOffsetY = my - g.Y;
                Compose();
                args.Handled = true;
                return;
            }

            base.OnMouseDown(args);   // let the toolbox have it
        }

        public override void OnMouseMove(MouseEvent args)
        {
            if (dragging == null) { base.OnMouseMove(args); return; }

            dragging.X = capi.Input.MouseX / RuntimeEnv.GUIScale - grabOffsetX;
            dragging.Y = capi.Input.MouseY / RuntimeEnv.GUIScale - grabOffsetY;
            Clamp(dragging);

            // Move the existing composer instead of calling Compose(), which clears and recreates
            // all of them - re-uploading GUI textures on every mouse-move event.
            var composer = Composers[dragging.Panel.LayoutKey];
            if (composer?.Bounds != null)
            {
                composer.Bounds.fixedX = dragging.X;
                composer.Bounds.fixedY = dragging.Y;
                composer.Bounds.CalcWorldBounds();
            }
            args.Handled = true;
        }

        public override void OnMouseUp(MouseEvent args)
        {
            if (dragging == null) { base.OnMouseUp(args); return; }

            double screenW = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
            double screenH = capi.Render.FrameHeight / RuntimeEnv.GUIScale;
            if (screenW > 0 && screenH > 0)
            {
                ClaimsHudLayout.Current?.Set(dragging.Panel.LayoutKey, dragging.X / screenW, dragging.Y / screenH);
            }

            dirty = true;
            dragging = null;
            Compose();
            Save();            // keep the layout the moment it is let go of
            NotifyPanels();    // the HUD re-composes at the new position
            args.Handled = true;
        }
    }
}
