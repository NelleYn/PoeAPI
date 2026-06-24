using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory;
public class CurrencyItemDat : RemoteMemoryObject
{
    private string _action;
    private BaseItemType _type;
    private int? _stackSize;
    private string _description;
    public BaseItemType Type
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int StackSize
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string Action
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string Description
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