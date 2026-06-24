using ExileCore.PoEMemory.FilesInMemory.Village;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageWorker : BaseVillageWorker
{
    public VillageJob Job => (VillageJob)(this + (long)this);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}