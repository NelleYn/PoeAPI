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

    /// <summary>Gets the world position.</summary>
    public Vector2 WorldPos => PositionedStruct.WorldPosition;

    /// <summary>Gets the sub-grid position.</summary>
    public Vector2 GridPosition => PositionedStruct.GridPosition;

    /// <summary>Gets the rotation, in radians.</summary>
    public float Rotation => PositionedStruct.Rotation;

    /// <summary>Gets the X world coordinate.</summary>
    public float WorldX => PositionedStruct.WorldX;

    /// <summary>Gets the Y world coordinate.</summary>
    public float WorldY => PositionedStruct.WorldY;

    /// <summary>Gets the rotation, in degrees.</summary>
    public float RotationDeg => Rotation * (180 / MathUtil.Pi);

    /// <summary>Gets the entity's reaction value (used to determine hostility).</summary>
    public byte Reaction => PositionedStruct.Reaction;
}
