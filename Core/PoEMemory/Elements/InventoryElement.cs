using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element exposing the player's inventories keyed by <see cref="InventoryIndex"/>.
/// </summary>
public class InventoryElement : Element
{
    private InventoryList _allInventories;
    private InventoryList AllInventories => _allInventories = _allInventories ?? GetObjectAt<InventoryList>(0x340);

    /// <summary>
    /// Gets the <see cref="Inventory"/> for the specified inventory index.
    /// </summary>
    /// <param name="k">The inventory index to look up.</param>
    public Inventory this[InventoryIndex k] => AllInventories[k];
}
