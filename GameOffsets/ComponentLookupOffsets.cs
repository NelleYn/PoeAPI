using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// The metadata-owned table that lists one slot per component of an entity of that metadata: a
/// 32-bit key and the component's INDEX inside the owning entity's component pointer array.
/// </summary>
/// <remarks>
/// <para>
/// WHAT THIS TABLE IS FOR TODAY: A CONTROL, AND NOTHING ELSE. It is NOT on the path that resolves a
/// component to a type — that path is <see cref="ComponentVtables"/>, keyed by the component's own
/// vtable. Reading this table is an extra dereference chain that buys nothing for resolution, so
/// <c>Entity.GetComponents</c> does not touch it at all; <c>Entity.CountNonEmptyLookupSlots</c>
/// reads it on demand, for the one independent check described below.
/// </para>
/// <para>
/// THE STRUCTURE IS CONFIRMED BY CONTENT (2026-09-16, live client, zone "The Reliquary", 132-142
/// entities). For every entity examined, the number of NON-EMPTY slots here equals the number of
/// component pointers in the entity's own array (4=4, 9=9, 13=13, 14=14), every index resolved to a
/// component whose owner pointer at <c>[component + 0x08]</c> pointed back at that same entity, and
/// the table's slot capacity was always a power of two (8 / 16 / 32). That makes "non-empty slots
/// == component count" a cheap, INDEPENDENT second opinion on the component array — independent
/// because the count comes from the metadata object and the array comes from the entity.
/// </para>
/// <para>
/// THE KEY IS NOT A TYPE ID, AND THAT IS MEASURED, NOT SUSPECTED. See <see cref="ComponentSlot.Key"/>
/// and the refutation written out in <see cref="ComponentVtables"/>: the same component type appears
/// under different keys depending on the owner's kind (BaseEvents 0x114 / 0x214), and the same key
/// covers different types on different metadata (0x1D8 is Chest on chests and WorldItem on ground
/// items). Anything that maps this key to a type name is therefore wrong in both directions, and no
/// table of such pairs can be completed into a correct one.
/// </para>
/// <para>
/// THE PATH TO THE TABLE, measured 2026-09-16 on a live client:
/// <code>
/// details   = [entity + 0x08]
/// lookup    = [details + 0x28]
/// slots     = lookup + 0x40                    (std::vector of 8-byte slots)
/// component = [entity.ComponentsArray.First + slot.Index * 8]
/// </code>
/// The code this replaced walked <c>[details + 0x38]</c> and then <c>+0x30</c>, and that middle
/// link is DEAD on this client: it reads 0x0000000200000007, which is not a mapped address.
/// </para>
/// <para>
/// PROBE ORDER IS IRRELEVANT AND MUST NOT BE GUESSED. The reader SCANS EVERY SLOT and keeps the
/// non-empty ones, so nothing here depends on the client's hash function, on its probing sequence,
/// or on the table being densely packed.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentLookupOffsets
{
    /// <summary>
    /// Offset, inside the EntityDetails object, of the pointer to this lookup object. Measured
    /// 2026-09-16 on a live client. Re-confirm by reading the vtable at the target: it must be the
    /// same value for entities with different metadata paths.
    /// </summary>
    public const int PointerOffsetInEntityDetails = 0x28;

    /// <summary>Offset of <see cref="Slots"/> inside this object. Measured 2026-09-16 on a live client.</summary>
    public const int SlotsOffset = 0x40;

    /// <summary>
    /// This object's vtable. The survey saw one and the same value (PathOfExile_KG.exe+0x35ABE08)
    /// on entities with different metadata paths, which is how the live root was separated from the
    /// dead one. The VALUE is deliberately not frozen into a constant here: it is a module-relative
    /// address measured in one run, and this file does not carry numbers that nothing re-checks.
    /// </summary>
    [FieldOffset(0x00)] public long VTable;

    /// <summary>
    /// Vtable of the component descriptor. LOCATION MEASURED, VALUES NOT, AND NO LONGER NEEDED. It
    /// was once reserved as a fallback way to name a component type; that problem is solved, and
    /// solved better, by the component's OWN vtable at <c>[component + 0x00]</c> — see
    /// <see cref="ComponentVtables"/>, which needs no metadata lookup at all. Kept declared so the
    /// next reader does not re-discover the field, and unused so nobody builds on it by accident.
    /// </summary>
    [FieldOffset(0x10)] public long DescriptorVTable;

    /// <summary>
    /// std::vector of <see cref="ComponentSlot"/>. The element count is a power of two (8 / 16 / 32
    /// measured) and is derived from the vector's own span — never assumed.
    /// </summary>
    [FieldOffset(0x40)] public NativePtrArray Slots;

    /// <summary>Number of 8-byte slots in <see cref="Slots"/>, empty ones included. Derived, not read.</summary>
    public long SlotCount => Slots.Size / 8;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"VTable: {VTable:X} DescriptorVTable: {DescriptorVTable:X} " +
               $"Slots: [{Slots.First:X}..{Slots.Last:X}] ({SlotCount})";
    }
}

/// <summary>
/// One fixed 8-byte slot of the metadata's component table: a 32-bit key, and the index of that
/// component inside the owning entity's component pointer array.
/// </summary>
/// <remarks>
/// An empty slot is eight zero bytes. <see cref="Index"/> indexes
/// <see cref="EntityOffsets.ComponentsArray"/> and is meaningless without it: AN INDEX IS NOT AN
/// ADDRESS, and it must be bounds-checked against that array's real length before use. Measured
/// 2026-09-16 on a live client.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ComponentSlot
{
    /// <summary>
    /// The slot's 32-bit key. IT IS NOT A COMPONENT TYPE ID — that was measured and refuted, in both
    /// directions, on 2026-09-16; see <see cref="ComponentVtables"/> for the evidence. It is left
    /// unnamed here because what it actually encodes (it appears to fold the owner's kind into the
    /// value: the same type shifts by exactly 0x100 between chests and everything else) has not been
    /// measured, and this repository does not name numbers it has not established.
    /// </summary>
    public uint Key;

    /// <summary>Index into the owning entity's component pointer array.</summary>
    public uint Index;

    /// <summary>Whether this slot is empty, i.e. all eight bytes are zero.</summary>
    public bool IsEmpty => Key == 0 && Index == 0;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Key: 0x{Key:X} Index: {Index}";
    }
}
