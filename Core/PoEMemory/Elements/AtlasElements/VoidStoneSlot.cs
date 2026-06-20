using ExileCore.PoEMemory.Elements.InventoryElements;

namespace ExileCore.PoEMemory.Elements.AtlasElements;
public class VoidStoneSlot : Element
{
    public bool IsEmpty => this == null;

    public NormalInventoryItem Voidstone
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}