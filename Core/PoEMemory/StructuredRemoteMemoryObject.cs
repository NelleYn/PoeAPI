using System;
using System.Runtime.InteropServices;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory;

/// <summary>
/// Base class for a <see cref="RemoteMemoryObject"/> whose layout in the game's memory
/// is described by the blittable struct <typeparamref name="T"/> (defined in GameOffsets).
/// The struct is read from <see cref="RemoteMemoryObject.Address"/> and cached per frame;
/// derived types expose individual fields through <see cref="Structure"/>.
/// </summary>
/// <typeparam name="T">The blittable offsets struct describing this object's memory layout.</typeparam>
public abstract class StructuredRemoteMemoryObject<T> : RemoteMemoryObject where T : struct
{
    private readonly CachedValue<T> _cachedStructValue;

    /// <summary>Size, in bytes, of the backing <typeparamref name="T"/> structure.</summary>
    public static int StructureSize = Marshal.SizeOf<T>();

    /// <summary>Initializes the per-frame cache that reads the structure from <see cref="RemoteMemoryObject.Address"/>.</summary>
    protected StructuredRemoteMemoryObject()
    {
        _cachedStructValue = new FrameCache<T>(() => Address == 0 ? default : M.Read<T>(Address));
    }

    /// <summary>Gets the structure read from <see cref="RemoteMemoryObject.Address"/> (cached for the current frame).</summary>
    public T Structure => _cachedStructValue.Value;
}
