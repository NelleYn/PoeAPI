// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;

namespace ExileCore.TreeRoutine.DefaultBehaviors.Helpers;

/// <summary>
/// Player-state reader ported from upstream <c>TreeRoutine/DefaultBehaviors/Helpers/PlayerHelper.cs</c>,
/// rewritten against this fork. Wraps the local player's <see cref="Life"/> and <see cref="Actor"/>
/// components so tree conditions can ask "HP below X%?", "has buff?", "skill usable?" without repeating
/// component look-ups. Instantiate once per plugin and reuse; the underlying reads are frame-cached by
/// the engine.
/// </summary>
/// <remarks>
/// This fork exposes buffs on <see cref="Life"/> (<see cref="Life.Buffs"/>/<see cref="Life.HasBuff"/>),
/// NOT via a <c>Buffs</c> component, and life pools as flat <c>Cur*</c>/<c>Max*</c> + <c>*Percentage</c>
/// helpers (no <c>Life.Health</c>/<c>Mana</c>/<c>EnergyShield</c> aggregates). There is no
/// <c>Entity.TryGetComponent&lt;T&gt;</c>; <see cref="Entity.GetComponent{T}"/> is null-checked instead.
/// </remarks>
public sealed class PlayerHelper
{
    private readonly GameController _gameController;

    public PlayerHelper(GameController gameController)
    {
        _gameController = gameController;
    }

    /// <summary>The local player's <see cref="Life"/> component, or <c>null</c> if unavailable.</summary>
    public Life Life => _gameController?.Player?.GetComponent<Life>();

    /// <summary>
    /// The player's current buff list (may be <c>null</c>). Read via <see cref="Life"/> rather than the
    /// equivalent <c>Entity.Buffs</c> convenience accessor: <c>Life.Buffs</c> is frame-cached (recomputed
    /// at most once per render), whereas <c>Entity.Buffs</c> re-reads process memory on every access while
    /// the entity is valid — going through <see cref="Life"/> avoids redundant reads across the several
    /// buff checks a tree tick typically performs.
    /// </summary>
    public List<Buff> Buffs => Life?.Buffs;

    /// <summary>Unreserved HP as a 0..100 percentage.</summary>
    public float HpPercent => (Life?.HPPercentage ?? 0f) * 100f;

    /// <summary>Energy shield as a 0..100 percentage (0 when there is no ES).</summary>
    public float EsPercent => (Life?.ESPercentage ?? 0f) * 100f;

    /// <summary>Unreserved mana as a 0..100 percentage.</summary>
    public float ManaPercent => (Life?.MPPercentage ?? 0f) * 100f;

    /// <summary><c>true</c> when the player currently has positive current HP.</summary>
    public bool IsAlive => (Life?.CurHP ?? 0) > 0;

    /// <summary>HP fraction (unreserved) is strictly below <paramref name="percent"/> (0..100).</summary>
    public bool IsHealthBelow(float percent) => HpPercent < percent;

    /// <summary>ES fraction is strictly below <paramref name="percent"/> (0..100).</summary>
    public bool IsEnergyShieldBelow(float percent) => EsPercent < percent;

    /// <summary>Mana fraction (unreserved) is strictly below <paramref name="percent"/> (0..100).</summary>
    public bool IsManaBelow(float percent) => ManaPercent < percent;

    /// <summary>Exact-name buff membership test (delegates to <see cref="Life.HasBuff"/>).</summary>
    public bool HasBuff(string buffName) => Life?.HasBuff(buffName) ?? false;

    /// <summary>Case-insensitive buff membership test over the cached snapshot.</summary>
    public bool HasBuffIgnoreCase(string buffName) =>
        Buffs?.Any(b => string.Equals(b.Name, buffName, StringComparison.OrdinalIgnoreCase)) ?? false;

    /// <summary>Stack count of the named buff, or 0 if absent (<see cref="Buff.Charges"/>).</summary>
    public int BuffStacks(string buffName) =>
        Buffs?.FirstOrDefault(b => b.Name == buffName)?.Charges ?? 0;

    /// <summary>Seconds remaining on the named buff, or 0 if absent (<see cref="Buff.Timer"/>).</summary>
    public float BuffTimeLeft(string buffName) =>
        Buffs?.FirstOrDefault(b => b.Name == buffName)?.Timer ?? 0f;

    /// <summary>
    /// <c>true</c> when a skill with the given internal name is slotted and currently usable
    /// (<see cref="ActorSkill.Name"/> / <see cref="ActorSkill.CanBeUsed"/>).
    /// </summary>
    public bool IsSkillUsable(string internalName) =>
        _gameController?.Player?.GetComponent<Actor>()?.ActorSkills?
            .Any(s => string.Equals(s.Name, internalName, StringComparison.OrdinalIgnoreCase) && s.CanBeUsed)
        ?? false;
}
