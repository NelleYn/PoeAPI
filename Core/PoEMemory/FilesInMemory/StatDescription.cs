using System.Collections.Generic;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;
public class StatDescription : RemoteMemoryObject
{
    private List<StatDescriptionSection> _sections;
    private List<GameStat> _stats;
    public List<StatDescriptionSection> Sections
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<GameStat> Stats
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