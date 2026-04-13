using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace claims.src;

public class ImGuiInventoryGrid
{
    private readonly ICoreClientAPI _capi;
    private readonly ImGuiSlotRenderer _slotRenderer;
    private readonly ItemIconAtlas _iconAtlas;
    private readonly Action<object> _sendPacket;
    private readonly int _cols;
    private readonly float _padding;
    private InventoryBase _inventory;
    private int _hoveredSlotId = -1;

    /// <summary>When true, DropMouseSlotItems is blocked by Harmony patch.</summary>
    public static bool SuppressMouseDrop { get; set; }

    public ImGuiInventoryGrid(
        ICoreClientAPI capi,
        ImGuiSlotRenderer slotRenderer,
        ItemIconAtlas iconAtlas,
        Action<object> sendPacket = null,
        int cols = 4,
        float padding = 3f)
    {
        _capi = capi;
        _slotRenderer = slotRenderer;
        _iconAtlas = iconAtlas;
        _sendPacket = sendPacket;
        _cols = cols;
        _padding = padding;
    }

    public void SetInventory(InventoryBase inventory)
    {
        _inventory = inventory;
    }

    public void Draw()
    {
        if (_inventory == null) return;

        SuppressMouseDrop = true;

        int slotCount = _inventory.Count;

        int slotSize = _slotRenderer.SlotSize;
        float step = slotSize + _padding;
        int rows = (int)Math.Ceiling((float)slotCount / _cols);

        Vector2 gridSize = new Vector2(_cols * step - _padding, rows * step - _padding);
        Vector2 origin = ImGui.GetCursorScreenPos();

        ImGui.Dummy(gridSize);

        _hoveredSlotId = -1;

        for (int i = 0; i < slotCount; i++)
        {
            int col = i % _cols;
            int row = i / _cols;
            Vector2 slotPos = origin + new Vector2(col * step, row * step);
            DrawSlot(i, slotPos, slotSize);
        }
    }


    private void DrawSlot(int slotId, Vector2 pos, int size)
    {
        _slotRenderer.DrawSlotBackground(pos);

        ImGui.SetCursorScreenPos(pos);
        ImGui.InvisibleButton("slot" + slotId, new Vector2(size, size));

        bool hovered = ImGui.IsItemHovered();
        bool leftClick = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        bool rightClick = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right);

        if (hovered)
        {
            _hoveredSlotId = slotId;
            _slotRenderer.DrawSlotHighlight(pos);
        }

        ItemSlot slot = _inventory[slotId];
        if (slot?.Itemstack != null)
        {
            float iconSize = size * 0.75f;
            float iconOffset = (size - iconSize) * 0.5f;
            ImGui.SetCursorScreenPos(pos + new Vector2(iconOffset, iconOffset));
            _iconAtlas.Draw(slot.Itemstack, new Vector2(iconSize, iconSize), showTooltip: false);

            if (slot.Itemstack.StackSize > 1)
            {
                string countText = slot.Itemstack.StackSize.ToString();
                Vector2 textSize = ImGui.CalcTextSize(countText);
                Vector2 textPos = pos + new Vector2(size - textSize.X - 2, size - textSize.Y - 1);
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddText(textPos + new Vector2(1, 1), 0xFF000000, countText);
                drawList.AddText(textPos, 0xFFFFFFFF, countText);
            }

            if (hovered)
            {
                ImGui.BeginTooltip();
                ImGui.Text(slot.Itemstack.GetName());
                ImGui.EndTooltip();
            }
        }

        if (leftClick)
            OnSlotClick(slotId, EnumMouseButton.Left);
        else if (rightClick)
            OnSlotClick(slotId, EnumMouseButton.Right);
    }

    private void OnSlotClick(int slotId, EnumMouseButton button)
    {
        var player = _capi.World.Player;
        var mouseSlot = player.InventoryManager.MouseItemSlot;
        if (mouseSlot == null) return;

        EnumModifierKey modifiers = 0;
        if (_capi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] || _capi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight])
            modifiers |= EnumModifierKey.SHIFT;
        if (_capi.Input.KeyboardKeyState[(int)GlKeys.ControlLeft] || _capi.Input.KeyboardKeyState[(int)GlKeys.ControlRight])
            modifiers |= EnumModifierKey.CTRL;

        bool shiftDown = (modifiers & EnumModifierKey.SHIFT) != 0;

        var op = new ItemStackMoveOperation(
            _capi.World,
            button,
            modifiers,
            shiftDown ? EnumMergePriority.AutoMerge : EnumMergePriority.DirectMerge,
            0
        );
        op.ActingPlayer = player;

        if (shiftDown)
            op.RequestedQuantity = _inventory[slotId].StackSize;

        ItemSlot sourceSlot = shiftDown ? _inventory[slotId] : mouseSlot;

        // Pause server sync so it doesn't overwrite local changes (like the game does during drag)
        _inventory.InvNetworkUtil.PauseInventoryUpdates = true;
        mouseSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = true;

        object packet = _inventory.ActivateSlot(slotId, sourceSlot, ref op);

        _inventory.InvNetworkUtil.PauseInventoryUpdates = false;
        mouseSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = false;

        if (packet != null && _sendPacket != null)
        {
            object[] packets = packet as object[];
            if (packets != null)
            {
                for (int i = 0; i < packets.Length; i++)
                    _sendPacket(packets[i]);
            }
            else
            {
                _sendPacket(packet);
            }
        }

        _capi.Input.TriggerOnMouseClickSlot(_inventory[slotId]);
    }

}
