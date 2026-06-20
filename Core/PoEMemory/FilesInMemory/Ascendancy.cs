using SharpDX;

namespace ExileCore.PoEMemory.FilesInMemory;
public class Ascendancy : RemoteMemoryObject
{
    private string _id;
    private string _name;
    private int? _classId;
    private string _coordRect;
    public int ClassId
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string Id
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string Name
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Rectangle CoordinateRect => (Rectangle)this;

    public Rectangle GetCoordinateRect()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}