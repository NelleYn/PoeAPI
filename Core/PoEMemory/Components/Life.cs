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

    /// <summary>
    /// Gets whether the reservation readings on this component are measurements rather than
    /// placeholders. FALSE on the installed client, where the reservation offsets were never found:
    /// the four <c>Reserved*</c> properties are therefore <c>null</c> and
    /// <see cref="HPPercentage"/>/<see cref="MPPercentage"/> are <see cref="float.NaN"/>. A caller
    /// that must have a finite number reads <see cref="HPPercentageOfTotal"/> or
    /// <see cref="MPPercentageOfTotal"/> instead — those are measured, but they answer a different
    /// question. See <see cref="LifeComponentOffsets"/>.
    /// </summary>
    public bool ReservationsMeasured => LifeComponentOffsets.ReservationsMeasured;

    /// <summary>
    /// Gets the flat amount of life reserved, or <c>null</c> when it is not known. NOT MEASURED on
    /// the installed client, so this is always <c>null</c>. The type carries the distinction the
    /// caller needs: a measured 0 means the game reserves nothing, <c>null</c> means nobody has
    /// found the offset. See <see cref="ReservationsMeasured"/>. When the offset is measured this
    /// body becomes a read of the struct and that flag flips; deliberately no <c>0</c> branch is
    /// written here in advance, because a placeholder zero is exactly the value this fix removes.
    /// </summary>
    public int? ReservedFlatHP => null;

    /// <summary>
    /// Gets the percentage of life reserved, or <c>null</c> when it is not known. NOT MEASURED on
    /// the installed client — always <c>null</c>. See <see cref="ReservedFlatHP"/>.
    /// </summary>
    public int? ReservedPercentHP => null;

    /// <summary>Gets the maximum mana.</summary>
    public int MaxMana => Address != 0 ? LifeComponentOffsetsStruct.MaxMana : 1;

    /// <summary>Gets the current mana.</summary>
    public int CurMana => Address != 0 ? LifeComponentOffsetsStruct.CurMana : 1;

    /// <summary>
    /// Gets the flat amount of mana reserved, or <c>null</c> when it is not known. NOT MEASURED on
    /// the installed client — always <c>null</c>. See <see cref="ReservedFlatHP"/>.
    /// </summary>
    public int? ReservedFlatMana => null;

    /// <summary>
    /// Gets the percentage of mana reserved, or <c>null</c> when it is not known. NOT MEASURED on
    /// the installed client — always <c>null</c>. See <see cref="ReservedFlatHP"/>.
    /// </summary>
    public int? ReservedPercentMana => null;

    /// <summary>Gets the maximum energy shield.</summary>
    public int MaxES => LifeComponentOffsetsStruct.MaxES;

    /// <summary>Gets the current energy shield.</summary>
    public int CurES => LifeComponentOffsetsStruct.CurES;

    /// <summary>
    /// Gets the current life as a fraction of UNRESERVED maximum life — a definition, not a reading
    /// taken from the game's UI. <see cref="float.NaN"/> while <see cref="ReservationsMeasured"/> is false,
    /// because the quantity is then not computable; see the remarks on <see cref="MPPercentage"/>
    /// for why NaN and not a substitute number. <see cref="HPPercentageOfTotal"/> is the measured
    /// fraction of the FULL pool.
    /// </summary>
    public float HPPercentage => UnreservedFraction(CurHP, MaxHP, ReservedFlatHP, ReservedPercentHP);

    /// <summary>
    /// Gets the current mana as a fraction of UNRESERVED maximum mana — the quantity a "do I have
    /// mana for this skill" threshold is meant to be tuned against. What the client's mana globe
    /// displays was not measured here; this summary states what the property is defined to compute,
    /// not a reading taken from the game's UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="float.NaN"/> WHILE <see cref="ReservationsMeasured"/> IS FALSE, which it is on the
    /// installed client. The reservation offsets are unknown, so the denominator of this fraction is
    /// unknown, and there is no substitute that is not a claim nobody measured. In particular
    /// <c>CurMana / MaxMana</c> is not one: it is the fraction of the TOTAL pool, it equals this
    /// fraction only when nothing is reserved, and how far the two diverge on any given character is
    /// exactly what is not known here. Handing it to a caller comparing against a threshold can
    /// therefore invert the answer rather than merely blur it.
    /// </para>
    /// <para>
    /// WHAT WAS MEASURED ON THE SURVEYED CHARACTER IS TWO READINGS AND NO MORE: the mana block held
    /// 67 of 1135 at the moment of the dump, and 1135 of 1135 later in the same session. Whether a
    /// reservation accounts for the first is NOT MEASURED — spending and regeneration produce the
    /// same pair of numbers, and the second reading sits against the reservation story rather than
    /// with it. An earlier revision of this comment presented that story as fact and derived a
    /// "5.9% versus the globe" example from it; both are struck. Any explanation of the 67 is a
    /// HYPOTHESIS, and the case for NaN never depended on one.
    /// </para>
    /// <para>
    /// WHY NaN RATHER THAN A NULLABLE OR AN EXCEPTION. NaN keeps the signature <c>float</c>, so every
    /// existing caller still compiles — a <c>float?</c> would break <c>ProbeNum(life.MPPercentage *
    /// 100)</c>-style call sites in consuming plugins at build time. More importantly NaN has exactly
    /// the right comparison behaviour for a gate: EVERY comparison against NaN is false, so
    /// <c>if (life.MPPercentage &lt; threshold)</c> does not fire, which is precisely how these gates
    /// behaved before the component model was fixed and <c>GetComponent&lt;Life&gt;()</c> returned
    /// null. An unmeasurable quantity thus disables the gate that depends on it instead of deciding
    /// it wrongly, and anything that prints the value prints "NaN" where a substituted number would
    /// have looked authoritative. A caller that would rather have a finite number takes
    /// <see cref="MPPercentageOfTotal"/> and accepts what it means.
    /// </para>
    /// </remarks>
    public float MPPercentage => UnreservedFraction(CurMana, MaxMana, ReservedFlatMana, ReservedPercentMana);

    /// <summary>
    /// Gets the current life as a fraction of TOTAL maximum life, reservations ignored — the plain
    /// <c>CurHP / MaxHP</c> of two measured fields, always finite, 0 when <see cref="MaxHP"/> is not
    /// positive. It is a different quantity from <see cref="HPPercentage"/> whenever anything
    /// reserves life; because reserved life can only shrink that denominator, this is a LOWER BOUND
    /// on <see cref="HPPercentage"/>,
    /// which makes it safe for an "is it above X" test and unsafe for an "is it below X" one.
    /// </summary>
    public float HPPercentageOfTotal => MaxHP > 0 ? CurHP / (float) MaxHP : 0f;

    /// <summary>
    /// Gets the current mana as a fraction of TOTAL maximum mana, reservations ignored — the plain
    /// <c>CurMana / MaxMana</c> of two measured fields, always finite, 0 when <see cref="MaxMana"/>
    /// is not positive. Same lower-bound caveat as <see cref="HPPercentageOfTotal"/>: reserved mana
    /// can only shrink the denominator of <see cref="MPPercentage"/>, so this value is at or below
    /// it, and by how much is not known on this build.
    /// </summary>
    public float MPPercentageOfTotal => MaxMana > 0 ? CurMana / (float) MaxMana : 0f;

    /// <summary>Gets the current energy shield as a fraction of maximum energy shield.</summary>
    /// <remarks>
    /// No NaN case here: energy shield has no reservation term in this struct or in the fork's older
    /// one, so both operands are measured and the fraction is fully defined.
    /// </remarks>
    public float ESPercentage => MaxES == 0 ? 0 : CurES / (float) MaxES;

    /// <summary>
    /// Divides a current pool by its unreserved maximum, or returns <see cref="float.NaN"/> when
    /// that maximum cannot be computed — either because a reservation term is unknown, or because
    /// the arithmetic left nothing to divide by.
    /// </summary>
    /// <param name="current">Current value of the pool.</param>
    /// <param name="maximum">Total maximum of the pool.</param>
    /// <param name="reservedFlat">Flat amount reserved, or null when unknown.</param>
    /// <param name="reservedPercent">Percentage reserved, or null when unknown.</param>
    /// <returns>The fraction, or NaN when it is not defined.</returns>
    private static float UnreservedFraction(int current, int maximum, int? reservedFlat, int? reservedPercent)
    {
        if (reservedFlat == null || reservedPercent == null)
            return float.NaN;

        var unreserved = maximum - reservedFlat.Value - Math.Round(reservedPercent.Value * 0.01 * maximum);

        // A full reservation, or a reservation reading past 100%, leaves a zero or negative
        // denominator. Dividing anyway would hand out an infinity that compares as "plenty left".
        return unreserved > 0 ? (float) (current / unreserved) : float.NaN;
    }

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
