using System;
using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// Mirror of the game's native contiguous array (begin/end/capacity pointers),
/// equivalent to a C++ <c>std::vector</c> layout.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativePtrArray : IEquatable<NativePtrArray>
{
    /// <summary>Pointer to the first element.</summary>
    public readonly long First;

    /// <summary>Pointer to one past the last element.</summary>
    public readonly long Last;

    /// <summary>Pointer to the end of the allocated capacity.</summary>
    public readonly long End;

    /// <summary>The number of bytes between <see cref="First"/> and <see cref="Last"/>.</summary>
    public long Size => Last - First;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"First: 0x{First}, Last: 0x{Last}, End: 0x{End} Size:{Size}";
    }

    /// <inheritdoc/>
    public bool Equals(NativePtrArray other)
    {
        if (First == other.First && Last == other.Last)
            return End == other.End;

        return false;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is NativePtrArray other)
            return Equals(other);

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return (((First.GetHashCode() * 397) ^ Last.GetHashCode()) * 397) ^ End.GetHashCode();
    }
}
