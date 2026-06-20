using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory;
public class StatDescriptionSection : StructuredRemoteMemoryObject<StatDescriptionStringContainer>
{
    private List<(int, int)> _statRanges;
    private List<StatHandling> _statConversionTypes;
    private string _string;
    public List<(int Min, int Max)> StatRanges
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<StatHandling> StatConversionTypes
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string String
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}