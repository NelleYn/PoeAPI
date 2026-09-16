using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// Layout of a game entity on the installed client.
/// </summary>
/// <remarks>
/// <para>
/// PROVENANCE. This struct was first laid out by the 2026-09-16 survey in "The Sewers" against three
/// entities. It was then RE-MEASURED on 2026-09-16 in "The Reliquary" (PathOfExile_KG, pid 13660,
/// module base 0x7FF78FCD0000, zone 1_5_7, 132-142 entities) with the whole pointer chain read
/// inside ONE process, each snapshot bracketed by a check that the zone and the IngameData base had
/// not changed, and snapshots showing any state change discarded. That second pass CONFIRMED
/// <see cref="ComponentsArray"/>, <see cref="IngameDataPtr"/>, <see cref="Id"/> and
/// <see cref="PositionedPtr"/> by content, and REFUTED <see cref="InventoryId"/>. The per-field
/// remarks say which of the two each number rests on.
/// </para>
/// <para>
/// HOW TO RE-CONFIRM IN ONE PASS, in this order, because each step feeds the next:
/// (1) <c>Head.VTable</c> is the same value on every entity and equals the qword the entity-list
/// node hands out;
/// (2) <see cref="ComponentsArray"/>'s length divided by 8 equals the number of non-empty slots in
/// this entity's component lookup table — confirmed at 4, 9, 13 and 14 components on entities of
/// different kinds, which is a content match and not a range check;
/// (3) every component address resolved through that table holds THIS ENTITY'S ADDRESS at its own
/// +0x08;
/// (4) <see cref="PositionedPtr"/> equals the Positioned component the table resolves — 50 of 50
/// entities across seven kinds.
/// Step (4) is free and is the cheapest proof that the component model built on top of this struct
/// is right — see <see cref="ComponentLookupOffsets"/>.
/// </para>
/// <para>
/// WHAT WAS HERE BEFORE AND WHY IT WAS WRONG. The previous version declared the object header at
/// 0x8 and a single <c>long ComponentList</c> at 0x10, plus commented-out Id 0x40 /
/// InventoryId 0x58 while the live code read Id at 0x50 and InventoryId at 0x68. A single long at
/// 0x10 is the BEGIN POINTER of a std::vector, so it silently discards the COUNT of components —
/// and the count is the only bound that can reject an out-of-range slot index in the new lookup
/// model. Reading it as a vector is therefore not cosmetic; without it the model has no bound at
/// all.
/// </para>
/// <para>
/// DELIBERATELY NOT DECLARED, though the survey saw them: a second vtable at +0x28 and a second
/// vector at +0x30/+0x38/+0x40 that is exactly 0x58 bytes on EVERY entity and has nothing to do with
/// components. They are written down here so the next reader does not re-discover them, and left
/// undeclared so that nobody builds on them by accident.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityOffsets
{
    /// <summary>
    /// Offset of <see cref="Id"/> inside the entity, exposed as a constant because the entity id is
    /// ALSO read straight off a raw address in <c>EntityList.ParseEntity</c>, before any Entity
    /// object exists. The two readers MUST use the same number: if they disagree, every entity
    /// fails its own identity check in <c>Entity.Check(uint)</c> and the entity list comes back
    /// empty while the rest of the model is perfectly correct.
    /// </summary>
    public const int IdOffset = 0x88;

    /// <summary>
    /// False: <see cref="InventoryId"/> is NOT a measured field. It is exposed as a constant so a
    /// consumer can tell "never measured" apart from "measured and happens to be zero" without
    /// reading a comment — see the remarks on <see cref="InventoryId"/> for the evidence that
    /// refuted it.
    /// </summary>
    public const bool InventoryIdMeasured = false;

    /// <summary>
    /// The entity's vtable and its pointer to the shared EntityDetails object — see
    /// <see cref="ObjectHeaderOffsets"/>. Declared at 0x0 (it used to be at 0x8, with MainObject at
    /// its own 0x0), so <c>Head.MainObject</c> still reads the very same qword at entity+0x08.
    /// CONFIRMED BY CONTENT 2026-09-16: the path reached through <c>MainObject</c> — string at
    /// details+0x08, length at details+0x18 — had its declared length equal to the ACTUAL length of
    /// the string and began with "Metadata/" on 141 of 141 entities in the zone.
    /// </summary>
    [FieldOffset(0x00)] public ObjectHeaderOffsets Head;

    /// <summary>
    /// std::vector of pointers to this entity's components, in index order. The lookup table stores
    /// an INDEX into this array, so this vector's length is the only bound that can reject a bad
    /// index — which is why this is a vector here and not the bare begin pointer it used to be.
    /// CONFIRMED BY CONTENT 2026-09-16: (Last - First) / 8 equalled the number of non-empty slots in
    /// the entity's own lookup table, exactly, at 4, 9, 13 and 14 components.
    /// </summary>
    [FieldOffset(0x10)] public NativePtrArray ComponentsArray;

    /// <summary>
    /// THE WHOLE QWORD AT +0x70, declared wide ON PURPOSE. There is no measured field here; what is
    /// declared is the raw bytes, so that a reader can SEE what is actually there instead of being
    /// handed a plausible-looking 32-bit number. See <see cref="InventoryId"/>.
    /// </summary>
    [FieldOffset(0x70)] public ulong InventoryIdRaw;

    /// <summary>
    /// Pointer to the IngameData object this entity belongs to. CONFIRMED BY CONTENT 2026-09-16:
    /// equal to the CURRENT IngameData base on every entity in the zone, not merely equal to each
    /// other. Nothing in the fork reads it today — it is declared because it is a cheap way to tell
    /// a live entity from a stale address once there is a reason to check.
    /// </summary>
    [FieldOffset(0x78)] public long IngameDataPtr;

    /// <summary>
    /// Entity id. CONFIRMED 2026-09-16 that the id lives at THIS offset. See <see cref="IdOffset"/>
    /// for why this number also lives outside this struct and what a disagreement between the two
    /// readers costs, and <see cref="Flags"/> for the qword it shares.
    /// <para>
    /// TWO THINGS THE FORK BELIEVES ABOUT THIS NUMBER ARE NOT PART OF THAT MEASUREMENT, and are
    /// recorded here as INHERITED ASSUMPTIONS so nobody reads them as results: that the id is stable
    /// for the lifetime of an area, and that server-side entities carry ids above
    /// <c>int.MaxValue</c>. The second one is load-bearing — <c>Entity.ParseType</c> returns
    /// <c>EntityType.ServerObject</c> on it — and neither was tested in the 2026-09-16 passes.
    /// </para>
    /// </summary>
    [FieldOffset(0x88)] public uint Id;

    /// <summary>
    /// Entity flags. READ AS ONE BYTE ON PURPOSE, AND THAT IS NOT A STYLE CHOICE. MEASURED
    /// 2026-09-16: the low byte here is 0x0C on 40 of 40 entities, while the bytes above it DIFFER —
    /// so the field's WIDTH IS NOT PROVEN, and a uint read would silently fold unrelated neighbouring
    /// data into the value.
    /// <para>
    /// A RULE THAT WAS WRITTEN HERE AND IS NOW RETRACTED. A previous version of this comment claimed
    /// the high bytes sorted with the KIND of entity — 0x00 on doodads, 0x26 on monsters, 0x22 on
    /// chests and other objects. A SECOND MEASUREMENT ON 2026-09-16 CONTRADICTS IT DIRECTLY: monsters
    /// produce BOTH of those values (<c>Metadata/Monsters/ReliquaryMonsterEmerge</c> read
    /// 0x0000220C), and chests produce 0x0000220C as well as 0xFFFF220C. The grouping does not hold:
    /// the sample it was read off did not contain the entities that break it. The retraction is
    /// written out rather than the claim quietly deleted, so that a later pass looking at a similarly
    /// partial sample does not re-derive the same rule and believe it is new.
    /// </para>
    /// <para>
    /// WHAT IS MEASURED HERE, AND IT IS ONLY THIS. The low byte is 0x0C on 40 of 40 entities. The
    /// bytes above it DIFFER between entities, and BY WHAT RULE IS NOT ESTABLISHED — the high half is
    /// undecoded, not "mostly decoded". <see cref="Id"/> and this byte sit in the SAME QWORD: one
    /// entity read 0x0000260C_000004CE, i.e. id 0x4CE with 0x260C above it, which is why a wide read
    /// here would pull the id's neighbours in with it. Do not widen this field and do not name its
    /// bits until something is measured.
    /// </para>
    /// <para>
    /// TO SETTLE IT: find an entity whose flags the reference distribution reports as something other
    /// than 12 and check which bytes move. A monster and a chest sharing 0x220C while another monster
    /// carries 0x260C means the discriminator, if there is one, is NOT the entity kind — so the next
    /// pass needs the value tracked against something that changes ON ONE ENTITY, not against what
    /// the entity is.
    /// </para>
    /// <para>
    /// Declared as a plain <c>byte</c> and NOT as <see cref="EntityFlags"/> on purpose. That enum
    /// claims <c>Valid = 1</c>, and the only byte anybody has actually read here is 0x0C, which does
    /// not have bit 0 set on an entity the client was happily rendering. Typing the field as the
    /// enum would import an unmeasured meaning into a measured number; naming the bits is a separate
    /// measurement that nobody has made.
    /// </para>
    /// </summary>
    [FieldOffset(0x8C)] public byte Flags;

    /// <summary>
    /// Direct pointer to this entity's Positioned component, bypassing the lookup table. CONFIRMED
    /// BY CONTENT 2026-09-16: equal to the Positioned component that the lookup table resolves, on
    /// 50 of 50 entities across seven kinds.
    /// <para>
    /// This exists as a CROSS-CHECK on the lookup model (see <c>Entity.GetComponents</c>) and
    /// deliberately NOT as a fallback. If the table and this pointer disagree, the MODEL is wrong,
    /// and quietly preferring this pointer would hide the single cheapest signal that says so.
    /// </para>
    /// </summary>
    [FieldOffset(0x98)] public long PositionedPtr;

    /// <summary>
    /// Inventory slot id, where applicable. NOT MEASURED — THIS NUMBER IS REFUTED, AND IT IS KEPT
    /// ONLY BECAUSE <c>Entity.InventoryId</c> IS PUBLIC SURFACE THAT STILL HAS TO COMPILE. Check
    /// <see cref="InventoryIdMeasured"/> (always false) or <see cref="InventoryIdLooksLikePointer"/>
    /// before treating what comes out of here as data.
    /// <para>
    /// THE EVIDENCE. The 2026-09-16 survey accepted 0x70 after comparing it on THREE entities. The
    /// 2026-09-16 re-measurement over FORTY entities broke it: on Metadata/Chests/DarkPot2v2 the
    /// qword at +0x70 is 0x00007FF792EFFF00 — A POINTER INTO THE GAME MODULE — and reading its low
    /// half as a uint32 yields 0x92EFFF00, a number that looks like an id and is nothing but the
    /// bottom of a pointer. Across the rest the value was 0, 0x100, 0x400, and also 0x6050100,
    /// 0x9D690300, 0x459FD100, 0x5050500, 0x1010100. THIS IS EXACTLY THE FAILURE A THREE-ENTITY
    /// AGREEMENT CANNOT CATCH: three samples that all happen to hold 0 or 0x500 agree beautifully
    /// and prove nothing.
    /// </para>
    /// </summary>
    public uint InventoryId => (uint) InventoryIdRaw;

    /// <summary>
    /// Whether the qword at +0x70 has the shape of a pointer, in which case <see cref="InventoryId"/>
    /// is its low half and is certainly not an inventory id. This is the discriminator the refutation
    /// turned on, kept executable so a caller can apply it per entity instead of trusting a comment.
    /// A value of <c>false</c> does NOT make the field measured — see <see cref="InventoryIdMeasured"/>.
    /// </summary>
    public bool InventoryIdLooksLikePointer => NativePointer.IsCanonical((long) InventoryIdRaw);

    /// <summary>
    /// Pointer to the EntityDetails object shared by every entity with this entity's metadata path.
    /// Derived from <see cref="Head"/>, not a separate read; it is the name that says what the
    /// value is, where <c>Head.MainObject</c> is the name history left behind.
    /// </summary>
    public long EntityDetails => Head.MainObject;

    /// <summary>
    /// Number of component pointers in <see cref="ComponentsArray"/>. Derived from the vector's own
    /// span, never assumed.
    /// </summary>
    public long ComponentCount => ComponentsArray.Size / 8;

    /// <inheritdoc/>
    public override string ToString()
    {
        var inventory = InventoryIdLooksLikePointer
            ? $"[+0x70 not measured, holds a pointer {InventoryIdRaw:X}]"
            : $"[+0x70 not measured, raw {InventoryIdRaw:X}]";

        return $"VTable: {Head.VTable:X} Details: {EntityDetails:X} " +
               $"Components: [{ComponentsArray.First:X}..{ComponentsArray.Last:X}] ({ComponentCount}) " +
               $"Id: {Id:X} InventoryId: {inventory} Flags: {Flags:X2}";
    }
}
