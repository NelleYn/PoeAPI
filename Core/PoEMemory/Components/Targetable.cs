using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing whether an entity is targetable and whether it is currently targeted.
/// </summary>
public class Targetable : Component
{
    private readonly CachedValue<TargetableComponentOffsets> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="Targetable"/> class.</summary>
    public Targetable()
    {
        _cachedValue = new FrameCache<TargetableComponentOffsets>(() => M.Read<TargetableComponentOffsets>(Address));
    }

    /// <summary>Gets the raw targetable offsets struct (cached per frame).</summary>
    public TargetableComponentOffsets TargetableComponent => _cachedValue.Value;

    /// <summary>Gets a value indicating whether the entity can be targeted.</summary>
    public bool isTargetable => TargetableComponent.isTargetable;

    /// <summary>Gets a value indicating whether the entity is currently targeted.</summary>
    public bool isTargeted => TargetableComponent.isTargeted;
}
