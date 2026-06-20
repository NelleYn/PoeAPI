namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the visual appearance of items equipped in each equipment slot.
/// </summary>
public class Inventories : Component
{
    /// <summary>Gets the visual of the item in the left-hand (main hand) slot.</summary>
    public InventoryVisual LeftHand => ReadVisual(0);

    /// <summary>Gets the visual of the item in the right-hand (off hand) slot.</summary>
    public InventoryVisual RightHand => ReadVisual(1);

    /// <summary>Gets the visual of the equipped body armour.</summary>
    public InventoryVisual Chest => ReadVisual(2);

    /// <summary>Gets the visual of the equipped helmet.</summary>
    public InventoryVisual Helm => ReadVisual(3);

    /// <summary>Gets the visual of the equipped gloves.</summary>
    public InventoryVisual Gloves => ReadVisual(4);

    /// <summary>Gets the visual of the equipped boots.</summary>
    public InventoryVisual Boots => ReadVisual(5);

    /// <summary>Gets the visual of the item in the unknown slot.</summary>
    public InventoryVisual Unknown => ReadVisual(6);

    /// <summary>Gets the visual of the item in the left ring slot.</summary>
    public InventoryVisual LeftRing => ReadVisual(7);

    /// <summary>Gets the visual of the item in the right ring slot.</summary>
    public InventoryVisual RightRing => ReadVisual(8);

    /// <summary>Gets the visual of the equipped belt.</summary>
    public InventoryVisual Belt => ReadVisual(9);

    /// <summary>Reads the <see cref="InventoryVisual"/> at the given equipment slot index.</summary>
    /// <param name="index">Zero-based equipment slot index.</param>
    internal InventoryVisual ReadVisual(int index)
    {
        index++; //Mean (Address + 0x40 + index * 0x40)
        return ReadObject<InventoryVisual>(Address + index * 0x40);
    }
}
