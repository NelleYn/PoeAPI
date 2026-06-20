using System;

namespace ExileCore.Shared.Interfaces;

/// <summary>
/// Describes a native (unmanaged) contiguous array by its boundary pointers.
/// </summary>
public interface INativePtrArray
{
    /// <summary>Gets the pointer to the first element.</summary>
    IntPtr First { get; }

    /// <summary>Gets the pointer to the last element.</summary>
    IntPtr Last { get; }

    /// <summary>Gets the pointer to the position just past the last element.</summary>
    IntPtr End { get; }

    /// <summary>Returns a string representation of the array.</summary>
    string ToString();
}
