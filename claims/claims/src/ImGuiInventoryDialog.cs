using Vintagestory.API.Client;

namespace claims.src;

/// <summary>
/// Invisible GuiDialog that keeps DialogsOpened > 0 while ImGui inventory is active.
/// Without this, the game auto-drops items from mouse cursor when no dialog is open.
/// </summary>
public class ImGuiInventoryDialog : GuiDialog
{
    public ImGuiInventoryDialog(ICoreClientAPI capi) : base(capi)
    {
    }

    public override string ToggleKeyCombinationCode => null;

    public override bool ShouldReceiveMouseEvents() => false;
    public override bool ShouldReceiveKeyboardEvents() => false;
    public override bool ShouldReceiveRenderEvents() => false;

    public override void OnGuiOpened()
    {
        // No composer needed — this dialog is invisible
    }

    public override void OnGuiClosed()
    {
    }
}
