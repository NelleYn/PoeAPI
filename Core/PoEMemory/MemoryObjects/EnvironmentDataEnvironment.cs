using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class EnvironmentDataEnvironment : RemoteMemoryObject
{
    private readonly CachedValue<string> _nameCache;
    private readonly CachedValue<TypedEnvironmentData<Type1EnvironmentSettingsOffsets>> _type1Cache;
    private readonly CachedValue<TypedEnvironmentData<Type2EnvironmentSettingsOffsets>> _type2Cache;
    private readonly CachedValue<TypedEnvironmentData<Type3EnvironmentSettingsOffsets>> _type3Cache;
    private readonly CachedValue<TypedEnvironmentData<Type4EnvironmentSettingsOffsets>> _type4Cache;
    private readonly CachedValue<TypedEnvironmentData<Type5EnvironmentSettingsOffsets>> _type5Cache;
    private readonly CachedValue<TypedEnvironmentData<Type6EnvironmentSettingsOffsets>> _type6Cache;
    private readonly CachedValue<TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>> _type7Cache;
    private readonly CachedValue<TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>> _type8Cache;
    private readonly CachedValue<TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>> _type9Cache;
    private readonly CachedValue<TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>> _type10Cache;
    private const int EnvironmentNameSize = 32;
    public long EnvironmentDataAddress
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I8, but got O
            return (long)this;
        }

        [CompilerGenerated]
        internal set
        {
        }
    }

    public Dictionary<long, DefaultEnvironmentSetting> DefaultSettings
    {
        [CompilerGenerated]
        get
        {
            return (Dictionary<long, DefaultEnvironmentSetting>)(object)this;
        }

        [CompilerGenerated]
        internal set
        {
        }
    }

    public string Name => (string)(object)this;
    public TypedEnvironmentData<Type1EnvironmentSettingsOffsets> Type1Settings => (TypedEnvironmentData<Type1EnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type2EnvironmentSettingsOffsets> Type2Settings => (TypedEnvironmentData<Type2EnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type3EnvironmentSettingsOffsets> Type3Settings => (TypedEnvironmentData<Type3EnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type4EnvironmentSettingsOffsets> Type4Settings => (TypedEnvironmentData<Type4EnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type5EnvironmentSettingsOffsets> Type5Settings => (TypedEnvironmentData<Type5EnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type6EnvironmentSettingsOffsets> Type6Settings => (TypedEnvironmentData<Type6EnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets> Type7Settings => (TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets> Type8Settings => (TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets> Type9Settings => (TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>)(object)this;
    public TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets> Type10Settings => (TypedEnvironmentData<Type7PlusEnvironmentSettingsOffsets>)(object)this;

    private int GetTypeNDefaultDataOffset(int n)
    {
        _ = new int[10];
        return (n - 1) * 8 + 24;
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}