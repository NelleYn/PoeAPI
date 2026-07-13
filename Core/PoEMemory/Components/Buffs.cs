using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Buff/debuff list for an entity. Upstream ExileApi-Compiled plugins read buffs through
/// <c>GetComponent&lt;Buffs&gt;()?.BuffsList</c>. On this fork's (pre-328.8) build the buff vector
/// still lives inside the <see cref="Life"/> component (<c>LifeComponentOffsets.Buffs</c>), so
/// rather than carry a second build-specific offset, <see cref="BuffsList"/> returns the owning
/// entity's <see cref="Life"/> buffs — the exact same list <c>Entity.Buffs</c> already exposes.
///
/// The result is wrapped in a per-frame cache: <c>Owner</c> materializes a fresh <see cref="Entity"/>
/// (rebuilding its component lookup), so caching keeps repeated same-frame reads cheap — the same
/// pattern <see cref="Life"/> uses for its own buff list. The component only resolves (via
/// <c>GetComponent&lt;Buffs&gt;()</c>) when the game entity carries a "Buffs" component in its
/// component list; when it does not, callers get an empty list, which is a safe no-op. If a future
/// build moves buffs out of Life (as retail 328.8 did), point the reader at this component's own
/// vector instead.
/// </summary>
public class Buffs : Component
{
    private readonly CachedValue<List<Buff>> _cachedBuffs;

    /// <summary>Initializes a new instance of the <see cref="Buffs"/> class.</summary>
    public Buffs()
    {
        _cachedBuffs = new FrameCache<List<Buff>>(() => Owner?.GetComponent<Life>()?.Buffs ?? new List<Buff>());
    }

    /// <summary>The buffs/debuffs currently affecting the owning entity (cached per frame).</summary>
    public List<Buff> BuffsList => _cachedBuffs.Value;

    /// <summary>True when a buff with the exact given name is present on the entity.</summary>
    public bool HasBuff(string buff) => BuffsList.Any(b => b.Name == buff);

    /// <summary>Finds a buff by exact name; returns <c>false</c> (and a null <paramref name="buff"/>) when absent.</summary>
    public bool TryGetBuff(string name, out Buff buff)
    {
        buff = BuffsList.FirstOrDefault(b => b.Name == name);
        return buff != null;
    }
}
