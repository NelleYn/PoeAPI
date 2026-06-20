namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Wraps the visual appearance (name, texture, and model) of an equipped inventory item.
/// </summary>
public class InventoryVisual : RemoteMemoryObject
{
    /// <summary>Gets the visual name of the equipped item.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the texture path of the equipped item.</summary>
    public string Texture => M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>Gets the model path of the equipped item.</summary>
    public string Model => M.ReadStringU(M.Read<long>(Address + 0x10));
}
