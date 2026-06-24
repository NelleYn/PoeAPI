using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory;
public class NecropolisCraftingMod : RemoteMemoryObject
{
    private string _id;
    private List<StatsDat.StatRecord> _stats;
    private List<int> _statValues;
    public string Id
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<StatsDat.StatRecord> Stats
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<int> StatValues
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