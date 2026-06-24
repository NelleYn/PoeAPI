using System.Runtime.CompilerServices;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;
public class QuestFlagDat : RemoteMemoryObject
{
    private string _id;
    private uint? _hash;
    public string Id
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public uint Hash
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int Index
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (int)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public QuestFlag MatchingFlag => (QuestFlag)this;
}