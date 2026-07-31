using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.part.structure;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Designs the coat of arms of the player's city or alliance: a preview, the layers so far, and
    /// pickers for the next one. The draft lives in ClaimsGuiState and is sent only on Apply.
    /// </summary>
    public sealed class EmblemPage : CANGuiPage
    {
        private const double PreviewSize = 96;

        /// <summary>Size of the two previews under the pickers: the layer alone, and the emblem with
        /// that layer on top.</summary>
        private const double SwatchSize = 76;

        /// <summary>Height of a drop-down in the picker card - taller than the mod's usual 26, this
        /// card being what the page is for.</summary>
        private const double InputHeight = 30;

        private const double ButtonHeight = 28;

        private EmblemEditState Draft => State.Emblem;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = "claims:gui-emblem-no-city";
            return Player?.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var lineBounds = ctx.Line;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            // Two columns, like the city and prices tabs - stacked, the three cards run past the
            // navigation row.
            double columnWidth = (lineBounds.fixedWidth - Card.ColumnGap) / 2;

            var column = anchor.FlatCopy();
            column.fixedWidth = columnWidth;

            var rightColumn = anchor.FlatCopy();
            rightColumn.fixedWidth = columnWidth;
            rightColumn.fixedX += columnWidth + Card.ColumnGap;

            // Top row: the finished emblem beside the stack it is made of. The pickers go full width
            // underneath - two drop-downs, a button and both previews need the room.
            double leftY = BuildPreview(compo, column, column.fixedY);
            double rightY = BuildLayers(compo, rightColumn, rightColumn.fixedY);

            var wideColumn = anchor.FlatCopy();
            wideColumn.fixedWidth = lineBounds.fixedWidth;
            BuildPicker(compo, wideColumn, System.Math.Max(leftY, rightY));

            NavRow.Build(Gui, ctx.Current, lineBounds, 0,
                new NavButton("claims:fast-backward-button",
                    () => GoTo(Draft.ForAlliance ? EnumSelectedTab.AllianceInfoPage : EnumSelectedTab.City),
                    Lang.Get("claims:gui-nav-back")));
        }

        /// <summary>The emblem as it would look once applied, plus what it is being applied to.</summary>
        private double BuildPreview(GuiComposer compo, ElementBounds column, double y)
        {
            double height = Card.HeaderHeight + PreviewSize + Card.ActionGap + ButtonHeight + Card.Padding * 2;
            ElementBounds inner = Card.Frame(compo, column, y, height,
                Lang.Get(Draft.ForAlliance ? "claims:gui-emblem-preview-alliance" : "claims:gui-emblem-preview-city"));

            var preview = inner.FlatCopy().WithFixedSize(PreviewSize, PreviewSize);
            compo.AddEmblem(Draft.AsString(), preview, "emblem-preview");

            // The layer string next to the preview - it is what /city set emblem takes.
            var textBounds = inner.FlatCopy().WithFixedSize(inner.fixedWidth - PreviewSize - Card.Gap, PreviewSize);
            textBounds.fixedX += PreviewSize + Card.Gap;
            compo.AddStaticText(Draft.Layers.Count == 0 ? Lang.Get("claims:gui-emblem-empty") : Draft.AsString(),
                CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), textBounds, "emblem-string");

            // The two buttons split the card's width - a fixed 140 does not fit half a window.
            double buttonWidth = (inner.fixedWidth - Card.ActionGap) / 2;

            var applyBounds = inner.FlatCopy().WithFixedSize(buttonWidth, ButtonHeight);
            applyBounds.fixedY = inner.fixedY + PreviewSize + Card.ActionGap;
            compo.AddButton(Lang.Get("claims:gui-emblem-apply"), new ActionConsumable(Apply),
                applyBounds, EnumButtonStyle.Normal);
            Tooltip.Add(compo, Lang.Get("claims:gui-emblem-apply-tooltip"), applyBounds, "tip-emblemapply");

            var clearBounds = applyBounds.FlatCopy();
            clearBounds.fixedX += buttonWidth + Card.ActionGap;
            compo.AddButton(Lang.Get("claims:gui-emblem-clear"), new ActionConsumable(() =>
            {
                Draft.Layers.Clear();
                Gui.BuildMainWindow();
                return true;
            }), clearBounds, EnumButtonStyle.Normal);

            return y + height + Card.Gap;
        }

        /// <summary>The stack, bottom layer first, with a button to take the top one back off.</summary>
        private double BuildLayers(GuiComposer compo, ElementBounds column, double y)
        {
            var rows = new List<CardRow>();
            for (int i = 0; i < Draft.Layers.Count; i++)
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get(i == 0 ? "claims:gui-emblem-layer-background" : "claims:gui-emblem-layer", i + 1),
                    Value = LayerName(Draft.Layers[i]),
                    Key = "emblem-layer-" + i
                });
            }
            if (rows.Count == 0)
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-emblem-layers-none"),
                    Value = "",
                    Key = "emblem-layer-none"
                });
            }

            return Card.RowsWithActions(compo, column, y,
                Lang.Get("claims:gui-emblem-layers", Draft.Layers.Count, EmblemHandler.MaxLayers()),
                rows, slot =>
                {
                    var actions = new ActionRow(compo, slot);
                    if (Draft.Layers.Count > 0)
                    {
                        actions.Add("claims:cancel", "emblemRemoveLast", on =>
                        {
                            if (!on) return;
                            Draft.Layers.RemoveAt(Draft.Layers.Count - 1);
                            Gui.BuildMainWindow();
                        }, Lang.Get("claims:gui-emblem-remove-last"));
                    }
                });
        }

        /// <summary>
        /// Pattern and colour of the next layer, with both previews under them. Full width, so the
        /// two drop-downs and the Add button share one row. Returns the y the next card starts at.
        /// </summary>
        private double BuildPicker(GuiComposer compo, ElementBounds column, double y)
        {
            // Offer depends on where the layer lands: the bottom one fills the cloth, the rest are
            // charges on top of it.
            var patterns = EmblemHandler.AvailablePatternsForLayer(Draft.Layers.Count);
            var colors = EmblemHandler.AvailableColors();
            bool full = Draft.Layers.Count >= EmblemHandler.MaxLayers();

            const double AddButtonWidth = 130;

            double height = Card.HeaderHeight + InputHeight + Card.Gap
                          + SwatchSize + Card.LineHeight + Card.Padding * 2;
            ElementBounds inner = Card.Frame(compo, column, y,
                height, Lang.Get(Draft.Layers.Count == 0 ? "claims:gui-emblem-add-background" : "claims:gui-emblem-add"));

            double dropWidth = (inner.fixedWidth - Card.Gap * 2 - AddButtonWidth) / 2;
            if (dropWidth < 80) dropWidth = 80;

            string[] patternCodes = patterns.ToArray();
            string[] patternNames = patternCodes.Select(PatternName).ToArray();
            var patternBounds = inner.FlatCopy().WithFixedSize(dropWidth, InputHeight);
            // Max(0, ...) falls back to the first entry when the remembered pick is not in this
            // list - the same fallback SelectedLayer makes.
            compo.AddDropDown(patternCodes, patternNames, System.Math.Max(0, System.Array.IndexOf(patternCodes, Draft.Pattern)),
                (code, selected) =>
                {
                    if (!selected) return;
                    Draft.Pattern = code;
                    Gui.BuildMainWindow();
                }, patternBounds, "emblem-pattern");

            string[] colorCodes = colors.ToArray();
            string[] colorNames = colorCodes.Select(ColorName).ToArray();
            var colorBounds = inner.FlatCopy().WithFixedSize(dropWidth, InputHeight);
            colorBounds.fixedX += dropWidth + Card.Gap;
            compo.AddDropDown(colorCodes, colorNames, System.Math.Max(0, System.Array.IndexOf(colorCodes, Draft.Color)),
                (code, selected) =>
                {
                    if (!selected) return;
                    Draft.Color = code;
                    Gui.BuildMainWindow();
                }, colorBounds, "emblem-color");

            // Worked out once for the button and both previews, so they cannot disagree.
            string nextLayer = SelectedLayer(patternCodes, colorCodes);

            var addBounds = inner.FlatCopy().WithFixedSize(AddButtonWidth, InputHeight);
            addBounds.fixedX += dropWidth * 2 + Card.Gap * 2;
            compo.AddButton(Lang.Get("claims:gui-emblem-add-layer"),
                new ActionConsumable(() => AddLayer(nextLayer)), addBounds, EnumButtonStyle.Normal);

            if (full)
            {
                Tooltip.Add(compo, Lang.Get("claims:gui-emblem-full"), addBounds, "tip-emblemfull");
            }

            // Two captioned previews: the layer alone shows its shape, the result shows whether the
            // colours read against the field.
            double previewY = inner.fixedY + InputHeight + Card.Gap;

            var swatch = inner.FlatCopy().WithFixedSize(SwatchSize, SwatchSize);
            swatch.fixedY = previewY;
            compo.AddEmblem(nextLayer, swatch, "emblem-next");
            AddCaption(compo, inner, previewY + SwatchSize, 0, Lang.Get("claims:gui-emblem-preview-layer"));

            var resultBounds = inner.FlatCopy().WithFixedSize(SwatchSize, SwatchSize);
            resultBounds.fixedX += SwatchSize + Card.Gap;
            resultBounds.fixedY = previewY;
            compo.AddEmblem(EmblemHandler.Join(Draft.Layers.Concat(new[] { nextLayer })),
                resultBounds, "emblem-result");
            AddCaption(compo, inner, previewY + SwatchSize, SwatchSize + Card.Gap,
                Lang.Get("claims:gui-emblem-preview-result"));

            return y + height + Card.Gap;
        }

        /// <summary>Small grey label under a preview, aligned to its left edge.</summary>
        private void AddCaption(GuiComposer compo, ElementBounds inner, double y, double offsetX, string text)
        {
            var bounds = inner.FlatCopy().WithFixedSize(SwatchSize, Card.LineHeight);
            bounds.fixedX += offsetX;
            bounds.fixedY = y;
            compo.AddStaticText(text, CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                bounds, "emblem-caption-" + offsetX);
        }

        /// <summary>The layer the pickers point at. A pick the current position does not allow falls
        /// back to the first entry, which is what the drop-down shows as selected.</summary>
        private string SelectedLayer(string[] patterns, string[] colors)
        {
            string pattern = patterns.Contains(Draft.Pattern) ? Draft.Pattern : patterns.FirstOrDefault();
            string color = colors.Contains(Draft.Color) ? Draft.Color : colors.FirstOrDefault();
            if (pattern == null || color == null) return "";
            return EmblemHandler.MakeLayer(pattern, color);
        }

        /// <summary>Adds the layer the previews are showing.</summary>
        private bool AddLayer(string layer)
        {
            if (Draft.Layers.Count >= EmblemHandler.MaxLayers()) return true;
            if (layer.Length == 0 || !EmblemHandler.IsValidLayer(layer)) return true;

            Draft.Layers.Add(layer);
            Gui.BuildMainWindow();
            return true;
        }

        private bool Apply()
        {
            // An empty draft clears the emblem; the commands treat a missing argument that way.
            string command = Draft.ForAlliance ? "/alliance set emblem " : "/city set emblem ";
            Send((command + Draft.AsString()).TrimEnd());
            GoTo(Draft.ForAlliance ? EnumSelectedTab.AllianceInfoPage : EnumSelectedTab.City);
            return true;
        }

        /// <summary>"cross_white" as "Cross, white" - translated where a translation exists.</summary>
        private static string LayerName(string layer)
        {
            if (!EmblemHandler.TrySplitLayer(layer, out string pattern, out string color)) return layer;
            return PatternName(pattern) + ", " + ColorName(color);
        }

        private static string PatternName(string pattern) => Translated("claims:emblem-pattern-" + pattern, pattern);
        private static string ColorName(string color) => Translated("claims:emblem-color-" + color, color);

        /// <summary>
        /// Falls back to the raw asset name when a pattern has no translation - there are hundreds
        /// of them, and a missing entry should read as a name, not as a lang key.
        /// </summary>
        private static string Translated(string key, string fallback)
            => Lang.HasTranslation(key, true, false) ? Lang.Get(key) : fallback.Replace('_', ' ');
    }
}
