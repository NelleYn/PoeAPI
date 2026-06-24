using System.Numerics;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class DefaultEnvironmentSetting : StructuredRemoteMemoryObject<DefaultEnvironmentSettingsOffsets>
{
    private string _category;
    private string _name;
    public string Category
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

    public int IndexInGroup => (int)this;
    public Vector3 Value => (Vector3)this;

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}