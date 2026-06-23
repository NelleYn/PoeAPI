// EXPERIMENTAL candidate — see proposals/Compat/README.md. Not part of the build.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExileCore.Shared.Interfaces;
using GameOffsets.Native;

namespace ExileCore.Shared.Compat;

/// <summary>
/// <see cref="IMemory"/> extensions that emulate the ExileApi-Compiled member
/// <c>IMemory.ReadStdVector&lt;T&gt;</c> for reading a contiguous C++ <c>std::vector</c> of
/// <b>value</b> (unmanaged) elements.
/// <para>
/// This fork has no <c>ReadStdVector</c> in either tree (compatibility doc, "Memory (low-level)" —
/// upstream's <c>ReadStdVector&lt;T&gt;</c> maps to the fork's <c>ReadStructsArray&lt;T&gt;</c> for
/// <see cref="ExileCore.PoEMemory.RemoteMemoryObject"/> classes, or <c>ReadNativeArray&lt;T&gt;</c> for
/// a vector of <i>pointers</i>). These helpers cover the remaining case — a vector storing structs
/// inline — by reading each element with the fork's existing <c>Read&lt;T&gt;(long)</c>
/// (<c>Core/Memory.cs:288</c>) and walking the <see cref="NativePtrArray"/> (<c>begin</c>/<c>end</c>)
/// bounds (<c>GameOffsets/Native/NativePtrArray.cs</c>).
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
    /// <c>[StructLayout(LayoutKind.Sequential)]</c> structs. Each element is read with the fork's
    /// <c>IMemory.Read&lt;T&gt;(long)</c> (<c>Core/Memory.cs:288</c>); for a vector of pointers prefer
    /// the fork's <c>ReadNativeArray&lt;T&gt;</c> instead.
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

        var count = (endAddress - startAddress) / elementSize;
        if (count <= 0 || count > MaxElements)
            return result;

        result.Capacity = (int)count;

        for (var address = startAddress; address < endAddress; address += elementSize)
        {
            result.Add(memory.Read<T>(address));
        }

        return result;
    }
}
