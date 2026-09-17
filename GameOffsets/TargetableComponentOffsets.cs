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
    /// EntCap.exe --igs &lt;address&gt; --census 0x346C238 --census-bytes 128
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
    /// HOW THE OFFSET WAS FOUND, AND WHY A SINGLE ORACLE ANSWER WOULD NOT HAVE DONE. Out of 128
    /// bytes, exactly TWO are strictly boolean and non-constant across the 71 entities in the zone
    /// that carry this component: 0x50 and 0x51. Everything else is either constant for monsters,
    /// chests and scenery alike — and so cannot be a per-entity flag — or not boolean. The reference
    /// distribution cannot separate 0x50 from 0x51: asked about one monster it answers
    /// "isTargetable True, isTargeted False", and BOTH candidates produce exactly that, because a
    /// monster is 1 at both. This is the same shape of error as the InventoryId +0x70 episode, where
    /// a number agreed on three entities and fell apart on forty.
    /// </para>
    /// <para>
    /// WHAT SEPARATED THEM. The two candidates disagree on exactly five entities out of 71, and the
    /// census prints them by name: four breakable pots (<c>Metadata/Chests/Pot4v1</c> and
    /// <c>Pot4v2</c>) and one cosmetic pet (<c>Metadata/Pet/ScientistLabRat</c>). Both are things a
    /// player cannot take as a target by clicking. At 0x50 all five are false; at 0x51 all five are
    /// true. So 0x50 carries "can be targeted" and 0x51 carries something broader — every entity the
    /// targeting system knows about at all. Note that this last step is decided by MEANING, not by a
    /// number: it would be overturned by showing that a breakable pot or a cosmetic pet can in fact
    /// be click-targeted.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct TargetableComponentOffsets
    {
        /// <summary>
        /// Whether the entity can be taken as a target. MEASURED 2026-09-17 at 0x50 by a population
        /// census over 71 components in six kinds of entity: monsters 58 of 59 true, miscellaneous
        /// objects 5 of 5, the player 1 of 1, a shrine 1 of 1, breakable pots 0 of 4, a cosmetic pet
        /// 0 of 1. Was 0x30, which is not a boolean byte at all.
        /// </summary>
        [FieldOffset(0x50)] public bool isTargetable;

        /// <summary>
        /// Whether the entity is the one currently targeted. NOT POSITIVELY CONFIRMED — only placed
        /// and not refuted, and the distinction is deliberate.
        /// </summary>
        /// <remarks>
        /// It sits where the old layout's +2 gap from <see cref="isTargetable"/> puts it, and the
        /// census shows 0x52 is constant 0 over the whole zone, which is what this flag should look
        /// like while the cursor rests on nothing. What rules out the nearer candidate is a
        /// population argument rather than a guess: at most ONE entity in a zone can be the targeted
        /// one, and 0x51 is true for 70 of 71, so 0x51 cannot be this flag. Confirming 0x52
        /// positively needs one cheap action — hover the cursor over a monster and re-run the census;
        /// exactly one entity should turn 1. Until that is done, treat a <c>true</c> here as
        /// unverified; <see cref="isTargetable"/> does not depend on it.
        /// </remarks>
        [FieldOffset(0x52)] public bool isTargeted;
    }
}
