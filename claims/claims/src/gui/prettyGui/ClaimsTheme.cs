using System.Numerics;
using VSImGui;

namespace claims.src.gui.prettyGui
{
    /// <summary>
    /// Единая «пергамент/средневековье» тема для всего ImGui-интерфейса мода.
    /// Применяется один раз к <see cref="ImGuiModSystem.DefaultStyle"/> — система каждый кадр
    /// re-push'ит DefaultStyle, поэтому мутация её свойств подхватывается автоматически
    /// (Style.Push/Pop и FontManager.Loaded помечены internal и из мода недоступны).
    /// Меняет «нетематические» части (фон, рамки, скроллбары, заголовки, табы, разделители),
    /// локальные PushStyleColor в табах продолжают переопределять цвета поверх темы.
    /// </summary>
    public static class ClaimsTheme
    {
        // --- Палитра ---
        public static readonly Vector4 WindowBg   = new(0.10f, 0.07f, 0.03f, 0.99f); // тёмный пергамент/дерево
        public static readonly Vector4 ChildBg    = new(0.16f, 0.11f, 0.05f, 0.55f); // карточки
        public static readonly Vector4 PopupBg    = new(0.09f, 0.06f, 0.03f, 0.99f);
        public static readonly Vector4 FrameBg     = new(0.26f, 0.18f, 0.09f, 1.0f); // инпуты/комбо
        public static readonly Vector4 FrameBgH    = new(0.34f, 0.24f, 0.12f, 1.0f);
        public static readonly Vector4 FrameBgA    = new(0.42f, 0.30f, 0.15f, 1.0f);

        public static readonly Vector4 Text        = new(0.99f, 0.96f, 0.89f, 1.0f); // тёплый почти-белый, высокий контраст
        public static readonly Vector4 TextDisabled= new(0.68f, 0.60f, 0.48f, 1.0f);

        public static readonly Vector4 Border      = new(0.46f, 0.34f, 0.16f, 0.55f);
        public static readonly Vector4 Gold        = new(0.85f, 0.66f, 0.26f, 1.0f);  // акцент
        public static readonly Vector4 GoldDim     = new(0.62f, 0.46f, 0.18f, 1.0f);
        public static readonly Vector4 Bronze      = new(0.40f, 0.28f, 0.13f, 1.0f);  // база кнопок
        public static readonly Vector4 BronzeA     = new(0.32f, 0.22f, 0.10f, 1.0f);

        public static void Apply(Style s)
        {
            if (s == null) return;

            // Шрифт: Lora — серифный, «книжный», с кириллицей (безопасно для русской локали).
            // Чтобы перейти на чисто-медиевальный Almendra (только латиница!), замените на "Almendra-Bold".
            s.FontSize = 18;
            s.FontName = "Lora-Regular";

            // Формы
            s.RoundingWindow    = 6f;
            s.RoundingChild     = 6f;
            s.RoundingPopup     = 6f;
            s.RoundingFrame     = 4f;
            s.RoundingScrollbar = 4f;
            s.RoundingGrab      = 3f;
            s.RoundingTab       = 4f;

            s.BorderWindow = 1f;
            s.BorderChild  = 1f;
            s.BorderPopup  = 1f;
            s.BorderFrame  = 1f;
            s.BorderTab    = 0f;

            s.PaddingWindow   = new Vector2(12, 10);
            s.PaddingFrame    = new Vector2(8, 5);
            s.PaddingCell     = new Vector2(6, 4);
            s.SpacingItem     = new Vector2(8, 7);
            s.SpacingItemInner= new Vector2(6, 4);
            s.SizeScrollbar   = 14f;
            s.SizeGrabMin     = 10f;

            // Цвета
            s.ColorText               = Text;
            s.ColorTextDisabled       = TextDisabled;
            s.ColorBackgroundWindow   = WindowBg;
            s.ColorBackgroundChild    = ChildBg;
            s.ColorBackgroundPopup    = PopupBg;
            s.ColorBorder             = Border;
            s.ColorBorderShadow       = new Vector4(0, 0, 0, 0);

            s.ColorBackgroundFrame        = FrameBg;
            s.ColorBackgroundFrameHovered = FrameBgH;
            s.ColorBackgroundFrameActive  = FrameBgA;

            s.ColorBackgroundTitle          = new Vector4(0.20f, 0.13f, 0.05f, 1.0f);
            s.ColorBackgroundTitleActive    = new Vector4(0.30f, 0.19f, 0.07f, 1.0f);
            s.ColorBackgroundTitleCollapsed = new Vector4(0.16f, 0.11f, 0.05f, 0.7f);
            s.ColorBackgroundMenuBar        = new Vector4(0.20f, 0.13f, 0.05f, 1.0f);

            s.ColorBackgroundScrollbar = new Vector4(0.12f, 0.08f, 0.04f, 0.6f);
            s.ColorScrollbarGrab        = GoldDim;
            s.ColorScrollbarGrabHovered = Gold;
            s.ColorScrollbarGrabActive  = Gold;

            s.ColorCheckMark        = Gold;
            s.ColorSliderGrab       = GoldDim;
            s.ColorSliderGrabActive = Gold;

            s.ColorButton        = Bronze;
            s.ColorButtonHovered = GoldDim;
            s.ColorButtonActive  = BronzeA;

            s.ColorHeader        = new Vector4(0.40f, 0.28f, 0.13f, 0.65f);
            s.ColorHeaderHovered = new Vector4(0.55f, 0.40f, 0.18f, 0.85f);
            s.ColorHeaderActive  = GoldDim;

            s.ColorSeparator        = Border;
            s.ColorSeparatorHovered = GoldDim;
            s.ColorSeparatorActive  = Gold;

            s.ColorResizeGrip        = new Vector4(0.46f, 0.34f, 0.16f, 0.4f);
            s.ColorResizeGripHovered = GoldDim;
            s.ColorResizeGripActive  = Gold;

            s.ColorTab               = new Vector4(0.30f, 0.20f, 0.09f, 1.0f);
            s.ColorTabHovered        = GoldDim;
            s.ColorTabActive         = new Vector4(0.45f, 0.32f, 0.14f, 1.0f);
            s.ColorTabUnfocused      = new Vector4(0.22f, 0.15f, 0.07f, 1.0f);
            s.ColorTabUnfocusedActive= new Vector4(0.34f, 0.24f, 0.11f, 1.0f);

            s.ColorBackgroundTableHeader = new Vector4(0.30f, 0.20f, 0.09f, 1.0f);
            s.ColorTableBorderStrong     = new Vector4(0.46f, 0.34f, 0.16f, 1.0f);
            s.ColorTableBorderLight      = new Vector4(0.34f, 0.25f, 0.12f, 1.0f);
            s.ColorBackgroundTableRow    = new Vector4(0, 0, 0, 0);
            s.ColorBackgroundTableRowAlt = new Vector4(1f, 1f, 1f, 0.03f);

            s.ColorBackgroundTextSelected = new Vector4(0.62f, 0.46f, 0.18f, 0.45f);
            s.ColorPlotHistogram          = Gold; // используется ProgressBar
            s.ColorPlotHistogramHovered   = Gold;
            s.ColorNavHighlight           = Gold;
        }
    }
}
