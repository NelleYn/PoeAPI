using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class ExpeditionSaga : Component
{
    private readonly CachedValue<ExpeditionSagaOffsets> _cachedValue;
    internal ExpeditionSagaOffsets SagaStruct => (ExpeditionSagaOffsets)this;
    public int AreaLevel => (int)this;

    public List<ExpeditionAreaData> Areas
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}