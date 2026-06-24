using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class EnvironmentData : StructuredRemoteMemoryObject<EnvironmentDataOffsets>
{
    private readonly CachedValue<List<DefaultEnvironmentSetting>> _defaultSettings;
    private readonly CachedValue<List<EnvironmentDataEnvironment>> _environments;
    public List<DefaultEnvironmentSetting> DefaultSettingMap => (List<DefaultEnvironmentSetting>)(object)this;
    public List<EnvironmentDataEnvironment> Environments => (List<EnvironmentDataEnvironment>)(object)this;
}