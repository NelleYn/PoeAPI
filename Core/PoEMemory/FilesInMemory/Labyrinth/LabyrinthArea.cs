using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.FilesInMemory.Labyrinth;
public class LabyrinthArea : RemoteMemoryObject
{
    private string _id;
    private List<WorldArea> _normalWorldAreas;
    private List<WorldArea> _cruelWorldAreas;
    private List<WorldArea> _mercilessWorldAreas;
    private List<WorldArea> _endgameWorldAreas;
    public string Id
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<WorldArea> NormalWorldAreas
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<WorldArea> CruelWorldAreas
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<WorldArea> MercilessWorldAreas
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<WorldArea> EndgameWorldAreas
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private List<WorldArea> GetAreas(int offset)
    {
        //IL_0004: Unknown result type (might be due to invalid IL or missing references)
        _ = this + offset;
        return (List<WorldArea>)(object)this;
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}