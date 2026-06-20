using System;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.InventoryElements;

/// <summary>
/// UI element wrapping a single item slot in an inventory, exposing its grid position, size, and item entity.
/// </summary>
public class NormalInventoryItem : Element
{
    private static int InventPosXOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.InventPosX));
    private static int InventPosYOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.InventPosY));
    private static int WidthOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.Width));
    private static int HeightOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.Height));
    private static int ItemOff = Extensions.GetOffset<NormalInventoryItemOffsets>(nameof(NormalInventoryItemOffsets.Item));
    private Entity _item;
    private readonly Lazy<NormalInventoryItemOffsets> cachedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalInventoryItem"/> class.
    /// </summary>
    public NormalInventoryItem()
    {
        cachedValue = new Lazy<NormalInventoryItemOffsets>(() => M.Read<NormalInventoryItemOffsets>(Address));
    }

    /// <summary>
    /// Gets the item's X position within the inventory grid.
    /// </summary>
    public virtual int InventPosX => cachedValue.Value.InventPosX;

    /// <summary>
    /// Gets the item's Y position within the inventory grid.
    /// </summary>
    public virtual int InventPosY => cachedValue.Value.InventPosY;

    /// <summary>
    /// Gets the item's width in inventory grid cells.
    /// </summary>
    public virtual int ItemWidth => cachedValue.Value.Width;

    /// <summary>
    /// Gets the item's height in inventory grid cells.
    /// </summary>
    public virtual int ItemHeight => cachedValue.Value.Height;

    /// <summary>
    /// Gets the item entity occupying this slot.
    /// </summary>
    public Entity Item
    {
        get
        {
            if (_item == null) _item = GetObject<Entity>(cachedValue.Value.Item);
            return _item;
        }
    }

    /// <summary>
    /// Gets the tooltip kind for an inventory item, which is always <see cref="ToolTipType.InventoryItem"/>.
    /// </summary>
    public ToolTipType toolTipType => ToolTipType.InventoryItem;

    //0xB40 0xB48 some inf about image DDS
}
