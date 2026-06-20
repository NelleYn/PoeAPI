using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExileCore.Shared.Cache;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;
public sealed class Buffs : Component
{
    private readonly CachedValue<List<Buff>> _cachedValueBuffs;
    public List<Buff> BuffsList => (List<Buff>)(object)this;

    public Buffs()
    {
        _ = null;
    }

    public List<Buff> ParseBuffs()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private unsafe List<Buff> ParseBuffs(List<Buff> lastValue)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public bool HasBuff(string buff)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public unsafe bool TryGetBuff(string name, out Buff buff)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}