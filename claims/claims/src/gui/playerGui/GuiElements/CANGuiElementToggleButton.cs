using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using static System.Net.Mime.MediaTypeNames;

namespace claims.src.gui.playerGui.GuiElements
{
    public class CANGuiElementToggleButton: GuiElementControl
    {
        private Action<bool> handler;

        //
        // Souhrn:
        //     Is this button toggleable?
        public bool Toggleable;

        //
        // Souhrn:
        //     Is this button on?
        public bool On;

        /// <summary>
        /// A button that only displays a state the player does not own - the enemy's half of the war
        /// schedule. Clearing Toggleable was not enough: this class toggles and fires its handler on
        /// mouse down regardless, so the enemy's slots could be edited.
        /// </summary>
        public bool ReadOnly;

        private LoadedTexture releasedTexture;

        private LoadedTexture pressedTexture;

        private LoadedTexture hoverTexture;

        private int unscaledDepth = 4;

        private string icon;

        private double pressedYOffset;

        private double nonPressedYOffset;
        public static double[] ReleaseColor = new double[4]
        {
            0.4,
            0.43,
            0.43,
            1.0
        };
        bool useReleaseColor = false;

        //
        // Souhrn:
        //     Is this element capable of being in the focus?
        public override bool Focusable => enabled;

        //
        // Souhrn:
        //     Constructor for the button
        //
        // Parametry:
        //   capi:
        //     The core client API.
        //
        //   icon:
        //     The icon name
        //
        //   OnToggled:
        //     The action that happens when the button is toggled.
        //
        //   bounds:
        //     The bounding box of the button.
        //
        //   toggleable:
        //     Can the button be toggled on or off?
        public CANGuiElementToggleButton(ICoreClientAPI capi, string icon, Action<bool> OnToggled, ElementBounds bounds, bool toggleable = false, bool useReleaseColor = false)
            : base(capi, bounds)
        {
            releasedTexture = new LoadedTexture(capi);
            pressedTexture = new LoadedTexture(capi);
            hoverTexture = new LoadedTexture(capi);
            handler = OnToggled;
            Toggleable = toggleable;
            this.icon = icon;
            this.useReleaseColor = useReleaseColor;
        }

        //
        // Souhrn:
        //     Composes the element in both the pressed, and released states.
        //
        // Parametry:
        //   ctx:
        //     The context of the element.
        //
        //   surface:
        //     The surface of the element.
        //
        // Poznámky:
        //     Neither the context, nor the surface is used in this function.
        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();
            ComposeReleasedButton();
            ComposePressedButton();
        }

        private void ComposeReleasedButton()
        {
            double num = GuiElement.scaled(unscaledDepth);
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context context = genContext(imageSurface);
            context.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
            GuiElement.RoundRectangle(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
            context.FillPreserve();
            if (useReleaseColor)
            {
                context.SetSourceRGBA(ReleaseColor);
            }
            else
            {
                context.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
            }
            context.Fill();
            EmbossRoundRectangleElement(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)num);
            if (icon != null && icon.Length > 0)
            {
                api.Gui.Icons.DrawIcon(context, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(9.0), Bounds.InnerHeight - GuiElement.scaled(9.0), new double[] { 1.0, 1.0, 1.0, 1.0 });
            }

            generateTexture(imageSurface, ref releasedTexture);
            context.Dispose();
            imageSurface.Dispose();
        }

        private void ComposePressedButton()
        {
            double num = GuiElement.scaled(unscaledDepth);
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context context = genContext(imageSurface);
            context.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
            GuiElement.RoundRectangle(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
            context.FillPreserve();
            context.SetSourceRGBA(0.0, 0.0, 0.0, 0.1);
            context.Fill();
            EmbossRoundRectangleElement(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: true, (int)num);
            if (icon != null && icon.Length > 0)
            {
                context.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
                api.Gui.Icons.DrawIcon(context, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
            }

            generateTexture(imageSurface, ref pressedTexture);
            context.Dispose();
            imageSurface.Dispose();
            imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            context = genContext(imageSurface);
            context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
            context.Fill();
            if (icon != null && icon.Length > 0)
            {
                context.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
                api.Gui.Icons.DrawIcon(context, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
            }
            generateTexture(imageSurface, ref hoverTexture);
            context.Dispose();
            imageSurface.Dispose();
        }

        //
        // Souhrn:
        //     Renders the button.
        //
        // Parametry:
        //   deltaTime:
        //     The time elapsed.
        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.Render2DTexturePremultipliedAlpha(On ? pressedTexture.TextureId : releasedTexture.TextureId, Bounds);
            if (icon == null && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
            {
                api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.renderX, Bounds.renderY + (On ? pressedYOffset : nonPressedYOffset), Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            }
        }

        //
        // Souhrn:
        //     Handles the mouse button press while the mouse is on this button.
        //
        // Parametry:
        //   api:
        //     The client API
        //
        //   args:
        //     The mouse event arguments.
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (ReadOnly) return;

            base.OnMouseDownOnElement(api, args);
            On = !On;
            handler?.Invoke(On);
            api.Gui.PlaySound("toggleswitch");
        }

        //
        // Souhrn:
        //     Handles the mouse button release while the mouse is on this button.
        //
        // Parametry:
        //   api:
        //     The client API
        //
        //   args:
        //     The mouse event arguments
        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            /*if (!Toggleable)
            {
                On = false;
            }*/
        }

        //
        // Souhrn:
        //     Handles the event fired when the mouse is released.
        //
        // Parametry:
        //   api:
        //     The client API
        //
        //   args:
        //     Mouse event arguments
        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            /*if (!Toggleable)
            {
                On = false;
            }*/

            base.OnMouseUp(api, args);
        }

        public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
        {
            if (!ReadOnly && base.HasFocus && args.KeyCode == 49)
            {
                args.Handled = true;
                On = !On;
                handler?.Invoke(On);
                api.Gui.PlaySound("toggleswitch");
            }
        }

        //
        // Souhrn:
        //     Sets the value of the button.
        //
        // Parametry:
        //   on:
        //     Am I on or off?
        public void SetValue(bool on)
        {
            On = on;
        }

        //
        // Souhrn:
        //     Disposes of the button.
        public override void Dispose()
        {
            base.Dispose();
            releasedTexture.Dispose();
            pressedTexture.Dispose();
            hoverTexture.Dispose();
        }
    }
}
