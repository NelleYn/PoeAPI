using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;
using JM.LinqFaster;
using ProcessMemoryUtilities.Memory;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the list of buffs and debuffs currently affecting an entity (client 328.8).
/// In 328.8 buffs were moved out of the Life component into their own component.
/// </summary>
public sealed class Buffs : Component
{
    private const int MaxBuffCount = 512;
    private readonly CachedValue<List<Buff>> _cachedValueBuffs;

    /// <summary>Initializes a new instance of the <see cref="Buffs"/> class.</summary>
    public Buffs()
    {
        _cachedValueBuffs = new FrameCache<List<Buff>>(ParseBuffs);
    }

    /// <summary>Gets the list of buffs and debuffs currently affecting the entity (cached per frame).</summary>
    public List<Buff> BuffsList => _cachedValueBuffs.Value;

    /// <summary>Reads and parses the entity's current buffs from memory.</summary>
    public List<Buff> ParseBuffs()
    {
        try
        {
            if (Address == 0) return new List<Buff>();

            var buffsArray = M.Read<BuffsOffsets>(Address).Buffs;
            var start = buffsArray.First;
            var length = buffsArray.Last - start;
            var numBuffs = (int)(length / 8); // each entry is an 8-byte pointer

            if (length <= 0 || numBuffs >= MaxBuffCount || numBuffs <= 0 || buffsArray.End <= 0)
                return new List<Buff>();

            var buffer = new long[numBuffs];
            ProcessMemory.ReadProcessMemoryArray(M.OpenProcessHandle, (IntPtr)start, buffer, 0, numBuffs);

            var result = new List<Buff>(numBuffs);
            foreach (var pointer in buffer)
            {
                var buff = ReadObject<Buff>(pointer + 0x8);

                if (buff.Address == 0 || buff.BuffOffsets.Name == 0)
                    continue;

                if (!string.IsNullOrEmpty(buff.Name))
                    result.Add(buff);
            }

            return result;
        }
        catch (Exception e)
        {
            DebugWindow.LogError($"Buffs Component problem.{Environment.NewLine}{e}");
            return new List<Buff>();
        }
    }

    /// <summary>Determines whether the entity currently has a buff with the given name.</summary>
    /// <param name="buff">The buff name to search for.</param>
    /// <returns><c>true</c> if a matching buff is present; otherwise <c>false</c>.</returns>
    public bool HasBuff(string buff)
    {
        return BuffsList?.AnyF(x => x.Name == buff) ?? false;
    }

    /// <summary>Tries to get the buff with the given name.</summary>
    /// <param name="name">The buff name to search for.</param>
    /// <param name="buff">The matching buff, or <c>null</c> if none was found.</param>
    /// <returns><c>true</c> if a matching buff is present; otherwise <c>false</c>.</returns>
    public bool TryGetBuff(string name, out Buff buff)
    {
        var list = BuffsList;
        if (list != null)
        {
            foreach (var b in list)
            {
                if (b.Name == name)
                {
                    buff = b;
                    return true;
                }
            }
        }

        buff = null;
        return false;
    }
}
