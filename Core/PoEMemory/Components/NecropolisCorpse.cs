using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Components;
public class NecropolisCorpse : Component
{
    public int Level => this + 40;
    public MonsterVariety MonsterVariety => (MonsterVariety)(this + 24);
    public NecropolisCraftingMod CraftingMod => (NecropolisCraftingMod)(this + 48);
}