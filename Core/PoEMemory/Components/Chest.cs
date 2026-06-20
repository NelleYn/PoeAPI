using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the state of a chest or strongbox (opened, locked, and strongbox flags).
/// </summary>
public class Chest : Component
{
    private readonly CachedValue<ChestComponentOffsets> _cachedValue;
    private readonly CachedValue<StrongboxChestComponentData> _cachedValueStrongboxData;

    /// <summary>Initializes a new instance of the <see cref="Chest"/> class.</summary>
    public Chest()
    {
        _cachedValue = new FramesCache<ChestComponentOffsets>(() => M.Read<ChestComponentOffsets>(Address), 3);

        _cachedValueStrongboxData =
            new FramesCache<StrongboxChestComponentData>(() => M.Read<StrongboxChestComponentData>(_cachedValue.Value.StrongboxData),
                3);
    }

    /// <summary>Gets a value indicating whether the chest is opened.</summary>
    public bool IsOpened => Address != 0 && _cachedValue.Value.IsOpened;

    /// <summary>Gets a value indicating whether the chest is locked.</summary>
    public bool IsLocked => Address != 0 && _cachedValue.Value.IsLocked;

    /// <summary>Gets a value indicating whether the chest is a strongbox.</summary>
    public bool IsStrongbox => Address != 0 && _cachedValue.Value.IsStrongbox;

    private long StrongboxData => _cachedValue.Value.StrongboxData;

    /// <summary>Gets a value indicating whether the strongbox is destroyed after being opened.</summary>
    public bool DestroyingAfterOpen => Address != 0 && _cachedValueStrongboxData.Value.DestroyingAfterOpen;

    /// <summary>Gets a value indicating whether the strongbox is large.</summary>
    public bool IsLarge => Address != 0 && _cachedValueStrongboxData.Value.IsLarge;

    /// <summary>Gets a value indicating whether the strongbox can be stomped.</summary>
    public bool Stompable => Address != 0 && _cachedValueStrongboxData.Value.Stompable;

    /// <summary>Gets a value indicating whether the strongbox opens when damaged.</summary>
    public bool OpenOnDamage => Address != 0 && _cachedValueStrongboxData.Value.OpenOnDamage;
}
