using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using GameOffsets;
using JM.LinqFaster;
using ProcessMemoryUtilities.Memory;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's life, mana, energy shield, reservation, and active buffs.
/// </summary>
public class Life : Component
{
    private static long BuffStartOffset = Extensions.GetOffset<LifeComponentOffsets>(nameof(LifeComponentOffsets.Buffs));
    private static long BuffLastOffset = Extensions.GetOffset<LifeComponentOffsets>(nameof(LifeComponentOffsets.Buffs)) + 0x8;
    private readonly CachedValue<List<Buff>> _cachedValueBuffs;
    private readonly CachedValue<LifeComponentOffsets> _life;

    /// <summary>Initializes a new instance of the <see cref="Life"/> class.</summary>
    public Life()
    {
        _life = new FrameCache<LifeComponentOffsets>(() => Address == 0 ? default : M.Read<LifeComponentOffsets>(Address));
        _cachedValueBuffs = new FrameCache<List<Buff>>(ParseBuffs);
    }

    /// <summary>Gets the address of the entity that owns this component.</summary>
    public long OwnerAddress => LifeComponentOffsetsStruct.Owner;

    private LifeComponentOffsets LifeComponentOffsetsStruct => _life.Value;

    /// <summary>Gets the maximum life.</summary>
    public int MaxHP => Address != 0 ? LifeComponentOffsetsStruct.MaxHP : 1;

    /// <summary>Gets the current life.</summary>
    public int CurHP => Address != 0 ? LifeComponentOffsetsStruct.CurHP : 0;

    /// <summary>Gets the flat amount of life reserved.</summary>
    public int ReservedFlatHP => LifeComponentOffsetsStruct.ReservedFlatHP;

    /// <summary>Gets the percentage of life reserved.</summary>
    public int ReservedPercentHP => LifeComponentOffsetsStruct.ReservedPercentHP;

    /// <summary>Gets the maximum mana.</summary>
    public int MaxMana => Address != 0 ? LifeComponentOffsetsStruct.MaxMana : 1;

    /// <summary>Gets the current mana.</summary>
    public int CurMana => Address != 0 ? LifeComponentOffsetsStruct.CurMana : 1;

    /// <summary>Gets the flat amount of mana reserved.</summary>
    public int ReservedFlatMana => LifeComponentOffsetsStruct.ReservedFlatMana;

    /// <summary>Gets the percentage of mana reserved.</summary>
    public int ReservedPercentMana => LifeComponentOffsetsStruct.ReservedPercentMana;

    /// <summary>Gets the maximum energy shield.</summary>
    public int MaxES => LifeComponentOffsetsStruct.MaxES;

    /// <summary>Gets the current energy shield.</summary>
    public int CurES => LifeComponentOffsetsStruct.CurES;

    /// <summary>Gets the current life as a fraction of unreserved maximum life.</summary>
    public float HPPercentage => CurHP / (float) (MaxHP - ReservedFlatHP - Math.Round(ReservedPercentHP * 0.01 * MaxHP));

    /// <summary>Gets the current mana as a fraction of unreserved maximum mana.</summary>
    public float MPPercentage => CurMana / (float) (MaxMana - ReservedFlatMana - Math.Round(ReservedPercentMana * 0.01 * MaxMana));

    /// <summary>Gets the current energy shield as a fraction of maximum energy shield.</summary>
    public float ESPercentage => MaxES == 0 ? 0 : CurES / (float) MaxES;

    //public bool CorpseUsable => M.ReadMem(Address + 0x238, 1)[0] == 1; // Total guess, didn't verify
    private long BuffStart => LifeComponentOffsetsStruct.Buffs.First;
    private long BuffEnd => LifeComponentOffsetsStruct.Buffs.End;
    private long BuffLast => LifeComponentOffsetsStruct.Buffs.Last;
    private long MaxBuffCount => 512; // Randomly bumping to 512 from 32 buffs... no idea what real value is.

    /// <summary>Gets the list of buffs and debuffs currently affecting the entity.</summary>
    public List<Buff> Buffs => _cachedValueBuffs.Value;

    /// <summary>Reads and parses the entity's current buffs from memory.</summary>
    public List<Buff> ParseBuffs()
    {
        try
        {
            var length = BuffLast - BuffStart;
            var numBuffs = (int) length / 8;

            if (length <= 0 || numBuffs >= MaxBuffCount || numBuffs <= 0 || BuffEnd <= 0) // * 8 as we buff pointer takes 8 bytes.
                return new List<Buff>();

            var buffer = new long[numBuffs];
            ProcessMemory.ReadProcessMemoryArray(M.OpenProcessHandle, (IntPtr) BuffStart, buffer, 0, numBuffs);

            var result = new List<Buff>(numBuffs);

            for (var index = 0; index < buffer.Length; index++)
            {
                var l = buffer[index];
                var buff = ReadObject<Buff>(l + 0x8);

                if (buff.Address == 0 || buff.BuffOffsets.Name == 0)
                    continue;

                if (!string.IsNullOrEmpty(buff.Name)) result.Add(buff);
            }

            return result;
        }
        catch (Exception e)
        {
            DebugWindow.LogError(
                $"Life Component Buffs problem. {LifeComponentOffsetsStruct.Buffs} Len: {BuffLast - BuffStart} Div: {(BuffLast - BuffStart) / 8} {Environment.NewLine}{e}");

            return null;
        }
    }

    /// <summary>Determines whether the entity currently has a buff with the given name.</summary>
    /// <param name="buff">The buff name to search for.</param>
    /// <returns><c>true</c> if a matching buff is present; otherwise <c>false</c>.</returns>
    public bool HasBuff(string buff)
    {
        return Buffs?.AnyF(x => x.Name == buff) ?? false;
    }
}
