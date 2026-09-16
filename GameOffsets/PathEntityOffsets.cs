using System.Runtime.InteropServices;

namespace GameOffsets
{
    /// <summary>
    /// The metadata path of an entity, stored in the EntityDetails object that every entity with the
    /// same metadata shares (<see cref="EntityOffsets.EntityDetails"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. Both offsets were measured by the 2026-09-16 survey against a live client and
    /// RE-CONFIRMED BY CONTENT the same day: the criterion below held on 141 of 141 entities of the
    /// zone, and again on entities of every kind met later (items, projectiles, town NPCs), in three
    /// different zones. This is the strongest confirmation any offset in this repository carries —
    /// it is not "one match in a window" but a self-checking relation that a wrong offset cannot
    /// satisfy. The previous numbers (Path 0x10, Length 0x20) were shifted by +0x8, which
    /// made <c>Path.Ptr</c> read zero. A zero there sets <c>IsValid = false</c> and classifies the
    /// entity as <c>EntityType.Error</c> — so these two numbers alone killed the ENTIRE entity
    /// classification. That is the price of the bug, and it is why this file is the first thing to
    /// re-confirm.
    /// </para>
    /// <para>
    /// THE IRON RE-CONFIRMATION CRITERION, and it needs no reference distribution and no tooling:
    /// THE NUMBER AT +0x18 MUST EQUAL THE CHARACTER LENGTH OF THE STRING AT +0x08. It held on 141 of
    /// 141 entities of a whole zone — 36 for "Metadata/MiscellaneousObjects/Doodad", 26 for
    /// "Metadata/Chests/DarkPot2v2", 68 for
    /// "Metadata/Monsters/OriathCivilian/OriathCivilianGhostFemaleRanged1@44" — and the survey had
    /// already seen it three times before that:
    /// 33 for "Metadata/Characters/DexInt/DexInt",
    /// 46 for "Metadata/MiscellaneousObjects/DoodadNoBlocking",
    /// 44 for "Metadata/Pet/ScientistLabRat/..." .
    /// Read the qword at +0x18, read the UTF-16 string at +0x08, compare the two. Equal on several
    /// entities of different kinds means both offsets are right; unequal means at least one of them
    /// is wrong, however plausible the text looks. A path that merely "starts with Metadata" is NOT
    /// the criterion — a stale pointer into a neighbouring path satisfies it and proves nothing.
    /// </para>
    /// <para>
    /// WHY THE SHIFT WAS EXACTLY 0x8 — HYPOTHESIS, NOT A MEASUREMENT, stated as a hypothesis and
    /// offered only because it predicts the rest of the object. The layout is consistent with an
    /// MSVC std::wstring beginning at EntityDetails+0x08: an 8-byte buffer pointer, 8 bytes reserved
    /// for the small-string optimisation, the size at +0x18 and the capacity at +0x20 — i.e. exactly
    /// <see cref="GameOffsets.Native.NativeUnicodeText"/>. The same survey measured that same shape
    /// independently in Render.Name (text 0x148, length 0x158, capacity 0x160). If that reading
    /// holds, this struct could later collapse into a single text field and get the small-string
    /// case for free. It is left as two fields here so that the call sites (Entity.Path and
    /// MiscHelpers.ToString(PathEntityOffsets, IMemory)) compile unchanged and this remains ONE
    /// measurable step. Metadata paths are far longer than 7 characters, so the small-string case
    /// cannot bite today.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PathEntityOffsets
    {
        /// <summary>
        /// Pointer to the UTF-16 metadata path. Measured by the 2026-09-16 survey and re-confirmed
        /// by content the same day on 141 of 141 entities of a zone. Was 0x10.
        /// </summary>
        [FieldOffset(0x08)] public StringPtr Path;

        /// <summary>
        /// Length of <see cref="Path"/> IN CHARACTERS, not bytes — callers double it to get the
        /// byte count. Measured by the 2026-09-16 survey and re-confirmed by content the same day on
        /// 141 of 141 entities of a zone. Was 0x20. Only the OFFSET is measured: the width is
        /// declared <c>long</c>
        /// because that is what the call sites expect and what the hypothesised std::wstring layout
        /// implies, and nothing has proven the field is eight bytes wide.
        /// </summary>
        [FieldOffset(0x18)] public long Length;

        /*public  string ToString(IMemory mem) {
            return mem.ReadStringU(Path.Ptr,(int) Length * 2);
        } */

        /// <summary>A bare pointer to a remote string.</summary>
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct StringPtr
        {
            /// <summary>Address of the first character.</summary>
            [FieldOffset(0x0)] public long Ptr;
        }
    }
}
