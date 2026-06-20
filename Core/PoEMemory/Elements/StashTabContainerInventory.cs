using ExileCore.PoEMemory.MemoryObjects;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class StashTabContainerInventory : StructuredRemoteMemoryObject<StashTabContainerInventoryOffsets>
{
    public virtual Inventory Inventory
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element TabButton => (Element)(object)this;

    public string TabName
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}