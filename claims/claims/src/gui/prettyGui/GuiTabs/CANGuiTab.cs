using claims.src;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public abstract class CANGuiTab
    {
        protected static readonly Vector4 ColAdmin   = new Vector4(1.00f, 0.42f, 0.42f, 1f);
        protected static readonly Vector4 ColSection = new Vector4(0.95f, 0.78f, 0.35f, 1f);
        protected static readonly Vector4 ColLabel   = new Vector4(0.68f, 0.68f, 0.68f, 1f);
        protected static readonly Vector4 ColHint    = new Vector4(0.50f, 0.50f, 0.50f, 1f);
        protected static readonly Vector4 ColValue   = new Vector4(1.00f, 0.88f, 0.45f, 1f);
        protected static readonly Vector4 ColRedBtn  = new Vector4(0.55f, 0.10f, 0.10f, 1f);
        protected static readonly Vector4 ColRedBtnH = new Vector4(0.75f, 0.20f, 0.20f, 1f);
        protected static readonly Vector4 ColRedBtnA = new Vector4(0.38f, 0.05f, 0.05f, 1f);

        // Палитра для подтверждающих/отменяющих действий (совпадает с цветами, что были разбросаны по табам)
        protected static readonly Vector4 ColGreen  = new Vector4(0.20f, 0.55f, 0.30f, 1f);
        protected static readonly Vector4 ColGreenH = new Vector4(0.30f, 0.65f, 0.40f, 1f);
        protected static readonly Vector4 ColGreenA = new Vector4(0.15f, 0.45f, 0.25f, 1f);
        protected static readonly Vector4 ColRed    = new Vector4(0.70f, 0.25f, 0.20f, 1f);
        protected static readonly Vector4 ColRedH   = new Vector4(0.80f, 0.35f, 0.30f, 1f);
        protected static readonly Vector4 ColRedA   = new Vector4(0.60f, 0.20f, 0.15f, 1f);

        public ICoreClientAPI capi;
        public IconHandler iconHandler;
        private claimsGui _guiSys;
        protected claimsGui GuiSys => _guiSys ??= capi.ModLoader.GetModSystem<claimsGui>();
        public abstract void DrawTab();

        protected static void AdminHeader(string title, string subtitle = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColAdmin);
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text(title);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
            if (subtitle != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
                ImGui.TextWrapped(subtitle);
                ImGui.PopStyleColor();
            }
            ImGui.Separator();
            ImGui.Spacing();
        }

        /// <summary>Window-width centered title with the given color and scale.</summary>
        protected static void CenteredTitle(string text, Vector4 color, float scale = 1.3f)
        {
            ImGui.SetWindowFontScale(scale);
            float textWidth = ImGui.CalcTextSize(text).X;
            float windowWidth = ImGui.GetWindowSize().X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
        }

        /// <summary>Centered clickable title (transparent button). Returns true on click.</summary>
        protected static bool CenteredTitleButton(string text, Vector4 color, float scale = 1.3f)
        {
            ImGui.SetWindowFontScale(scale);
            float textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetWindowFontScale(1.0f);
            float windowWidth = ImGui.GetWindowSize().X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.05f));
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.SetWindowFontScale(scale);
            bool r = ImGui.Button(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor(4);
            return r;
        }

        protected static void SectionTitle(string text)
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(text);
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }

        /// <summary>Push the cursor down so the next row sits at the bottom, reserving <paramref name="reservedHeight"/> px for it.</summary>
        protected static void AlignBottom(float reservedHeight = 80f)
        {
            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - reservedHeight);
        }

        protected static void Hint(string text)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
            ImGui.TextWrapped(text);
            ImGui.PopStyleColor();
        }

        protected static void HelpMarker(string desc)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
            ImGui.Text("(?)");
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(desc);
        }

        protected static void Label(string text)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
            ImGui.Text(text);
            ImGui.PopStyleColor();
        }

        /// <summary>Серая подпись + значение акцентным цветом на той же строке.</summary>
        protected static void LabelValue(string label, string value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColLabel);
            ImGui.Text(label);
            ImGui.PopStyleColor();
            ImGui.SameLine(0, 6);
            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
            ImGui.Text(value);
            ImGui.PopStyleColor();
        }

        protected const ImGuiTableFlags TableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;

        protected static readonly Vector4 ColWarning = new Vector4(1.00f, 0.62f, 0.12f, 1f);
        protected static readonly Vector4 ColDanger  = new Vector4(0.90f, 0.28f, 0.28f, 1f);

        /// <summary>Returns a warning/danger color when ratio is near or over the limit, null when normal.</summary>
        protected static Vector4? StateColor(float ratio)
        {
            if (ratio >= 1.0f) return ColDanger;
            if (ratio >= 0.85f) return ColWarning;
            return null;
        }

        // --- Стилизованные кнопки (убирают повторяющиеся Push/PopStyleColor по табам) ---

        protected static bool GreenButton(string label)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColGreen);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColGreenH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColGreenA);
            bool r = ImGui.Button(label);
            ImGui.PopStyleColor(3);
            return r;
        }

        protected static bool RedButton(string label)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColRed);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColRedA);
            bool r = ImGui.Button(label);
            ImGui.PopStyleColor(3);
            return r;
        }

        protected static readonly Vector4 ColBlue  = new Vector4(0.20f, 0.45f, 0.70f, 1f);
        protected static readonly Vector4 ColBlueH = new Vector4(0.30f, 0.55f, 0.80f, 1f);
        protected static readonly Vector4 ColBlueA = new Vector4(0.15f, 0.35f, 0.60f, 1f);

        protected static bool BlueButton(string label)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColBlue);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColBlueH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColBlueA);
            bool r = ImGui.Button(label);
            ImGui.PopStyleColor(3);
            return r;
        }

        protected static bool BackButton() => BlueButton(Lang.Get("claims:gui-back"));

        /// <summary>Иконка-кнопка темы (без перекраски) с опциональным тултипом.</summary>
        protected bool IconButton(string strId, string icon, float size, string tooltip = null)
        {
            bool r = ImGui.ImageButton(strId, iconHandler.GetOrLoadIcon(icon), new Vector2(size));
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            return r;
        }

        /// <summary>Зелёная иконка-кнопка (созидательное действие) с опциональным тултипом.</summary>
        protected bool GreenIconButton(string strId, string icon, float size, string tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColGreen);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColGreenH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColGreenA);
            bool r = ImGui.ImageButton(strId, iconHandler.GetOrLoadIcon(icon), new Vector2(size));
            ImGui.PopStyleColor(3);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            return r;
        }

        /// <summary>Красная иконка-кнопка (разрушительное действие) с опциональным тултипом.</summary>
        protected bool RedIconButton(string strId, string icon, float size, string tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColRed);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColRedH);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColRedA);
            bool r = ImGui.ImageButton(strId, iconHandler.GetOrLoadIcon(icon), new Vector2(size));
            ImGui.PopStyleColor(3);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            return r;
        }

        /// <summary>Прогресс-бар с золотой заливкой темы.</summary>
        protected static void ProgressBar(float fraction, Vector2 size, string overlay = null)
        {
            if (fraction < 0f) fraction = 0f;
            if (fraction > 1f) fraction = 1f;
            ImGui.ProgressBar(fraction, size, overlay ?? ((int)(fraction * 100f)).ToString() + "%");
        }
    }
}
