using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    /// <summary>
    /// Layout of the Life component on the installed client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. The three pools and <see cref="Owner"/> were MEASURED against a live client on
    /// 2026-09-16 (PathOfExile_KG, pid 13660, module base 0x7FF78FCD0000, zone "The Reliquary"
    /// 1_5_7, character a Shadow with Chaos Inoculation). <see cref="Buffs"/> is INHERITED FROM THE
    /// PRE-2026 FORK AND NOT MEASURED, and the reservation fields are not declared at all any more —
    /// see below.
    /// </para>
    /// <para>
    /// THE STRONGEST CONFIRMATION THIS STRUCT HAS, and it comes from outside memory entirely: the
    /// character carries Chaos Inoculation, and the three blocks read maximum/current of 1 and 1 for
    /// life, 1135 and 67 for mana, 10353 and 10353 for energy shield. Chaos Inoculation sets maximum
    /// life to exactly 1 and leaves the character on energy shield. A wrong offset can produce a
    /// plausible number; it cannot produce the one number that a game mechanic known OUTSIDE memory
    /// says must be there, twice over. Note the direction of the pair while reading those figures:
    /// MAXIMUM COMES FIRST in memory (head+0x24), current second (head+0x28).
    /// </para>
    /// <para>
    /// HOW TO FIND THE BLOCKS AGAIN, because the numbers below are not derivable from each other.
    /// Each pool is a block whose HEAD HOLDS A POINTER BACK TO THE COMPONENT ITSELF — head 0x180 for
    /// health, 0x1D0 for mana, 0x218 for energy shield, and on every entity disassembled the qword at
    /// each head equalled the Life component's own address. That self-pointer is the marker: scan for
    /// qwords equal to the component address and the three block heads fall out. Inside a block the
    /// shape is constant (maximum at head+0x24, current at head+0x28), but
    /// THE STRIDE BETWEEN BLOCKS IS NOT: 0x180 -&gt; 0x1D0 is 0x50, 0x1D0 -&gt; 0x218 is 0x48.
    /// DO NOT EXTRAPOLATE A FOURTH BLOCK FROM THESE THREE. The order in memory is health, mana,
    /// energy shield — which is NOT the order the pre-2026 fork's other Life struct
    /// (<c>GameOffsets.Components.Life</c>) records, so that struct is not a cross-check for this one.
    /// </para>
    /// <para>
    /// WHAT THE OLD NUMBERS WERE, AND WHY THE RESERVATION FIELDS ARE GONE. Life was declared at
    /// 0x154/0x15C, mana at 0xBC/0xC4, energy shield at 0xF4/0xFC, with reservations at
    /// 0x158/0x160/0xC0/0xC8 and regeneration at 0x90/0xB8. All of it was inherited and none of it
    /// matches this client. The reservations were the dangerous ones: <c>Life.HPPercentage</c> and
    /// <c>Life.MPPercentage</c> divide by <c>Max - ReservedFlat - ReservedPercent% * Max</c>, so an
    /// unmeasured reservation offset does not produce a visibly wrong reading — it produces a
    /// percentage that is quietly wrong, or a division by zero, in the very expression a plugin uses
    /// to decide whether to drink a flask or cast a skill. Regeneration is simply deleted: nothing in
    /// this fork read it, and an unmeasured number with no reader is not worth carrying.
    /// </para>
    /// <para>
    /// SUBSTITUTING ZERO FOR THE RESERVATIONS WAS TRIED AND IS WRONG. An earlier revision of this
    /// struct exposed the four reservation fields as properties hard-returning 0, on the theory that
    /// it degraded the percentages to an honest <c>Current / Maximum</c>. It does not: zero is the
    /// same value the game would report for "nothing is reserved", so the caller cannot tell a
    /// measurement from a placeholder. The argument does not need an example, and the one this
    /// paragraph used to carry was not measured — see the next paragraph for what the readings
    /// actually were. So the fields are not declared here at all, the flag above says why, and
    /// <c>Life</c> turns the absence into <c>null</c> reservations and a <c>NaN</c> percentage —
    /// values that cannot be mistaken for measurements.
    /// </para>
    /// <para>
    /// WHAT WAS READ FROM THE MANA BLOCK, AND NOTHING BEYOND IT. At the moment of the dump quoted
    /// above the block held CURRENT 67 AGAINST MAXIMUM 1135. A LATER READ IN THE SAME SESSION HELD
    /// 1135 AGAINST 1135. Both numbers are measurements; the reason the first one was low is NOT.
    /// Spending, regeneration, a reserved pool and a stale frame would all produce it, this struct
    /// has no field that tells them apart, and an earlier revision of this comment asserted the
    /// reservation answer as established fact — it was not, and the second reading of the same
    /// session sits against it. ANY EXPLANATION OF THE 67 IS A HYPOTHESIS and must be written as
    /// one. What survives without an explanation is the arithmetic: <c>Current / Maximum</c> is the
    /// fraction of the TOTAL pool, the fraction a reservation-aware caller asks for has a
    /// denominator this struct cannot compute while the reservation offsets are unknown, and the two
    /// coincide only when nothing is reserved.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct LifeComponentOffsets
    {
        /// <summary>
        /// Address of the entity that owns this component. MEASURED 2026-09-16: every component
        /// resolved through the lookup table held its own entity's address at +0x08, on every entity
        /// walked.
        /// </summary>
        [FieldOffset(0x008)] public long Owner;

        /// <summary>
        /// Head of the health block: a pointer back to this very component. MEASURED 2026-09-16.
        /// Declared because it is the marker that identifies the block — a diagnostic comparing it
        /// with the component address gets a yes/no answer on whether this whole struct still fits
        /// the client. See <see cref="BlockHeadsPointAtComponent"/>.
        /// </summary>
        [FieldOffset(0x180)] public long HealthBlockSelfPtr;

        /// <summary>Maximum life. MEASURED 2026-09-16 (health block head + 0x24).</summary>
        [FieldOffset(0x1A4)] public int MaxHP;

        /// <summary>Current life. MEASURED 2026-09-16 (health block head + 0x28).</summary>
        [FieldOffset(0x1A8)] public int CurHP;

        /// <summary>
        /// Head of the mana block: a pointer back to this very component. MEASURED 2026-09-16. Note
        /// the stride from the health block is 0x50 and the stride to the next block is 0x48 — the
        /// blocks are not evenly spaced.
        /// </summary>
        [FieldOffset(0x1D0)] public long ManaBlockSelfPtr;

        /// <summary>Maximum mana. MEASURED 2026-09-16 (mana block head + 0x24).</summary>
        [FieldOffset(0x1F4)] public int MaxMana;

        /// <summary>Current mana. MEASURED 2026-09-16 (mana block head + 0x28).</summary>
        [FieldOffset(0x1F8)] public int CurMana;

        /// <summary>
        /// Head of the energy shield block: a pointer back to this very component. MEASURED
        /// 2026-09-16.
        /// </summary>
        [FieldOffset(0x218)] public long EnergyShieldBlockSelfPtr;

        /// <summary>Maximum energy shield. MEASURED 2026-09-16 (ES block head + 0x24).</summary>
        [FieldOffset(0x23C)] public int MaxES;

        /// <summary>Current energy shield. MEASURED 2026-09-16 (ES block head + 0x28).</summary>
        [FieldOffset(0x240)] public int CurES;

        /// <summary>
        /// Vector of buff pointers. NOT MEASURED on this client — inherited from the pre-2026 fork,
        /// which put it here, as did that fork's other Life struct. The pools moved by ~0x50-0x150
        /// bytes since then, so agreement between two stale sources is not evidence. Kept as a FIELD
        /// and not a property on purpose: <c>Life</c> resolves this offset by reflection over the
        /// <c>FieldOffset</c> attribute in a static initialiser, and removing the field would throw a
        /// <c>TypeInitializationException</c> for every Life component in the game rather than fail
        /// quietly. <c>Life.ParseBuffs</c> already bounds the walk and catches, so a wrong offset
        /// yields an empty buff list, not a crash. See <see cref="BuffsMeasured"/>.
        /// </summary>
        [FieldOffset(0x080)] public NativePtrArray Buffs;

        /// <summary>
        /// False: the reservation offsets are not known on this client, so this struct declares no
        /// reservation members at all. <c>ExileCore.PoEMemory.Components.Life</c> reads this flag and
        /// reports reservations as <c>null</c> rather than as a number; see the class remarks.
        /// </summary>
        public const bool ReservationsMeasured = false;

        /// <summary>False: <see cref="Buffs"/> is an inherited offset, not a measurement.</summary>
        public const bool BuffsMeasured = false;

        /// <summary>
        /// Whether all three block heads point back at the given component address — the measurement's
        /// own criterion for "these are the blocks", kept executable so a diagnostic can re-run it
        /// instead of trusting a comment.
        /// </summary>
        /// <param name="componentAddress">The address this struct was read from.</param>
        /// <returns>True when every block head equals <paramref name="componentAddress"/>.</returns>
        public bool BlockHeadsPointAtComponent(long componentAddress)
        {
            return componentAddress != 0 &&
                   HealthBlockSelfPtr == componentAddress &&
                   ManaBlockSelfPtr == componentAddress &&
                   EnergyShieldBlockSelfPtr == componentAddress;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Owner: {Owner:X} HP: {CurHP}/{MaxHP} Mana: {CurMana}/{MaxMana} ES: {CurES}/{MaxES}";
        }
    }
}
