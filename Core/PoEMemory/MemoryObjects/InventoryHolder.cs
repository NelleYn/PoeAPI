namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps an in-memory inventory holder record, pairing an id with a <see cref="ServerInventory"/>.
/// </summary>
public class InventoryHolder : RemoteMemoryObject
{
    /// <summary>Size, in bytes, of the native inventory holder struct.</summary>
    internal const int StructSize = 0x20;

    /// <summary>Gets the holder's identifier.</summary>
    public int Id => M.Read<int>(Address);

    /// <summary>Gets the server inventory owned by this holder.</summary>
    public ServerInventory Inventory => ReadObject<ServerInventory>(Address + 0x8);

    /// <inheritdoc />
    public override string ToString()
    {
        return
            $"InventoryType: {Inventory.InventType}, InventorySlot: {Inventory.InventSlot}, IsRequested: {Inventory.IsRequested}, ItemsCount: {Inventory.Items.Count} CountItems: {Inventory.CountItems}";
    }
}
