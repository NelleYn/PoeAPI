// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.PoEMemory.MemoryObjects;
public partial class TypedEnvironmentData<T>
{
    public ExileCore.Shared.Interfaces.IMemory _m;
    public System.Int64 _environmentDataPtr;
    public System.Int64 _environmentPtr;
    public System.Int32 _startOffset;
    public System.Int32 _defaultStartOffset;
    public System.Int32 _count;
    public System.Collections.Generic.Dictionary<System.Int64, ExileCore.PoEMemory.MemoryObjects.DefaultEnvironmentSetting> _defaultSettings;
    public ExileCore.Shared.Cache.CachedValue<System.Collections.Generic.List<ExileCore.PoEMemory.MemoryObjects.EnvironmentSettingValue<T>>> _entriesCache;
    public System.Collections.Generic.List<ExileCore.PoEMemory.MemoryObjects.EnvironmentSettingValue<T>> Entries
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }
}