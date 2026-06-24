using System;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Wraps a single buff or debuff affecting an entity, exposing its name, charges, and timers.
/// </summary>
public class Buff : RemoteMemoryObject
{
    private readonly Lazy<string> _name;
    private BuffOffsets? _offsets;

    /// <summary>Initializes a new instance of the <see cref="Buff"/> class.</summary>
    public Buff()
    {
        _name = new Lazy<string>(() =>
        {
            var formattableString = $"{nameof(Buff)}{BuffOffsets.Name}";
            string read;
            var tries = 0;

            do
            {
                read = Cache.StringCache.Read(formattableString,
                    () => M.ReadStringU(M.Read<BuffStringOffsets>(BuffOffsets.Name).String));

                if (read == string.Empty) Cache.StringCache.Remove(formattableString);

                tries++;
            } while (read == string.Empty && tries < 7);

            return read;
        });
    }

    /// <summary>Gets the raw buff offsets struct (cached after the first read).</summary>
    public BuffOffsets BuffOffsets => (BuffOffsets) (_offsets = _offsets ?? M.Read<BuffOffsets>(Address));

    /// <summary>Gets the buff name.</summary>
    public string Name => _name.Value;

    /// <summary>Gets the number of charges currently stacked on the buff.</summary>
    public byte Charges => (byte) BuffOffsets.Charges;

    //public int SkillId => M.Read<int>(Address + 0x5C); // I think this is part of another structure referenced in a pointer at 0x58

    /// <summary>Gets the total duration of the buff (infinity for auras and always-on buffs).</summary>
    public float MaxTime => BuffOffsets.MaxTime; // infinity for auras and always on buff

    /// <summary>Gets the time remaining on the buff.</summary>
    public float Timer => BuffOffsets.Timer; // timeleft

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name} - Chargs: {Charges} MaxTime: {MaxTime} Timer: {Timer}";
    }
}
