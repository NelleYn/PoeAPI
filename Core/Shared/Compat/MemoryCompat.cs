using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExileCore.Shared.Interfaces;
using GameOffsets.Native;

namespace ExileCore.Shared;

/// <summary>
/// <see cref="IMemory"/> extensions that emulate the ExileApi-Compiled member
/// <c>IMemory.ReadStdVector&lt;T&gt;</c> for reading a contiguous C++ <c>std::vector</c> of
/// <b>value</b> (unmanaged) elements.
/// <para>
/// The fork's instance overloads (<c>IMemory.ReadStdVector&lt;T&gt;(long address)</c> /
/// <c>(long, RemoteMemoryObject)</c>, <c>Core/Shared/Interfaces/IMemory.cs:58,64</c>) read the
/// vector <i>header</i> from a pointer; upstream's <c>ReadStdVector&lt;T&gt;</c> also accepts an
/// already-read header or raw begin/end bounds. These extensions cover those shapes — sharing the
/// <c>ReadStdVector</c> name intentionally for plugin compatibility (the parameter lists do not
/// collide) — with one bulk <c>IMemory.ReadMem</c> (<c>Core/Shared/Interfaces/IMemory.cs:41</c>)
/// over the <see cref="NativePtrArray"/> (<c>begin</c>/<c>end</c>) bounds
/// (<c>GameOffsets/Native/NativePtrArray.cs</c>), decoded per element exactly like the fork's
/// instance overload (<c>Core/Memory.cs:229</c>). For a vector of
/// <see cref="ExileCore.PoEMemory.RemoteMemoryObject"/> classes prefer
/// <c>ReadStructsArray&lt;T&gt;</c>; for a vector of <i>pointers</i> prefer
/// <c>ReadNativeArray&lt;T&gt;</c>.
/// </para>
/// </summary>
public static class MemoryCompat
{
    // Guard against runaway reads from a corrupt/transient vector header.
    private const int MaxElements = 100000;

    /// <summary>
    /// Emulates upstream <c>IMemory.ReadStdVector&lt;T&gt;</c>: reads a contiguous array of
    /// unmanaged value structs from a native <c>std::vector</c> described by a
    /// <see cref="NativePtrArray"/> (begin/end/capacity pointers).
    /// </summary>
    /// <typeparam name="T">The unmanaged element struct type.</typeparam>
    /// <param name="memory">The process memory reader.</param>
    /// <param name="nativePtrArray">The vector header (<c>First</c> = begin, <c>Last</c> = one past end).</param>
    /// <returns>
    /// The decoded elements, or an empty list when the vector is empty or its bounds look invalid.
    /// </returns>
    /// <remarks>
    /// Element stride is <see cref="Marshal.SizeOf{T}()"/>, which is correct for blittable
    /// <c>[StructLayout(LayoutKind.Sequential)]</c> structs. The whole range is fetched with a single
    /// bulk <c>IMemory.ReadMem</c> call; for a vector of pointers prefer the fork's
    /// <c>ReadNativeArray&lt;T&gt;</c> instead.
    /// </remarks>
    public static List<T> ReadStdVector<T>(this IMemory memory, NativePtrArray nativePtrArray)
        where T : unmanaged
    {
        return memory.ReadStdVector<T>(nativePtrArray.First, nativePtrArray.Last, Marshal.SizeOf<T>());
    }

    /// <summary>
    /// Emulates upstream <c>IMemory.ReadStdVector&lt;T&gt;</c> using an explicit element size, mirroring
    /// the upstream overload that accepts a custom stride.
    /// </summary>
    /// <typeparam name="T">The unmanaged element struct type.</typeparam>
    /// <param name="memory">The process memory reader.</param>
    /// <param name="nativePtrArray">The vector header (<c>First</c> = begin, <c>Last</c> = one past end).</param>
    /// <param name="elementSize">The stride, in bytes, between consecutive elements.</param>
    /// <returns>The decoded elements, or an empty list when the bounds look invalid.</returns>
    public static List<T> ReadStdVector<T>(this IMemory memory, NativePtrArray nativePtrArray, int elementSize)
        where T : unmanaged
    {
        return memory.ReadStdVector<T>(nativePtrArray.First, nativePtrArray.Last, elementSize);
    }

    /// <summary>
    /// Emulates upstream <c>IMemory.ReadStdVector&lt;T&gt;</c> from raw begin/end addresses.
    /// </summary>
    /// <typeparam name="T">The unmanaged element struct type.</typeparam>
    /// <param name="memory">The process memory reader.</param>
    /// <param name="startAddress">Address of the first element (<c>begin</c>).</param>
    /// <param name="endAddress">Address one past the last element (<c>end</c>).</param>
    /// <param name="elementSize">The stride, in bytes, between consecutive elements.</param>
    /// <returns>The decoded elements, or an empty list when the bounds look invalid.</returns>
    public static List<T> ReadStdVector<T>(this IMemory memory, long startAddress, long endAddress, int elementSize)
        where T : unmanaged
    {
        var result = new List<T>();

        if (elementSize <= 0 || startAddress <= 0 || endAddress <= startAddress)
            return result;

        var length = endAddress - startAddress;
        var count = length / elementSize;
        if (count <= 0 || count > MaxElements)
            return result;

        // One bulk read + per-element decode, mirroring the fork's instance
        // ReadStdVector<T>(long) (Core/Memory.cs:229), instead of one process-memory
        // read per element — these helpers sit on hot per-frame plugin paths.
        var bytes = memory.ReadMem(startAddress, (int)length);
        result.Capacity = (int)count;

        // Offset-bounded so a range that is not an exact multiple of elementSize never
        // decodes past the buffer (the trailing partial element is skipped).
        for (var offset = 0; offset + elementSize <= bytes.Length; offset += elementSize)
        {
            result.Add(MemoryMarshal.Read<T>(bytes.AsSpan(offset, elementSize)));
        }

        return result;
    }
}
