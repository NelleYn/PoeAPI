using System.Runtime.InteropServices;

namespace GameOffsets
{
    /// <summary>
    /// Layout of the Targetable component on the installed client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. MEASURED 2026-09-17 against a live client (PathOfExile_KG, pid 24676, module base
    /// 0x7FF78FCD0000, an act-5 zone at level 48) with <c>tools/EntCap --census 0x346C238</c>, which
    /// collects this component from EVERY entity in the zone and reports, per byte offset, whether
    /// the value is strictly boolean across the whole population and whether it varies. The
    /// component is located by its own vtable RVA, not by the slot table. Reproduce with:
    /// <code>
    /// FindOffset.exe --ingame-state
    /// EntCap.exe --igs &lt;address&gt; --census 0x346C238 --census-bytes 2048
    /// </code>
    /// The byte count is DECIMAL (0x-prefixed hex is also accepted); 2048 and not 128, because
    /// 128 is what produced the overstated claim corrected below.
    /// <code>
    /// </code>
    /// </para>
    /// <para>
    /// WHY THE OLD NUMBERS WERE WRONG. <see cref="isTargetable"/> was declared at 0x30 and
    /// <see cref="isTargeted"/> at 0x32. The census shows 0x30..0x34 are not boolean at all — they
    /// hold values above 1 — so the old fields read an arbitrary byte and, since C# marshals any
    /// non-zero byte as <c>true</c>, reported nearly everything as targetable. That is worse than
    /// reporting nothing: a false <c>true</c> is indistinguishable from a real one at the call site.
    /// </para>
    /// <para>
    /// HOW THE OFFSET WAS FOUND, AND WHY A SINGLE ORACLE ANSWER WOULD NOT HAVE DONE. The reference
    /// distribution cannot separate 0x50 from 0x51: asked about one monster it answers
    /// "isTargetable True, isTargeted False", and BOTH candidates produce exactly that, because a
    /// monster is 1 at both. This is the same shape of error as the InventoryId +0x70 episode, where
    /// a number agreed on three entities and fell apart on forty. What separates them is a census
    /// over the whole zone, described below.
    /// </para>
    /// <para>
    /// A CLAIM THAT WAS FIRST WRITTEN HERE TOO STRONGLY, AND IS CORRECTED. The first version of this
    /// note said "out of 128 bytes exactly TWO are strictly boolean and non-constant". That is a
    /// statement about a 128-byte window, not about the component. Re-run over 2048 bytes there are
    /// FIFTY-FIVE such bytes. The extra ones are not scattered: the component carries a REPEATING
    /// sub-structure with a stride of 0x90 — runs of 48 boolean bytes begin at 0xE0, 0x170, 0x200,
    /// 0x290, 0x320 and 0x440, each with the same five varying positions — which is an array of
    /// records, not a set of flags. This is the radius trap the camera measurement already paid for
    /// once: a short window does not refute a twin, it fails to SEE it. The claim that survives is
    /// narrower and is the one that matters: within the component HEAD, the 74-byte run of boolean
    /// bytes from 0x36 to 0x7F, exactly TWO are non-constant, and they are 0x50 and 0x51.
    /// </para>
    /// <para>
    /// WHAT SEPARATED THEM. The two candidates disagree on exactly five entities, and the census
    /// prints them by name: four breakable pots (<c>Metadata/Chests/Pot4v1</c> and <c>Pot4v2</c>)
    /// and one cosmetic pet (<c>Metadata/Pet/ScientistLabRat</c>). At 0x50 all five are false; at
    /// 0x51 all five are true, so 0x51 is strictly the broader of the two. The inherited layout in
    /// <c>GameOffsets/Components/Targetable.cs</c> names exactly that pairing in exactly that order —
    /// <c>IsTargetable</c> then <c>IsHighlightable</c> — and a breakable pot and a cosmetic pet are
    /// precisely the things an outline highlights but a click cannot take as a target. Field ORDER
    /// is form, which this project takes from the reference; the numbers are not. So the assignment
    /// rests on the measured breadth relation plus the inherited order, and it would be overturned by
    /// showing that a pot can in fact be click-targeted.
    /// </para>
    /// <para>
    /// A STRUCTURAL ARGUMENT THAT WAS TRIED AND FAILED, recorded because a failed check is a result.
    /// If the whole block had simply moved by +0x20, the inherited <c>UnknownPtr0</c> at 0x28 would
    /// land on 0x48 and would have to be a canonical pointer on every carrier. It is not: 0x48
    /// through 0x4F fall inside the boolean run, so every byte there is 0 or 1 on every carrier and
    /// the qword cannot be a pointer. The block did NOT simply shift, so "the old layout moved by
    /// 0x20" cannot be used to justify any offset here — see the note on <see cref="isTargeted"/>,
    /// which is what that argument was holding up.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct TargetableComponentOffsets
    {
        /// <summary>
        /// Whether the entity can be taken as a target. MEASURED 2026-09-17 at 0x50 by a population
        /// census over every carrier in the zone across six kinds of entity, repeated in two runs
        /// (71 and 66 carriers as the zone changed around the player): monsters 58 of 59 and 53 of 54
        /// true, miscellaneous objects 5 of 5, the player 1 of 1, a shrine 1 of 1, breakable pots
        /// 0 of 4, a cosmetic pet 0 of 1. Was 0x30, which is not a boolean byte at all.
        /// </summary>
        [FieldOffset(0x50)] public bool isTargetable;

        /// <summary>
        /// Whether the entity is the one currently targeted. NOT MEASURED. This offset is a
        /// PLACEHOLDER kept only so the field keeps compiling; do not rely on its value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// What is actually known is only negative. One population argument does hold: at most ONE
        /// entity in a zone can be the targeted one, and 0x51 is true for nearly every carrier, so
        /// 0x51 is certainly not this flag. And 0x52 does read a constant 0 across the zone, which is
        /// what the flag should look like while the cursor rests on nothing — but so do dozens of
        /// other bytes in the 0x36..0x7F run, and nothing measured distinguishes 0x52 from any of
        /// them. The one structural argument that would have — "the old block moved by +0x20, so the
        /// old 0x32 becomes 0x52" — was tested and FAILED, as recorded on the struct.
        /// </para>
        /// <para>
        /// Settling it costs one cursor movement and no click: hover a monster and re-run
        /// <c>EntCap --census 0x346C238</c>. The flag is self-identifying, because the criterion is
        /// uniqueness rather than a value — exactly ONE carrier must turn 1, and it must be the one
        /// under the cursor. Nothing in this fork or in the autopilot reads this property today
        /// (<see cref="isTargetable"/> is read in five places and does not depend on it), so the
        /// placeholder costs nothing as long as it stays labelled.
        /// </para>
        /// </remarks>
        [FieldOffset(0x52)] public bool isTargeted;
    }
}
