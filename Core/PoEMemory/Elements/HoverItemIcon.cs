using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element describing the item currently hovered, resolving the relevant tooltip,
/// frame, and item across inventory, ground, and chat contexts.
/// </summary>
public class HoverItemIcon : Element
{
    private static readonly int InventPosXOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.InventPosX));
    private static readonly int InventPosYOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.InventPosY));

    private static readonly int ItemsOnGroundLabelElementOffset =
        Extensions.GetOffset<IngameUElementsOffsets>(nameof(IngameUElementsOffsets.itemsOnGroundLabelRoot));

    private ToolTipType? toolTip;

    /// <summary>
    /// Gets the tooltip element shown for a hovered inventory item.
    /// </summary>
    public Element InventoryItemTooltip => ReadObject<Element>(Address + 0x338);

    /// <summary>
    /// Gets the tooltip element shown for an item linked in chat.
    /// </summary>
    public Element ItemInChatTooltip => ReadObject<Element>(Address + 0x1A8);

    /// <summary>
    /// Gets the tooltip element shown for an item lying on the ground.
    /// </summary>
    public ItemOnGroundTooltip ToolTipOnGround => TheGame.IngameState.IngameUi.ItemOnGroundTooltip;

    /// <summary>
    /// Gets the hovered item's X position within its inventory.
    /// </summary>
    public int InventPosX => M.Read<int>(Address + InventPosXOff);

    /// <summary>
    /// Gets the hovered item's Y position within its inventory.
    /// </summary>
    public int InventPosY => M.Read<int>(Address + InventPosYOff);

    /// <summary>
    /// Gets the kind of tooltip currently displayed, caching the resolved value.
    /// </summary>
    public ToolTipType ToolTipType
    {
        get
        {
            try
            {
                return (ToolTipType) (toolTip ?? (toolTip = GetToolTipType()));
            }
            catch (Exception e)
            {
                Core.Logger.Error($"{e.Message} {e.StackTrace}");
                return ToolTipType.None;
            }
        }
    }

    /// <summary>
    /// Gets the tooltip element for the current <see cref="ToolTipType"/>, or <c>null</c> when none applies.
    /// </summary>
    public Element Tooltip
    {
        get
        {
            switch (ToolTipType)
            {
                case ToolTipType.ItemOnGround:
                    return ToolTipOnGround.Tooltip;

                case ToolTipType.InventoryItem:
                    return InventoryItemTooltip;
                case ToolTipType.ItemInChat:
                    return ItemInChatTooltip.Children[1];
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the item frame element for the current <see cref="ToolTipType"/>, or <c>null</c> when none applies.
    /// </summary>
    public Element ItemFrame
    {
        get
        {
            switch (ToolTipType)
            {
                case ToolTipType.ItemOnGround:
                    return ToolTipOnGround.ItemFrame;
                case ToolTipType.ItemInChat:
                    return ItemInChatTooltip.Children[0];
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Gets the hovered item entity for the current <see cref="ToolTipType"/>, or <c>null</c> when unavailable.
    /// </summary>
    public Entity Item
    {
        get
        {
            switch (ToolTipType)
            {
                case ToolTipType.ItemOnGround:
                    // This offset is same as Game.IngameState.IngameUi.ItemsOnGroundLabels offset.
                    var le = TheGame.IngameState.IngameUi.ReadObjectAt<ItemsOnGroundLabelElement>(ItemsOnGroundLabelElementOffset);

                    if (le == null)
                        return null;

                    var e = le.ItemOnHover;
                    return e?.GetComponent<WorldItem>()?.ItemEntity;
                case ToolTipType.InventoryItem:
                    return ReadObject<Entity>(Address + 0x388);
                case ToolTipType.ItemInChat:
                    // currently cannot find it.
                    return null;
            }

            return null;
        }
    }

    private ToolTipType GetToolTipType()
    {
        try
        {
            if (InventoryItemTooltip != null && InventoryItemTooltip.IsVisible) return ToolTipType.InventoryItem;

            if (ToolTipOnGround != null && ToolTipOnGround.Tooltip != null && ToolTipOnGround.TooltipUI != null &&
                ToolTipOnGround.TooltipUI.IsVisible) return ToolTipType.ItemOnGround;

            if (ItemInChatTooltip != null && ItemInChatTooltip.IsVisible && ItemInChatTooltip.ChildCount > 1 &&
                ItemInChatTooltip.Children[0].IsVisible && ItemInChatTooltip.Children[1].IsVisible) return ToolTipType.ItemInChat;
        }
        catch (Exception e)
        {
            Core.Logger.Error($"HoverItemIcon.cs -> {e}");
        }

        return ToolTipType.None;
    }
}
