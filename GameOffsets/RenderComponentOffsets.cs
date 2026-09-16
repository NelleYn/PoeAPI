using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets
{
    /// <summary>
    /// Layout of the Render component on the installed client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. <see cref="Pos"/>, <see cref="Bounds"/> and <see cref="Name"/> were MEASURED
    /// against a live client on 2026-09-16 (PathOfExile_KG, pid 13660, module base 0x7FF78FCD0000,
    /// zone "The Reliquary" 1_5_7). <see cref="Rotation"/> and <see cref="Height"/> are INHERITED
    /// FROM THE PRE-2026 FORK AND NOT MEASURED. The numbers themselves live in
    /// <see cref="Offsets"/>, one file over, with the same provenance notes.
    /// </para>
    /// <para>
    /// HOW TO RE-CONFIRM, cheapest first:
    /// (1) <see cref="Pos"/>.X and .Y equal the Positioned component's WorldPosition X and Y on the
    /// same entity, exactly — this is how the offset was identified and it costs one extra read;
    /// (2) <see cref="Name"/>'s length field equals the ACTUAL length of the decoded string. That
    /// was checked against strings of both storage forms: "Risen Saint" (11), "Gold Pot" (8),
    /// "Reliquarian" (11), "Shadow" (6) and the empty string (0), and matched every time.
    /// </para>
    /// <para>
    /// WHY <see cref="NativeStringU"/> AND NOT <c>NativeUnicodeText</c>. The measured shape — buffer
    /// at +0x00, eight reserved bytes at +0x08, length at +0x10, capacity at +0x18, with capacity
    /// &gt;= 8 meaning the characters live on the heap and anything smaller meaning they are stored
    /// INSIDE the object — is the shape of both types. <see cref="NativeStringU"/> is the one that
    /// already has the reader implementing exactly that rule (<c>MiscHelpers.ToString</c>), so it is
    /// used here. Its <c>[Obsolete]</c> marker is about the type's NAME, not about the layout being
    /// stale; replacing it is a rename, not a measurement, and does not belong in this change.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct RenderComponentOffsets
    {
        /// <summary>
        /// Render world position, three floats. MEASURED 2026-09-16; X and Y agree exactly with
        /// Positioned.WorldPosition.
        /// </summary>
        [FieldOffset((int) Offsets.RenderComponentOffsetsPos)]
        public Vector3 Pos;

        /// <summary>
        /// Model bounds, three floats, directly after <see cref="Pos"/>. MEASURED 2026-09-16.
        /// </summary>
        [FieldOffset((int) Offsets.RenderComponentOffsetsBounds)]
        public Vector3 Bounds;

        /// <summary>
        /// Render name, an EMBEDDED native UTF-16 string — the struct is inline here, not behind a
        /// pointer. MEASURED 2026-09-16; see the class remarks for the length/capacity criterion and
        /// for why the storage is sometimes inline and sometimes on the heap.
        /// </summary>
        [FieldOffset((int) Offsets.RenderComponentOffsetsName)]
        public NativeStringU Name;

        /// <summary>
        /// NOT MEASURED on this client. Inherited from the pre-2026 fork. See
        /// <see cref="RotationMeasured"/>.
        /// </summary>
        [FieldOffset((int) Offsets.RenderComponentOffsetsRotation)]
        public Vector3 Rotation;

        /// <summary>
        /// NOT MEASURED on this client. Inherited from the pre-2026 fork. See
        /// <see cref="HeightMeasured"/>. <c>Render.Height</c> and <c>Render.TerrainHeight</c> already
        /// gate it with <c>&gt; 0.01f</c>, so an unmeasured offset that happens to hold a small or
        /// negative float degrades to 0 rather than to a wild number — that gate is a guard, not a
        /// measurement.
        /// </summary>
        [FieldOffset((int) Offsets.RenderComponentOffsetsHeight)]
        public float Height;

        /// <summary>
        /// False: <see cref="Rotation"/> is an inherited number, not a measurement. A diagnostic must
        /// be able to say "not measured" rather than print the value as if it were data.
        /// </summary>
        public const bool RotationMeasured = false;

        /// <summary>False: <see cref="Height"/> is an inherited number, not a measurement.</summary>
        public const bool HeightMeasured = false;
    }
}
