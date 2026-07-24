using System;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.GuiElements
{
    public class GuiElementConflictCell : GuiElementTextBase, IGuiElementCell, IDisposable
    {
        public enum HighlightedTexture
        {
            FIRST, SECOND, THIRD
        }
        public static double unscaledRightBoxWidth = 40.0;

        public ClientConflictCellElement cell;

        private bool showModifyIcons = true;

        public bool On;

        internal int leftHighlightTextureId;

        internal int middleHighlightTextureId;

        internal int rightHighlightTextureId;

        internal int switchOnTextureId;

        internal double unscaledSwitchPadding = 4.0;

        internal double unscaledSwitchSize = 25.0;

        private LoadedTexture modcellTexture;

        private IAsset cancelIcon;
        private IAsset approveIcon;

        private ICoreClientAPI capi;

        public Action<int> OnMouseDownOnCellLeft;
        public Action<int> OnMouseDownOnCellMiddle;
        public Action<int> OnMouseDownOnCellRight;

        private IAsset dove;
        private IAsset sword;
        private LoadedTexture normalTexture;

        ElementBounds IGuiElementCell.Bounds => Bounds;

        public GuiElementConflictCell(ICoreClientAPI capi, ClientConflictCellElement cell, ElementBounds bounds)
            : base(capi, "", null, bounds)
        {
            this.cell = cell;
            this.Font = CairoFont.WhiteSmallishText();
            modcellTexture = new LoadedTexture(capi);

            this.cancelIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/cancel.svg"));
            this.approveIcon = capi.Assets.Get(new AssetLocation("claims:textures/icons/check-mark.svg"));


            this.dove = capi.Assets.Get(new AssetLocation("claims:textures/icons/peace-dove.svg"));
            //this.sword = capi.Assets.Get(new AssetLocation("claims:sword-brandish.svg"));

            this.capi = capi;
            normalTexture = new LoadedTexture(capi);
        }

        private void Compose()
        {
            ComposeHover(HighlightedTexture.FIRST, ref leftHighlightTextureId);
            ComposeHover(HighlightedTexture.SECOND, ref middleHighlightTextureId);
            ComposeHover(HighlightedTexture.THIRD, ref rightHighlightTextureId);
            genOnTexture();
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            Context context = new Context(imageSurface);
            double num = GuiElement.scaled(unscaledRightBoxWidth);
            Bounds.CalcWorldBounds();

            string cellName = Lang.Get("claims:gui_conflict_cell_first_line", cell.FirstPartyName, cell.SecondPartyName);
            TextExtents textExtents = Font.GetTextExtents(cellName);
            textUtil.AutobreakAndDrawMultilineTextAt(context, Font, cellName, Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(10), textExtents.Width + 1.0, EnumTextOrientation.Left);
            string expDate = Lang.Get("claims:gui_conflict_cell_started_line", TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeStampCreated, true));
            textExtents = Font.GetTextExtents(expDate);
            textUtil.AutobreakAndDrawMultilineTextAt(context, CairoFont.WhiteDetailText(), expDate, Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(36), textExtents.Width + 1.0, EnumTextOrientation.Left);
            string scoreLine = Lang.Get("claims:gui_conflict_cell_score_line", cell.FirstScore, cell.SecondScore);
            textExtents = Font.GetTextExtents(scoreLine);
            textUtil.AutobreakAndDrawMultilineTextAt(context, CairoFont.WhiteDetailText(), scoreLine, Bounds.absPaddingX, Bounds.absPaddingY + GuiElement.scaled(54), textExtents.Width + 1.0, EnumTextOrientation.Left);

            //make border as button
            EmbossRoundRectangleElement(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)GuiElement.scaled(4.0), 0);

            double num5 = GuiElement.scaled(unscaledSwitchSize);
            double num6 = GuiElement.scaled(unscaledSwitchPadding);
            double num7 = Bounds.absPaddingX + Bounds.InnerWidth - GuiElement.scaled(0.0) - num5 - num6;
            double num8 = Bounds.absPaddingY + Bounds.absPaddingY;

            capi.Gui.DrawSvg(cancelIcon, imageSurface, (int)(num7 - GuiElement.scaled(3.0)),
                                                            (int)(num8 + GuiElement.scaled(15.0)),
                                                            (int)GuiElement.scaled(30.0),
                                                            (int)GuiElement.scaled(30.0),
                                                            ColorUtil.ColorFromRgba(255, 128, 0, 255));
            if (claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Guid?.Equals(this.cell.Guid) ?? false)
            {
                capi.Gui.DrawSvg(approveIcon, imageSurface,
                (int)(num7 - GuiElement.scaled(unscaledRightBoxWidth) - GuiElement.scaled(10.0)),
                (int)(num8 + GuiElement.scaled(15.0)),
                (int)GuiElement.scaled(30.0),
                (int)GuiElement.scaled(30.0),
                ColorUtil.ColorFromRgba(0, 153, 0, 255));
            }


            generateTexture(imageSurface, ref modcellTexture);

            context.Dispose();
            imageSurface.Dispose();

            ImageSurface imageSurface2 = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            Context context2 = new Context(imageSurface2);
            Bounds.CalcWorldBounds();
            generateTexture(imageSurface2, ref normalTexture);
            context2.Dispose();
            imageSurface2.Dispose();
        }

        private void genOnTexture()
        {
            double num = GuiElement.scaled(unscaledSwitchSize - 2.0 * unscaledSwitchPadding);
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)num, (int)num);
            Context context = genContext(imageSurface);
            GuiElement.RoundRectangle(context, 0.0, 0.0, num, num, 2.0);
            GuiElement.fillWithPattern(api, context, GuiElement.waterTextureName);
            generateTexture(imageSurface, ref switchOnTextureId);
            context.Dispose();
            imageSurface.Dispose();
        }

        private void ComposeHover(HighlightedTexture highlightedTexutre, ref int textureId)
        {
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context context = genContext(imageSurface);
            double num = GuiElement.scaled(unscaledRightBoxWidth);
            if (highlightedTexutre == HighlightedTexture.FIRST)
            {
                context.NewPath();
                context.LineTo(0.0, 0.0);
                context.LineTo(Bounds.InnerWidth - num * 2, 0.0);
                context.LineTo(Bounds.InnerWidth - num * 2, Bounds.OuterHeight);
                context.LineTo(0.0, Bounds.OuterHeight);
                context.ClosePath();
            }
            else if (highlightedTexutre == HighlightedTexture.SECOND)
            {
                context.NewPath();
                context.LineTo(Bounds.InnerWidth - num * 2, 0);
                context.LineTo(Bounds.InnerWidth - num, 0);
                context.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
                context.LineTo(Bounds.InnerWidth - num * 2, Bounds.OuterHeight);
                context.ClosePath();
            }
            else
            {
                context.NewPath();
                context.LineTo(Bounds.InnerWidth - num, 0.0);
                context.LineTo(Bounds.OuterWidth, 0.0);
                context.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
                context.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
                context.ClosePath();
            }

            context.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
            context.Fill();
            generateTexture(imageSurface, ref textureId);
            context.Dispose();
            imageSurface.Dispose();
        }

        public void UpdateCellHeight()
        {
            Bounds.CalcWorldBounds();
            if (showModifyIcons && Bounds.fixedHeight < 90.0)
            {
                Bounds.fixedHeight = 90.0;
            }
        }

        public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
        {
            if (modcellTexture.TextureId == 0)
            {
                Compose();
            }

            ElementBounds imageBounds = ElementBounds.Fixed(Bounds.absX + 10, Bounds.absY + 10, 32, 32);
            //imageBounds.CalcWorldBounds();


            api.Render.Render2DTexturePremultipliedAlpha(
                normalTexture.TextureId,
                this.Bounds.renderX / 1.5, this.Bounds.renderY / 1.05
                , (double)this.Bounds.OuterWidthInt, (double)this.Bounds.OuterHeightInt
            );

            api.Render.Render2DTexturePremultipliedAlpha(modcellTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            int mouseX = api.Input.MouseX;
            int mouseY = api.Input.MouseY;
            Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
            if (vec2d != null)
            {
                if (vec2d.X > (Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth) * 2)
                    && vec2d.X < (Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth)))
                {
                    api.Render.Render2DTexturePremultipliedAlpha(middleHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
                }
                else
                if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
                {
                    api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
                }
                else
                {
                    api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            modcellTexture?.Dispose();
            api.Render.GLDeleteTexture(leftHighlightTextureId);
            api.Render.GLDeleteTexture(middleHighlightTextureId);
            api.Render.GLDeleteTexture(rightHighlightTextureId);
            api.Render.GLDeleteTexture(switchOnTextureId);
            api.Render.GLDeleteTexture(normalTexture.TextureId);
            normalTexture?.Dispose();
        }

        public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
        {
            int mouseX = api.Input.MouseX;
            int mouseY = api.Input.MouseY;
            Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
            api.Gui.PlaySound("menubutton_press");
            if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth) * 2 &&
                    vec2d.X < Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
            {
                args.Handled = true;
            }
            else if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
            {
                claims.CANCityGui.CreateNewCityState = CANClaimsGui.EnumUpperWindowSelectedState.ALLIANCE_SEND_PEACE_OFFER_CONFIRM;
                claims.CANCityGui.selectedString = cell.Guid;
                string targetAlliance = cell.FirstPartyName.Equals(claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Name) ? cell.SecondPartyName : cell.FirstPartyName;
                claims.CANCityGui.secondSelectedString = targetAlliance;
                claims.CANCityGui.BuildUpperWindow();                
            }
            else
            {
                args.Handled = true;
                claims.CANCityGui.selectedString = cell.Guid;
                claims.CANCityGui.SelectedTab = CANClaimsGui.EnumSelectedTab.ConflictInfoPage;
                claims.CANCityGui.BuildMainWindow();
            }
        }

        public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
        {
        }

        public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
        {
        }
    }
}
