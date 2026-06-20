using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.MemoryObjects;
public class MapDevicePrimordialChoiceElement : Element
{
    public bool OptionIsSelected => (int)(this + (long)this) > 0;
    public AtlasPrimordialBossOption Option => (AtlasPrimordialBossOption)(this + (long)this);
}