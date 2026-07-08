namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory prophecy chain record referenced by <see cref="ProphecyDat.ProphecyChainPtr"/>.
/// Only the address is currently known, so this exposes no fields beyond the memory object itself.
/// </summary>
public class ProphecyChainDat : RemoteMemoryObject
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"0x{Address:X}";
    }
}
