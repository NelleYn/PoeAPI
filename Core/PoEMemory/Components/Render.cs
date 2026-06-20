using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using GameOffsets;
using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's render position, bounds, rotation, height, and name.
/// </summary>
public class Render : Component
{
    private readonly CachedValue<RenderComponentOffsets> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="Render"/> class.</summary>
    public Render()
    {
        _cachedValue = new FrameCache<RenderComponentOffsets>(() => M.Read<RenderComponentOffsets>(Address));
    }

    /// <summary>Gets the raw render offsets struct (cached per frame).</summary>
    public RenderComponentOffsets RenderStruct => _cachedValue.Value;

    /// <summary>Gets the X component of the render position.</summary>
    public float X => Pos.X;

    /// <summary>Gets the Y component of the render position.</summary>
    public float Y => Pos.Y;

    /// <summary>Gets the Z component of the render position.</summary>
    public float Z => Pos.Z;

    /// <summary>Gets the render world position.</summary>
    public Vector3 Pos => RenderStruct.Pos;

    /// <summary>Gets the center point used for interaction (position offset by half the bounds).</summary>
    public Vector3 InteractCenter => Pos + Bounds / 2;

    /// <summary>Gets the model height.</summary>
    public float Height => RenderStruct.Height > 0.01f ? RenderStruct.Height : 0f;

    /// <summary>Gets the entity render name (cached).</summary>
    public string Name => Cache.StringCache.Read($"{nameof(Render)}{RenderStruct.Name.buf}", () => RenderStruct.Name.ToString(M));

    /// <summary>Gets the model rotation.</summary>
    public Vector3 Rotation => RenderStruct.Rotation;

    /// <summary>Gets the model bounds.</summary>
    public Vector3 Bounds => RenderStruct.Bounds;

    /// <summary>Gets the mesh rotation.</summary>
    public Vector3 MeshRoration => RenderStruct.Rotation;

    /// <summary>Gets the terrain height at the entity's position.</summary>
    public float TerrainHeight => RenderStruct.Height > 0.01f ? RenderStruct.Height : 0f;
}
