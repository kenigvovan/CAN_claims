using System;
using System.Numerics;
using Cairo;
using ImGuiNET;
using Vintagestory.API.Client;

namespace claims.src;

public class ImGuiSlotRenderer : IDisposable
{
    private readonly ICoreClientAPI _capi;
    private int _slotBgTextureId;
    private int _slotHighlightTextureId;
    private int _slotSize;

    public int SlotSize => _slotSize;

    public ImGuiSlotRenderer(ICoreClientAPI capi, int slotSize = 48)
    {
        _capi = capi;
        _slotSize = slotSize;
        GenerateSlotBackground();
        GenerateSlotHighlight();
    }

    private void GenerateSlotBackground()
    {
        int size = _slotSize;
        using var surface = new ImageSurface(Format.Argb32, size, size);
        using var ctx = new Context(surface);

        // Fill — warm parchment (GuiStyle.DialogSlotBackColor / ColorSchematic)
        ctx.SetSourceRGBA(1.0, 0.886, 0.761, 1.0);
        RoundRect(ctx, 0, 0, size, size, 2);
        ctx.Fill();

        // Border — dark brown (GuiStyle.DialogSlotFrontColor / ColorWood)
        ctx.SetSourceRGBA(0.518, 0.361, 0.263, 1.0);
        RoundRect(ctx, 0, 0, size, size, 2);
        ctx.LineWidth = 3.0;
        ctx.Stroke();

        // Dark outline
        ctx.SetSourceRGBA(0, 0, 0, 0.8);
        RoundRect(ctx, 0, 0, size, size, 1);
        ctx.LineWidth = 2.0;
        ctx.Stroke();

        _slotBgTextureId = _capi.Gui.LoadCairoTexture(surface, true);
    }

    private void GenerateSlotHighlight()
    {
        int size = _slotSize + 4;
        using var surface = new ImageSurface(Format.Argb32, size, size);
        using var ctx = new Context(surface);

        // Cyan glow (GuiStyle.ActiveSlotColor)
        ctx.SetSourceRGBA(0.384, 0.773, 0.859, 1.0);
        RoundRect(ctx, 0, 0, size, size, 2);
        ctx.LineWidth = 6.0;
        ctx.Stroke();

        // Inner border
        ctx.SetSourceRGBA(0.384, 0.773, 0.859, 0.6);
        RoundRect(ctx, 2, 2, size - 4, size - 4, 1);
        ctx.LineWidth = 2.0;
        ctx.Stroke();

        _slotHighlightTextureId = _capi.Gui.LoadCairoTexture(surface, true);
    }

    private static void RoundRect(Context ctx, double x, double y, double w, double h, double r)
    {
        ctx.NewPath();
        ctx.Arc(x + r, y + r, r, Math.PI, -Math.PI / 2);
        ctx.Arc(x + w - r, y + r, r, -Math.PI / 2, 0);
        ctx.Arc(x + w - r, y + h - r, r, 0, Math.PI / 2);
        ctx.Arc(x + r, y + h - r, r, Math.PI / 2, Math.PI);
        ctx.ClosePath();
    }

    public void DrawSlotBackground(Vector2 screenPos)
    {
        var size = new Vector2(_slotSize, _slotSize);
        ImGui.SetCursorScreenPos(screenPos);
        ImGui.Image((IntPtr)_slotBgTextureId, size);
    }

    public void DrawSlotHighlight(Vector2 screenPos)
    {
        var offset = new Vector2(2, 2);
        var size = new Vector2(_slotSize + 4, _slotSize + 4);
        ImGui.SetCursorScreenPos(screenPos - offset);
        ImGui.Image((IntPtr)_slotHighlightTextureId, size);
    }

    public void Dispose()
    {
        if (_slotBgTextureId > 0)
            _capi.Render.GLDeleteTexture(_slotBgTextureId);
        if (_slotHighlightTextureId > 0)
            _capi.Render.GLDeleteTexture(_slotHighlightTextureId);
    }
}
