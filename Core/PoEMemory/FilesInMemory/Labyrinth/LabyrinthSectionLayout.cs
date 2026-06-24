using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory.Labyrinth;
public class LabyrinthSectionLayout : RemoteMemoryObject
{
    private LabyrinthSectionDat _section;
    private LabyrinthArea _area;
    private List<LabyrinthNodeOverride> _nodeOverrides;
    public LabyrinthSectionDat Section
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public LabyrinthArea Area
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<LabyrinthNodeOverride> NodeOverrides
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