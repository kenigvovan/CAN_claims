using claims.src;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;

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

        protected static void SectionTitle(string text)
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
            ImGui.Text(text);
            ImGui.PopStyleColor();
            ImGui.Spacing();
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
    }
}
