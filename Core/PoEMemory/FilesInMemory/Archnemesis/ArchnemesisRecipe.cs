using System;
using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory.Archnemesis;
[Obsolete]
public class ArchnemesisRecipe : RemoteMemoryObject
{
    private ArchnemesisMod _outcome;
    private List<ArchnemesisMod> _components;
    public ArchnemesisMod Outcome
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<ArchnemesisMod> Components
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