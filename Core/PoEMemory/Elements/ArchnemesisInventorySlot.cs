using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Elements;
public class ArchnemesisInventorySlot : Element
{
    private const int ItemOffset = 992;
    public bool HasItem => this + (long)this > 0L;
    public ArchnemesisMod Item => (ArchnemesisMod)(this + (long)this);
}