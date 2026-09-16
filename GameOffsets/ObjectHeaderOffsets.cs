using System.Runtime.InteropServices;

namespace GameOffsets
{
    /// <summary>
    /// The first two qwords every game entity starts with: its vtable, and the pointer to the
    /// EntityDetails object shared by every entity that has the same metadata path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. Both offsets were measured by the 2026-09-16 survey against a live client; NOT yet
    /// re-confirmed. The survey read three entities of different kinds at once — the player, a doodad
    /// (Metadata/MiscellaneousObjects/DoodadNoBlocking) and a pet (Metadata/Pet/ScientistLabRat/...) —
    /// and both fields matched on all three, which is what makes this a layout rather than a
    /// coincidence in one object.
    /// </para>
    /// <para>
    /// WHAT CHANGED AND WHY. This struct used to declare <c>MainObject</c> at 0x0 and a
    /// <c>NativePtrArray ComponentList</c> at 0x40, and it sat inside
    /// <see cref="EntityOffsets"/> at 0x8. The 0x40 field was fiction: the survey read
    /// 0x8600000000010000 there on the player and zero on the next entity — neither a pointer, nor a
    /// count, nor a meaningful zero. It is removed, not "left in case it is useful": a field that
    /// nothing can distinguish from garbage is how this repository has repeatedly built objects out
    /// of noise (see LabDataPtr in GameOffsets/IngameDataOffsets.cs).
    /// </para>
    /// <para>
    /// The struct now starts at the entity's own address (<see cref="EntityOffsets"/> declares it at
    /// 0x0, not 0x8) and carries the vtable at 0x0 with <see cref="MainObject"/> at 0x8. THE
    /// ABSOLUTE ADDRESS OF <see cref="MainObject"/> IS UNCHANGED — it was entity+0x8+0x0 and it is
    /// now entity+0x0+0x8 — so every existing <c>EntityOffsets.Head.MainObject</c> call site keeps
    /// reading exactly the same qword. Only the name of the thing containing it became honest.
    /// </para>
    /// <para>
    /// RE-CONFIRM IN ONE PASS: <see cref="VTable"/> must be the SAME value on every entity in the
    /// area — the survey saw PathOfExile_KG.exe+0x3456508 on all three — and it must equal the qword
    /// that the entity-list node hands out. <see cref="MainObject"/> must differ between entities
    /// with different metadata paths and be EQUAL between two entities with the same path; the
    /// reference distribution prints this number as <c>MainObject</c> and the survey matched it
    /// (0x5F522251CA0 on the player).
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ObjectHeaderOffsets
    {
        /// <summary>
        /// The entity's vtable, identical on every entity — which is exactly what lets an address be
        /// recognised as an entity at all. Measured by the 2026-09-16 survey against a live client;
        /// NOT yet re-confirmed.
        /// </summary>
        [FieldOffset(0x0)] public long VTable;

        /// <summary>
        /// Pointer to the EntityDetails object. IT IS SHARED by every entity with the same metadata
        /// path, so NOTHING ENTITY-SPECIFIC MAY BE READ THROUGH IT: it owns the metadata path
        /// (<see cref="PathEntityOffsets"/>) and the component name-to-index lookup table
        /// (<see cref="ComponentLookupOffsets"/>), and both of those describe the KIND of entity,
        /// never this one instance. Measured by the 2026-09-16 survey against a live client; NOT yet
        /// re-confirmed. The name is kept from the previous version of this struct so that existing
        /// call sites compile unchanged; <see cref="EntityOffsets.EntityDetails"/> is the name that
        /// says what it is.
        /// </summary>
        [FieldOffset(0x8)] public long MainObject;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"VTable: {VTable:X} MainObject: {MainObject:X}";
        }
    }
}
