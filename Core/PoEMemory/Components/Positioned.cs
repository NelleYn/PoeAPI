using ExileCore.Shared.Cache;
using GameOffsets;
using GameOffsets.Native;
using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's grid and world position, rotation, and reaction.
/// </summary>
public class Positioned : Component
{
    private readonly CachedValue<PositionedComponentOffsets> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="Positioned"/> class.</summary>
    public Positioned()
    {
        _cachedValue = new FrameCache<PositionedComponentOffsets>(() => M.Read<PositionedComponentOffsets>(Address));
    }

    /// <summary>Gets the raw positioned offsets struct (cached per frame).</summary>
    public PositionedComponentOffsets PositionedStruct => _cachedValue.Value;

    /// <summary>Gets the address of the entity that owns this component.</summary>
    public long OwnerAddress => PositionedStruct.OwnerAddress;

    /// <summary>Gets the X grid coordinate.</summary>
    public int GridX => PositionedStruct.GridX;

    /// <summary>Gets the Y grid coordinate.</summary>
    public int GridY => PositionedStruct.GridY;

    /// <summary>Gets the grid position as a floating-point vector.</summary>
    public Vector2 GridPos => new(GridX, GridY);

    /// <summary>Gets the grid position as an integer vector.</summary>
    public Vector2i GridPosI => new(GridX, GridY);

    /// <summary>
    /// Gets the world position, X and Y. Built from the two measured floats rather than handed out
    /// as a whole vector, because the measured field is three floats wide (0x2B8..0x2C0) while this
    /// property's type — which <c>NumericsCompat</c> and callers depend on — is two-dimensional.
    /// </summary>
    public Vector2 WorldPos => new(PositionedStruct.WorldX, PositionedStruct.WorldY);

    /// <summary>Gets the world position including the Z axis.</summary>
    public Vector3 WorldPos3 => PositionedStruct.WorldPosition;

    /// <summary>
    /// Gets the grid position. IDENTICAL TO <see cref="GridPos"/>, and that is the fix rather than a
    /// redundancy: this used to return a separate struct field declared ON TOP OF the two grid
    /// integers and typed <c>Vector2</c>, so it reinterpreted int32 grid coordinates as floats and
    /// could only ever yield denormals near zero. Kept as a name so existing callers keep compiling,
    /// now reading the same measured integers as everything else.
    /// </summary>
    public Vector2 GridPosition => GridPos;

    /// <summary>
    /// Gets the rotation, in radians. NOT MEASURED on the installed client; the offset is inherited
    /// from the pre-2026 fork. See <see cref="PositionedComponentOffsets.RotationMeasured"/>.
    /// </summary>
    public float Rotation => PositionedStruct.Rotation;

    /// <summary>Gets the X world coordinate.</summary>
    public float WorldX => PositionedStruct.WorldX;

    /// <summary>Gets the Y world coordinate.</summary>
    public float WorldY => PositionedStruct.WorldY;

    /// <summary>Gets the rotation, in degrees.</summary>
    public float RotationDeg => Rotation * (180 / MathUtil.Pi);

    /// <summary>
    /// Gets the entity's reaction value (used to determine hostility). NOT MEASURED on the installed
    /// client: the offset is inherited from the pre-2026 fork, and everything else in this component
    /// moved by ~0x1B0 bytes since then. See <see cref="PositionedComponentOffsets.ReactionMeasured"/>.
    /// </summary>
    public byte Reaction => PositionedStruct.Reaction;
}
