using System.Runtime.InteropServices;
using SharpDX;

namespace GameOffsets
{
    /// <summary>
    /// Layout of the Positioned component on the installed client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. <see cref="OwnerAddress"/>, <see cref="GridX"/>, <see cref="GridY"/> and
    /// <see cref="WorldPosition"/> were MEASURED against a live client on 2026-09-16 (PathOfExile_KG,
    /// pid 13660, module base 0x7FF78FCD0000, zone "The Reliquary" 1_5_7, 132-142 entities). The rest
    /// of this struct is INHERITED FROM THE PRE-2026 FORK AND HAS NOT BEEN MEASURED — see the
    /// per-field remarks, and see the warning below about why those numbers are now suspect.
    /// </para>
    /// <para>
    /// THE TRAP, AND IT ALREADY CAUGHT ONE ATTEMPT: THE GRID POSITION IS TWO int32, NOT TWO float.
    /// The first measurement pass declared "does not match" and nearly discarded a correct offset,
    /// purely because it read 0x294/0x298 as floats. The bit pattern of a small integer read as a
    /// float is a denormal near zero, so a float read here does not produce an obviously wrong
    /// number — it produces a plausible-looking near-zero coordinate, which is exactly the failure
    /// that leaves an A* search starting from {0,0}. If you re-measure and it "does not match",
    /// CHECK THE TYPE FIRST.
    /// </para>
    /// <para>
    /// HOW TO RE-CONFIRM, and this is the criterion the measurement used: read
    /// <see cref="WorldPosition"/> and the grid pair off the SAME component and divide. World over
    /// grid is 10.87 on BOTH axes, on every entity. The four pairs measured were
    /// 4114.13/378 = 10.884, 11005.44/1012 = 10.875, 4984.34/458 = 10.883, 5896.74/542 = 10.879.
    /// A ratio that is not ~10.87, or that differs between the axes, means one of the two offsets
    /// moved. Second, free cross-check: <see cref="WorldPosition"/>.X/.Y equals the Render
    /// component's Pos.X/.Y exactly (see <see cref="RenderComponentOffsets"/>), so two independent
    /// components have to agree before this is believed.
    /// </para>
    /// <para>
    /// WHAT THE OLD NUMBERS WERE. Grid was declared at 0xE0/0xE4 and world at 0x110 — both from the
    /// pre-2026 fork, both wrong for this client by ~0x1B0 bytes. That shift is the reason the
    /// unmeasured fields below (<see cref="Reaction"/>, <see cref="Rotation"/>, <see cref="Size"/>)
    /// are not merely "unconfirmed" but ACTIVELY LIKELY TO BE WRONG: everything else in this struct
    /// moved, so there is no reason to expect they did not. They are left in place, unchanged and
    /// labelled, because inventing replacement numbers is worse than carrying an honest label — but
    /// nothing should be built on them until somebody measures them.
    /// </para>
    /// <para>
    /// ALSO DELETED, ON PURPOSE: a <c>Vector2 GridPosition</c> that was declared at 0xE0, i.e. ON TOP
    /// OF the two grid ints, reinterpreting them as floats. That field could never have held
    /// anything but the denormal garbage described above. <c>Positioned.GridPosition</c> in the
    /// wrapper now builds its vector from the two ints instead.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PositionedComponentOffsets
    {
        /// <summary>
        /// Address of the entity that owns this component. MEASURED 2026-09-16: every component
        /// resolved through the lookup table, on every entity walked, held its own entity's address
        /// at +0x08 — which is what makes this the cheapest validity test there is, and why
        /// <c>Entity.GridPos</c> refuses a Positioned whose owner is not itself.
        /// </summary>
        [FieldOffset(0x08)] public long OwnerAddress;

        /// <summary>
        /// X grid coordinate. MEASURED 2026-09-16. INT32 — read the class remarks before changing
        /// this type.
        /// </summary>
        [FieldOffset(0x294)] public int GridX;

        /// <summary>
        /// Y grid coordinate. MEASURED 2026-09-16. INT32 — read the class remarks before changing
        /// this type.
        /// </summary>
        [FieldOffset(0x298)] public int GridY;

        /// <summary>
        /// World position, three floats. MEASURED 2026-09-16: X and Y agree exactly with the Render
        /// component's Pos, and divided by the grid pair give 10.87 on both axes.
        /// </summary>
        [FieldOffset(0x2B8)] public Vector3 WorldPosition;

        /// <summary>X world coordinate. Same bytes as <see cref="WorldPosition"/>.X.</summary>
        [FieldOffset(0x2B8)] public float WorldX;

        /// <summary>Y world coordinate. Same bytes as <see cref="WorldPosition"/>.Y.</summary>
        [FieldOffset(0x2BC)] public float WorldY;

        /// <summary>Z world coordinate. Same bytes as <see cref="WorldPosition"/>.Z.</summary>
        [FieldOffset(0x2C0)] public float WorldZ;

        /// <summary>
        /// NOT MEASURED on this client. Inherited from the pre-2026 fork, where it was 0x58.
        /// <c>Entity.IsHostile</c> reads it as <c>(Reaction &amp; 0x7f) != 1</c>, so whatever byte
        /// actually lives here today is already deciding hostility — and the rest of this struct
        /// moved by ~0x1B0 bytes between that fork and this client, so this number should be assumed
        /// wrong until measured. See <see cref="ReactionMeasured"/>.
        /// </summary>
        [FieldOffset(0x58)] public byte Reaction;

        /// <summary>
        /// NOT MEASURED on this client. Inherited from the pre-2026 fork, where it was 0xE8 — which
        /// on this client is nowhere near the grid/world block that moved to 0x294..0x2C0. Assume
        /// wrong. See <see cref="RotationMeasured"/>.
        /// </summary>
        [FieldOffset(0xE8)] public float Rotation;

        /// <summary>
        /// NOT MEASURED on this client, and nothing in this fork reads it. Inherited from the
        /// pre-2026 fork, where it was 0x64. See <see cref="SizeMeasured"/>.
        /// </summary>
        [FieldOffset(0x64)] public int Size;

        /// <summary>
        /// False: <see cref="Reaction"/> is an inherited number, not a measurement. A diagnostic that
        /// prints hostility must be able to say "not measured" instead of printing a value as if it
        /// were data; this constant is how it tells the difference.
        /// </summary>
        public const bool ReactionMeasured = false;

        /// <summary>False: <see cref="Rotation"/> is an inherited number, not a measurement.</summary>
        public const bool RotationMeasured = false;

        /// <summary>False: <see cref="Size"/> is an inherited number, not a measurement.</summary>
        public const bool SizeMeasured = false;

        /// <summary>
        /// Whether the grid and world readings are consistent with each other: world divided by grid
        /// is ~10.87 on both axes. This is the measurement's own acceptance criterion, kept
        /// executable so a diagnostic can re-run it instead of trusting a comment. Returns false when
        /// either grid coordinate is 0 (nothing to divide by), which is also the exact symptom of a
        /// grid offset that has moved.
        /// </summary>
        public bool GridMatchesWorld
        {
            get
            {
                if (GridX == 0 || GridY == 0)
                    return false;

                var rx = WorldX / GridX;
                var ry = WorldY / GridY;

                return rx > 10.5f && rx < 11.2f && ry > 10.5f && ry < 11.2f;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Owner: {OwnerAddress:X} Grid: ({GridX}, {GridY}) " +
                   $"World: ({WorldX:F1}, {WorldY:F1}, {WorldZ:F1}) ratio-ok: {GridMatchesWorld}";
        }
    }
}
