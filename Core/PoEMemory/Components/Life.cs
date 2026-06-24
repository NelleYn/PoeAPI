using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's life, mana, energy shield, and reservation.
/// </summary>
/// <remarks>
/// In client 328.8 the per-vital values are grouped into <see cref="VitalStruct"/> instances
/// (Health/Mana/EnergyShield), and buffs were moved into a dedicated <see cref="Buffs"/>
/// component; the <see cref="Buffs"/> members here delegate to it for backwards compatibility.
/// </remarks>
public class Life : Component
{
    private readonly CachedValue<LifeComponentOffsets> _life;

    /// <summary>Initializes a new instance of the <see cref="Life"/> class.</summary>
    public Life()
    {
        _life = new FrameCache<LifeComponentOffsets>(() => Address == 0 ? default : M.Read<LifeComponentOffsets>(Address));
    }

    /// <summary>Gets the address of the entity that owns this component.</summary>
    public long OwnerAddress => LifeComponentOffsetsStruct.Owner;

    private LifeComponentOffsets LifeComponentOffsetsStruct => _life.Value;

    /// <summary>Gets the maximum life.</summary>
    public int MaxHP => Address != 0 ? LifeComponentOffsetsStruct.Health.Max : 1;

    /// <summary>Gets the current life.</summary>
    public int CurHP => Address != 0 ? LifeComponentOffsetsStruct.Health.Current : 0;

    /// <summary>Gets the flat amount of life reserved.</summary>
    public int ReservedFlatHP => LifeComponentOffsetsStruct.Health.ReservedFlat;

    /// <summary>Gets the fraction of life reserved.</summary>
    public int ReservedPercentHP => LifeComponentOffsetsStruct.Health.ReservedFraction;

    /// <summary>Gets the life regeneration per second.</summary>
    public float HPRegen => LifeComponentOffsetsStruct.Health.Regen;

    /// <summary>Gets the maximum mana.</summary>
    public int MaxMana => Address != 0 ? LifeComponentOffsetsStruct.Mana.Max : 1;

    /// <summary>Gets the current mana.</summary>
    public int CurMana => Address != 0 ? LifeComponentOffsetsStruct.Mana.Current : 1;

    /// <summary>Gets the flat amount of mana reserved.</summary>
    public int ReservedFlatMana => LifeComponentOffsetsStruct.Mana.ReservedFlat;

    /// <summary>Gets the fraction of mana reserved.</summary>
    public int ReservedPercentMana => LifeComponentOffsetsStruct.Mana.ReservedFraction;

    /// <summary>Gets the mana regeneration per second.</summary>
    public float ManaRegen => LifeComponentOffsetsStruct.Mana.Regen;

    /// <summary>Gets the maximum energy shield.</summary>
    public int MaxES => LifeComponentOffsetsStruct.EnergyShield.Max;

    /// <summary>Gets the current energy shield.</summary>
    public int CurES => LifeComponentOffsetsStruct.EnergyShield.Current;

    /// <summary>Gets the current life as a fraction of unreserved maximum life.</summary>
    public float HPPercentage => CurHP / (float) (MaxHP - ReservedFlatHP - Math.Round(ReservedPercentHP * 0.01 * MaxHP));

    /// <summary>Gets the current mana as a fraction of unreserved maximum mana.</summary>
    public float MPPercentage => CurMana / (float) (MaxMana - ReservedFlatMana - Math.Round(ReservedPercentMana * 0.01 * MaxMana));

    /// <summary>Gets the current energy shield as a fraction of maximum energy shield.</summary>
    public float ESPercentage => MaxES == 0 ? 0 : CurES / (float) MaxES;

    /// <summary>Gets the list of buffs and debuffs currently affecting the entity.</summary>
    /// <remarks>Delegates to the entity's <see cref="Buffs"/> component (328.8).</remarks>
    public List<Buff> Buffs => Owner?.GetComponent<Buffs>()?.BuffsList ?? new List<Buff>();

    /// <summary>Determines whether the entity currently has a buff with the given name.</summary>
    /// <param name="buff">The buff name to search for.</param>
    /// <returns><c>true</c> if a matching buff is present; otherwise <c>false</c>.</returns>
    public bool HasBuff(string buff)
    {
        return Owner?.GetComponent<Buffs>()?.HasBuff(buff) ?? false;
    }
}
