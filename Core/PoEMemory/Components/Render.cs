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

    /// <summary>
    /// Gets the render world position. MEASURED 2026-09-16 at component + 0x120, three floats; its X
    /// and Y equal <c>Positioned.WorldPos</c> exactly, which is how the offset was confirmed.
    /// </summary>
    public Vector3 Pos => RenderStruct.Pos;

    /// <summary>Gets the center point used for interaction (position offset by half the bounds).</summary>
    public Vector3 InteractCenter => Pos + Bounds / 2;

    /// <summary>
    /// Gets the model height. NOT MEASURED on the installed client — the offset is inherited from the
    /// pre-2026 fork while everything measured in this component moved by ~0xA8 bytes. See
    /// <see cref="RenderComponentOffsets.HeightMeasured"/>. The <c>&gt; 0.01f</c> gate below is a
    /// guard against an unmeasured offset, not evidence that the number is right.
    /// </summary>
    public float Height => RenderStruct.Height > 0.01f ? RenderStruct.Height : 0f;

    /// <summary>
    /// Gets the entity render name (cached). MEASURED 2026-09-16 at component + 0x148 as an EMBEDDED
    /// native UTF-16 string: capacity (0x160) of 8 or more means the characters are on the heap,
    /// anything less means they sit inside the component itself, and the reader below implements
    /// exactly that rule. Verified against strings of both forms — "Risen Saint", "Gold Pot",
    /// "Reliquarian", "Shadow" and the empty string all had lengths matching the length field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// THE CACHE KEY CARRIES THE WHOLE STRUCT, AND THE MEASUREMENT IS WHY. The key used to be
    /// <c>$"{nameof(Render)}{Name.buf}"</c>, which was defensible only as long as <c>buf</c> was
    /// believed to be a pointer. It is not always one: the 2026-09-16 measurement established that
    /// this string is EMBEDDED whenever its capacity is under 8, and in that form <c>buf</c> holds
    /// the string's FIRST FOUR UTF-16 CHARACTERS. Any two embedded names sharing four leading
    /// characters — the measured "Shadow" and any other name beginning "Shad" is enough — then
    /// produced the SAME key in a cache that is global and shared by every Render component in the
    /// process, so the second entity to ask was handed the first one's name. RenderName is what
    /// consumers match entities by, which makes a foreign value here the most expensive kind.
    /// </para>
    /// <para>
    /// WHY ALL FOUR FIELDS AND NOT A SWITCH ON CAPACITY. In the embedded form <c>buf</c>,
    /// <c>buf2</c> and <c>Size</c> ARE the string: equal keys then mean identical bytes, so a hit
    /// is the same string by construction rather than by luck. In the heap form <c>buf</c> is the
    /// buffer address and <c>Size</c> its length, which is the same identity
    /// <c>Entity.Path</c> already keys on. <c>Capacity</c> is included because it is the field that
    /// decides WHICH of the two ways <c>MiscHelpers.ToString</c> will read the other three. Keying
    /// this way means the key stays correct without this file re-stating the decode rule; branching
    /// on <c>Capacity &gt;= 8</c> here would put a second copy of that rule one file away from the
    /// first, and the two going out of step brings the collision straight back.
    /// </para>
    /// <para>
    /// WHY NOT KEY BY THE COMPONENT ADDRESS. It was the reviewers' other proposal, and it would
    /// trade this wrong-name bug for the same wrong-name bug on a different trigger. The cache is
    /// global and its entries live on a 300-second sliding expiry, so an entry outlives the
    /// component it was made for; an address-keyed entry is then only as safe as the assumption that
    /// the client never hands that address to something else. This fork already carries
    /// <c>Entity.Check(uint)</c>, whose whole purpose is catching a RECYCLED ENTITY ADDRESS, so that
    /// is not an assumption to make here without measuring it. The key chosen instead costs at worst
    /// a duplicate entry — one extra read — when the reserved bytes beside a heap pointer differ.
    /// </para>
    /// </remarks>
    public string Name
    {
        get
        {
            // Read the struct out of the frame cache ONCE: the key and the decode must describe the
            // same bytes, and both must survive the frame boundary landing between them.
            var name = RenderStruct.Name;

            return Cache.StringCache.Read(
                $"{nameof(Render)}{name.buf:X}:{name.buf2:X}:{name.Size}:{name.Capacity}",
                () => name.ToString(M));
        }
    }

    /// <summary>
    /// Gets the model rotation. NOT MEASURED on the installed client; the offset is inherited from
    /// the pre-2026 fork. See <see cref="RenderComponentOffsets.RotationMeasured"/>.
    /// </summary>
    public Vector3 Rotation => RenderStruct.Rotation;

    /// <summary>Gets the model bounds. MEASURED 2026-09-16 at component + 0x12C, three floats.</summary>
    public Vector3 Bounds => RenderStruct.Bounds;

    /// <summary>Gets the mesh rotation.</summary>
    public Vector3 MeshRoration => RenderStruct.Rotation;

    /// <summary>
    /// Gets the terrain height at the entity's position. Same unmeasured field as
    /// <see cref="Height"/>; see the note there.
    /// </summary>
    public float TerrainHeight => RenderStruct.Height > 0.01f ? RenderStruct.Height : 0f;
}
