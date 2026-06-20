using ExileCore.PoEMemory.FilesInMemory.Harvest;

namespace ExileCore.PoEMemory.Components;
public class HarvestSeedSpawnDescriptor : RemoteMemoryObject
{
    private HarvestSeed _seed;
    public HarvestSeed Seed
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int Count => this + 16;
}